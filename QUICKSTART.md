# ⚡ TenderAI - Hızlı Başlangıç Kılavuzu

## 🚀 5 Dakikada Çalıştırın!

### Seçenek 1: Docker ile (Önerilen)

```bash
# 1. Repo'yu klonlayın
git clone https://github.com/your-username/TenderAI.git
cd TenderAI

# 2. .env dosyasını oluşturun
cp .env.example .env

# 3. OpenAI API key'inizi ekleyin
# .env dosyasını düzenleyin: OPENAI_API_KEY=sk-...

# 4. Docker servisleri başlatın
docker-compose up -d

# 5. Migration'ları çalıştırın
docker-compose exec web dotnet ef database update

# 6. Tarayıcınızda açın
# http://localhost:5000
```

### Seçenek 2: Manuel Kurulum

```bash
# 1. PostgreSQL kurulu ve çalışıyor olmalı

# 2. Veritabanı oluşturun
createdb tenderai

# 3. Connection string'i güncelleyin
# TenderAI.Web/appsettings.json dosyasında:
# "DefaultConnection": "Host=localhost;Database=tenderai;..."

# 4. Migration'ları çalıştırın
cd TenderAI.Web
dotnet ef database update

# 5. Uygulamayı başlatın
dotnet run

# 6. Tarayıcınızda açın
# http://localhost:5000
```

## 📝 İlk İhale Analizi

### 1. Demo Veri Ekleyin (Opsiyonel)

```bash
# Seed data script'i çalıştırın
dotnet run --project TenderAI.Web -- seed-data
```

### 2. Dashboard'a Gidin

```
http://localhost:5000/Dashboard
```

### 3. İhale Listesini Görüntüleyin

```
http://localhost:5000/Tender
```

### 4. Analiz Başlatın

1. Herhangi bir ihaleye tıklayın
2. "Analiz Et" butonuna basın
3. 9 adımlı wizard'ı takip edin

## 🔧 Yaygın Sorunlar

### "Connection refused" hatası

```bash
# PostgreSQL çalışıyor mu kontrol edin
docker ps | grep postgres

# Veya manuel kurulumda:
sudo systemctl status postgresql
```

### "OpenAI API error"

```bash
# .env dosyasında API key'i kontrol edin
cat .env | grep OPENAI_API_KEY

# Geçerli bir key mi test edin:
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer $OPENAI_API_KEY"
```

### Migration hataları

```bash
# Mevcut migration'ları silin ve yeniden oluşturun
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 📚 Sonraki Adımlar

- [Ana README](README.md) - Detaylı dokümantasyon
- [API Dokümantasyonu](docs/API.md) - REST API endpoints
- [Mimari Dökümanı](docs/ARCHITECTURE.md) - Sistem tasarımı

## 💡 İpuçları

### Development Ortamı

```bash
# Watch mode ile çalıştırın (otomatik yeniden başlatma)
dotnet watch run --project TenderAI.Web
```

### Debug için Loglar

```bash
# Docker loglarını izleyin
docker-compose logs -f web

# Veya sadece hataları:
docker-compose logs -f web | grep ERROR
```

### Test Verileri

```bash
# Sample data ekleyin
cd TenderAI.Web
dotnet run -- seed-sample-data

# Bu komut:
# - 10 adet demo ihale
# - 5 adet risk analizi
# - 20 adet BFTC kalemi ekler
```

## 🎯 Performans Optimizasyonu

### Production Ayarları

```bash
# appsettings.Production.json oluşturun:
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Production connection string"
  }
}
```

### Redis Cache Aktifleştirme

```csharp
// Program.cs'e ekleyin:
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

---

**Herhangi bir sorunla karşılaşırsanız, lütfen [Issues](https://github.com/your-username/TenderAI/issues) bölümünden bildirin!**
