#!/bin/bash
# DMINLauncher - Linux Diagnostic Script
# Run this if you're having issues launching on Linux/Batocera

echo "=========================================="
echo "DMINLauncher Diagnostic Tool"
echo "=========================================="
echo ""

# Test 1: Script execution
echo "[✓] Script is running (Bash works)"

# Test 2: Check .NET installation
echo ""
echo "[2] Checking .NET installation..."
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "    ✓ .NET found: v$DOTNET_VERSION"
    
    # Check if .NET 9 or higher
    MAJOR_VERSION=$(echo $DOTNET_VERSION | cut -d. -f1)
    if [ "$MAJOR_VERSION" -ge 9 ]; then
        echo "    ✓ .NET version is compatible"
    else
        echo "    ⚠ .NET 9+ recommended, you have: $DOTNET_VERSION"
    fi
else
    echo "    ✗ .NET NOT found"
    echo "    Install: https://dotnet.microsoft.com/download/dotnet/9.0"
fi

# Test 3: Check executable
echo ""
echo "[3] Checking DMINLauncher executable..."
EXECUTABLE=""
if [ -f "DMINLauncher" ]; then
    EXECUTABLE="DMINLauncher"
elif [ -f "DMINLauncher-linux-x64" ]; then
    EXECUTABLE="DMINLauncher-linux-x64"
fi

if [ -n "$EXECUTABLE" ]; then
    echo "    ✓ Found: $EXECUTABLE"
    ls -lh "$EXECUTABLE"
    
    if [ -x "$EXECUTABLE" ]; then
        echo "    ✓ Executable permissions set"
    else
        echo "    ✗ NOT executable - run: chmod +x $EXECUTABLE"
    fi
else
    echo "    ✗ DMINLauncher executable NOT found"
    echo "    Expected: DMINLauncher or DMINLauncher-linux-x64"
fi

# Test 4: Check display server
echo ""
echo "[4] Checking display server..."
if [ -n "$DISPLAY" ]; then
    echo "    ✓ DISPLAY set: $DISPLAY"
else
    echo "    ✗ DISPLAY not set"
    echo "    Run: export DISPLAY=:0"
fi

if command -v xdpyinfo &> /dev/null; then
    if xdpyinfo &> /dev/null; then
        echo "    ✓ X11 server is running"
    else
        echo "    ✗ X11 server not responding"
    fi
fi

# Test 5: Check required libraries
echo ""
echo "[5] Checking system libraries..."
REQUIRED_LIBS=("libX11.so.6" "libICE.so.6" "libSM.so.6")
MISSING_LIBS=()

for LIB in "${REQUIRED_LIBS[@]}"; do
    if ldconfig -p | grep -q "$LIB"; then
        echo "    ✓ $LIB found"
    else
        echo "    ✗ $LIB NOT found"
        MISSING_LIBS+=("$LIB")
    fi
done

if [ ${#MISSING_LIBS[@]} -gt 0 ]; then
    echo ""
    echo "    Install missing libraries:"
    echo "    Ubuntu/Debian: sudo apt install libx11-6 libice6 libsm6"
    echo "    Fedora: sudo dnf install libX11 libICE libSM"
fi

# Test 6: Check configuration
echo ""
echo "[6] Checking configuration..."
if [ -f "launcher.cfg" ]; then
    echo "    ✓ launcher.cfg found"
    
    # Check if paths are set
    if grep -q "^wads=" launcher.cfg; then
        WADS_PATH=$(grep "^wads=" launcher.cfg | cut -d= -f2)
        echo "    ✓ WADs path: $WADS_PATH"
        if [ -d "$WADS_PATH" ]; then
            WAD_COUNT=$(find "$WADS_PATH" -maxdepth 1 -iname "*.wad" | wc -l)
            echo "      Found $WAD_COUNT WAD files"
        else
            echo "      ⚠ Directory does not exist"
        fi
    fi
    
    if grep -q "^engine=" launcher.cfg; then
        ENGINE_PATH=$(grep "^engine=" launcher.cfg | cut -d= -f2)
        echo "    ✓ Engine path: $ENGINE_PATH"
        if [ -d "$ENGINE_PATH" ]; then
            ENGINE_COUNT=$(find "$ENGINE_PATH" -maxdepth 1 -type f -executable | wc -l)
            echo "      Found $ENGINE_COUNT executables"
        else
            echo "      ⚠ Directory does not exist"
        fi
    fi
else
    echo "    ⚠ launcher.cfg not found (will be created on first run)"
fi

# Test 7: Batocera specific checks
echo ""
echo "[7] Batocera detection..."
if [ -f "/usr/bin/batocera-info" ]; then
    echo "    ✓ Running on Batocera"
    
    if [ -d "/userdata/roms/doom" ]; then
        echo "    ✓ Doom folder exists"
        WAD_COUNT=$(find /userdata/roms/doom -maxdepth 1 -iname "*.wad" 2>/dev/null | wc -l)
        echo "      Found $WAD_COUNT WAD files"
    else
        echo "    ⚠ /userdata/roms/doom not found"
    fi
    
    if [ -f "/usr/bin/gzdoom" ]; then
        echo "    ✓ GZDoom installed (system)"
    else
        echo "    ⚠ GZDoom not found at /usr/bin/gzdoom"
    fi
else
    echo "    ℹ Not running on Batocera (generic Linux)"
fi

# Summary
echo ""
echo "=========================================="
echo "Summary"
echo "=========================================="

if [ -n "$EXECUTABLE" ] && [ -x "$EXECUTABLE" ] && command -v dotnet &> /dev/null; then
    echo "✓ Basic requirements met"
    echo ""
    echo "Try running:"
    echo "  ./$EXECUTABLE"
    echo ""
    echo "If GUI doesn't appear, check display server and libraries above."
else
    echo "✗ Some requirements missing - see details above"
fi

echo ""
echo "For more help, see:"
echo "  - README.md"
echo "  - BATOCERA-SETUP.md"
echo "  - https://github.com/RobG66/DMINLauncher"
echo ""
