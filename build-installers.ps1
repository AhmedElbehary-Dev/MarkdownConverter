# build-installers.ps1
$ErrorActionPreference = "Stop"

# Create release directory
if (!(Test-Path -Path .\release)) {
    New-Item -ItemType Directory -Path .\release | Out-Null
}

Write-Host "Publishing the application..."
dotnet publish .\MarkdownConverter.csproj -c Release -r win-x64 --self-contained true

Write-Host "Building EXE installer with Inno Setup..."
# Check if Inno Setup is in PATH, if not try default install path
$innoPaths = @("iscc", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "${env:LocalAppData}\Programs\Inno Setup 6\ISCC.exe")
$iscc = $null
foreach ($path in $innoPaths) {
    if (Get-Command $path -ErrorAction Ignore) {
        $iscc = $path
        break
    } elseif (Test-Path $path) {
        $iscc = $path
        break
    }
}

if ($iscc) {
    & $iscc .\packaging\windows\setup.iss
    Write-Host "Inno Setup built successfully."
} else {
    Write-Warning "Inno Setup (iscc) not found. Skipping EXE generation. Please install from https://jrsoftware.org/isdl.php"
}

Write-Host "Building MSI installer with WiX Toolset..."
if (Get-Command wix -ErrorAction SilentlyContinue) {
    # Build the MSI directly using native WiX v4+ Files element
    wix build .\packaging\windows\setup.wxs -o .\release\MarkdownConverter.msi
    Write-Host "WiX Toolset built successfully."
} else {
    Write-Warning "WiX not found. Run 'dotnet tool install --global wix' to install WiX."
}

Write-Host "All done! Installers are in the 'release' directory."
