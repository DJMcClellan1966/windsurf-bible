#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate synthetic training conversations for fine-tuning

.DESCRIPTION
    Uses the existing phi3:mini model to generate high-quality synthetic
    conversations between users and biblical characters. This helps supplement
    real user data for fine-tuning.

.PARAMETER Count
    Number of conversations to generate (default: 100)

.PARAMETER Characters
    Comma-separated list of character IDs (default: moses,david,ruth,esther,paul,peter)

.PARAMETER Topics
    Comma-separated list of topics (default: faith,fear,leadership,doubt,forgiveness)

.EXAMPLE
    .\generate-synthetic-data.ps1 -Count 200
    Generate 200 synthetic conversations

.EXAMPLE
    .\generate-synthetic-data.ps1 -Characters "moses,david" -Count 50
    Generate 50 conversations only for Moses and David
#>

param(
    [int]$Count = 100,
    [string[]]$Characters = @("moses", "david", "ruth", "esther", "paul", "peter", "mary", "joshua"),
    [string[]]$Topics = @("faith", "fear", "leadership", "doubt", "forgiveness", "suffering", "obedience", "courage")
)

$ErrorActionPreference = "Stop"

Write-Host "🤖 Generating Synthetic Training Data" -ForegroundColor Cyan
Write-Host "=" * 60

$dataDir = "$env:LOCALAPPDATA\AIBibleApp\TrainingData"
$outputFile = "$dataDir\synthetic-conversations-$(Get-Date -Format 'yyyyMMdd-HHmmss').jsonl"

# Create directory
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null

Write-Host "📊 Configuration:" -ForegroundColor Yellow
Write-Host "   Conversations: $Count" -ForegroundColor White
Write-Host "   Characters: $($Characters -join ', ')" -ForegroundColor White
Write-Host "   Topics: $($Topics -join ', ')" -ForegroundColor White
Write-Host ""

# Character system prompts
$characterPrompts = @{
    "moses" = "You are Moses, the Hebrew prophet who led the Israelites out of Egyptian slavery. You speak from your experiences confronting Pharaoh, receiving the Ten Commandments, and leading a rebellious people through the wilderness for 40 years."
    "david" = "You are King David, the shepherd boy who became Israel's greatest king. You speak from your experiences defeating Goliath, fleeing from Saul, your moral failures, and your deep relationship with God expressed in the Psalms."
    "ruth" = "You are Ruth, the Moabite woman who showed extraordinary loyalty to her mother-in-law Naomi. You speak from your experience of loss, loyalty, faith in a foreign land, and finding redemption through Boaz."
    "esther" = "You are Queen Esther, the Jewish woman who became queen of Persia. You speak from your experience of hidden identity, courage in approaching the king unbidden, and saving your people from genocide."
    "paul" = "You are Paul the Apostle, formerly Saul the persecutor of Christians. You speak from your dramatic conversion, missionary journeys, imprisonments, and writing many New Testament letters."
    "peter" = "You are Simon Peter, Jesus' chief disciple. You speak from your impulsive declarations, walking on water, denying Jesus three times, and your restoration and leadership in the early church."
    "mary" = "You are Mary, the mother of Jesus. You speak from your experience of angelic visitation, virgin birth, raising the Messiah, and watching your son's crucifixion and resurrection."
    "joshua" = "You are Joshua, Moses' successor who led Israel into the Promised Land. You speak from your experience as a warrior, conquering Jericho, and distributing the land to the twelve tribes."
}

# Question templates
$questionTemplates = @(
    "How did you handle {topic} in your life?",
    "Tell me about a time when you experienced {topic}",
    "What would you say to someone struggling with {topic}?",
    "How did {topic} shape your faith journey?",
    "Can you share a personal story about {topic}?",
    "What did you learn about {topic} through your experiences?",
    "How would you encourage someone facing {topic}?",
    "What was your biggest challenge with {topic}?"
)

$generated = 0
$failed = 0

for ($i = 0; $i -lt $Count; $i++) {
    $character = $Characters | Get-Random
    $topic = $Topics | Get-Random
    $template = $questionTemplates | Get-Random
    $question = $template -replace '\{topic\}', $topic
    
    $systemPrompt = $characterPrompts[$character]
    
    # Progress indicator
    if ($generated % 10 -eq 0) {
        $percent = [math]::Round(($generated / $Count) * 100, 1)
        Write-Host "   Progress: $percent% ($generated/$Count) - Current: $character on $topic" -ForegroundColor DarkGray
    }
    
    try {
        # Generate response using Ollama
        $response = ollama run phi3:mini "$systemPrompt`n`nUser: $question`n`nRespond naturally in first person as this character. Draw from specific biblical events. Be conversational and authentic, not preachy. Keep it 2-3 paragraphs." --verbose false 2>$null
        
        if ([string]::IsNullOrWhiteSpace($response)) {
            $failed++
            continue
        }
        
        # Create training conversation
        $conversation = @{
            characterId = $character
            topic = $topic
            qualityScore = 4.0
            syntheticData = $true
            generatedAt = (Get-Date -Format "o")
            messages = @(
                @{
                    role = "system"
                    content = $systemPrompt
                },
                @{
                    role = "user"
                    content = $question
                },
                @{
                    role = "assistant"
                    content = $response.Trim()
                }
            )
        } | ConvertTo-Json -Compress
        
        # Append to file
        Add-Content -Path $outputFile -Value $conversation
        $generated++
    }
    catch {
        Write-Host "⚠️  Failed to generate conversation: $_" -ForegroundColor Yellow
        $failed++
    }
}

Write-Host ""
Write-Host "✅ Generation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "   Generated: $generated" -ForegroundColor Green
Write-Host "   Failed: $failed" -ForegroundColor $(if($failed -gt 0){"Yellow"}else{"White"})
Write-Host "   Output File: $outputFile" -ForegroundColor White
Write-Host "   File Size: $([math]::Round((Get-Item $outputFile).Length / 1MB, 2)) MB" -ForegroundColor White

Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Review quality: code $outputFile" -ForegroundColor White
Write-Host "   2. Merge with real data (or use standalone)" -ForegroundColor White
Write-Host "   3. Run fine-tuning: .\scripts\fine-tune-model.ps1" -ForegroundColor White

# Append to main conversations file if it exists
$mainFile = "$dataDir\conversations.jsonl"
if (Test-Path $mainFile) {
    Get-Content $outputFile | Add-Content $mainFile
    Write-Host ""
    Write-Host "✅ Synthetic data appended to main conversations file" -ForegroundColor Green
}
