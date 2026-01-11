# Mobile Deployment Guide
*iOS and Android deployment for AI Bible App*

**Last Updated:** January 08, 2026

---

## Platform Support Overview

| Platform | Status | AI Backend | Build Requirement |
|----------|--------|------------|-------------------|
| **Windows** | ✅ Ready | Local Ollama | Windows + .NET 9 |
| **macOS** | ✅ Ready | Local Ollama | Mac + .NET 9 |
| **iOS/iPad** | ✅ Ready | Groq Cloud | Mac + Xcode |
| **Android** | ✅ Ready | Groq Cloud | Android SDK |

---

## Mobile Architecture

### Smart AI Backend Selection

The app automatically selects the best AI backend based on platform:

```
┌─────────────────────────────────────────────────────────┐
│                    HybridAIService                      │
├─────────────────────────────────────────────────────────┤
│  Desktop (Windows/macOS)     │  Mobile (iOS/Android)   │
│  ─────────────────────────   │  ─────────────────────  │
│  1. Try Local Ollama         │  1. Use Groq Cloud      │
│  2. Fallback to Groq Cloud   │  2. Fallback to Ollama* │
│                              │     (*if custom server) │
└─────────────────────────────────────────────────────────┘
```

**Why?** Ollama cannot run directly on mobile devices, so cloud AI is used by default.

---

## iOS / iPad Deployment

### Requirements
- macOS computer with Xcode 15+
- Apple Developer Account ($99/year for App Store)
- .NET 9 SDK installed

### Configuration ✅

**Info.plist** (already configured):
```xml
<!-- Device support -->
<key>UIDeviceFamily</key>
<array>
    <integer>1</integer>  <!-- iPhone -->
    <integer>2</integer>  <!-- iPad -->
</array>

<!-- Microphone for speech-to-text -->
<key>NSMicrophoneUsageDescription</key>
<string>Voice input for chatting with biblical characters</string>

<!-- Speech recognition -->
<key>NSSpeechRecognitionUsageDescription</key>
<string>Speech recognition for voice-to-text input</string>

<!-- All orientations supported -->
<key>UISupportedInterfaceOrientations~ipad</key>
<array>
    <string>UIInterfaceOrientationPortrait</string>
    <string>UIInterfaceOrientationPortraitUpsideDown</string>
    <string>UIInterfaceOrientationLandscapeLeft</string>
    <string>UIInterfaceOrientationLandscapeRight</string>
</array>
```

### Build Commands

```bash
# Build for iOS Simulator
dotnet build -f net9.0-ios

# Build for physical device (requires signing)
dotnet build -f net9.0-ios -c Release

# Run on connected device/simulator
dotnet run -f net9.0-ios
```

### App Store Deployment

1. **Archive the app** in Visual Studio for Mac or Xcode
2. **Upload to App Store Connect**
3. **TestFlight** - Invite beta testers
4. **App Store Review** - Submit for public release

---

## Android Deployment

### Requirements
- Android SDK (via Android Studio or standalone)
- .NET 9 SDK
- JDK 11+

### Configuration ✅

**AndroidManifest.xml** (already configured):
```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application 
        android:allowBackup="true" 
        android:icon="@mipmap/appicon" 
        android:roundIcon="@mipmap/appicon_round" 
        android:supportsRtl="true">
    </application>
    
    <!-- Network access for Groq API -->
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    <uses-permission android:name="android.permission.INTERNET" />
    
    <!-- Microphone for speech-to-text -->
    <uses-permission android:name="android.permission.RECORD_AUDIO" />
</manifest>
```

### Build Commands

```bash
# Install Android SDK (if not installed)
winget install Google.AndroidStudio

# Build for Android
dotnet build -f net9.0-android

# Build APK for sideloading
dotnet build -f net9.0-android -c Release

# Build AAB for Play Store
dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=aab
```

### Play Store Deployment

1. **Create a keystore** for signing:
   ```bash
   keytool -genkey -v -keystore voices-of-scripture.keystore -alias release -keyalg RSA -keysize 2048 -validity 10000
   ```

2. **Sign the AAB** using the keystore

3. **Upload to Google Play Console**

4. **Internal testing** → **Closed testing** → **Production**

---

## Groq API Setup (Required for Mobile)

Since mobile devices use Groq cloud AI, you need an API key:

### 1. Get a Groq API Key

1. Visit [console.groq.com](https://console.groq.com)
2. Sign up / Log in
3. Create an API key

### 2. Configure the App

Edit `appsettings.json`:
```json
{
  "Groq": {
    "ApiKey": "YOUR_GROQ_API_KEY_HERE",
    "ModelName": "llama-3.1-8b-instant"
  }
}
```

### 3. Secure the API Key (Production)

For production builds, use secure storage:
- **iOS**: Use Keychain via `SecureStorage`
- **Android**: Use EncryptedSharedPreferences via `SecureStorage`

```csharp
// Store securely
await SecureStorage.SetAsync("groq_api_key", apiKey);

// Retrieve
var apiKey = await SecureStorage.GetAsync("groq_api_key");
```

---

## Testing on Physical Devices

### iOS Testing

1. **Connect iPad/iPhone** via USB
2. **Trust the computer** on the device
3. **Enable Developer Mode** (Settings → Privacy & Security)
4. Run: `dotnet run -f net9.0-ios`

### Android Testing

1. **Enable Developer Options** on device
2. **Enable USB Debugging**
3. Connect via USB
4. Run: `dotnet run -f net9.0-android`

---

## Cloud Build Options (Without Mac)

If you don't have a Mac for iOS builds:

| Service | iOS Support | Cost |
|---------|-------------|------|
| **GitHub Actions** | ✅ macOS runners | Free tier available |
| **Azure DevOps** | ✅ macOS agents | Free tier available |
| **App Center** | ✅ Build service | Free tier available |
| **MacStadium** | ✅ Mac VMs | ~$99/month |
| **MacinCloud** | ✅ Mac VMs | ~$20/month |

### GitHub Actions Example

```yaml
# .github/workflows/ios-build.yml
name: iOS Build

on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Install MAUI Workload
        run: dotnet workload install maui
      
      - name: Build iOS
        run: dotnet build -f net9.0-ios -c Release
```

---

## Minimum OS Versions

| Platform | Minimum Version | Reason |
|----------|-----------------|--------|
| iOS | 15.0 | .NET MAUI requirement |
| Android | 5.0 (API 21) | Modern UI components |
| Windows | 10.0.17763 | WinUI 3 support |
| macOS | 12.0 (Monterey) | .NET MAUI requirement |

---

## Known Limitations on Mobile

1. **No local AI** - Requires internet for Groq API
2. **Battery usage** - Speech recognition uses battery
3. **Data storage** - Limited compared to desktop
4. **Background limits** - iOS restricts background processing

---

## Troubleshooting

### iOS: "Provisioning profile not found"
- Ensure Apple Developer account is configured in Xcode
- Create provisioning profile at developer.apple.com

### Android: "Android SDK not found"
- Set `ANDROID_HOME` environment variable
- Or specify in build: `-p:AndroidSdkDirectory="C:\path\to\sdk"`

### Groq API: "401 Unauthorized"
- Verify API key is correct
- Check key hasn't expired
- Ensure key has proper permissions

---

## Next Steps

1. ✅ Mobile permissions configured
2. ✅ Smart AI backend selection
3. 🔲 Set up Groq API key for production
4. 🔲 Create app icons for mobile
5. 🔲 Test on physical devices
6. 🔲 Submit to App Store / Play Store
