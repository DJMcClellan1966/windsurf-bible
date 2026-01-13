# Fix permissions and run app
$binPath = "C:\Users\DJMcC\OneDrive\Desktop\bible-playground\src\AI-Bible-App.Maui\bin\Debug\net9.0-windows10.0.19041.0\win10-x64"

Write-Host "Step 1: Building app..." -ForegroundColor Cyan
dotnet build "$PSScriptRoot\src\AI-Bible-App.Maui\AI-Bible-App.Maui.csproj" -f net9.0-windows10.0.19041.0 -c Debug --no-incremental

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nStep 2: Fixing permissions..." -ForegroundColor Cyan
icacls $binPath /grant "*S-1-15-2-1:(OI)(CI)RX" /T /Q | Out-Null

Write-Host "`nStep 3: Launching app..." -ForegroundColor Cyan
Start-Sleep -Milliseconds 500
& "$binPath\AI-Bible-App.Maui.exe"

Write-Host "`nApp launched! Check your screen." -ForegroundColor Green
