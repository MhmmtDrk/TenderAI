# TenderAI Backup Script
# Her publish öncesi çalıştır

$date = Get-Date -Format "yyyyMMdd-HHmmss"
$projectPath = "C:\Users\DELL4800\Desktop\TenderAI-Project"
$backupRoot = "$projectPath\Backups"
$backupPath = "$backupRoot\TenderAI-Backup-$date.zip"

# Backup klasörü yoksa oluştur
if (!(Test-Path $backupRoot)) {
    New-Item -ItemType Directory -Path $backupRoot | Out-Null
}

Write-Host "🔄 Backup oluşturuluyor..." -ForegroundColor Yellow

# Projeyi ziple (bin ve obj klasörlerini hariç tut)
Compress-Archive -Path "$projectPath\*" `
                 -DestinationPath $backupPath `
                 -Force `
                 -CompressionLevel Optimal

Write-Host "✅ Backup oluşturuldu: $backupPath" -ForegroundColor Green
Write-Host "📦 Boyut: $([math]::Round((Get-Item $backupPath).Length / 1MB, 2)) MB" -ForegroundColor Cyan

# Son 5 backup'ı tut, eskilerini sil
$backups = Get-ChildItem $backupRoot -Filter "*.zip" | Sort-Object CreationTime -Descending
if ($backups.Count -gt 5) {
    $backups | Select-Object -Skip 5 | Remove-Item
    Write-Host "🗑️ Eski backup'lar temizlendi" -ForegroundColor Gray
}

Write-Host "`n✨ Artık Azure'a publish edebilirsin!" -ForegroundColor Green
