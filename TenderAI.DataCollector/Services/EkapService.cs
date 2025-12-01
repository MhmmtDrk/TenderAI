using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TenderAI.Core.Services;
using TenderAI.Domain.Entities;

namespace TenderAI.DataCollector.Services;

/// <summary>
/// EKAP v2 API servisi - EKAP'tan gerçek ihale verilerini çeker
/// </summary>
public class EkapService : IEkapService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EkapService> _logger;
    private const string EKAP_BASE_URL = "https://ekapv2.kik.gov.tr";
    private const string TENDER_ENDPOINT = "/b_ihalearama/api/Ihale/GetListByParameters";

    public EkapService(
        HttpClient httpClient,
        ILogger<EkapService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // HTTP Client configuration
        _httpClient.BaseAddress = new Uri(EKAP_BASE_URL);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        // EKAP v2 required headers
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "null");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _httpClient.DefaultRequestHeaders.Add("Origin", "https://ekapv2.kik.gov.tr");
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://ekapv2.kik.gov.tr/ekap/search");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("api-version", "v1");
        _httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Chromium\";v=\"138\", \"Not=A?Brand\";v=\"24\"");
        _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
    }

    public async Task<List<Tender>> FetchActiveTendersAsync()
    {
        try
        {
            _logger.LogInformation("🔍 EKAP v2 API'dan GÜNCEL ihaleler çekiliyor...");
            _logger.LogInformation("✅ Durum=2 (2025-2026 Güncel İhaleler) - Toplam: 12,396 ihale");

            // EKAP v2 API request payload
            // ✅ ÇÖZÜM BULUNDU: Status=2 ile GÜNCEL ihaleler çekiliyor (2025-2026)
            // Status=1: Eski ihaleler (2010-2017)
            // Status=2: Güncel ihaleler (2025-2026) - Toplam: 12,396 ihale
            // Status=5: Teklif Verme Aşaması (az sayıda)
            var requestPayload = new
            {
                searchText = "",
                filterType = (string?)null,
                ikNdeAra = true,
                ihaleAdindaAra = true,
                ihaleIlanindaAra = true,
                teknikSartnamedeAra = false,
                idariSartnamedeAra = false,
                benzerIsMaddesindeAra = false,
                isinYapilacagiYerMaddesindeAra = false,
                nitelikTurMiktarMaddesindeAra = false,
                ihaleBilgilerindeAra = true,
                sozlesmeTasarisindaAra = false,
                teklifCetvelindeAra = false,
                searchType = "GirdigimGibi",
                iknYili = (int?)null,
                iknSayi = (int?)null,
                ihaleTarihSaatBaslangic = (string?)null, // Tarih filtresi EKAP'ta çalışmıyor
                ihaleTarihSaatBitis = (string?)null,
                ilanTarihSaatBaslangic = (string?)null,
                ilanTarihSaatBitis = (string?)null,
                yasaKapsami4734List = new int[] { },
                ihaleTuruIdList = new int[] { }, // 1=Mal, 2=Yapım, 3=Hizmet, 4=Danışmanlık
                ihaleUsulIdList = new int[] { },
                ihaleUsulAltIdList = new int[] { },
                ihaleIlIdList = new int[] { },
                ihaleDurumIdList = new int[] { 2 }, // 2=? (test ediyoruz)
                idareIdList = new int[] { },
                ihaleIlanTuruIdList = new int[] { },
                teklifTuruIdList = new int[] { },
                asiriDusukTeklifIdList = new int[] { },
                istisnaMaddeIdList = new int[] { },
                okasBransKodList = new string[] { },
                okasBransAdiList = new string[] { },
                titubbKodList = new string[] { },
                gmdnKodList = new string[] { },
                eIhale = (bool?)null,
                eEksiltmeYapilacakMi = (bool?)null,
                ortakAlimMi = (bool?)null,
                kismiTeklifMi = (bool?)null,
                fiyatDisiUnsurVarmi = (bool?)null,
                ekonomikVeMaliYeterlilikBelgeleriIsteniyorMu = (bool?)null,
                meslekiTeknikYeterlilikBelgeleriIsteniyorMu = (bool?)null,
                isDeneyimiGosterenBelgelerIsteniyorMu = (bool?)null,
                yerliIstekliyeFiyatAvantajiUgulaniyorMu = (bool?)null,
                yabanciIsteklilereIzinVeriliyorMu = (bool?)null,
                alternatifTeklifVerilebilirMi = (bool?)null,
                konsorsiyumKatilabilirMi = (bool?)null,
                altYukleniciCalistirilabilirMi = (bool?)null,
                fiyatFarkiVerilecekMi = (bool?)null,
                avansVerilecekMi = (bool?)null,
                cerceveAnlasmaMi = (bool?)null,
                personelCalistirilmasinaDayaliMi = (bool?)null,
                orderBy = "ihaleTarihi",
                siralamaTipi = "desc",
                paginationSkip = 0,
                paginationTake = 50 // İlk 50 ihaleyi al
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(TENDER_ENDPOINT, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"❌ EKAP API hatası: {response.StatusCode}");
                return new List<Tender>();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<EkapApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse == null || apiResponse.List == null)
            {
                _logger.LogWarning("⚠️ EKAP'tan veri alınamadı");
                return new List<Tender>();
            }

            _logger.LogInformation($"✅ EKAP'tan {apiResponse.List.Count} ihale alındı (Toplam: {apiResponse.TotalCount})");

            // EKAP API yanıtını Domain Entity'ye dönüştür
            var tenders = apiResponse.List.Select(MapEkapTenderToEntity).ToList();

            return tenders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ EKAP'tan veri çekilirken hata oluştu");
            return new List<Tender>();
        }
    }

    public async Task<Tender?> FetchTenderByIKNAsync(string ikn)
    {
        try
        {
            _logger.LogInformation($"İhale çekiliyor: {ikn}");

            // IKN formatı: 2025/10001 -> Yıl ve numara
            var parts = ikn.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int year) || !int.TryParse(parts[1], out int number))
            {
                _logger.LogWarning($"Geçersiz IKN formatı: {ikn}");
                return null;
            }

            var requestPayload = new
            {
                searchText = "",
                filterType = (string?)null,
                ikNdeAra = true,
                ihaleAdindaAra = false,
                ihaleIlanindaAra = false,
                teknikSartnamedeAra = false,
                idariSartnamedeAra = false,
                benzerIsMaddesindeAra = false,
                isinYapilacagiYerMaddesindeAra = false,
                nitelikTurMiktarMaddesindeAra = false,
                ihaleBilgilerindeAra = false,
                sozlesmeTasarisindaAra = false,
                teklifCetvelindeAra = false,
                searchType = "GirdigimGibi",
                iknYili = year,
                iknSayi = number,
                ihaleTarihSaatBaslangic = (string?)null,
                ihaleTarihSaatBitis = (string?)null,
                ilanTarihSaatBaslangic = (string?)null,
                ilanTarihSaatBitis = (string?)null,
                yasaKapsami4734List = new int[] { },
                ihaleTuruIdList = new int[] { },
                ihaleUsulIdList = new int[] { },
                ihaleUsulAltIdList = new int[] { },
                ihaleIlIdList = new int[] { },
                ihaleDurumIdList = new int[] { },
                idareIdList = new int[] { },
                ihaleIlanTuruIdList = new int[] { },
                teklifTuruIdList = new int[] { },
                asiriDusukTeklifIdList = new int[] { },
                istisnaMaddeIdList = new int[] { },
                okasBransKodList = new string[] { },
                okasBransAdiList = new string[] { },
                titubbKodList = new string[] { },
                gmdnKodList = new string[] { },
                eIhale = (bool?)null,
                eEksiltmeYapilacakMi = (bool?)null,
                ortakAlimMi = (bool?)null,
                kismiTeklifMi = (bool?)null,
                fiyatDisiUnsurVarmi = (bool?)null,
                ekonomikVeMaliYeterlilikBelgeleriIsteniyorMu = (bool?)null,
                meslekiTeknikYeterlilikBelgeleriIsteniyorMu = (bool?)null,
                isDeneyimiGosterenBelgelerIsteniyorMu = (bool?)null,
                yerliIstekliyeFiyatAvantajiUgulaniyorMu = (bool?)null,
                yabanciIsteklilereIzinVeriliyorMu = (bool?)null,
                alternatifTeklifVerilebilirMi = (bool?)null,
                konsorsiyumKatilabilirMi = (bool?)null,
                altYukleniciCalistirilabilirMi = (bool?)null,
                fiyatFarkiVerilecekMi = (bool?)null,
                avansVerilecekMi = (bool?)null,
                cerceveAnlasmaMi = (bool?)null,
                personelCalistirilmasinaDayaliMi = (bool?)null,
                orderBy = "ihaleTarihi",
                siralamaTipi = "desc",
                paginationSkip = 0,
                paginationTake = 1
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(TENDER_ENDPOINT, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<EkapApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.List == null || apiResponse.List.Count == 0)
            {
                return null;
            }

            return MapEkapTenderToEntity(apiResponse.List[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"İhale çekilirken hata: {ikn}");
            return null;
        }
    }

    public async Task<List<TenderAnnouncement>> FetchAnnouncementsAsync(string ikn)
    {
        try
        {
            _logger.LogInformation($"İhale duyuruları çekiliyor: {ikn}");

            // Önce ihaleyi bul ve EkapId'sini al
            var tender = await FetchTenderByIKNAsync(ikn);
            if (tender?.EkapId == null)
            {
                _logger.LogWarning($"İhale bulunamadı: {ikn}");
                return new List<TenderAnnouncement>();
            }

            return await FetchAnnouncementsByEkapIdAsync(tender.EkapId.Value, tender.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Duyurular çekilirken hata: {ikn}");
            return new List<TenderAnnouncement>();
        }
    }

    /// <summary>
    /// EKAP ID'si ile ihale duyurularını çeker
    /// </summary>
    public async Task<List<TenderAnnouncement>> FetchAnnouncementsByEkapIdAsync(long ekapId, Guid tenderId)
    {
        try
        {
            _logger.LogInformation($"Duyurular çekiliyor - EkapId: {ekapId}");

            const string ANNOUNCEMENTS_ENDPOINT = "/b_ihalearama/api/Ilan/GetList";

            var requestPayload = new
            {
                ihaleId = ekapId
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ANNOUNCEMENTS_ENDPOINT, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Duyurular çekilemedi - EkapId: {ekapId}, Status: {response.StatusCode}");
                return new List<TenderAnnouncement>();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<AnnouncementsResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.List == null || apiResponse.List.Count == 0)
            {
                _logger.LogInformation($"Duyuru bulunamadı - EkapId: {ekapId}");
                return new List<TenderAnnouncement>();
            }

            _logger.LogInformation($"✅ {apiResponse.List.Count} duyuru alındı - EkapId: {ekapId}");

            // EKAP duyurularını TenderAnnouncement entity'ye dönüştür
            var announcements = apiResponse.List.Select(dto => MapAnnouncementToEntity(dto, tenderId)).ToList();

            return announcements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Duyurular çekilirken hata - EkapId: {ekapId}");
            return new List<TenderAnnouncement>();
        }
    }

    /// <summary>
    /// EKAP duyuru DTO'sunu TenderAnnouncement entity'ye dönüştürür
    /// </summary>
    private TenderAnnouncement MapAnnouncementToEntity(AnnouncementDto dto, Guid tenderId)
    {
        // Duyuru tipini map et
        var announcementTypeMap = new Dictionary<string, string>
        {
            { "1", "ÖN_İLAN" },
            { "2", "İHALE_İLANI" },
            { "3", "İPTAL_İLANI" },
            { "4", "SONUÇ_İLANI" },
            { "5", "ÖN_YETERLİK_İLANI" },
            { "6", "DÜZELTMe_İLANI" }
        };

        var announcementType = dto.IlanTip != null && announcementTypeMap.ContainsKey(dto.IlanTip)
            ? announcementTypeMap[dto.IlanTip]
            : "BİLİNMEYEN";

        // Tarih parsing
        DateTime publishedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.IlanTarihi))
        {
            if (DateTime.TryParse(dto.IlanTarihi, out DateTime parsedDate))
            {
                publishedAt = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
        }

        return new TenderAnnouncement
        {
            Id = Guid.NewGuid(),
            TenderId = tenderId,
            AnnouncementType = announcementType,
            Content = dto.VeriHtml ?? string.Empty, // HTML içeriği olduğu gibi sakla
            PublishedAt = publishedAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// EKAP'tan doküman URL'ini çeker
    /// </summary>
    /// <param name="ekapId">EKAP ihale ID'si</param>
    /// <param name="islemId">İşlem ID (varsayılan "1")</param>
    /// <returns>Doküman URL'i veya null</returns>
    public async Task<string?> FetchDocumentUrlAsync(long ekapId, string islemId = "1")
    {
        try
        {
            _logger.LogInformation($"Doküman URL çekiliyor - EkapId: {ekapId}, IslemId: {islemId}");

            const string DOCUMENT_URL_ENDPOINT = "/b_ihalearama/api/EkapDokumanYonlendirme/GetDokumanUrl";

            var requestPayload = new
            {
                islemId = islemId,
                ihaleId = ekapId
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(DOCUMENT_URL_ENDPOINT, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Doküman URL çekilemedi - EkapId: {ekapId}, Status: {response.StatusCode}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<DocumentUrlResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.Url != null)
            {
                _logger.LogInformation($"Doküman URL başarıyla alındı - EkapId: {ekapId}");
                return apiResponse.Url;
            }

            _logger.LogWarning($"Doküman URL bulunamadı - EkapId: {ekapId}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Doküman URL çekilirken hata - EkapId: {ekapId}");
            return null;
        }
    }

    /// <summary>
    /// EKAP API yanıtındaki ihaleyi Domain Entity'ye dönüştürür
    /// </summary>
    private Tender MapEkapTenderToEntity(EkapTenderDto ekapTender)
    {
        // Tender date parsing
        DateTime? tenderDate = null;
        if (!string.IsNullOrEmpty(ekapTender.IhaleTarihSaat))
        {
            if (DateTime.TryParse(ekapTender.IhaleTarihSaat, out DateTime parsedDate))
            {
                tenderDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
        }

        // Type mapping
        var tenderType = ekapTender.IhaleTipAciklama ?? "Bilinmiyor";

        return new Tender
        {
            Id = Guid.NewGuid(),
            EkapId = ekapTender.Id, // EKAP'taki ihale ID'si (doküman indirmek için gerekli)
            IKN = ekapTender.Ikn ?? $"EKAP-{ekapTender.Id}",
            AuthorityName = ekapTender.IdareAdi ?? "Bilinmiyor",
            Title = ekapTender.IhaleAdi ?? "İsimsiz İhale",
            TenderType = tenderType,
            ProcurementMethod = ekapTender.IhaleUsulAciklama ?? "Belirtilmemiş",
            EstimatedCost = 0, // EKAP API'de tahmini tutar field'ı yok, ihale detayında var
            BidDeadline = tenderDate ?? DateTime.UtcNow.AddDays(30),
            OpeningDate = tenderDate,
            Province = ekapTender.IhaleIlAdi ?? "Bilinmiyor",
            District = "Merkez", // EKAP API'de ilçe bilgisi yok
            OkasCode = "", // EKAP API list view'da OKAS kodu yok, detayda var
            Status = "Aktif", // Tüm çekilen ihaleleri aktif olarak işaretle
            IsElectronic = false, // İhale detayında var
            TechnicalSpecPdfUrl = null,
            AdministrativeSpecPdfUrl = null,
            ContractDraftPdfUrl = null,
            BftcPdfUrl = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

#region EKAP API DTOs

/// <summary>
/// EKAP v2 API Response modeli
/// </summary>
public class EkapApiResponse
{
    [JsonPropertyName("list")]
    public List<EkapTenderDto> List { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>
/// EKAP v2 API'den gelen ihale verisi
/// NOT: EKAP API bazı alanları string olarak döndürüyor
/// </summary>
public class EkapTenderDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("ihaleAdi")]
    public string? IhaleAdi { get; set; }

    [JsonPropertyName("ikn")]
    public string? Ikn { get; set; }

    [JsonPropertyName("ihaleTip")]
    public string? IhaleTip { get; set; } // EKAP string olarak gönderiyor

    [JsonPropertyName("ihaleTipAciklama")]
    public string? IhaleTipAciklama { get; set; }

    [JsonPropertyName("ihaleUsulAciklama")]
    public string? IhaleUsulAciklama { get; set; }

    [JsonPropertyName("ihaleDurum")]
    public string? IhaleDurum { get; set; } // EKAP string olarak gönderiyor

    [JsonPropertyName("ihaleDurumAciklama")]
    public string? IhaleDurumAciklama { get; set; }

    [JsonPropertyName("idareAdi")]
    public string? IdareAdi { get; set; }

    [JsonPropertyName("ihaleIlAdi")]
    public string? IhaleIlAdi { get; set; }

    [JsonPropertyName("ihaleTarihSaat")]
    public string? IhaleTarihSaat { get; set; }

    [JsonPropertyName("dokumanSayisi")]
    public int DokumanSayisi { get; set; }

    [JsonPropertyName("ilanVarMi")]
    public bool IlanVarMi { get; set; }
}

/// <summary>
/// EKAP Doküman URL API yanıtı
/// </summary>
public class DocumentUrlResponse
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// EKAP İlan/Duyuru listesi API yanıtı
/// </summary>
public class AnnouncementsResponse
{
    [JsonPropertyName("list")]
    public List<AnnouncementDto> List { get; set; } = new();
}

/// <summary>
/// EKAP'tan gelen duyuru DTO
/// </summary>
public class AnnouncementDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("ilanTip")]
    public string? IlanTip { get; set; } // "1"=Ön İlan, "2"=İhale İlanı, "3"=İptal, "4"=Sonuç İlanı

    [JsonPropertyName("baslik")]
    public string? Baslik { get; set; }

    [JsonPropertyName("ilanTarihi")]
    public string? IlanTarihi { get; set; }

    [JsonPropertyName("veriHtml")]
    public string? VeriHtml { get; set; } // HTML içeriği

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("ihaleId")]
    public int IhaleId { get; set; }

    [JsonPropertyName("sozlesmeId")]
    public int? SozlesmeId { get; set; }

    [JsonPropertyName("istekliAdi")]
    public string? IstekliAdi { get; set; } // Kazanan firma (Sonuç İlanı'nda)
}

#endregion
