# Security Policy

## 🔒 Our Security Commitment

The AI Bible App is designed with privacy as a core principle. We prioritize local-first AI processing to keep your spiritual conversations private.

## 🛡️ Security Features

### Local AI Processing
- **Default**: All AI processing happens on your device using Phi-4/Ollama
- **No data sent** to external servers in local mode
- Your prayers, conversations, and reflections stay private

### Optional Cloud Features
When cloud features are enabled (opt-in only):
- Azure OpenAI: Enterprise-grade encryption and compliance
- Data transmission uses TLS 1.3 encryption
- No conversation content is stored on cloud servers (stateless API calls)

### Data Storage
- **Local database**: SQLite with device-level encryption
- **No telemetry**: Usage metrics are anonymized and stored locally
- **Export/Delete**: Full control over your data

## 🔐 Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | ✅ Security updates |
| < 1.0   | ❌ Beta/Preview     |

## 📝 Reporting a Vulnerability

We take security seriously. If you discover a vulnerability:

### How to Report
1. **DO NOT** open a public issue
2. Email security concerns to the maintainer
3. Or use GitHub's private vulnerability reporting

### What to Include
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

### Response Timeline
- **Acknowledgment**: Within 48 hours
- **Initial assessment**: Within 7 days
- **Fix timeline**: Based on severity
  - Critical: 24-48 hours
  - High: 7 days
  - Medium: 30 days
  - Low: Next release

### What to Expect
- We'll keep you informed of progress
- Credit in release notes (if desired)
- No legal action for good-faith reporting

## 🌐 Privacy Considerations

### Data We DON'T Collect
- ❌ Personal identification
- ❌ Location data
- ❌ Contact information
- ❌ Conversation transcripts (cloud mode)
- ❌ Device identifiers

### Data Stored Locally
- ✅ Chat history (on device only)
- ✅ Prayer history (on device only)
- ✅ User preferences
- ✅ Anonymized usage metrics (opt-in)

### Third-Party Services
When using cloud AI (opt-in):
- **Azure OpenAI**: [Microsoft Privacy Statement](https://privacy.microsoft.com/)
- **Groq**: [Groq Privacy Policy](https://groq.com/privacy-policy/)

API calls are stateless - no conversation history is retained by providers.

## ⚖️ Compliance

### GDPR (EU Users)
- **Right to Access**: Export all your data
- **Right to Erasure**: Delete all local data
- **Data Minimization**: We only store what's necessary
- **No profiling**: No automated decision-making

### CCPA (California Users)
- **Right to Know**: See what data is stored
- **Right to Delete**: Clear all data
- **No Sale**: We never sell user data

### Religious Data Protection
Given the spiritual nature of this app:
- Religious conversations are treated as sensitive data
- Extra care in handling prayer and reflection content
- No sharing with third parties

## 🔧 Security Best Practices for Users

1. **Keep the app updated** for latest security patches
2. **Use device encryption** for additional protection
3. **Review cloud opt-ins** before enabling
4. **Regular backups** of important data
5. **Report suspicious behavior** promptly

## 🏗️ Security Architecture

```
┌─────────────────────────────────────────────────────┐
│                    User Device                       │
├─────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────────────────────┐ │
│  │   MAUI UI   │───▶│   Local AI (Phi-4/Ollama)   │ │
│  └─────────────┘    └─────────────────────────────┘ │
│         │                                           │
│         ▼                                           │
│  ┌─────────────────────────────────────────────────┐│
│  │        SQLite (Encrypted Local Storage)         ││
│  │   • Chat History  • Prayers  • Preferences      ││
│  └─────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────┘
                         │
                    (Opt-in Only)
                         │
                         ▼
         ┌───────────────────────────────┐
         │    Cloud AI (Azure OpenAI)    │
         │   • TLS 1.3 Encryption        │
         │   • Stateless API calls       │
         │   • No data retention         │
         └───────────────────────────────┘
```

## 📞 Contact

For security concerns:
- **GitHub**: Private vulnerability reporting
- **Response**: Within 48 hours

---

*Last updated: January 2026*
