#!/bin/bash
# Build DMINLauncher for Linux

echo "=========================================="
echo "Building DMINLauncher for Linux x64"
echo "=========================================="

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf bin/Release/net9.0/publish/linux-x64/

# Publish for Linux
echo "Publishing for linux-x64..."
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Build successful!"
    echo ""
    echo "Output directory:"
    echo "  bin/Release/net9.0/publish/linux-x64/"
    echo ""
    
    # Make executable
    chmod +x bin/Release/net9.0/publish/linux-x64/DMINLauncher
    
    # Show file info
    ls -lh bin/Release/net9.0/publish/linux-x64/DMINLauncher
    
    echo ""
    echo "To test, run:"
    echo "  cd bin/Release/net9.0/publish/linux-x64/"
    echo "  ./DMINLauncher"
else
    echo ""
    echo "✗ Build failed!"
    exit 1
fi
