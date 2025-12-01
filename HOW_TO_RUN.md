# 🚀 TenderAI - Nasıl Çalıştırılır?

## 📦 Proje Yapısı Özeti

```
TenderAI-Project/
│
├── TenderAI.Domain/              ✅ Entity modelleri
├── TenderAI.Infrastructure/      ✅ Veritabanı (EF Core + PostgreSQL)
├── TenderAI.Core/                ✅ Business logic
├── TenderAI.Web/                 ✅ MVC Web App
├── TenderAI.DataCollector/       ✅ Worker Service
│
├── docker-compose.yml            ✅ Docker orchestration
├── README.md                     ✅ Proje dokümantasyonu
├── QUICKSTART.md                 ✅ Hızlı başlangıç
└── PROJECT_SUMMARY.md            ✅ Proje özeti
```

---

## ⚡ Hızlı Başlatma (3 Adım)

### 1️⃣ Projeyi Build Edin

```bash
cd C:\Users\DELL4800\Desktop\TenderAI-Project
dotnet build
```

**Beklenen Çıktı:**
```
Build succeeded.
    0 Error(s)
    1 Warning(s)
```

### 2️⃣ PostgreSQL'i Başlatın

**Seçenek A: Docker ile (Önerilen)**
```bash
docker-compose up postgres -d
```

**Seçenek B: Yerel PostgreSQL**
```bash
# PostgreSQL'in çalıştığından emin olun
# Veritabanı oluşturun:
createdb tenderai
```

### 3️⃣ Migration'ları Çalıştırın ve Uygulamayı Başlatın

```bash
# Migration'ları uygula
cd TenderAI.Web
dotnet ef database update

# Uygulamayı başlat
dotnet run
```

**Tarayıcınızda açın:**
```
http://localhost:5000
```

---

## 🐳 Docker ile Tam Kurulum

Tüm servisleri (PostgreSQL + Redis + Elasticsearch + Web + DataCollector) birlikte çalıştırmak için:

```bash
# Tüm servisleri başlat
docker-compose up -d

# Logları izle
docker-compose logs -f web

# Durumu kontrol et
docker-compose ps
```

**Servisler:**
- **Web App**: http://localhost:5000
- **PostgreSQL**: localhost:5432
- **Redis**: localhost:6379
- **Elasticsearch**: http://localhost:9200

---

## 📝 Önemli Notlar

### ⚠️ İlk Çalıştırmada

1. **Veritabanı boş olacak** - Henüz ihale verisi yok
2. **Migration'lar çalıştı mı?** - `dotnet ef database update` komutu başarılı olmalı
3. **Connection string doğru mu?** - `appsettings.json` kontrol edin

### 🔧 Connection String

**Varsayılan:**
```json
"DefaultConnection": "Host=localhost;Database=tenderai;Username=postgres;Password=postgres123;Port=5432"
```

**Kendi PostgreSQL'iniz varsa:**
```json
"DefaultConnection": "Host=localhost;Database=tenderai;Username=YOUR_USER;Password=YOUR_PASS;Port=5432"
```

---

## 🧪 Test Verisi Ekle

Şu an veritabanı boş. Test için manuel olarak veri ekleyebilirsiniz:

### SQL ile Test İhalesi Ekle

```sql
-- PostgreSQL'e bağlan
psql -d tenderai -U postgres

-- Test ihalesi ekle
INSERT INTO "Tenders" (
    "Id", "IKN", "AuthorityName", "Title", "TenderType",
    "EstimatedCost", "BidDeadline", "Province", "Status",
    "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    '2025/12345',
    'Ankara Büyükşehir Belediyesi',
    'CNC Torna Tezgahı Alımı',
    'Mal',
    500000.00,
    '2025-12-31',
    'Ankara',
    'Aktif',
    NOW(),
    NOW()
);
```

### C# Code ile (gelecekte seed data service eklenecek)

```csharp
// TenderAI.Web/Program.cs sonuna ekleyebilirsiniz:
// await SeedDataAsync(app.Services);
```

---

## 🔍 Veritabanı Kontrol

Migration'ların başarılı olup olmadığını kontrol edin:

```bash
# Tablolar oluştu mu?
psql -d tenderai -U postgres -c "\dt"
```

**Beklenen Tablolar:**
- Tenders
- TenderAnnouncements
- RiskAnalyses
- TechnicalAnalyses
- TechnicalItems
- BftcItems
- PriceAnalyses
- UserProducts
- HistoricalTenders
- HistoricalBftcItems

---

## 🐛 Sorun Giderme

### "Build failed" hatası

```bash
# Tüm projeleri temizle ve yeniden build et
dotnet clean
dotnet build
```

### "Connection refused" (PostgreSQL)

```bash
# Docker container çalışıyor mu?
docker ps | grep postgres

# Çalışmıyorsa başlat
docker-compose up postgres -d

# Veya yerel PostgreSQL
sudo systemctl status postgresql
sudo systemctl start postgresql
```

### "Migration not found"

```bash
# Migration'ları yeniden oluştur
cd TenderAI.Web
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Port 5000 zaten kullanımda

```bash
# Farklı port ile başlat
dotnet run --urls "http://localhost:5001"
```

---

## 📚 Sonraki Adımlar

1. ✅ Projeyi çalıştırdınız
2. 🔜 **Dashboard'u inceleyin** - http://localhost:5000/Dashboard
3. 🔜 **İhaleler sayfasına gidin** - http://localhost:5000/Tender
4. 🔜 **AI Servisleri entegre edin** - OpenAI API key ekleyin
5. 🔜 **EKAP veri toplama** - ihale-mcp entegrasyonu
6. 🔜 **Production'a deploy** - Docker Compose ile Azure/AWS

---

## 🎯 Geliştirmeye Hazır

Proje şu anda **development mode**'da çalışıyor. Production için:

```bash
# Production build
dotnet publish -c Release

# Docker image oluştur
docker build -t tenderai:latest .

# Docker Compose ile production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

## 💡 İpuçları

### Hot Reload ile Geliştirme

```bash
cd TenderAI.Web
dotnet watch run
```

Kod değişikliklerinde otomatik yeniden başlar.

### Debug Modunda Çalıştır

Visual Studio veya VS Code'da F5 tuşuna basın.

### Logları İzle

```bash
# Console'da
dotnet run --verbosity detailed

# Docker'da
docker-compose logs -f web
```

---

## 📞 Yardım

Sorun yaşıyorsanız:
1. Build hatası mı? → `dotnet build` çıktısını kontrol edin
2. Veritabanı hatası mı? → Connection string'i kontrol edin
3. Runtime hatası mı? → Browser console'u açın (F12)

**Destek:**
- Email: info@akpaya.com.tr
- GitHub: Issues bölümü

---

**🎉 Başarılar! TenderAI projesi çalışıyor.**

**Sonraki Adım:** [README.md](README.md) dosyasından projenin tüm özelliklerini öğrenin.
