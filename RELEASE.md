# Release Process Guide

This document explains how to create and publish releases for DMINLauncher.

## 🤖 Automated Release (Recommended)

The project uses GitHub Actions to automatically build and publish releases when you push a version tag.

### Step-by-Step:

1. **Update version in project file:**
   ```xml
   <!-- DMINLauncher.csproj -->
   <Version>1.0.1</Version>
   <AssemblyVersion>1.0.1</AssemblyVersion>
   <FileVersion>1.0.1</FileVersion>
   <InformationalVersion>1.0.1</InformationalVersion>
   ```

2. **Update CHANGELOG.md:**
   ```markdown
   ## [1.0.1] - 2024-01-XX
   
   ### Added
   - New feature
   
   ### Fixed
   - Bug fix
   ```

3. **Commit your changes:**
   ```bash
   git add .
   git commit -m "Release v1.0.1"
   git push origin main
   ```

4. **Create and push a version tag:**
   ```bash
   git tag v1.0.1
   git push origin v1.0.1
   ```

5. **Wait for GitHub Actions:**
   - Go to: https://github.com/RobG66/DMINLauncher/actions
   - Watch the "Build and Release" workflow run
   - When complete, a new release will appear at: https://github.com/RobG66/DMINLauncher/releases

### What Gets Created:
- ✅ GitHub Release with version tag (e.g., v1.0.1)
- ✅ Windows binary: `DMINLauncher-win-x64.zip`
- ✅ Linux binary: `DMINLauncher-linux-x64.zip`
- ✅ Batocera config: `launcher.cfg.batocera-template`
- ✅ Automatic release notes from CHANGELOG

---

## 🛠️ Manual Release (Alternative)

If you prefer to build locally or GitHub Actions isn't working:

### Option 1: PowerShell Script

```powershell
# Run the build script
.\build-release.ps1 -Version "1.0.1"

# Upload files manually to GitHub
# https://github.com/RobG66/DMINLauncher/releases/new
```

### Option 2: Manual Commands

**Windows Build:**
```powershell
dotnet publish -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish/win-x64
```

**Linux Build:**
```powershell
dotnet publish -c Release -r linux-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish/linux-x64
```

**Create Release on GitHub:**
1. Go to: https://github.com/RobG66/DMINLauncher/releases/new
2. Click "Choose a tag" → Type `v1.0.1` → "Create new tag"
3. Set title: `DMINLauncher v1.0.1`
4. Copy release notes from CHANGELOG.md
5. Upload ZIP files
6. Click "Publish release"

---

## 📋 Release Checklist

Before creating a release, make sure:

- [ ] All code is committed and pushed
- [ ] Version numbers updated in `DMINLauncher.csproj`
- [ ] CHANGELOG.md updated with new version
- [ ] README.md is up to date
- [ ] Build succeeds locally: `dotnet build`
- [ ] App runs correctly on Windows
- [ ] App runs correctly on Linux (if possible)
- [ ] No sensitive data in code
- [ ] Git tag follows format: `v#.#.#` (e.g., `v1.0.1`)

---

## 🔄 Version Numbering

Follow [Semantic Versioning](https://semver.org/):

- **MAJOR** (1.x.x) - Incompatible changes
- **MINOR** (x.1.x) - New features (backward compatible)
- **PATCH** (x.x.1) - Bug fixes (backward compatible)

Examples:
- `v1.0.0` - Initial release
- `v1.0.1` - Bug fix
- `v1.1.0` - New feature added
- `v2.0.0` - Major rewrite or breaking changes

---

## 🐛 Troubleshooting

### GitHub Actions fails to build

**Check the logs:**
1. Go to: https://github.com/RobG66/DMINLauncher/actions
2. Click on the failed workflow
3. Review error messages

**Common issues:**
- Missing .NET SDK → Workflow should handle this
- Build errors → Fix code and push again
- Permission issues → Check GITHUB_TOKEN permissions

### Tag already exists

If you need to re-release a tag:
```bash
# Delete local tag
git tag -d v1.0.1

# Delete remote tag
git push origin :refs/tags/v1.0.1

# Create new tag
git tag v1.0.1
git push origin v1.0.1
```

### Release not appearing

- Check that tag starts with `v` (e.g., `v1.0.0`, not `1.0.0`)
- Verify GitHub Actions workflow completed successfully
- Check repository permissions allow workflow to create releases

---

## 📦 What's Included in Releases

Each release contains:

### Windows Package (`DMINLauncher-win-x64.zip`)
- `DMINLauncher-win-x64.exe` - Self-contained executable
- Includes .NET 9 runtime (no installation required)
- Supports Windows 10/11 (x64)

### Linux Package (`DMINLauncher-linux-x64.zip`)
- `DMINLauncher-linux-x64` - Self-contained executable
- Includes .NET 9 runtime (no installation required)
- Supports Ubuntu 20.04+, Debian 11+, Fedora 36+, Batocera 35+

### Additional Files
- `launcher.cfg.batocera-template` - Default config for Batocera
- Release notes from CHANGELOG.md

---

## 🚀 First Release Setup

For the initial v1.0.0 release:

1. **Ensure all files are committed:**
   ```bash
   git status
   git add .
   git commit -m "Prepare for v1.0.0 release"
   git push origin main
   ```

2. **Create the first tag:**
   ```bash
   git tag v1.0.0 -m "DMINLauncher v1.0.0 - Initial Release"
   git push origin v1.0.0
   ```

3. **Monitor GitHub Actions:**
   - Go to: https://github.com/RobG66/DMINLauncher/actions
   - Watch the build complete
   - Release will appear at: https://github.com/RobG66/DMINLauncher/releases

4. **Announce:**
   - Share on Doomworld forums
   - Post on Reddit r/Doom
   - Update social media

---

## 📞 Support

If you have issues with the release process:
- Check GitHub Actions logs
- Review this guide
- Open an issue: https://github.com/RobG66/DMINLauncher/issues
