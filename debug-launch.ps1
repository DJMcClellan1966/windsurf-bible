# Launch app with debugger support
$appPath = "C:\Users\DJMcC\OneDrive\Desktop\bible-playground\src\AI-Bible-App.Maui\bin\Debug\net9.0-windows10.0.19041.0\win10-x64\AI-Bible-App.Maui.exe"

Write-Host "Launching app in debug mode..." -ForegroundColor Cyan
Write-Host "Process will wait for 5 seconds before starting to allow debugger attach" -ForegroundColor Yellow
Write-Host ""

# Start process with output redirection
$process = Start-Process -FilePath $appPath -PassThru -RedirectStandardOutput "app-output.log" -RedirectStandardError "app-error.log" -NoNewWindow -ErrorAction Stop

Write-Host "App started with PID: $($process.Id)" -ForegroundColor Green
Write-Host "Waiting 5 seconds..." -ForegroundColor Yellow

Start-Sleep -Seconds 5

if ($process.HasExited) {
    Write-Host "App has already exited with code: $($process.ExitCode)" -ForegroundColor Red
    Write-Host "`nStandard Output:" -ForegroundColor Cyan
    Get-Content "app-output.log" -ErrorAction SilentlyContinue
    Write-Host "`nError Output:" -ForegroundColor Red
    Get-Content "app-error.log" -ErrorAction SilentlyContinue
} else {
    Write-Host "App is running" -ForegroundColor Green
    Write-Host "Press any key to stop the app..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    Stop-Process -Id $process.Id -Force
}
