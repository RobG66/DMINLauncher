#!/bin/bash
echo "=========================================="
echo "DMINLauncher Diagnostic Test"
echo "=========================================="
echo ""

# Test 1: Check if script is executable
echo "[1] Script is running..."
echo "    ✓ Bash works"

# Test 2: Check .NET installation
echo ""
echo "[2] Checking .NET installation..."
if command -v dotnet &> /dev/null; then
    echo "    ✓ dotnet command found"
    dotnet --version
else
    echo "    ✗ dotnet command NOT found"
    echo "    You need to install .NET 9 runtime"
fi

# Test 3: Check if DMINLauncher executable exists
echo ""
echo "[3] Checking DMINLauncher files..."
if [ -f "DMINLauncher" ]; then
    echo "    ✓ DMINLauncher executable found"
    ls -lh DMINLauncher
    
    # Check if executable
    if [ -x "DMINLauncher" ]; then
        echo "    ✓ DMINLauncher is executable"
    else
        echo "    ✗ DMINLauncher is NOT executable"
        echo "    Run: chmod +x DMINLauncher"
    fi
else
    echo "    ✗ DMINLauncher executable NOT found"
fi

# Test 4: Check for required libraries
echo ""
echo "[4] Checking system libraries..."
if [ -f "DMINLauncher.dll" ]; then
    echo "    ✓ DMINLauncher.dll found"
else
    echo "    ✗ DMINLauncher.dll NOT found"
fi

# Test 5: Check display
echo ""
echo "[5] Checking display..."
if [ -n "$DISPLAY" ]; then
    echo "    ✓ DISPLAY is set: $DISPLAY"
else
    echo "    ✗ DISPLAY is NOT set"
    echo "    Run: export DISPLAY=:0"
fi

# Test 6: Check write permissions
echo ""
echo "[6] Checking write permissions..."
TEST_FILE="test_write_$$.txt"
if touch "$TEST_FILE" 2>/dev/null; then
    echo "    ✓ Can write to current directory"
    rm "$TEST_FILE"
else
    echo "    ✗ Cannot write to current directory"
fi

if touch "/tmp/test_write_$$.txt" 2>/dev/null; then
    echo "    ✓ Can write to /tmp"
    rm "/tmp/test_write_$$.txt"
else
    echo "    ✗ Cannot write to /tmp"
fi

# Test 7: Try to run DMINLauncher
echo ""
echo "[7] Attempting to run DMINLauncher..."
echo "    (This will try to start the app)"
echo ""

if [ -f "DMINLauncher" ] && [ -x "DMINLauncher" ]; then
    echo "Starting DMINLauncher..."
    ./DMINLauncher 2>&1 | head -20 &
    APP_PID=$!
    sleep 2
    
    if ps -p $APP_PID > /dev/null 2>&1; then
        echo "    ✓ DMINLauncher started (PID: $APP_PID)"
        echo "    Killing test instance..."
        kill $APP_PID 2>/dev/null
    else
        echo "    ✗ DMINLauncher exited immediately"
    fi
fi

# Test 8: Check for log files
echo ""
echo "[8] Checking for log files..."
LOG_LOCATIONS=(
    "./dminlauncher_startup.log"
    "/tmp/dminlauncher_startup.log"
    "$(dirname $(readlink -f DMINLauncher 2>/dev/null || echo .))/dminlauncher_startup.log"
)

FOUND_LOG=0
for log in "${LOG_LOCATIONS[@]}"; do
    if [ -f "$log" ]; then
        echo "    ✓ Found log: $log"
        echo "    Last 10 lines:"
        tail -10 "$log" | sed 's/^/      /'
        FOUND_LOG=1
    fi
done

if [ $FOUND_LOG -eq 0 ]; then
    echo "    ✗ No log files found in:"
    printf "      %s\n" "${LOG_LOCATIONS[@]}"
fi

echo ""
echo "=========================================="
echo "Diagnostic Complete"
echo "=========================================="
