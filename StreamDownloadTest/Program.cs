using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

Console.WriteLine("===EKAP Stream Download Test ===\n");

// Senin bulduğun örnek URL (JWT token eski olabilir, yeni bir tane kullanman gerekebilir)
var testUrl = "https://stream.kik.gov.tr/cmapi/download/eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJraWt0bXAiLCJqdGkiOiI2NTExOTI2NyIsImF1ZCI6IjAifQ.HCqF1Y846cIn6fN0BGEvbptDpRVtWoYAZAHoUTHfy3o";

Console.WriteLine($"Test URL: {testUrl}\n");
Console.WriteLine("NOT: Bu JWT token eski olabilir. Eğer 403/404 alırsan EKAP'tan yeni bir doküman linki almanız gerekir.\n");

try
{
    var handler = new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 10,
        UseCookies = true,
        CookieContainer = new System.Net.CookieContainer(),
        AutomaticDecompression = System.Net.DecompressionMethods.All
    };

    using var httpClient = new HttpClient(handler);
    httpClient.Timeout = TimeSpan.FromMinutes(5);

    // Stream.kik.gov.tr için gerekli header'lar
    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
    httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
    httpClient.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
    httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
    httpClient.DefaultRequestHeaders.Add("Referer", "https://ekap.kik.gov.tr/");
    httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
    httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
    httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
    httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

    Console.WriteLine("Downloading...");
    var response = await httpClient.GetAsync(testUrl);

    Console.WriteLine($"\nStatus Code: {response.StatusCode}");
    Console.WriteLine($"Content-Type: {response.Content.Headers.ContentType?.MediaType}");

    if (response.Content.Headers.ContentDisposition != null)
    {
        Console.WriteLine($"Content-Disposition: {response.Content.Headers.ContentDisposition}");
        Console.WriteLine($"Filename: {response.Content.Headers.ContentDisposition.FileName}");
    }

    Console.WriteLine($"Content-Length: {response.Content.Headers.ContentLength} bytes");

    // Response headers yazdır
    Console.WriteLine("\nResponse Headers:");
    foreach (var header in response.Headers)
    {
        Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
    }

    if (response.IsSuccessStatusCode)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Console.WriteLine($"\nActual Downloaded Size: {bytes.Length} bytes");

        // Dosya tipini kontrol et
        if (bytes.Length >= 4)
        {
            var magic = $"{bytes[0]:X2} {bytes[1]:X2} {bytes[2]:X2} {bytes[3]:X2}";
            Console.WriteLine($"Magic Bytes: {magic}");

            if (bytes[0] == 0x50 && bytes[1] == 0x4B)
            {
                Console.WriteLine("File Type: ZIP");

                // Dosyayı kaydet
                var outputPath = Path.Combine(Path.GetTempPath(), $"ekap_download_test_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                await File.WriteAllBytesAsync(outputPath, bytes);
                Console.WriteLine($"\n✓ ZIP dosyası kaydedildi: {outputPath}");
            }
            else if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
            {
                Console.WriteLine("File Type: PDF");

                var outputPath = Path.Combine(Path.GetTempPath(), $"ekap_download_test_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                await File.WriteAllBytesAsync(outputPath, bytes);
                Console.WriteLine($"\n✓ PDF dosyası kaydedildi: {outputPath}");
            }
            else
            {
                Console.WriteLine("File Type: UNKNOWN");

                // İlk 500 karakteri yazdır (HTML olabilir)
                var previewLength = Math.Min(500, bytes.Length);
                var preview = System.Text.Encoding.UTF8.GetString(bytes.Take(previewLength).ToArray());
                Console.WriteLine($"\nContent Preview (first {previewLength} chars):\n{preview}");

                // HTML ise dosyaya kaydet
                if (preview.Contains("<html") || preview.Contains("<!DOCTYPE"))
                {
                    var htmlPath = Path.Combine(Path.GetTempPath(), $"ekap_response_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                    await File.WriteAllBytesAsync(htmlPath, bytes);
                    Console.WriteLine($"\nHTML response kaydedildi: {htmlPath}");
                }
            }
        }

        Console.WriteLine("\n✓ İndirme BAŞARILI!");
    }
    else
    {
        Console.WriteLine($"\n✗ İndirme BAŞARISIZ: {response.StatusCode}");
        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Error Content:\n{errorContent}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ HATA: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}

Console.WriteLine("\n=== Test Tamamlandı ===");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
