# Open the solution in Visual Studio for proper WinUI3 deployment
$solutionPath = "$PSScriptRoot\AI-Bible-App.sln"

Write-Host "Opening solution in Visual Studio..." -ForegroundColor Cyan
Write-Host "In Visual Studio:" -ForegroundColor Yellow
Write-Host "  1. Set 'AI-Bible-App.Maui' as startup project" -ForegroundColor Yellow
Write-Host "  2. Select 'Windows Machine' as the target" -ForegroundColor Yellow  
Write-Host "  3. Press F5 to run" -ForegroundColor Yellow
Write-Host ""
Write-Host "Visual Studio handles WinUI3 deployment correctly, unlike dotnet CLI" -ForegroundColor Green
Write-Host ""

Start-Process $solutionPath
