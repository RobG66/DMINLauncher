# Build script for DMINLauncher - creates self-contained builds for Windows and Linux

Write-Host "Building DMINLauncher for Windows and Linux..." -ForegroundColor Cyan
Write-Host ""

# Detect target framework from .csproj
$csprojContent = Get-Content "DMINLauncher.csproj" -Raw
if ($csprojContent -match '<TargetFramework>(net\d+\.\d+)</TargetFramework>') {
    $targetFramework = $matches[1]
    Write-Host "Detected target framework: $targetFramework" -ForegroundColor Cyan
} else {
    $targetFramework = "net9.0"
    Write-Host "Could not detect framework, defaulting to: $targetFramework" -ForegroundColor Yellow
}

# Define publish directories once (avoids fragile strings)
$winPublishDir   = "bin\Release\$targetFramework\win-x64\publish"
$linuxPublishDir = "bin\Release\$targetFramework\linux-x64\publish"

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
Remove-Item -Path "bin\Release" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "releases" -Recurse -Force -ErrorAction SilentlyContinue

# Create releases structure
New-Item -ItemType Directory -Path "releases" -Force | Out-Null

# Common publish arguments
$publishArgs = @(
    "-c", "Release",
    "--self-contained",
    "-p:PublishSingleFile=true",
    "-p:PublishReadyToRun=true"
)

# =========================
# WINDOWS BUILD
# =========================
Write-Host ""
Write-Host "Building for Windows x64..." -ForegroundColor Yellow

dotnet publish @publishArgs -r win-x64
$winExit = $LASTEXITCODE   # <-- capture immediately

if ($winExit -eq 0) {
    $winExe = Join-Path $winPublishDir "DMINLauncher.exe"

    if (Test-Path $winExe) {
        Copy-Item $winExe -Destination "releases\DMINLauncher-win-x64.exe" -Force
        Write-Host "Windows x64 build complete: releases\DMINLauncher-win-x64.exe" -ForegroundColor Green
    } else {
        Write-Host "Windows executable not found at: $winExe" -ForegroundColor Red
        if (Test-Path $winPublishDir) {
            Write-Host "Publish folder contents:" -ForegroundColor Yellow
            Get-ChildItem $winPublishDir | Format-Table Name
        }
    }
} else {
    Write-Host "Windows x64 build failed with exit code: $winExit" -ForegroundColor Red
}

# =========================
# LINUX BUILD
# =========================
Write-Host ""
Write-Host "Restoring for linux-x64..." -ForegroundColor Cyan
dotnet restore -r linux-x64

Write-Host ""
Write-Host "Building for Linux x64..." -ForegroundColor Yellow

dotnet publish @publishArgs -r linux-x64
$linuxExit = $LASTEXITCODE   # <-- capture immediately

if ($linuxExit -eq 0) {
    $linuxExe = Join-Path $linuxPublishDir "DMINLauncher"

    if (Test-Path $linuxExe) {
        Copy-Item $linuxExe -Destination "releases\DMINLauncher-linux-x64" -Force
        Write-Host "Linux x64 build complete: releases\DMINLauncher-linux-x64" -ForegroundColor Green
    } else {
        Write-Host "Linux executable not found at: $linuxExe" -ForegroundColor Red
        if (Test-Path $linuxPublishDir) {
            Write-Host "Publish folder contents:" -ForegroundColor Yellow
            Get-ChildItem $linuxPublishDir | Format-Table Name
        }
    }
} else {
    Write-Host "Linux x64 build failed with exit code: $linuxExit" -ForegroundColor Red
}

# =========================
# SUMMARY
# =========================
Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Build complete! Executables are in .\releases" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

Get-ChildItem -Path "releases" -ErrorAction SilentlyContinue |
    Format-Table Name, Length, LastWriteTime

