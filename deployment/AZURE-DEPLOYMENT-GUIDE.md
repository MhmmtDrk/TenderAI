# TenderAI - Azure Deployment Rehberi

Bu rehber, TenderAI sistemini Azure'da tenderAI.net domain'i ile yayınlamanız için adım adım talimatlar içerir.

## 🎯 Genel Bakış

Azure'da şunları oluşturacağız:
- **Azure App Service** - TenderAI.Web uygulaması için
- **Azure Database for PostgreSQL** - Veritabanı için
- **Azure App Service (WebJob)** - DataCollector arka plan servisi için
- **Custom Domain** - tenderAI.net domain bağlantısı
- **SSL Sertifikası** - Ücretsiz Azure tarafından sağlanacak

---

## 📋 Ön Hazırlık

### 1. Azure Portal'a Giriş
- https://portal.azure.com adresine git
- Yeni oluşturduğun hesapla giriş yap

### 2. Resource Group Oluştur
```
1. Sol menüden "Resource groups" seç
2. "+ Create" butonuna tıkla
3. Bilgileri doldur:
   - Subscription: Free Trial
   - Resource group name: rg-tenderai
   - Region: West Europe (Avrupa'ya en yakın)
4. "Review + create" > "Create"
```

---

## 🗄️ ADIM 1: PostgreSQL Veritabanı Oluştur

### 1.1. PostgreSQL Server Oluştur
```
1. Azure Portal'da "Create a resource" tıkla
2. "Azure Database for PostgreSQL" ara
3. "Flexible Server" seç (Önerilen)
4. "Create" tıkla

Bilgileri doldur:
- Resource group: rg-tenderai
- Server name: tenderai-db (benzersiz olmalı)
- Region: West Europe
- PostgreSQL version: 15
- Workload type: Development (ücretsiz kredi için)

Authentication:
- Admin username: tenderadmin
- Password: Güçlü bir şifre oluştur (kaydet!)

Networking:
- Connectivity method: Public access
- ✅ Allow public access from any Azure service

5. "Review + create" > "Create"
```

### 1.2. Firewall Kuralı Ekle (Geliştirme İçin)
```
1. Oluşturulan PostgreSQL server'a git
2. Sol menüden "Networking" seç
3. "Add current client IP address" tıkla (kendi IP'n)
4. "Add 0.0.0.0 - 255.255.255.255" ekle (tüm IP'ler - geçici)
5. "Save"
```

### 1.3. Veritabanı Oluştur
```
1. PostgreSQL server'da "Databases" seç
2. "+ Add" tıkla
3. Name: tenderai
4. "Save"
```

### 1.4. Connection String'i Kaydet
```
1. Sol menüden "Connect" seç
2. Connection string'i kopyala, şuna benzer:

Host=tenderai-db.postgres.database.azure.com;
Database=tenderai;
Username=tenderadmin;
Password=YOUR_PASSWORD;
SSL Mode=Require
```

---

## 🌐 ADIM 2: Web App (TenderAI.Web) Oluştur

### 2.1. App Service Oluştur
```
1. Azure Portal'da "Create a resource"
2. "Web App" ara ve seç
3. "Create" tıkla

Basics:
- Resource group: rg-tenderai
- Name: tenderai-web (bu YOUR_APP_NAME.azurewebsites.net olacak)
- Publish: Code
- Runtime stack: .NET 8 (LTS)
- Operating System: Linux
- Region: West Europe

Pricing:
- Plan: Free F1 (başlangıç için, sonra upgrade edebilirsin)

4. "Review + create" > "Create"
```

### 2.2. App Service Configuration (Önemli!)
```
1. Oluşturulan App Service'e git
2. Sol menüden "Configuration" seç

Application Settings:
3. "+ New application setting" ile ekle:

Name: ConnectionStrings__DefaultConnection
Value: Host=tenderai-db.postgres.database.azure.com;Database=tenderai;Username=tenderadmin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true

Name: GeminiSettings__ApiKey
Value: YOUR_GEMINI_API_KEY

Name: ASPNETCORE_ENVIRONMENT
Value: Production

4. "Save" butonuna tıkla
5. "Continue" ile onayla
```

### 2.3. Custom Domain Ekle (tenderAI.net)
```
1. App Service'de "Custom domains" seç
2. "+ Add custom domain" tıkla
3. Domain name: tenderai.net
4. Azure sana DNS kayıtlarını gösterecek:

   A Record veya CNAME Record:
   - Type: A veya CNAME
   - Name: @ (root domain için)
   - Value: tenderai-web.azurewebsites.net

5. Domain sağlayıcına git (GoDaddy, Namecheap vs.)
6. DNS ayarlarından bu kayıtları ekle
7. 10-15 dakika bekle (DNS propagation)
8. Azure'da "Validate" tıkla
9. "Add" tıkla

SSL/TLS:
10. Custom domain eklendikten sonra
11. "Add binding" tıkla
12. "Managed Certificate" seç (ücretsiz)
13. "Add"
```

---

## ⚙️ ADIM 3: DataCollector WebJob Oluştur

### 3.1. WebJob için Hazırlık
DataCollector'ı WebJob olarak çalıştıracağız.

```
1. Visual Studio'da DataCollector projesini aç
2. Sağ tık > Publish
3. Target: Folder
4. Folder location: bin\Release\net8.0\publish
5. "Publish" tıkla
```

