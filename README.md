# 🚀 TenderAI - Yapay Zeka Destekli Kamu İhale Karar Platformu

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)
![License](https://img.shields.io/badge/License-MIT-green.svg)

**TenderAI**, Türkiye kamu ihalelerini yapay zeka ile analiz eden, risk skorlama ve fiyat optimizasyonu sunan profesyonel bir karar destek platformudur.

## 📋 İçindekiler

- [Genel Bakış](#-genel-bakış)
- [Özellikler](#-özellikler)
- [Teknoloji Stack](#-teknoloji-stack)
- [Proje Yapısı](#-proje-yapısı)
- [Kurulum](#-kurulum)
- [Kullanım](#-kullanım)
- [9 Adımlı Analiz Süreci](#-9-adımlı-analiz-süreci)
- [API Dokümantasyonu](#-api-dokümantasyonu)
- [Katkıda Bulunma](#-katkıda-bulunma)

---

## 🎯 Genel Bakış

TenderAI, kamu ihalelerine katılacak firmaların karar süreçlerini otomatikleştiren, **AI destekli** bir SaaS platformudur.

### Sorun
Firmalar her gün yüzlerce ihale ilanını manuel olarak inceler, saatlerce şartname okur ve sezgisel kararlar verir. Bu süreç:
- ⏱️ Zaman alıcı
- 🎲 Yüksek hata riski
- 📉 Rekabet dezavantajı

### Çözüm
TenderAI, bu süreci **9 adımda** otomatikleştirir:
1. İhale bilgilerini EKAP'tan otomatik çeker
2. İdari şartnameyi AI ile analiz eder
3. Sözleşme risklerini hesaplar
4. Teknik uygunluk skorlar
5. Fiyat optimizasyonu yapar
6. **Sonuç:** "Bu ihaleye katılmalı mısın?" sorusunu yanıtlar

---

## ✨ Özellikler

### 🔍 Otomatik Veri Toplama
- EKAP v2 entegrasyonu (ihale-mcp üzerinden)
- Günlük otomatik ihale çekimi
- PDF şartname ve sözleşme indirme

### 🤖 AI Destekli Analiz
- **İdari Şartname Analizi**: TSE, ISO, benzer iş gereklilikleri
- **Sözleşme Analizi**: Ödeme vadesi, garanti, cezai şartlar
- **Teknik Şartname**: Ürün/ekipman eşleştirme
- **Risk Puanlama**: 0-100 arası otomatik risk skoru

### 💰 Fiyat Optimizasyonu
- Geçmiş 3 yıl ihale fiyat karşılaştırması
- Risk bazlı marj hesaplama
- Kur ve finansman riski ekleme
- Rekabetçi teklif önerisi

### 📊 Dashboard & Raporlama
- Aktif ihaleler listesi
- Risk skorlarına göre filtreleme
- Analiz geçmişi

---

## 🛠️ Teknoloji Stack

### Backend
- **Framework**: ASP.NET Core MVC 8.0
- **ORM**: Entity Framework Core 8.0
- **Database**: PostgreSQL 16
- **Cache**: Redis 7
- **Search**: Elasticsearch 8.10

### AI & Machine Learning
- **OpenAI API**: GPT-4 Turbo
- **NLP**: Şartname metin analizi
- **Embeddings**: Ürün eşleştirme

### Altyapı
- **Containerization**: Docker & Docker Compose
- **Architecture**: Clean Architecture (Domain, Infrastructure, Core, Web)
- **Pattern**: Repository Pattern, Unit of Work

---

## 📁 Proje Yapısı

```
TenderAI-Project/
│
├── TenderAI.Domain/                # Entity modelleri (Tender, RiskAnalysis, vb.)
│   └── Entities/
│       ├── Tender.cs
│       ├── RiskAnalysis.cs
│       ├── TechnicalAnalysis.cs
│       ├── PriceAnalysis.cs
│       └── ...
│
├── TenderAI.Infrastructure/        # Veri erişim katmanı
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   └── Repositories/
│       ├── Repository.cs
│       └── UnitOfWork.cs
│
├── TenderAI.Core/                  # İş mantığı ve servisler
│   ├── Interfaces/
│   │   ├── ITenderService.cs
│   │   ├── IAIAnalysisService.cs
│   │   ├── IRiskCalculationService.cs
│   │   └── IPriceOptimizationService.cs
│   ├── Services/
│   │   ├── TenderService.cs
│   │   └── RiskCalculationService.cs
│   └── DTOs/
│
├── TenderAI.Web/                   # MVC Web Uygulaması
│   ├── Controllers/
│   │   ├── DashboardController.cs
│   │   └── TenderController.cs
│   ├── Views/
│   ├── wwwroot/
│   └── Program.cs
│
├── TenderAI.DataCollector/         # Worker Service (arka plan veri çekimi)
│   └── Worker.cs
│
├── docker-compose.yml              # Docker orchestration
└── README.md
```

---

## 🚀 Kurulum

### Ön Gereksinimler

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [PostgreSQL](https://www.postgresql.org/download/) (veya Docker ile)
- OpenAI API Key

### 1️⃣ Projeyi Klonlayın

```bash
git clone https://github.com/your-username/TenderAI.git
cd TenderAI
```

### 2️⃣ Environment Variables Ayarlayın

```bash
cp .env.example .env
```

`.env` dosyasını düzenleyin:

```env
OPENAI_API_KEY=sk-your-actual-openai-key
POSTGRES_PASSWORD=your-secure-password
```

### 3️⃣ Docker ile Çalıştırın

```bash
# Tüm servisleri ayağa kaldır
docker-compose up -d

# Veritabanı migration'larını çalıştır
docker-compose exec web dotnet ef database update
```

### 4️⃣ Manuel Kurulum (Docker olmadan)

```bash
# 1. PostgreSQL'i başlatın

# 2. Veritabanı oluşturun
createdb tenderai

# 3. Connection string'i güncelleyin
# TenderAI.Web/appsettings.json

# 4. Migration'ları çalıştırın
cd TenderAI.Web
dotnet ef database update

# 5. Uygulamayı başlatın
dotnet run
```

Tarayıcınızda açın: **http://localhost:5000**

---

## 📖 Kullanım

### Dashboard

Ana sayfa size aktif ihaleler ve analiz istatistiklerini gösterir:

```
http://localhost:5000/Dashboard
```

### İhale Arama

```
http://localhost:5000/Tender?keyword=CNC&province=Ankara
```

### İhale Analizi Başlatma

1. İhale listesinden bir ihale seçin
2. **"Analiz Et"** butonuna tıklayın
3. 9 adımlı wizard'ı takip edin

---

## 🔄 9 Adımlı Analiz Süreci

| Adım | İşlem | AI Kullanımı | Çıktı |
|------|-------|--------------|-------|
| **1** | Temel İhale Bilgisi | ❌ | İKN, Kurum, Maliyet |
| **2** | İdari Şartname Analizi | ✅ GPT-4 | Uygunluk skoru, gerekli belgeler |
| **3** | Sözleşme Tasarısı Analizi | ✅ GPT-4 | Ödeme vadesi, garanti, cezalar |
| **4** | Katılım Onayı | ❌ | Kullanıcı teyidi |
| **5** | Teknik Şartname | ✅ GPT-4 + Embeddings | Ürün eşleştirme |
| **6** | Operasyonel Maliyet | ✅ | Lojistik, eğitim maliyeti |
| **7** | BFTC Fiyat Girişi | ❌ | Kullanıcı fiyat girdisi |
| **8** | Finansal Risk Optimizasyonu | ✅ Algoritma | Risk marjı hesaplama |
| **9** | Nihai Teklif Önerisi | ✅ GPT-4 | Teklif bedeli + kazanma olasılığı |

---

## 🔌 API Dokümantasyonu

### Tender Endpoints

```http
GET /api/tenders                    # Tüm aktif ihaleler
GET /api/tenders/{ikn}              # İKN ile ihale getir
GET /api/tenders/search?q=keyword   # Arama
POST /api/tenders/{id}/analyze      # Analiz başlat
```

### Analysis Endpoints

```http
GET /api/analysis/{tenderId}/risk        # Risk analizi sonucu
GET /api/analysis/{tenderId}/technical   # Teknik analiz sonucu
GET /api/analysis/{tenderId}/price       # Fiyat önerisi
```

---

## 📊 Veritabanı Schema

```sql
-- Ana tablolar
Tenders                   -- İhale verileri
TenderAnnouncements       -- İlan metinleri
RiskAnalyses              -- Risk skorları
TechnicalAnalyses         -- Teknik uygunluk
PriceAnalyses             -- Fiyat önerileri
BftcItems                 -- BFTC kalemleri
UserProducts              -- Kullanıcı ürün kataloğu
HistoricalTenders         -- Geçmiş ihaleler (benchmark)
```

---

## 🧪 Testler

```bash
# Unit testleri çalıştır
dotnet test

# Integration testleri çalıştır
dotnet test --filter Category=Integration
```

---

## 🐛 Sorun Giderme

### PostgreSQL bağlantı hatası

```bash
# Container'ın çalıştığını kontrol edin
docker ps | grep postgres

# Connection string'i doğrulayın
# appsettings.json
```

### OpenAI API hataları

```bash
# API key'i kontrol edin
echo $OPENAI_API_KEY

# Rate limit hatası alıyorsanız, model'i değiştirin:
# "Model": "gpt-3.5-turbo"  # appsettings.json
```

---

## 🗺️ Roadmap

### V1.0 (Mevcut)
- ✅ EKAP entegrasyonu
- ✅ AI şartname analizi
- ✅ Risk skorlama
- ✅ Fiyat optimizasyonu

### V1.1 (Planlanan)
- 🔜 **TenderBot**: Şartname sorularına yanıt veren AI chatbot
- 🔜 **TenderMap**: Türkiye geneli ihale yoğunluk haritası
- 🔜 **Mobil uygulama**: Anlık bildirimler

### V2.0 (Gelecek)
- 🔮 **API Marketplace**: Diğer tedarikçiler için TenderAI API
- 🔮 **Avrupa İhaleler**: EU TED entegrasyonu
- 🔮 **Blockchain**: İhale geçmişi şeffaflığı

---

## 👥 Katkıda Bulunma

Katkılarınızı bekliyoruz! Lütfen şu adımları takip edin:

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/AmazingFeature`)
3. Commit yapın (`git commit -m 'Add some AmazingFeature'`)
4. Push edin (`git push origin feature/AmazingFeature`)
5. Pull Request açın

---

## 📄 Lisans

Bu proje **MIT Lisansı** altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakın.

---

## 📞 İletişim

**AKPAYA Teknoloji**
Kurucu: Yakup Yaşar
E-posta: info@akpaya.com.tr
Website: [www.akpaya.com.tr](https://www.akpaya.com.tr)

---

## 🙏 Teşekkürler

- [ihale-mcp](https://github.com/saidsurucu/ihale-mcp) - EKAP API entegrasyonu
- [OpenAI](https://openai.com) - GPT-4 API
- [PostgreSQL](https://www.postgresql.org/)
- [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)

---

<div align="center">

**⭐ Projeyi beğendiyseniz yıldız vermeyi unutmayın!**

Made with ❤️ by AKPAYA Teknoloji

</div>
