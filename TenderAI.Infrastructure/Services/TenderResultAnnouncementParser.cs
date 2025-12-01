using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace TenderAI.Infrastructure.Services;

/// <summary>
/// EKAP Sonuç İlanı HTML parser
/// Python ihale-mcp projesindeki logic'in C# port'u
/// </summary>
public class TenderResultAnnouncementParser : ITenderResultAnnouncementParser
{
    private readonly ILogger<TenderResultAnnouncementParser> _logger;

    public TenderResultAnnouncementParser(ILogger<TenderResultAnnouncementParser> logger)
    {
        _logger = logger;
    }

    public async Task<TenderResultInfo?> ParseResultAnnouncementAsync(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            _logger.LogWarning("HTML içeriği boş");
            return null;
        }

        var result = new TenderResultInfo
        {
            RawHtml = htmlContent,
            IsSuccess = false
        };

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // 1. Kazanan Firma Adı (EKAP HTML'inde genellikle "İstekli" veya "Yüklenici" başlığı altında)
            result.WinnerCompany = await ExtractWinnerCompanyAsync(doc);

            // 2. Vergi Kimlik Numarası
            result.WinnerTaxNumber = await ExtractTaxNumberAsync(doc);

            // 3. Sözleşme Bedeli
            result.ContractAmount = await ExtractContractAmountAsync(doc);

            // 4. Teklif Veren Sayısı
            result.NumberOfBidders = await ExtractNumberOfBiddersAsync(doc);

            // 5. İhale Sonuç Tarihi
            result.AwardDate = await ExtractAwardDateAsync(doc);

            // 6. İhale Sonucu (İhale Yapılmıştır / İptal Edilmiştir)
            result.ResultStatus = await ExtractResultStatusAsync(doc);

            // Başarı kontrolü - En azından kazanan firma veya sözleşme bedeli bulunmalı
            result.IsSuccess = !string.IsNullOrEmpty(result.WinnerCompany) || result.ContractAmount.HasValue;

            if (!result.IsSuccess)
            {
                result.Warnings.Add("Ne kazanan firma ne de sözleşme bedeli parse edilemedi");
            }

