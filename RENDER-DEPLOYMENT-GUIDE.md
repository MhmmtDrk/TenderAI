# TenderAI - Render.com Deployment Rehberi

## Render.com ile Docker Deployment

Bu proje Render.com üzerinde Docker container olarak çalışacak şekilde yapılandırılmıştır.

---

## 📋 Ön Gereksinimler

1. **GitHub Hesabı** - Proje zaten GitHub'da: https://github.com/MhmmtDrk/TenderAI
2. **Render.com Hesabı** - https://render.com adresinden ücretsiz hesap oluştur
3. **API Keyleri:**
   - OpenAI API Key (yeni key al: https://platform.openai.com/api-keys)
   - Gemini API Key (mevcut: AIzaSyC9rG7s8oH1VwgLc7S9rYqaLo7zwGqB5As)

---

## 🚀 Render'da Deployment Adımları

### 1. Render Dashboard'a Git

https://dashboard.render.com/ adresine giriş yap

### 2. New Web Service Oluştur

1. **"New +"** butonuna tıkla
2. **"Web Service"** seç
3. **"Build and deploy from a Git repository"** seç
4. **"Next"** tıkla

### 3. GitHub Repository Bağla

1. **"Connect GitHub"** tıkla (ilk sefer)
2. **MhmmtDrk/TenderAI** repository'sini seç
3. **"Connect"** tıkla

### 4. Web Service Ayarlarını Yap

**Basic Settings:**
- **Name:** `tenderai` (veya istediğin isim)
- **Region:** `Frankfurt` (veya yakın bölge)
- **Branch:** `main`
- **Runtime:** `Docker`
- **Dockerfile Path:** `./TenderAI.Web/Dockerfile`
- **Docker Context:** `.` (root directory)

**Instance Type:**
- **Plan:** `Free` (başlangıç için yeterli)

### 5. Environment Variables Ekle

**"Environment"** sekmesine git ve şu değişkenleri ekle:

```bash
# OpenAI Configuration
OpenAI__ApiKey=sk-proj-YENI-API-KEYIN
OpenAI__Model=gpt-4-turbo

# Gemini Configuration
Gemini__ApiKey=AIzaSyC9rG7s8oH1VwgLc7S9rYqaLo7zwGqB5As

# Anthropic (opsiyonel)
Anthropic__ApiKey=your-anthropic-key

# Database (SQLite kullanacaksan gerek yok)
# PostgreSQL kullanacaksan Render PostgreSQL database oluştur:
ConnectionStrings__DefaultConnection=Host=YOUR-RENDER-DB-HOST;Database=tenderai;Username=user;Password=pass;Port=5432

# TenderAI Settings
TenderAI__EkapApiUrl=http://localhost:8000
TenderAI__DataSyncIntervalHours=6

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
```

**NOT:** `OpenAI__ApiKey` için yeni key almayı unutma!

### 6. Deploy Et!

1. **"Create Web Service"** butonuna tıkla
2. Render otomatik olarak:
   - Docker image build edecek
   - Container'ı çalıştıracak
   - Public URL verecek (örnek: `https://tenderai.onrender.com`)

### 7. Build Sürecini İzle

- Dashboard'da build loglarını görebilirsin
- İlk build 5-10 dakika sürebilir
- Build başarılı olunca **"Live"** durumuna geçecek

---

## 🔧 PostgreSQL Database Ekleme (Opsiyonel)

Render ücretsiz PostgreSQL sunuyor:

1. Dashboard'da **"New +"** → **"PostgreSQL"**
2. **Free** plan seç
3. Database oluşturulduktan sonra **"Internal Database URL"** kopyala
4. Web Service'in **Environment Variables**'ına ekle:
   ```
   ConnectionStrings__DefaultConnection=[KOPYALADIĞIN-URL]
   ```
5. Web Service'i **"Manual Deploy"** ile yeniden başlat

---

## 📝 Önemli Notlar

### Free Plan Limitleri:
- ✅ 750 saat/ay çalışma (bir site için yeterli)
- ✅ Otomatik HTTPS sertifikası
- ✅ Custom domain desteği
- ⚠️ 15 dakika inactivity sonrası sleep mode (ilk istek 30-60 saniye sürer)
- ⚠️ 512 MB RAM limiti

### Sleep Mode'dan Kurtulma:
- Paid plan'e geçebilirsin ($7/ay)
- Veya UptimeRobot gibi servislerle her 5 dakikada ping at

---

## 🔄 Güncelleme Nasıl Yapılır?

### Otomatik Deployment (Önerilen):
```powershell
# 1. Kodunda değişiklik yap
# 2. Git'e commit et
git add .
git commit -m "Yeni özellik eklendi"
git push

# 3. Render otomatik olarak yeni versiyonu deploy eder
```

### Manuel Deployment:
1. Render Dashboard'a git
2. Web Service'i seç
3. **"Manual Deploy"** → **"Deploy latest commit"**

---

## 🐛 Troubleshooting

### Build Hatası Alıyorsan:
1. **Logs** sekmesinden hata detaylarını kontrol et
2. Dockerfile path doğru mu? → `./TenderAI.Web/Dockerfile`
3. Docker context root'ta mı? → `.`

### 500 Internal Server Error:
1. Environment variables doğru mu kontrol et
2. API keyleri geçerli mi test et
3. **Logs** sekmesinden runtime loglarını incele

### Database Bağlantı Hatası:
1. PostgreSQL database oluşturdun mu?
2. Connection string doğru mu?
3. Render PostgreSQL internal URL'ini kullan (external değil)

---

## 📊 Monitoring

Render otomatik olarak şunları sağlar:
- **Logs:** Real-time application logs
- **Metrics:** CPU, Memory, Request count
- **Health Checks:** Otomatik restart

---

## 💰 Maliyet

**Free Plan:**
- Web Service: $0
- PostgreSQL: $0 (90 gün sonra silinir)

**Paid Plan ($7/ay):**
- Always-on (no sleep)
- 512 MB RAM
- Daha iyi performance

---

## 🎉 Deployment Sonrası

Render deploy tamamlandığında URL'i göreceksin:
```
https://tenderai.onrender.com
```

Bu URL'i tarayıcıda aç ve TenderAI'ı kullanmaya başla!

---

## 📞 Destek

- Render Docs: https://render.com/docs
- GitHub Issues: https://github.com/MhmmtDrk/TenderAI/issues

**Başarılar!** 🚀
