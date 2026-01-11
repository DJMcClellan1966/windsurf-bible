# Launch Voices of Scripture with proper error handling

Write-Host "Launching Voices of Scripture..." -ForegroundColor Cyan

# Check Ollama is running
$ollamaRunning = $false
try {
    $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
    $ollamaRunning = $response.StatusCode -eq 200
} catch {
    $ollamaRunning = $false
}

if (-not $ollamaRunning) {
    Write-Host "Warning: Ollama is not running. Starting Ollama..." -ForegroundColor Yellow
    Start-Process -FilePath "$env:LOCALAPPDATA\Programs\Ollama\ollama.exe" -ArgumentList "serve" -WindowStyle Hidden
    Start-Sleep -Seconds 3
}

# Build app
Write-Host "Building app..." -ForegroundColor Cyan
dotnet build src\AI-Bible-App.Maui\AI-Bible-App.Maui.csproj -f net10.0-windows10.0.19041.0 -v minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Launch app
Write-Host "Starting app..." -ForegroundColor Green
$appPath = "src\AI-Bible-App.Maui\bin\Debug\net10.0-windows10.0.19041.0\win-x64\AI-Bible-App.Maui.exe"

if (-not (Test-Path $appPath)) {
    Write-Host "App executable not found at $appPath" -ForegroundColor Red
    exit 1
}

$process = Start-Process -FilePath $appPath -PassThru

Write-Host "Voices of Scripture launched (PID: $($process.Id))" -ForegroundColor Green
Write-Host "Monitoring app... Press Ctrl+C to stop." -ForegroundColor Cyan

# Monitor for crashes
$checkCount = 0
while ($checkCount -lt 30) {
    Start-Sleep -Seconds 2
    $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    
    if (-not $running) {
        Write-Host "`nApp crashed! Checking event log..." -ForegroundColor Red
        
        $crashEvent = Get-EventLog -LogName Application -Newest 1 -EntryType Error -ErrorAction SilentlyContinue |
                 Where-Object { $_.Source -eq "Application Error" -and $_.Message -like "*AI-Bible-App.Maui*" }
        
        if ($crashEvent) {
            Write-Host "Error Details:" -ForegroundColor Yellow
            Write-Host $crashEvent.Message
        }
        
        exit 1
    }
    
    $checkCount++
}

Write-Host "`nApp is running stable. Monitor stopped." -ForegroundColor Green
