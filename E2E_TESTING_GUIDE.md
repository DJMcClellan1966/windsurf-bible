# End-to-End Testing Guide

## Prerequisites

1. **Ollama Running**: Ensure Ollama is running with required models:
   ```powershell
   ollama run phi4
   ollama pull nomic-embed-text
   ```

2. **Bible Data**: Run the downloader to generate Bible data:
   ```powershell
   dotnet run --project src/AI-Bible-App.Console download-bible
   ```

## Test Scenarios

### 1. Initialization Flow ✅

**Expected Behavior:**
- App launches with InitializationPage
- Progress bar animates through stages:
  - "Checking Ollama availability..."
  - "Loading Bible verses..."
  - "Indexing Scripture (embedding generation)..."
- Completes in 3-10 seconds (depending on Ollama speed)
- Navigates to CharacterSelectionPage

**Error Scenarios:**
- If Ollama not running: Shows error message with "Continue Anyway" button
- If models missing: Shows specific error about which model is missing
- If Bible data missing: Falls back to empty verse list (still functional)

### 2. Character Selection ✅

**Expected Behavior:**
- Grid shows 7 characters:
  1. King David (Royal Blue) - "A man after God's own heart"
  2. Apostle Paul (Crimson) - "Ambassador for Christ"
  3. Moses (Goldenrod) - "Friend of God, deliverer of His people"
  4. Mary Mother of Jesus (Soft Pink) - "Blessed among women"
  5. Apostle Peter (Slate Blue) - "The Rock, restored and redeemed"
  6. Queen Esther (Deep Purple) - "For such a time as this"
  7. John the Beloved (Emerald) - "The disciple whom Jesus loved"

**Interactions:**
- Tap character → Navigate to ChatPage with selected character
- Character name and description visible
- Colors distinguishable

### 3. Chat Flow with RAG 🔄

**Test Questions:**
1. "Tell me about your relationship with God"
2. "What was your greatest moment of faith?"
3. "How did you handle failure?"
4. "What Scripture passage means most to you?"

**Expected Behavior:**
- User types message → Taps Send
- Loading indicator appears
- AI responds in character's unique voice:
  - **David**: Poetic, musical, emphasis on worship
  - **Paul**: Theological, doctrinal, emphasis on grace
  - **Moses**: Humble, law-focused, direct encounters with God
  - **Mary**: Gentle, maternal, surrender and trust
  - **Peter**: Bold, honest about failures, restoration
  - **Esther**: Strategic, courageous, divine timing
  - **John**: Loving, mystical, intimate with Jesus

**RAG Context:**
- AI should reference Scripture in responses
- Relevant verses should inform the character's answer
- Example: Asking David about failure should reference Psalm 51

### 4. Chat Persistence & Encryption 🔐

**Test Steps:**
1. Have conversation with a character
2. Close the app completely
3. Reopen the app
4. Navigate back to the same character

**Expected Behavior:**
- Chat history is preserved
- Messages appear in correct order
- Character context maintained

**Encryption Verification:**
```powershell
# Check that chat data is encrypted
Get-Content "$env:LOCALAPPDATA\AIBibleApp\Chats\*.json"
# Should see: {"Id":"...","CharacterId":"...","Messages":[...encrypted data...]}
# Look for "ENC:" prefix in message content
```

### 5. Prayer Generation 🙏

**Test Steps:**
1. Navigate to Prayer page (from menu/tab)
2. Enter prayer topic: "guidance", "healing", "thanksgiving"
3. Tap "Generate Prayer"

**Expected Behavior:**
- Loading indicator appears
- AI generates biblically-grounded prayer
- Prayer text displays in scrollable view
- "Save Prayer" button enabled

**Save Prayer:**
- Tap "Save Prayer"
- Prayer added to "Saved Prayers" list
- Encrypted and persisted to disk

### 6. Saved Prayers Persistence 💾

**Test Steps:**
1. Generate and save multiple prayers
2. Close app completely
3. Reopen app
4. Navigate to Prayer page

**Expected Behavior:**
- All saved prayers visible in list
- Prayers sorted by date (newest first)
- Can tap to view full prayer text

**Encryption Verification:**
```powershell
# Check that prayer data is encrypted
Get-Content "$env:LOCALAPPDATA\AIBibleApp\Prayers.json"
# Should see: [{"Id":"...","Topic":"...","Content":"ENC:...","CreatedAt":"..."}]
# Look for "ENC:" prefix in content
```

## Performance Benchmarks

- **Initialization**: 3-10 seconds (depends on Ollama startup)
- **Chat Response**: 2-5 seconds per message (phi4 generation time)
- **Prayer Generation**: 3-7 seconds (longer text generation)
- **Save Operations**: <100ms (local file I/O)
- **App Launch**: 2-3 seconds (after first run)

## Known Limitations (Current Implementation)

1. **Limited Bible Data**: Only 18 verses per translation
   - RAG context is limited
   - Responses may lack specific Scripture references
   - **Solution**: Add more passages via downloader enhancement

2. **No Streaming**: Chat responses appear all at once
   - Can feel slow for long responses
   - **Solution**: Implement streaming with token-by-token display

3. **Single User**: No multi-user support
   - All data stored locally for current Windows user
   - **Solution**: Add user profiles (future enhancement)

4. **Windows Only (Encryption)**: DPAPI encryption Windows-specific
   - iOS/Android will need alternative encryption
   - **Solution**: Use platform-specific secure storage

## Success Criteria

✅ **PASS** if:
- App launches without crashes
- All 7 characters selectable
- Chat produces coherent responses in character voice
- Chat history persists between sessions
- Prayers save and restore correctly
- Data files are encrypted (ENC: prefix present)

❌ **FAIL** if:
- App crashes during startup
- Ollama connection fails with no error message
- Chat responses are out of character
- Data loss between sessions
- Unencrypted data in JSON files
- Build warnings reappear

## Debugging Tips

**If app doesn't launch:**
```powershell
# Check build output
dotnet build src\AI-Bible-App.Maui\AI-Bible-App.Maui.csproj -f net10.0-windows10.0.19041.0

# Check for deployment errors
Get-AppxPackage | Where-Object { $_.Name -like "*AIBible*" }
```

**If Ollama connection fails:**
```powershell
# Verify Ollama is running
ollama list

# Check if models are available
ollama run phi4 "test"
ollama pull nomic-embed-text
```

**If data doesn't persist:**
```powershell
# Check data directory
Get-ChildItem "$env:LOCALAPPDATA\AIBibleApp" -Recurse

# Verify file permissions
icacls "$env:LOCALAPPDATA\AIBibleApp"
```

## Next Test: Run the App! 🚀

```powershell
# From project root
dotnet build src\AI-Bible-App.Maui\AI-Bible-App.Maui.csproj -f net10.0-windows10.0.19041.0 -t:Run
```

Then follow the test scenarios above to verify all functionality.
