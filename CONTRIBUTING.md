# Contributing to AI Bible App

Thank you for your interest in contributing to the AI Bible App! This project aims to help people engage with Scripture through AI-powered conversations with biblical characters.

## 🌟 Ways to Contribute

### 1. Report Bugs
- Use the [GitHub Issues](../../issues) page
- Include steps to reproduce the bug
- Describe expected vs actual behavior
- Include screenshots if applicable
- Note your OS and app version

### 2. Suggest Features
- Open an issue with the `enhancement` label
- Describe the feature and its benefits
- Explain how it aligns with the app's mission

### 3. Submit Code
- Bug fixes
- New biblical characters
- UI improvements
- Performance optimizations
- Documentation updates

### 4. Improve Documentation
- Fix typos or unclear instructions
- Add examples or screenshots
- Translate to other languages

## 🔧 Development Setup

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- MAUI workloads installed
- Git

### Getting Started

1. **Fork the repository**
   ```bash
   # Clone your fork
   git clone https://github.com/YOUR_USERNAME/bible-playground.git
   cd bible-playground
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feat/your-feature-name
   # Or for bugs:
   git checkout -b fix/bug-description
   ```

3. **Install dependencies**
   ```bash
   dotnet restore
   ```

4. **Build and run tests**
   ```bash
   dotnet build
   dotnet test
   ```

5. **Run the app**
   ```bash
   cd src/AI-Bible-App.Maui
   dotnet build -f net9.0-windows10.0.19041.0
   ```

## 📁 Project Structure

```
bible-playground/
├── src/
│   ├── AI-Bible-App.Core/           # Domain models, interfaces
│   ├── AI-Bible-App.Infrastructure/ # Services, repositories
│   └── AI-Bible-App.Maui/           # MAUI UI application
├── tests/
│   └── AI-Bible-App.Tests/          # Unit and integration tests
├── bible/                           # Bible HTML files
└── Data/                            # Embeddings and data files
```

## 📝 Coding Guidelines

### Code Style
- Use C# 12 features appropriately
- Follow Microsoft's [C# coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation for public APIs

### Commit Messages
Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add new biblical character Elijah
fix: resolve prayer generation timeout
docs: update installation instructions
test: add RAG service integration tests
refactor: simplify chat message handling
```

### Testing
- Write unit tests for new features
- Ensure all existing tests pass
- Aim for 70%+ code coverage
- Test on multiple platforms if possible

## 🔀 Pull Request Process

1. **Before submitting:**
   - Ensure all tests pass: `dotnet test`
   - Update documentation if needed
   - Add yourself to contributors (optional)

2. **PR Description:**
   - Describe what changes you made
   - Link related issues
   - Include screenshots for UI changes
   - Note any breaking changes

3. **Review process:**
   - PRs require at least one review
   - Address feedback promptly
   - Keep discussions respectful

4. **After merging:**
   - Delete your feature branch
   - Celebrate! 🎉

## 🧑‍🤝‍🧑 Adding New Biblical Characters

Want to add a new character? Here's how:

1. **Add to `InMemoryCharacterRepository.cs`:**
   ```csharp
   new BiblicalCharacter
   {
       Id = "elijah",
       Name = "Elijah the Prophet",
       Era = "9th century BC",
       Testament = "Old",
       PrimaryBooks = new[] { "1 Kings", "2 Kings" },
       Description = "Fiery prophet who challenged Baal worship...",
       SystemPrompt = "You are Elijah, the prophet of God...",
       // ... other properties
   }
   ```

2. **Add character image** (optional):
   - Place in `src/AI-Bible-App.Maui/Resources/Images/`
   - Use format: `character_{id}.png`

3. **Write tests:**
   - Verify character loads correctly
   - Test conversation flow

4. **Update documentation:**
   - Add to character list in README

## 🙏 Code of Conduct

### Our Pledge
We pledge to make participation in this project a harassment-free experience for everyone, regardless of background, identity, or belief.

### Our Standards
- **Be respectful** of differing viewpoints
- **Be constructive** in feedback
- **Be gracious** with mistakes
- **Be inclusive** and welcoming
- **Focus on the code**, not the person

### Unacceptable Behavior
- Harassment or discriminatory comments
- Personal attacks or trolling
- Publishing others' private information
- Inappropriate or unprofessional conduct

### Enforcement
Violations may result in:
1. Warning
2. Temporary ban
3. Permanent ban

Report issues to the maintainers.

## ❓ Questions?

- Open a [Discussion](../../discussions)
- Check existing [Issues](../../issues)
- Review the [Documentation](./README.md)

## 📜 License

By contributing, you agree that your contributions will be licensed under the project's license.

---

**Thank you for helping make biblical wisdom more accessible through technology! 🙏**
