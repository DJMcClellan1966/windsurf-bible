# Git Branching Strategy

This document outlines our branching strategy for the AI Bible App playground project.

## 🌳 Branch Structure

```
master (stable)
├── develop (integration)
│   ├── feat/new-character-elijah
│   ├── feat/cloud-sync
│   ├── feat/voice-input
│   └── fix/prayer-timeout
└── release/v1.0.0
```

## 📌 Main Branches

### `master` (Protected)
- **Purpose**: Production-ready code
- **Protection**: Requires PR review, all tests must pass
- **Deploys**: Triggers release builds
- **Never**: Push directly

### `develop`
- **Purpose**: Integration branch for features
- **Merges from**: Feature and fix branches
- **Merges to**: `master` via release branches
- **Tests**: Must pass before merge

## 🚀 Supporting Branches

### Feature Branches
```bash
# Naming: feat/<description>
git checkout -b feat/new-character-elijah develop

# Examples:
feat/cloud-sync-firebase
feat/voice-prayer-input
feat/dark-theme
feat/bible-reading-plans
```

**Lifecycle:**
1. Branch from `develop`
2. Implement feature with tests
3. Open PR to `develop`
4. Code review and CI checks
5. Squash merge when approved
6. Delete branch

### Bug Fix Branches
```bash
# Naming: fix/<description>
git checkout -b fix/prayer-generation-timeout develop

# Examples:
fix/character-loading-crash
fix/scripture-search-accuracy
fix/offline-mode-sync
```

**Lifecycle:**
1. Branch from `develop` (or `master` for hotfixes)
2. Fix bug with regression test
3. Open PR
4. Merge after approval

### Hotfix Branches
```bash
# Naming: hotfix/<version>-<description>
git checkout -b hotfix/1.0.1-crash-fix master

# For critical production bugs only
```

**Lifecycle:**
1. Branch from `master`
2. Fix critical issue
3. Merge to BOTH `master` AND `develop`
4. Tag new patch version

### Release Branches
```bash
# Naming: release/v<version>
git checkout -b release/v1.0.0 develop
```

**Lifecycle:**
1. Branch from `develop` when ready
2. Only bug fixes, no new features
3. Update version numbers, changelog
4. Merge to `master` and tag
5. Merge back to `develop`

### Experiment Branches
```bash
# Naming: experiment/<description>
git checkout -b experiment/llama3-integration

# For playground experiments that may not be merged
# Examples:
experiment/vr-bible-experience
experiment/multiplayer-devotionals
experiment/ai-art-generation
```

## 🏷️ Version Tags

Follow [Semantic Versioning](https://semver.org/):

```
v1.0.0    # Major release
v1.1.0    # New features (backward compatible)
v1.1.1    # Bug fixes
v1.2.0-beta.1  # Pre-release
v1.2.0-alpha.1 # Early testing
```

### Creating a Release
```bash
# 1. Create release branch
git checkout -b release/v1.1.0 develop

# 2. Update version in .csproj files
# 3. Update CHANGELOG.md

# 4. Merge to master
git checkout master
git merge release/v1.1.0

# 5. Tag the release
git tag -a v1.1.0 -m "Release v1.1.0: Cloud sync feature"
git push origin v1.1.0

# 6. Merge back to develop
git checkout develop
git merge release/v1.1.0
```

## 📋 Commit Message Format

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types
| Type | Description |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation |
| `style` | Formatting (no code change) |
| `refactor` | Code restructure |
| `test` | Add/update tests |
| `chore` | Maintenance |
| `perf` | Performance improvement |

### Examples
```bash
feat(characters): add Elijah the prophet

fix(prayer): resolve timeout on long prayers
Closes #123

docs(readme): add installation video

refactor(chat): simplify message handling
BREAKING CHANGE: ChatMessage.Content is now required
```

## 🔀 Pull Request Guidelines

### PR Title
Same format as commit messages:
```
feat(characters): add Elijah the prophet
```

### PR Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Manual testing completed
- [ ] Tested on Windows
- [ ] Tested on Android

## Screenshots (if applicable)

## Related Issues
Closes #123
```

### Review Checklist
- [ ] Code follows style guidelines
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] No merge conflicts
- [ ] CI passes

## 🛠️ Quick Reference

```bash
# Start new feature
git checkout develop
git pull
git checkout -b feat/my-feature

# Work on feature
git add .
git commit -m "feat: implement something"

# Keep up to date
git fetch origin
git rebase origin/develop

# Push and create PR
git push -u origin feat/my-feature
# Then create PR on GitHub

# After PR merged, cleanup
git checkout develop
git pull
git branch -d feat/my-feature
```

## 🚨 Branch Protection Rules

### `master`
- Require pull request before merging
- Require at least 1 approval
- Require status checks (CI) to pass
- Require branches to be up to date
- Include administrators

### `develop`
- Require status checks to pass
- Allow squash merging only

## 📊 Branch Diagram

```
Time ──────────────────────────────────────────────────▶

master     ●─────────────────●───────────●────────────●
            \               ↗           ↗            ↗
             \             /           /            /
release       \    ●──────●     ●─────●      ●─────●
               \  ↗            ↗            ↗
                \/            /            /
develop    ●────●────●──●────●────●──●────●────●──●
                 \    ↗       \    ↗       \    ↗
                  \  /         \  /         \  /
features           ●            ●            ●
                 feat/a       feat/b       feat/c
```

---

*This strategy keeps `master` stable while allowing experimentation in the playground!*