### 3.2. WebJob Dosyası Hazırla
```
1. publish klasöründeki tüm dosyaları seç
2. Sağ tık > Send to > Compressed (zipped) folder
3. Adını "DataCollector.zip" olarak değiştir
```

### 3.3. WebJob'u Azure'a Yükle
```
1. App Service (tenderai-web) git
2. Sol menüden "WebJobs" seç
3. "+ Add" tıkla

Name: DataCollector
File Upload: DataCollector.zip dosyasını seç
Type: Continuous (sürekli çalışsın)
Scale: Single Instance

4. "OK" tıkla
5. WebJob başlatıldığını kontrol et
```

---

## 🚀 ADIM 4: TenderAI.Web Deploy

### 4.1. Visual Studio'dan Publish

```
1. Visual Studio'da TenderAI.Web projesini aç
2. Sağ tık > Publish
3. "Azure" seç > Next
4. "Azure App Service (Linux)" seç > Next
5. Azure hesabınla giriş yap
6. "tenderai-web" App Service'i seç
7. "Finish"
8. "Publish" butonuna tıkla
```

### 4.2. Database Migration Çalıştır

Deploy edildikten sonra veritabanı tablolarını oluşturmamız gerekiyor.

**Seçenek A: Local'den Migration**
```bash
# TenderAI.Web klasöründe
dotnet ef database update --connection "Host=tenderai-db.postgres.database.azure.com;Database=tenderai;Username=tenderadmin;Password=YOUR_PASSWORD;SSL Mode=Require"
```

**Seçenek B: Azure Portal Console**
```
1. App Service > "SSH" veya "Console"
2. Komut:
dotnet ef database update
```

---

## ✅ ADIM 5: Test ve Doğrulama

### 5.1. Web Sitesini Test Et
```
1. https://tenderai-web.azurewebsites.net (geçici Azure URL)
2. https://tenderai.net (custom domain - DNS propagation sonrası)
```

### 5.2. Kontrol Listesi
- [ ] Ana sayfa açılıyor mu?
- [ ] Dosya yükleme çalışıyor mu?
- [ ] Analiz başlatılıyor mu?
- [ ] DataCollector WebJob çalışıyor mu? (App Service > WebJobs > Status: Running)
- [ ] HTTPS çalışıyor mu? (yeşil kilit simgesi)
- [ ] Custom domain çalışıyor mu?

### 5.3. Logları Kontrol Et
```
App Service > Log stream
- Canlı logları görebilirsin
- Hata varsa burada görünür
```

---

## 🔧 Sorun Giderme

### Problem: "500 Internal Server Error"
**Çözüm:**
```
1. App Service > Configuration
2. ASPNETCORE_ENVIRONMENT = Production olduğunu kontrol et
3. Connection string doğru mu kontrol et
4. Logs > Log stream'den hata mesajını oku
```

### Problem: "Database connection failed"
**Çözüm:**
```
1. PostgreSQL > Networking > Firewall
2. Azure services'e izin verilmiş mi?
3. Connection string şifre doğru mu?
4. SSL Mode=Require eklendi mi?
```

### Problem: "DataCollector çalışmıyor"
**Çözüm:**
```
1. App Service > WebJobs > DataCollector > Logs
2. Hata mesajını kontrol et
3. Connection string WebJob'da da var mı?
```

### Problem: "Custom domain çalışmıyor"
**Çözüm:**
```
1. DNS kayıtlarını kontrol et (nslookup tenderai.net)
2. 24 saat bekle (DNS propagation)
3. CNAME yerine A record dene veya tersi
```

---

## 💰 Maliyet Kontrolü

### Ücretsiz Tier Limitleri
- **App Service Free F1**: 60 dakika/gün CPU, 1GB RAM
- **PostgreSQL Flexible Server**: 750 saat/ay ücretsiz (12 ay)
- **SSL Certificate**: Ücretsiz (Azure Managed)
- **Bandwidth**: 5GB/ay

### Upgrade Gerekirse
Eğer Free tier yetersiz kalırsa:
- **App Service**: B1 (Basic) - ~$13/ay - Daha fazla CPU/RAM
- **PostgreSQL**: Burstable tier - ~$12/ay - Production için yeterli

---

## 🎉 Tebrikler!

TenderAI sisteminiz artık https://tenderai.net adresinde yayında!

Herkes şimdi:
1. Tender dokümanlarını yükleyebilir
2. AI analizi yapabilir
3. Risk raporlarını görebilir
4. Fiyat önerileri alabilir

**Güvenlik Önerileri (Production):**
- [ ] Rate limiting ekle (çok fazla istek engellemek için)
- [ ] File size limiti kontrol et (appsettings'de)
- [ ] PostgreSQL firewall'u daralt (sadece Azure'a izin ver)
- [ ] Application Insights ekle (monitoring için)
- [ ] Backup planı yap (veritabanı yedekleme)

---

## 📞 Destek

Sorun yaşarsan:
1. Azure Portal > Support > New support request
2. Veya Azure free tier documentation: https://azure.microsoft.com/free/

**Önemli Linkler:**
- Azure Portal: https://portal.azure.com
- App Service Dokümantasyon: https://docs.microsoft.com/azure/app-service/
- PostgreSQL Dokümantasyon: https://docs.microsoft.com/azure/postgresql/
