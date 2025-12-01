# TenderAI - Azure Publish Hazırlık Script'i
# Her publish öncesi MUTLAKA çalıştır!

param(
    [string]$CommitMessage = "Publish öncesi otomatik yedek - $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
)

$ErrorActionPreference = "Stop"
$projectPath = "C:\Users\DELL4800\Desktop\TenderAI-Project"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  TenderAI - Publish Hazırlık" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# 1. Git repository var mı kontrol et
if (!(Test-Path "$projectPath\.git")) {
    Write-Host "⚠️  Git repository bulunamadı. İlk kurulum yapılıyor..." -ForegroundColor Yellow

    Set-Location $projectPath
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

    Write-Host "✅ Git repository oluşturuldu" -ForegroundColor Green
}

Set-Location $projectPath

# 2. Değişiklikleri kontrol et
Write-Host "📝 Değişiklikler kontrol ediliyor..." -ForegroundColor Yellow

$status = git status --porcelain
if ([string]::IsNullOrEmpty($status)) {
    Write-Host "ℹ️  Hiç değişiklik yok!" -ForegroundColor Gray
} else {
    Write-Host "`nDeğişen dosyalar:" -ForegroundColor Cyan
    git status --short

    # 3. Git commit yap
    Write-Host "`n💾 Git commit yapılıyor..." -ForegroundColor Yellow
    git add .
    git commit -m $CommitMessage
    Write-Host "✅ Git commit başarılı" -ForegroundColor Green
}

# 4. Backup oluştur
Write-Host "`n📦 Backup oluşturuluyor..." -ForegroundColor Yellow
& "$projectPath\backup-before-publish.ps1"

# 5. Son durumu göster
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Son 5 commit:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
git log --oneline -5

Write-Host "`n✨ Hazırsın! Şimdi Visual Studio'dan publish yapabilirsin!" -ForegroundColor Green
Write-Host "`n📌 Sorun olursa:" -ForegroundColor Yellow
Write-Host "   1. Geri dönmek için: .\geri-don.ps1" -ForegroundColor Gray
Write-Host "   2. Commit'leri görmek için: git log --oneline -10" -ForegroundColor Gray
Write-Host ""
