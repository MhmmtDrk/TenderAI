# TenderAI - Geri Dönüş Script'i
# Yeni publish çalışmıyorsa bu script'i çalıştır!

$ErrorActionPreference = "Stop"
$projectPath = "C:\Users\DELL4800\Desktop\TenderAI-Project"

Write-Host "`n========================================" -ForegroundColor Red
Write-Host "  TenderAI - Geri Dönüş" -ForegroundColor Red
Write-Host "========================================`n" -ForegroundColor Red

Set-Location $projectPath

# Son commit'leri göster
Write-Host "Son 10 commit:" -ForegroundColor Yellow
git log --oneline -10 | ForEach-Object -Begin { $i = 0 } -Process {
    $i++
    Write-Host "  $i. $_" -ForegroundColor Cyan
}

Write-Host "`n📌 Kaç adım geri dönmek istiyorsun?" -ForegroundColor Yellow
Write-Host "   Örnek: 1 = bir önceki commit" -ForegroundColor Gray
Write-Host "   Örnek: 2 = iki önceki commit" -ForegroundColor Gray

$choice = Read-Host "`nSeçim (1-10)"

if ($choice -match '^\d+$' -and [int]$choice -ge 1 -and [int]$choice -le 10) {
    Write-Host "`n⚠️  UYARI: $choice adım geri dönülecek!" -ForegroundColor Red
    $confirm = Read-Host "Emin misin? (EVET/hayır)"

    if ($confirm -eq "EVET") {
        Write-Host "`n🔄 Geri dönülüyor..." -ForegroundColor Yellow

        $steps = [int]$choice
        git reset --hard HEAD~$steps

        Write-Host "✅ $choice adım geri dönüldü!" -ForegroundColor Green
        Write-Host "`n📌 Şimdi ne yapmalısın:" -ForegroundColor Yellow
        Write-Host "   1. Visual Studio'yu aç" -ForegroundColor Gray
        Write-Host "   2. Solution'ı Rebuild et" -ForegroundColor Gray
        Write-Host "   3. Azure'a tekrar publish et" -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host "`n❌ İptal edildi" -ForegroundColor Red
    }
} else {
    Write-Host "`n❌ Geçersiz seçim!" -ForegroundColor Red
}

Write-Host "`nMevcut durum:" -ForegroundColor Cyan
git log --oneline -1
Write-Host ""
