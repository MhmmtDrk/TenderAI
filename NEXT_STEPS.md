# TenderAI Wizard - Kalan Backend İşleri

## ✅ TAMAMLANAN:
- Adım 1: İhale Özeti (statik data)
- Adım 2: İdari Şartname (GERÇEK AI ANALİZİ - ViewBag.Analyses["idari"])
- Adım 4: Katılım Teyidi (statik)
- Adım 7: BFTC Fiyat Girişi (statik tablo)
- Adım 8: Teklif Önerisi (statik)
- Adım 9: Sonuç (statik)

## ⏳ KALAN İŞLER:

### 1. Adım 3: Sözleşme Tasarısı
**Dosya:** `AnalysisWizard.cshtml` satır 182-195

**Değiştir:**
```csharp
else if (currentStep == 3)
{
    @{
        var sozlesmeAnalysis = analyses?.ContainsKey("sozlesme") == true ? analyses["sozlesme"] : null;
    }
    // ... sozlesmeAnalysis.KeyPoints, sozlesmeAnalysis.FinancialRisks göster
}
```

### 2. Adım 5: Teknik Şartname
**Dosya:** `AnalysisWizard.cshtml` satır 228-285

**Değiştir:**
```csharp
else if (currentStep == 5)
{
    @{
        var teknikAnalysis = analyses?.ContainsKey("teknik") == true ? analyses["teknik"] : null;
    }
    // ... teknikAnalysis.KeyPoints, teknikAnalysis.OperationalRisks göster
}
```

### 3. Adım 6: Risk Değerlendirme
**Dosya:** `AnalysisWizard.cshtml` satır 286-348

**Hesaplama Mantığı:**
```csharp
var totalRisk = 0.0;
var count = 0;
foreach(var analysis in analyses.Values) {
    totalRisk += analysis.RiskScore;
    count++;
}
var avgRisk = count > 0 ? totalRisk / count : 0;
```

**Tablo:**
- Finansal Risk: `idariAnalysis.FinancialRisks` + `sozlesmeAnalysis.FinancialRisks`
- Yasal Risk: `idariAnalysis.LegalRisks`
- Operasyonel Risk: `teknikAnalysis.OperationalRisks`

## 🔥 ŞU AN YAPILACAK:

1. Visual Studio'dan uygulamayı başlat
2. Bir ihale seç, dökümanları yükle ve analiz et
3. "Toplu Analiz" butonuna tıkla
4. Adım 2'de GERÇEK AI analizi görünecek
5. Adım 3,5,6'yı yukarıdaki örneklere göre kendin düzenle

## 📝 NOT:
Context doluyor, bu yüzden sen manuel devam et. Yukarıdaki örneklere göre çok basit!
