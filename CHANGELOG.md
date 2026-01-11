# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- GitHub Actions CI/CD pipeline for automated builds and releases
- CONTRIBUTING.md with contributor guidelines
- SECURITY.md with privacy and security documentation
- BRANCHING_STRATEGY.md for Git workflow
- Usage metrics service for local analytics (anonymized)
- Feedback form for in-app user feedback
- Azure OpenAI integration as cloud AI alternative
- Serilog structured logging with file/console sinks
- SQLite repositories for chat and prayer persistence
- Bible data optimizer with GZip compression
- Comprehensive test suite (84+ tests)

### Changed
- Consolidated documentation with updated README
- Improved error handling across services

## [1.0.0] - 2026-01-11

### Added
- Initial release with .NET MAUI cross-platform UI
- 18 biblical characters with unique personalities
- RAG-powered Scripture grounding (KJV + WEB translations)
- Personalized prayer generator with moods and traditions
- Bible reader with chapter navigation
- History dashboard for conversations and prayers
- Swipe gestures for character selection
- Accessibility features (VoiceOver, screen reader support)
- Windows Text-to-Speech for responses
- Offline-first architecture with local Phi-4 AI
- Dark mode support

### Technical
- Clean Architecture with Core, Infrastructure, and MAUI layers
- Microsoft Semantic Kernel for AI integration
- Vector embeddings with nomic-embed-text
- LRU caching for performance
- Lazy loading for Bible data

## [0.9.0] - 2025-12-01 (Beta)

### Added
- Console application prototype
- David and Paul characters
- Basic prayer generation
- Chat history persistence

### Changed
- Migrated from GPT-4 to local Phi-4 model

## [0.1.0] - 2025-10-15 (Alpha)

### Added
- Initial project structure
- Azure OpenAI integration
- Basic chat functionality

---

## Upgrade Notes

### From 0.x to 1.0
- Install Ollama and pull required models
- Migrate any saved data to new format
- Update appsettings.json for new configuration options

### Future Releases
- Watch this space for cloud sync features
- New characters added regularly
- Performance improvements ongoing

---

[Unreleased]: https://github.com/DJMcClellan1966/bible-playground/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/DJMcClellan1966/bible-playground/releases/tag/v1.0.0
[0.9.0]: https://github.com/DJMcClellan1966/bible-playground/releases/tag/v0.9.0
[0.1.0]: https://github.com/DJMcClellan1966/bible-playground/releases/tag/v0.1.0
