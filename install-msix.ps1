# Install Voices of Scripture MSIX Package
# Run this script as Administrator

Write-Host "Installing Voices of Scripture MSIX Package" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# Trust the self-signed certificate
$certThumbprint = "AD2EEB7E3D391CFE41CE36A5D85A8B8AC49B8CE1"
$cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object {$_.Thumbprint -eq $certThumbprint}

if ($cert) {
    Write-Host "Exporting certificate..." -ForegroundColor Yellow
    $certPath = "$env:TEMP\VoicesOfScripture.cer"
    Export-Certificate -Cert $cert -FilePath $certPath | Out-Null
    
    Write-Host "Installing certificate to Trusted Root..." -ForegroundColor Yellow
    Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
    Write-Host "✓ Certificate trusted" -ForegroundColor Green
} else {
    Write-Host "✗ Certificate not found" -ForegroundColor Red
    exit 1
}

# Install MSIX package
$msixPath = "src\AI-Bible-App.Maui\bin\x64\Release\net10.0-windows10.0.19041.0\win-x64\AppPackages\AI-Bible-App.Maui_1.0.0.0_Test\AI-Bible-App.Maui_1.0.0.0_x64.msix"

if (Test-Path $msixPath) {
    Write-Host "Installing MSIX package..." -ForegroundColor Yellow
    Add-AppxPackage -Path $msixPath -ForceApplicationShutdown
    Write-Host "✓ Voices of Scripture installed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now launch 'Voices of Scripture' from the Start menu" -ForegroundColor Cyan
} else {
    Write-Host "✗ MSIX package not found at: $msixPath" -ForegroundColor Red
    exit 1
}
