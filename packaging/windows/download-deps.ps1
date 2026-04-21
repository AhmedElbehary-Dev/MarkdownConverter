# download-deps.ps1
$ErrorActionPreference = "Stop"

$depsDir = Join-Path $PSScriptRoot "deps"
if (-not (Test-Path $depsDir)) {
    New-Item -ItemType Directory -Path $depsDir | Out-Null
}

$installerUrl = "https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6-1/wkhtmltox-0.12.6-1.msvc2015-win64.exe"
$installerPath = Join-Path $depsDir "wkhtmltox-installer.exe"
$dllPath = Join-Path $depsDir "libwkhtmltox.dll"

if ((Test-Path $installerPath) -and (Test-Path $dllPath)) {
    Write-Host "Dependencies already downloaded." -ForegroundColor Green
    exit 0
}

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

if (-not (Test-Path $installerPath)) {
    Write-Error "wkhtmltopdf installer not found after download."
    exit 1
}

Write-Host "Extracting libwkhtmltox.dll..."
$extracted = $false

# Try 7z on PATH
$7z = Get-Command "7z" -ErrorAction SilentlyContinue
if (-not $7z) {
    # Try default install location
    if (Test-Path "C:\Program Files\7-Zip\7z.exe") {
        $7z = "C:\Program Files\7-Zip\7z.exe"
    }
}

if ($7z) {
    Write-Host "Using 7z to extract DLL..."
    & $7z e $installerPath "bin\libwkhtmltox.dll" -o"$depsDir" -y | Out-Null
    if (Test-Path $dllPath) {
        $extracted = $true
    }
}

if (-not $extracted) {
    Write-Host "7z not found. Extracting via temporary install (may prompt for admin)..." -ForegroundColor Yellow
    $tempInstallDir = Join-Path $depsDir "temp_install"
    if (Test-Path $tempInstallDir) {
        Remove-Item $tempInstallDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    $proc = Start-Process -FilePath $installerPath -ArgumentList "/S", "/D=$tempInstallDir" -PassThru -Wait -NoNewWindow
    if (Test-Path "$tempInstallDir\bin\libwkhtmltox.dll") {
        Copy-Item "$tempInstallDir\bin\libwkhtmltox.dll" -Destination $depsDir -Force
        $extracted = $true
    }

    Write-Host "Cleaning up temporary installation..."
    $uninstaller = "$tempInstallDir\unins000.exe"
    if (Test-Path $uninstaller) {
        Start-Process -FilePath $uninstaller -ArgumentList "/S" -Wait -NoNewWindow
    }
    Remove-Item $tempInstallDir -Recurse -Force -ErrorAction SilentlyContinue
}

if (-not (Test-Path $dllPath)) {
    Write-Error "Failed to extract libwkhtmltox.dll. Please install 7-Zip or run as Administrator."
    exit 1
}

Write-Host "Dependencies downloaded and extracted successfully." -ForegroundColor Green
exit 0
