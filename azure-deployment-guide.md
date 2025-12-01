# Azure Deployment Slots Kurulum Rehberi

## Deployment Slots Nedir?
Deployment slots, aynı App Service içinde birden fazla ortam (production, staging vb.) oluşturmanı sağlar.

## Adımlar:

### 1. Azure Portal'da Slot Oluştur

1. **Azure Portal** → App Service'ini aç
2. Sol menüde **"Deployment"** → **"Deployment slots"**
3. **"+ Add Slot"** butonuna tıkla
4. İsim ver: `staging`
5. **"Clone settings from"** → Production seç
6. **"Add"** tıkla

✅ Artık 2 environment'ın var:
- `your-app-name.azurewebsites.net` (Production)
- `your-app-name-staging.azurewebsites.net` (Staging)

---

### 2. Visual Studio'dan Staging'e Publish Et

#### İlk Kurulum (Tek Sefer):

1. **Solution Explorer** → Proje sağ tık → **"Publish"**
2. **"New"** → **"Azure"** → **"Next"**
3. **"Azure App Service (Windows)"** → **"Next"**
4. Subscription seç
5. **Resource Group** ve **App Service** seç
6. **⚠️ ÖNEMLİ:** Altta **"Deployment Slot"** dropdown'ını aç
7. **"staging"** seç
8. **"Finish"**
9. **"Publish"**

---

### 3. Test Et ve Production'a Al

#### Staging'de Test:
1. Tarayıcıda aç: `https://your-app-name-staging.azurewebsites.net`
2. Demo sayfasını test et
3. Her şey çalışıyorsa devam et

#### Production'a Swap:
1. **Azure Portal** → App Service
2. **"Deployment"** → **"Deployment slots"**
3. **"Swap"** butonuna tıkla
4. **Source:** staging
5. **Target:** production
6. **"Swap"** tıkla
7. ✅ 30 saniyede staging → production olur

---

### 4. Sorun Olursa Geri Dön

#### Hızlı Geri Dönüş (30 saniye):
1. **Azure Portal** → **"Deployment slots"**
2. **"Swap"** tıkla
3. **Source:** production
4. **Target:** staging
5. **"Swap"** → Eski versiyona dön!

---

## Alternatif: Azure CLI ile Swap

```powershell
# Azure CLI yükle (ilk sefer)
winget install Microsoft.AzureCLI

# Login
az login

# Swap yap
az webapp deployment slot swap `
  --name your-app-name `
  --resource-group your-resource-group `
  --slot staging

# Geri dön
az webapp deployment slot swap `
  --name your-app-name `
  --resource-group your-resource-group `
  --slot staging
```

---

## PowerShell Script ile Otomatik Backup + Staging Deploy

Hazırladığım script'i kullan:
```powershell
# backup-before-publish.ps1 çalıştır
.\backup-before-publish.ps1

# Visual Studio'dan staging'e publish et
# Test et
# Azure Portal'dan swap yap
```

---

## Deployment Slots Kullanmanın Avantajları:

✅ **Zero Downtime** - Production hiç kapanmaz
✅ **Anında Geri Dönüş** - 30 saniyede eski versiyona dön
✅ **Test Ortamı** - Production'ı bozmadan test et
✅ **Configuration Koruması** - Secrets korunur
✅ **Warming** - Uygulama önceden warm-up olur

---

## Slot Yoksa Ne Yapmalı?

### Bedava Planlarda Slot Yok!
- **Free/Shared:** Slot YOK ❌
- **Basic (B1+):** Deployment slots VAR ✅
- **Standard (S1+):** En fazla 5 slot
- **Premium (P1+):** En fazla 20 slot

### Çözüm:
1. **Azure Portal** → App Service
2. **"Scale up (App Service plan)"**
3. **"Production"** veya **"Isolated"** tier seç
4. En ucuz: **Basic B1** (~$55/ay)

---

## Bedava Planda İsem:

### Manuel Backup + Redeploy:
1. Her publish öncesi `backup-before-publish.ps1` çalıştır
2. Visual Studio'dan publish et
3. Sorun olursa:
   - Backups klasöründen eski ZIP'i aç
   - Tekrar publish et

---

## Özet - En İyi Yöntem:

1. ✅ **Backup Script** her zaman çalıştır
2. ✅ **Deployment Slots** kullan (Basic+ plan gerekli)
3. ✅ **Staging → Production Swap** ile güvenli deploy
4. ✅ Sorun olursa **30 saniyede geri dön**

---

## Slot Oluşturma Video:
https://learn.microsoft.com/en-us/azure/app-service/deploy-staging-slots
