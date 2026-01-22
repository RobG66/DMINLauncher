# Manual Release Build Script for DMINLauncher
# Run this if you want to build releases locally without GitHub Actions

param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DMINLauncher Release Builder v$Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "[1/6] Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "publish") {
    Remove-Item -Recurse -Force "publish"
}
New-Item -ItemType Directory -Path "publish" | Out-Null

# Build Windows x64
Write-Host ""
Write-Host "[2/6] Building Windows x64..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:Version=$Version `
    -o publish/win-x64

if ($LASTEXITCODE -ne 0) {
    Write-Host "Windows build failed!" -ForegroundColor Red
    exit 1
}

# Build Linux x64
Write-Host ""
Write-Host "[3/6] Building Linux x64..." -ForegroundColor Yellow
dotnet publish -c Release -r linux-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:Version=$Version `
    -o publish/linux-x64

if ($LASTEXITCODE -ne 0) {
    Write-Host "Linux build failed!" -ForegroundColor Red
    exit 1
}

# Rename executables
Write-Host ""
Write-Host "[4/6] Renaming executables..." -ForegroundColor Yellow
Move-Item "publish/win-x64/DMINLauncher.exe" "publish/win-x64/DMINLauncher-win-x64.exe" -Force
Move-Item "publish/linux-x64/DMINLauncher" "publish/linux-x64/DMINLauncher-linux-x64" -Force

# Copy additional files
Write-Host ""
Write-Host "[5/6] Copying additional files..." -ForegroundColor Yellow
Copy-Item "launcher.cfg.batocera-template" "publish/" -Force
Copy-Item "README.md" "publish/" -Force
Copy-Item "CHANGELOG.md" "publish/" -Force
Copy-Item "BATOCERA-SETUP.md" "publish/" -Force
Copy-Item "linux-diagnostic.sh" "publish/" -Force

# Create ZIP archives
Write-Host ""
Write-Host "[6/6] Creating ZIP archives..." -ForegroundColor Yellow
Compress-Archive -Path "publish/win-x64/DMINLauncher-win-x64.exe" `
    -DestinationPath "publish/DMINLauncher-win-x64-v$Version.zip" -Force

Compress-Archive -Path "publish/linux-x64/DMINLauncher-linux-x64" `
    -DestinationPath "publish/DMINLauncher-linux-x64-v$Version.zip" -Force

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "✓ Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Release files created in: publish/" -ForegroundColor Cyan
Write-Host ""
Write-Host "Windows:" -ForegroundColor White
Write-Host "  - DMINLauncher-win-x64-v$Version.zip" -ForegroundColor Gray
Write-Host "  - Size: $([math]::Round((Get-Item 'publish/DMINLauncher-win-x64-v*.zip').Length / 1MB, 2)) MB" -ForegroundColor Gray
Write-Host ""
Write-Host "Linux:" -ForegroundColor White
Write-Host "  - DMINLauncher-linux-x64-v$Version.zip" -ForegroundColor Gray
Write-Host "  - Size: $([math]::Round((Get-Item 'publish/DMINLauncher-linux-x64-v*.zip').Length / 1MB, 2)) MB" -ForegroundColor Gray
Write-Host ""
Write-Host "Additional Files:" -ForegroundColor White
Write-Host "  - launcher.cfg.batocera-template" -ForegroundColor Gray
Write-Host "  - README.md" -ForegroundColor Gray
Write-Host "  - CHANGELOG.md" -ForegroundColor Gray
Write-Host "  - BATOCERA-SETUP.md" -ForegroundColor Gray
Write-Host "  - linux-diagnostic.sh" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Test the executables" -ForegroundColor White
Write-Host "2. Create a git tag: git tag v$Version" -ForegroundColor White
Write-Host "3. Push tag: git push origin v$Version" -ForegroundColor White
Write-Host "4. GitHub Actions will automatically create the release" -ForegroundColor White
Write-Host ""
Write-Host "Or upload manually to:" -ForegroundColor Yellow
Write-Host "https://github.com/RobG66/DMINLauncher/releases/new" -ForegroundColor Cyan
Write-Host ""
