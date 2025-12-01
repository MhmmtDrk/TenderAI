# Azure Free Tier - Geri Dönüş Rehberi

## Senin Durumun:
- ✅ Azure Free Credit kullanıyorsun ($200)
- ❌ Deployment Slots YOK (Premium gerekli)
- ❌ Redeploy butonu YOK
- ✅ Sadece normal publish yapıyorsun

---

## EN KOLAY ÇÖZÜM: Git + Backup Script

### Adım 1: Git Repository Oluştur (İLK SEFER)

```powershell
# Proje klasöründe
cd C:\Users\DELL4800\Desktop\TenderAI-Project

# Git başlat (ilk sefer)
git init

# .gitignore oluştur
@"
bin/
obj/
.vs/
*.user
*.suo
Backups/
appsettings.Development.json
"@ | Out-File -FilePath .gitignore -Encoding utf8

# İlk commit
git add .
git commit -m "Initial commit - Working version"
```

---

### Adım 2: Her Publish Öncesi Commit Yap

```powershell
# Değişiklikleri kaydet
git add .
git commit -m "Before publish - $(Get-Date -Format 'yyyy-MM-dd HH:mm')"

# Backup script'i çalıştır
.\backup-before-publish.ps1

# Şimdi Visual Studio'dan publish et
```

---

### Adım 3: Sorun Olursa Geri Dön

#### 3A: Git ile Son Çalışan Versiyona Dön
```powershell
# Son commit'leri gör
git log --oneline -10

# Önceki commit'e dön (örnek)
git reset --hard HEAD~1

# Veya spesifik commit'e dön
git reset --hard abc1234

# Tekrar publish et
```

#### 3B: Backup ZIP'inden Geri Dön
```powershell
# Backups klasöründen son çalışan versiyonu seç
# ZIP'i aç
# Visual Studio'dan tekrar publish et
```

---

## HIZLI KULLANIM SCRIPT'İ

Sana hazır script hazırladım - her publish öncesi çalıştır:
