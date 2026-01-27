#!/bin/bash
# Quick Release Script for Linux/WSL
# Usage: ./quick-release.sh 1.0.1

VERSION=${1:-"1.0.0"}

echo "=========================================="
echo "DMINLauncher Quick Release v$VERSION"
echo "=========================================="
echo ""

# Update version in csproj
echo "[1/5] Updating version numbers..."
sed -i "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/g" DMINLauncher.csproj
sed -i "s/<AssemblyVersion>.*<\/AssemblyVersion>/<AssemblyVersion>$VERSION<\/AssemblyVersion>/g" DMINLauncher.csproj
sed -i "s/<FileVersion>.*<\/FileVersion>/<FileVersion>$VERSION<\/FileVersion>/g" DMINLauncher.csproj
sed -i "s/<InformationalVersion>.*<\/InformationalVersion>/<InformationalVersion>$VERSION<\/InformationalVersion>/g" DMINLauncher.csproj

# Commit changes
echo ""
echo "[2/5] Committing changes..."
git add .
git commit -m "Release v$VERSION"

# Push to master
echo ""
echo "[3/5] Pushing to master branch..."
git push origin master

# Create and push tag
echo ""
echo "[4/5] Creating and pushing tag v$VERSION..."
git tag "v$VERSION" -m "Release v$VERSION"
git push origin "v$VERSION"

# Done
echo ""
echo "[5/5] Done!"
echo ""
echo "=========================================="
echo "✓ Release v$VERSION initiated!"
echo "=========================================="
echo ""
echo "GitHub Actions will now:"
echo "  1. Build Windows and Linux binaries"
echo "  2. Create GitHub release"
echo "  3. Upload release assets"
echo ""
echo "Monitor progress at:"
echo "https://github.com/RobG66/DMINLauncher/actions"
echo ""
echo "Release will appear at:"
echo "https://github.com/RobG66/DMINLauncher/releases/tag/v$VERSION"
echo ""
