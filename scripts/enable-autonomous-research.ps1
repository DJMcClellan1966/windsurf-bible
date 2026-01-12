#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Enable autonomous character research system

.DESCRIPTION
    Configures the app to autonomously research biblical characters during
    off-peak hours, prioritizing the most popular characters.

.PARAMETER TopCharacters
    Number of top characters to research (default: 5)

.PARAMETER StartHour
    Hour to start research (24-hour format, default: 2 = 2 AM)

.PARAMETER EndHour
    Hour to end research (24-hour format, default: 6 = 6 AM)

.PARAMETER InitialResearch
    Run initial research immediately for all characters (default: false)

.EXAMPLE
    .\enable-autonomous-research.ps1 -TopCharacters 5
    Enable research for top 5 most-used characters

.EXAMPLE
    .\enable-autonomous-research.ps1 -TopCharacters 10 -StartHour 1 -EndHour 5
    Research top 10 characters between 1-5 AM
#>

param(
    [int]$TopCharacters = 5,
    [int]$StartHour = 2,
    [int]$EndHour = 6,
    [switch]$InitialResearch
)

$ErrorActionPreference = "Stop"

Write-Host "🔍 Configuring Autonomous Character Research" -ForegroundColor Cyan
Write-Host "=" * 60

# Update appsettings.json
$appSettingsPath = "src\AI-Bible-App.Maui\appsettings.json"

if (-not (Test-Path $appSettingsPath)) {
    Write-Host "❌ Could not find $appSettingsPath" -ForegroundColor Red
    exit 1
}

$appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json

# Create research config object
$researchConfig = [PSCustomObject]@{
    Enabled = $true
    TopCharactersCount = $TopCharacters
    StartHour = $StartHour
    EndHour = $EndHour
    WhitelistedSources = @(
        "biblehub.com"
        "blueletterbible.org"
        "biblegateway.com"
        "biblicalarchaeology.org"
        "worldhistory.org"
    )
    RequireMultiSourceValidation = $true
    RequireHumanReview = $false
    MaxFindingsPerSession = 10
    ResearchIntervalDays = 7
}

# Add or update the property
$appSettings | Add-Member -NotePropertyName "AutonomousResearch" -NotePropertyValue $researchConfig -Force

# Save updated settings
$appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath -Encoding UTF8

Write-Host "✅ Autonomous research configuration updated" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Settings:" -ForegroundColor Yellow
Write-Host "   Top Characters: $TopCharacters" -ForegroundColor White
Write-Host "   Research Window: $StartHour`:00 - $EndHour`:00" -ForegroundColor White
Write-Host "   Multi-Source Validation: Enabled" -ForegroundColor White
Write-Host "   Auto-Approve High Confidence: Yes" -ForegroundColor White

# Create research data directory
$researchDir = "$env:LOCALAPPDATA\AIBibleApp\Research"
New-Item -ItemType Directory -Force -Path $researchDir | Out-Null
Write-Host ""
Write-Host "📁 Research directory: $researchDir" -ForegroundColor Gray

# Create research queue file
$queueFile = "$researchDir\queue.json"
if (-not (Test-Path $queueFile)) {
    @{
        enabled = $true
        lastRun = $null
        nextRun = $null
        queue = @()
        completed = @()
    } | ConvertTo-Json -Depth 10 | Out-File $queueFile -Encoding UTF8
}

if ($InitialResearch) {
    Write-Host ""
    Write-Host "🚀 Starting initial research..." -ForegroundColor Yellow
    Write-Host "   This will run in the background and may take several hours" -ForegroundColor Gray
    Write-Host "   Check status with: .\scripts\get-research-status.ps1" -ForegroundColor Gray
    
    # Queue all characters for initial research
    $characters = @("moses", "david", "ruth", "esther", "paul", "peter", "mary", "joshua", "daniel", "joseph")
    $queue = Get-Content $queueFile | ConvertFrom-Json
    
    foreach ($char in $characters) {
        $queue.queue += @{
            characterId = $char
            priority = "normal"
            queuedAt = (Get-Date -Format "o")
            status = "queued"
            topicsToResearch = @("historical-context", "cultural-insight", "language-insight")
        }
    }
    
    $queue | ConvertTo-Json -Depth 10 | Out-File $queueFile -Encoding UTF8
    Write-Host "✅ Queued $($characters.Count) characters for initial research" -ForegroundColor Green
}

Write-Host ""
Write-Host "📖 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. App will research during $StartHour`:00-$EndHour`:00 daily" -ForegroundColor White
Write-Host "   2. Check status: .\scripts\get-research-status.ps1" -ForegroundColor White
Write-Host "   3. Review findings: .\scripts\review-research-findings.ps1" -ForegroundColor White
Write-Host "   4. Monitor in Admin page (coming soon)" -ForegroundColor White

Write-Host ""
Write-Host "⚙️  How It Works:" -ForegroundColor Yellow
Write-Host "   • Tracks which characters you use most" -ForegroundColor White
Write-Host "   • Researches top $TopCharacters during off-peak hours" -ForegroundColor White
Write-Host "   • Validates findings from multiple trusted sources" -ForegroundColor White
Write-Host "   • Auto-integrates high-confidence findings" -ForegroundColor White
Write-Host "   • Flags controversial content for your review" -ForegroundColor White

Write-Host ""
Write-Host "🛡️  Safety:" -ForegroundColor Green
Write-Host "   • Only whitelisted academic sources" -ForegroundColor White
Write-Host "   • Multi-source cross-validation" -ForegroundColor White
Write-Host "   • AI content validation" -ForegroundColor White
Write-Host "   • You can review/reject any findings" -ForegroundColor White

Write-Host ""
Write-Host "✨ Done! Autonomous research is now enabled." -ForegroundColor Green