            await Task.CompletedTask;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sonuç İlanı parse edilirken hata oluştu");
            result.Warnings.Add($"Parse hatası: {ex.Message}");
            return result;
        }
    }

    private async Task<string?> ExtractWinnerCompanyAsync(HtmlDocument doc)
    {
        try
        {
            // Stratejiler:
            // 1. "İstekli" veya "Yüklenici" label'ını içeren td'nin yanındaki td
            // 2. "Teklifi Kazanan" içeren metin
            // 3. Table row'larında pattern matching

            var keywords = new[] { "istekli", "yüklenici", "kazanan", "firma" };

            foreach (var keyword in keywords)
            {
                // Case-insensitive arama
                var labelNode = doc.DocumentNode.SelectSingleNode($"//td[contains(translate(., 'İISTEKLİYÜKLENİCİKAZANFİRMA', 'iistekliyüklenicikazanfirma'), '{keyword}')]");

                if (labelNode != null)
                {
                    // Yanındaki td'yi bul
                    var valueNode = labelNode.NextSibling;
                    while (valueNode != null && valueNode.Name != "td")
                    {
                        valueNode = valueNode.NextSibling;
                    }

                    if (valueNode != null)
                    {
                        var companyName = HtmlEntity.DeEntitize(valueNode.InnerText.Trim());
                        if (!string.IsNullOrWhiteSpace(companyName) && companyName.Length > 3)
                        {
                            _logger.LogInformation($"Kazanan firma bulundu (keyword: {keyword}): {companyName}");
                            return companyName;
                        }
                    }
                }
            }

            // Alternatif: Strong/Bold içinde "İhaleyi Kazanan"
            var strongNodes = doc.DocumentNode.SelectNodes("//strong | //b");
            if (strongNodes != null)
            {
                foreach (var strongNode in strongNodes)
                {
                    var text = strongNode.InnerText.ToLowerInvariant();
                    if (text.Contains("kazanan") || text.Contains("yüklenici"))
                    {
                        var parentTr = strongNode.Ancestors("tr").FirstOrDefault();
                        if (parentTr != null)
                        {
                            var tds = parentTr.SelectNodes(".//td");
                            if (tds != null && tds.Count >= 2)
                            {
                                var companyName = HtmlEntity.DeEntitize(tds[1].InnerText.Trim());
                                if (!string.IsNullOrWhiteSpace(companyName) && companyName.Length > 3)
                                {
                                    _logger.LogInformation($"Kazanan firma bulundu (strong pattern): {companyName}");
                                    return companyName;
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogWarning("Kazanan firma adı HTML'de bulunamadı");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kazanan firma parse edilirken hata");
            return null;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    private async Task<string?> ExtractTaxNumberAsync(HtmlDocument doc)
    {
        try
        {
            // "Vergi Kimlik No" veya "T.C. Kimlik No" içeren label
            var keywords = new[] { "vergi kimlik", "t.c. kimlik", "vkn", "tckn" };

            foreach (var keyword in keywords)
            {
                var labelNode = doc.DocumentNode.SelectSingleNode($"//td[contains(translate(., 'VERGİKİMLKNOTC.', 'vergikimliknotc'), '{keyword.Replace(" ", "")}')]");

                if (labelNode != null)
                {
                    var valueNode = labelNode.NextSibling;
                    while (valueNode != null && valueNode.Name != "td")
                    {
                        valueNode = valueNode.NextSibling;
                    }

                    if (valueNode != null)
                    {
                        var taxNumber = HtmlEntity.DeEntitize(valueNode.InnerText.Trim());
                        // Sadece rakamları al
                        taxNumber = Regex.Replace(taxNumber, @"[^\d]", "");

                        if (!string.IsNullOrWhiteSpace(taxNumber) && taxNumber.Length >= 10)
                        {
                            _logger.LogInformation($"VKN/TCKN bulundu: {taxNumber}");
                            return taxNumber;
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vergi kimlik no parse edilirken hata");
            return null;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    private async Task<decimal?> ExtractContractAmountAsync(HtmlDocument doc)
    {
        try
        {
            // "Sözleşme Bedeli", "Teklif Tutarı", "İhale Bedeli" gibi keyword'ler
            var keywords = new[] { "sözleşme bedel", "teklif tutar", "ihale bedel", "toplam tutar" };

            foreach (var keyword in keywords)
            {
                var labelNode = doc.DocumentNode.SelectSingleNode($"//td[contains(translate(., 'SÖZLEŞMEBEDELİTKLFUAİHATPM', 'sozlesmebedelitklufaihatapm'), '{keyword.Replace(" ", "")}')]");

                if (labelNode != null)
                {
                    var valueNode = labelNode.NextSibling;
                    while (valueNode != null && valueNode.Name != "td")
                    {
                        valueNode = valueNode.NextSibling;
                    }

                    if (valueNode != null)
                    {
                        var amountText = HtmlEntity.DeEntitize(valueNode.InnerText.Trim());
                        var amount = ParseCurrencyAmount(amountText);

                        if (amount.HasValue && amount.Value > 0)
                        {
                            _logger.LogInformation($"Sözleşme bedeli bulundu: {amount.Value:N2} TL");
                            return amount.Value;
                        }
                    }
                }
            }

            // Alternatif: Metin içinde "₺" veya "TL" içeren büyük sayılar
            var allText = doc.DocumentNode.InnerText;
            var currencyMatches = Regex.Matches(allText, @"[\d.,]+\s*(?:₺|TL|tl)");

            if (currencyMatches.Count > 0)
            {
                foreach (Match match in currencyMatches)
                {
                    var amount = ParseCurrencyAmount(match.Value);
                    if (amount.HasValue && amount.Value > 1000) // Minimum threshold
                    {
                        _logger.LogInformation($"Tutar regex ile bulundu: {amount.Value:N2} TL");
                        return amount.Value;
                    }
                }
            }

            _logger.LogWarning("Sözleşme bedeli HTML'de bulunamadı");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sözleşme bedeli parse edilirken hata");
            return null;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    private async Task<int> ExtractNumberOfBiddersAsync(HtmlDocument doc)
    {
        try
        {
            // "Teklif Veren İstekli Sayısı" veya benzeri
            var keywords = new[] { "teklif veren", "istekli sayı", "katılımcı sayı" };

            foreach (var keyword in keywords)
            {
                var labelNode = doc.DocumentNode.SelectSingleNode($"//td[contains(translate(., 'TEKLİFVERNİSTKYSAIÇ', 'teklifvernistklsaic'), '{keyword.Replace(" ", "")}')]");

                if (labelNode != null)
                {
                    var valueNode = labelNode.NextSibling;
                    while (valueNode != null && valueNode.Name != "td")
                    {
                        valueNode = valueNode.NextSibling;
                    }

                    if (valueNode != null)
                    {
                        var countText = HtmlEntity.DeEntitize(valueNode.InnerText.Trim());
                        // Sadece rakamları al
                        var digits = Regex.Match(countText, @"\d+");

                        if (digits.Success && int.TryParse(digits.Value, out int count))
                        {
                            _logger.LogInformation($"Teklif veren sayısı bulundu: {count}");
                            return count;
                        }
                    }
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Teklif veren sayısı parse edilirken hata");
            return 0;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    private async Task<DateTime?> ExtractAwardDateAsync(HtmlDocument doc)
    {
        try
        {
            // "İhale Tarihi" veya "Sonuç Tarihi"
            var keywords = new[] { "ihale tarihi", "sonuç tarihi", "karar tarihi" };

            foreach (var keyword in keywords)
            {
                var labelNode = doc.DocumentNode.SelectSingleNode($"//td[contains(translate(., 'İHALETARSNÜÇKAOMKRİ', 'ihaletarisnuckaomkri'), '{keyword.Replace(" ", "")}')]");

                if (labelNode != null)
                {
                    var valueNode = labelNode.NextSibling;
                    while (valueNode != null && valueNode.Name != "td")
                    {
                        valueNode = valueNode.NextSibling;
                    }

                    if (valueNode != null)
                    {
                        var dateText = HtmlEntity.DeEntitize(valueNode.InnerText.Trim());
                        var date = ParseTurkishDate(dateText);

                        if (date.HasValue)
                        {
                            _logger.LogInformation($"İhale tarihi bulundu: {date.Value:yyyy-MM-dd}");
                            return date.Value;
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "İhale tarihi parse edilirken hata");
            return null;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    private async Task<string?> ExtractResultStatusAsync(HtmlDocument doc)
    {
        try
        {
            // "İhale Sonucu" label'ı
            var labelNode = doc.DocumentNode.SelectSingleNode("//td[contains(translate(., 'İHALESNÜCU', 'ihalesonucu'), 'ihalesonuc')]");

            if (labelNode != null)
            {
                var valueNode = labelNode.NextSibling;
                while (valueNode != null && valueNode.Name != "td")
                {
                    valueNode = valueNode.NextSibling;
                }

                if (valueNode != null)
                {
                    var status = HtmlEntity.DeEntitize(valueNode.InnerText.Trim());
                    if (!string.IsNullOrWhiteSpace(status))
                    {
                        _logger.LogInformation($"İhale sonucu bulundu: {status}");
                        return status;
                    }
                }
            }

            // Alternatif: Başlıkta "İhale Yapılmıştır" / "İptal Edilmiştir" gibi metinler
            var headings = doc.DocumentNode.SelectNodes("//h1 | //h2 | //h3 | //strong");
            if (headings != null)
            {
                foreach (var heading in headings)
                {
                    var text = heading.InnerText.ToLowerInvariant();
                    if (text.Contains("yapılmıştır") || text.Contains("iptal") || text.Contains("sonuç"))
                    {
                        var status = HtmlEntity.DeEntitize(heading.InnerText.Trim());
                        _logger.LogInformation($"İhale sonucu başlıkta bulundu: {status}");
                        return status;
                    }
                }
            }

            return "Belirtilmemiş";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "İhale sonucu parse edilirken hata");
            return null;
        }
        finally
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Türkçe para miktarını parse eder (örn: "1.234.567,89 TL" -> 1234567.89)
    /// </summary>
    private decimal? ParseCurrencyAmount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            // Temizle: TL, ₺, para birimi sembolleri
            text = Regex.Replace(text, @"(TL|₺|tl)", "", RegexOptions.IgnoreCase).Trim();

            // Türkçe format: 1.234.567,89 -> 1234567.89
            // Binlik ayracı: . (nokta)
            // Ondalık ayracı: , (virgül)
            text = text.Replace(".", "").Replace(",", ".");

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
            {
                return amount;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Türkçe tarih formatını parse eder (DD.MM.YYYY veya DD/MM/YYYY)
    /// </summary>
    private DateTime? ParseTurkishDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            // Türkçe tarih formatları
            var formats = new[]
            {
                "dd.MM.yyyy",
                "dd/MM/yyyy",
                "dd-MM-yyyy",
                "dd.MM.yyyy HH:mm",
                "dd/MM/yyyy HH:mm",
                "dd.MM.yyyy HH:mm:ss",
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(text.Trim(), format, new CultureInfo("tr-TR"), DateTimeStyles.None, out DateTime date))
                {
                    return DateTime.SpecifyKind(date, DateTimeKind.Utc);
                }
            }

            // Fallback: Genel parse
            if (DateTime.TryParse(text, new CultureInfo("tr-TR"), DateTimeStyles.None, out DateTime fallbackDate))
            {
                return DateTime.SpecifyKind(fallbackDate, DateTimeKind.Utc);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
