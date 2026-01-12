#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Export training conversations to Ollama-compatible JSONL format

.DESCRIPTION
    Exports high-quality conversations from the app's training data repository
    to a format suitable for fine-tuning Ollama models.

.PARAMETER MinQuality
    Minimum quality score for conversations (default: 4.0)

.PARAMETER MaxConversations
    Maximum number of conversations to export (default: 1000)

.PARAMETER Character
    Filter by specific character ID (optional)

.PARAMETER OutputPath
    Custom output path (default: auto-generated timestamp)

.EXAMPLE
    .\export-training-data.ps1
    Export all high-quality conversations

.EXAMPLE
    .\export-training-data.ps1 -Character moses -MinQuality 4.5
    Export only Moses conversations with 4.5+ rating
#>

param(
    [double]$MinQuality = 4.0,
    [int]$MaxConversations = 1000,
    [string]$Character = "",
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

Write-Host "📦 Exporting Training Data" -ForegroundColor Cyan
Write-Host "=" * 60

# Paths
$dataDir = "$env:LOCALAPPDATA\AIBibleApp"
$learningDir = "$dataDir\AutonomousLearning"
$conversationsFile = "$dataDir\TrainingData\conversations.jsonl"

if (-not $OutputPath) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputPath = "$learningDir\training-$timestamp.jsonl"
}

# Create output directory
New-Item -ItemType Directory -Force -Path (Split-Path $OutputPath) | Out-Null

# Check source file
if (-not (Test-Path $conversationsFile)) {
    Write-Host "❌ No conversations found at: $conversationsFile" -ForegroundColor Red
    Write-Host "   Use the app to collect training data first." -ForegroundColor Yellow
    exit 1
}

$totalConversations = (Get-Content $conversationsFile | Measure-Object -Line).Lines
Write-Host "📊 Found $totalConversations total conversations" -ForegroundColor White

# Process and filter conversations
Write-Host "🔍 Filtering conversations..." -ForegroundColor Yellow
Write-Host "   Minimum Quality: $MinQuality" -ForegroundColor Gray
if ($Character) {
    Write-Host "   Character Filter: $Character" -ForegroundColor Gray
}

$exported = 0
$filtered = 0

Get-Content $conversationsFile | ForEach-Object {
    try {
        $conv = $_ | ConvertFrom-Json
        
        # Apply filters
        if ($conv.qualityScore -lt $MinQuality) {
            $filtered++
            return
        }
        
        if ($Character -and $conv.characterId -ne $Character) {
            $filtered++
            return
        }
        
        if ($exported -ge $MaxConversations) {
            return
        }
        
        # Convert to Ollama training format
        $messages = @()
        foreach ($msg in $conv.messages) {
            $messages += @{
                role = $msg.role
                content = $msg.content
            }
        }
        
        $trainingExample = @{
            messages = $messages
        } | ConvertTo-Json -Compress
        
        # Append to output file
        Add-Content -Path $OutputPath -Value $trainingExample
        $exported++
        
        if ($exported % 50 -eq 0) {
            Write-Host "   Exported: $exported..." -ForegroundColor DarkGray
        }
    }
    catch {
        Write-Host "⚠️  Skipped malformed conversation: $_" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "✅ Export complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "   Total Conversations: $totalConversations" -ForegroundColor White
Write-Host "   Filtered Out: $filtered" -ForegroundColor White
Write-Host "   Exported: $exported" -ForegroundColor Green
Write-Host "   Output File: $OutputPath" -ForegroundColor White
Write-Host "   File Size: $([math]::Round((Get-Item $OutputPath).Length / 1MB, 2)) MB" -ForegroundColor White

if ($exported -lt 100) {
    Write-Host ""
    Write-Host "⚠️  Warning: Less than 100 conversations exported" -ForegroundColor Yellow
    Write-Host "   Recommended: 300+ for good results, 100 minimum" -ForegroundColor Yellow
    Write-Host "   Consider generating synthetic data or lowering MinQuality" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Next: Run fine-tuning with:" -ForegroundColor Cyan
Write-Host "   .\scripts\fine-tune-model.ps1" -ForegroundColor White