# SIG # Begin signature block
# MIIFhQYJKoZIhvcNAQcCoIIFdjCCBXICAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUAqeywSRxB5nIo9Tf8LNRsW4C
# kaegggMYMIIDFDCCAfygAwIBAgIQRzXHKzzqObJJXOF5UNtAujANBgkqhkiG9w0B
# AQsFADAiMSAwHgYDVQQDDBdQb3dlclNoZWxsIENvZGUgU2lnbmluZzAeFw0yNjAx
# MjExMjU2NDRaFw0yNzAxMjExMzE2NDRaMCIxIDAeBgNVBAMMF1Bvd2VyU2hlbGwg
# Q29kZSBTaWduaW5nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAtvEZ
# LPnuugXWfIFJVcOKhZsJg3mPgtcFmpOMoBh4h6NszwZg8/7pfryuFyzyf+KEZIp0
# RGrreoi5Q2LMsw4qCHoYdW3xWucASnaRCgeMCj4p2EKaOopvBSy2O9+iAO9K8uNU
# ujXdsZUDl9p3BXMij/k8GngvAe9pY1oD9S13NTi7U5EN04MmJxZ/NDlZSDsaf0W0
# rip7YRJjGqBIKhamI/19+uWpnpR/VrConORXAwDFPcymDUsvTwNj22/T8A5VrFR9
# OJFt2BL5uUJywoTQ+v/EO4aSN3Gfj0bw7e780nkD55TFpDdMmYzjBpTIcQVmowbF
# 52AuJZvzW6LduCqcmQIDAQABo0YwRDAOBgNVHQ8BAf8EBAMCB4AwEwYDVR0lBAww
# CgYIKwYBBQUHAwMwHQYDVR0OBBYEFG8Fu1N4RFDVLo3NDeWkh02ZX56aMA0GCSqG
# SIb3DQEBCwUAA4IBAQBnva1asbMYwQJCQfBefFW58vNx1LW1sPP7z72mGE/Ki/v2
# tp2aASGegopAqc4yG352RISoqRlz3lrkqo2btlP5lC53yNoEvUr3swQT+afCqvPt
# H6AAueVpyaxtmoZQ0X02c0joMKf/uX+6ZtzXtBiRe2FlUD9BHu6kVafeuAEPvUVN
# NfLocbuPVqOoFYIAAeyfNQanVis570TMLwZo657AuJccLId/BGEs1XfTgSx7ouo0
# PDdudScg2EBUi5MKAmYMcmVJiU/ooSwHoRFvmsxLRO8aUTJp0TUd/MlWuzD6DjML
# 9J+B0l8ibMF5Nw898ncP09dxlW1fToAB+KQ7yvZqMYIB1zCCAdMCAQEwNjAiMSAw
# HgYDVQQDDBdQb3dlclNoZWxsIENvZGUgU2lnbmluZwIQRzXHKzzqObJJXOF5UNtA
# ujAJBgUrDgMCGgUAoHgwGAYKKwYBBAGCNwIBDDEKMAigAoAAoQKAADAZBgkqhkiG
# 9w0BCQMxDAYKKwYBBAGCNwIBBDAcBgorBgEEAYI3AgELMQ4wDAYKKwYBBAGCNwIB
# FTAjBgkqhkiG9w0BCQQxFgQUvnSfaZo4CAXP78AKxi/1DFatUlQwDQYJKoZIhvcN
# AQEBBQAEggEAUtLGvRZmlinBFGaSnDikb5cY17GxbR4UWYaJAScnq/0E6oi0NOzs
# JgyV+p1JsCRGXrA0kstcfvs0+2+GNNnhIF8bjtWkWyjTb5etBTb0Hvo5BsCMGxoK
# Rjd0L/TERS6rHsMqvDjEpCUt40J82YFzoMZd2/Cwxisq2a1tbc8EWQ80xhcDziOm
# ln9aC4l/Uz++7OAyFa7m3CzuydsUKIfgoN9+EwuKeWCHNLF0aU8Cj0rQzcuzahtW
# eJwtiY59REuwWy7vcsoll6oWvinNIAmZ017OL15NvcEKJAX+mVLtnaSbR/NGISHX
# WOXD4PYPjLG9y0LTRYBG35aIp5wlyZfORg==
# SIG # End signature block
