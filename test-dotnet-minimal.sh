#!/bin/bash
# Minimal test to see if .NET can run at all

echo "Creating minimal .NET test..."

# Create a simple test C# file
cat > MinimalTest.cs << 'EOF'
using System;
using System.IO;

class MinimalTest
{
    static void Main()
    {
        var testFile = "/tmp/dotnet_test_worked.txt";
        var message = $"[{DateTime.Now}] .NET 9 is working!\n";
        message += $"OS: {Environment.OSVersion}\n";
        message += $"Runtime: {Environment.Version}\n";
        message += $"Process: {Environment.ProcessPath}\n";
        
        try
        {
            File.WriteAllText(testFile, message);
            Console.WriteLine("SUCCESS: .NET runtime works!");
            Console.WriteLine($"Test file created: {testFile}");
            Console.WriteLine(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
    }
}
EOF

# Try to compile and run it
if command -v dotnet &> /dev/null; then
    echo "Compiling minimal test..."
    dotnet MinimalTest.cs --output /tmp/
    
    if [ -f "/tmp/MinimalTest.dll" ]; then
        echo "Running minimal test..."
        dotnet /tmp/MinimalTest.dll
    fi
else
    echo "ERROR: dotnet command not found!"
    echo ""
    echo "To install .NET 9 on Batocera/Linux:"
    echo "  wget https://dot.net/v1/dotnet-install.sh"
    echo "  chmod +x dotnet-install.sh"
    echo "  ./dotnet-install.sh --channel 9.0 --runtime dotnet"
fi
