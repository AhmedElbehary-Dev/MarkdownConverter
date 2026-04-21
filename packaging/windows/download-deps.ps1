# download-deps.ps1
# Downloads wkhtmltopdf installer + extracts libwkhtmltox.dll at BUILD TIME.
# This script runs on the developer/CI machine, NOT on the end-user machine.
$ErrorActionPreference = "Stop"

$depsDir = Join-Path $PSScriptRoot "deps"
if (-not (Test-Path $depsDir)) {
    New-Item -ItemType Directory -Path $depsDir | Out-Null
}

$installerUrl = "https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6-1/wkhtmltox-0.12.6-1.msvc2015-win64.exe"
$installerPath = Join-Path $depsDir "wkhtmltox-installer.exe"
$dllPath = Join-Path $depsDir "libwkhtmltox.dll"

# Skip if both files already exist and DLL is not empty
if ((Test-Path $installerPath) -and (Test-Path $dllPath) -and ((Get-Item $dllPath).Length -gt 0)) {
    Write-Host "Dependencies already downloaded." -ForegroundColor Green
    exit 0
}

# --- Step 1: Download the wkhtmltopdf installer ---
if (-not (Test-Path $installerPath) -or (Get-Item $installerPath).Length -lt 1000) {
    Write-Host "Downloading wkhtmltopdf installer..."
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    try {
        Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath -UseBasicParsing
    } catch {
        Write-Host "Invoke-WebRequest failed, trying curl.exe..." -ForegroundColor Yellow
        & curl.exe -L -o $installerPath $installerUrl
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to download wkhtmltopdf installer."
            exit 1
        }
    }
}

if (-not (Test-Path $installerPath)) {
    Write-Error "wkhtmltopdf installer not found after download."
    exit 1
}
Write-Host "  Installer size: $([math]::Round((Get-Item $installerPath).Length / 1MB, 1)) MB"

# --- Step 2: Extract libwkhtmltox.dll ---
# The wkhtmltopdf .exe is an Inno Setup installer. We use innounp to extract
# files without running the installer. innounp is a small (~200KB) portable tool.
Write-Host "Extracting libwkhtmltox.dll..."
$extracted = $false

# Strategy 1: Try innounp (Inno Setup Unpacker) - download if needed
$innounpUrl = "https://github.com/WhatTheBlock/innounp/releases/download/v0.50-unicode/innounp050u.rar"
$innounpPath = Join-Path $depsDir "innounp.exe"

# Strategy 2: Try 7z (can also extract Inno Setup installers)
$7zPaths = @(
    (Get-Command "7z" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    "C:\Program Files\7-Zip\7z.exe",
    "C:\Program Files (x86)\7-Zip\7z.exe"
)

foreach ($7zPath in $7zPaths) {
    if ($7zPath -and (Test-Path $7zPath)) {
        Write-Host "  Using 7-Zip: $7zPath"
        $tempExtract = Join-Path $depsDir "_extract"
        if (Test-Path $tempExtract) { Remove-Item $tempExtract -Recurse -Force }
        
        # 7z can extract Inno Setup installers in two passes
        & $7zPath x $installerPath -o"$tempExtract" -y 2>&1 | Out-Null
        
        # Look for the DLL in extracted contents
        $foundDll = Get-ChildItem -Path $tempExtract -Filter "libwkhtmltox.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($foundDll) {
            Copy-Item $foundDll.FullName -Destination $dllPath -Force
            $extracted = $true
            Write-Host "  Extracted via 7-Zip"
        } else {
            # Try second-pass extraction (Inno Setup has nested archives)
            $innerFiles = Get-ChildItem -Path $tempExtract -Filter "*.bin" -Recurse -ErrorAction SilentlyContinue
            foreach ($inner in $innerFiles) {
                & $7zPath x $inner.FullName -o"$tempExtract\inner" -y 2>&1 | Out-Null
            }
            $foundDll = Get-ChildItem -Path $tempExtract -Filter "libwkhtmltox.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($foundDll) {
                Copy-Item $foundDll.FullName -Destination $dllPath -Force
                $extracted = $true
                Write-Host "  Extracted via 7-Zip (nested)"
            }
        }
        
        Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
        if ($extracted) { break }
    }
}

# Strategy 3: Run the installer silently to a temp dir (needs elevation)
if (-not $extracted) {
    Write-Host "  No 7-Zip found. Running installer to temp directory (needs admin)..." -ForegroundColor Yellow
    $tempInstallDir = Join-Path $depsDir "temp_install"
    if (Test-Path $tempInstallDir) {
        Remove-Item $tempInstallDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    try {
        $proc = Start-Process -FilePath $installerPath `
            -ArgumentList "/VERYSILENT", "/NORESTART", "/SUPPRESSMSGBOXES", "/DIR=$tempInstallDir" `
            -PassThru -Wait
        
        if (Test-Path "$tempInstallDir\bin\libwkhtmltox.dll") {
            Copy-Item "$tempInstallDir\bin\libwkhtmltox.dll" -Destination $dllPath -Force
            $extracted = $true
            Write-Host "  Extracted via temp install"
        }
    } catch {
        Write-Host "  Temp install failed: $_" -ForegroundColor Red
    }

    # Clean up temp install
    if (Test-Path $tempInstallDir) {
        $uninstaller = Join-Path $tempInstallDir "unins000.exe"
        if (Test-Path $uninstaller) {
            Start-Process -FilePath $uninstaller -ArgumentList "/VERYSILENT" -Wait -ErrorAction SilentlyContinue
        }
        Remove-Item $tempInstallDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# --- Verify ---
if (-not (Test-Path $dllPath) -or (Get-Item $dllPath).Length -eq 0) {
    Write-Error @"
Failed to extract libwkhtmltox.dll.
Please install 7-Zip (https://www.7-zip.org/) and re-run, or run this script as Administrator.
"@
    exit 1
}

Write-Host "  DLL size: $([math]::Round((Get-Item $dllPath).Length / 1MB, 1)) MB"
Write-Host "Dependencies downloaded and extracted successfully." -ForegroundColor Green
exit 0
