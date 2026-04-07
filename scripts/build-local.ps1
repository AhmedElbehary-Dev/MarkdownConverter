param (
    [string]$Version = ""       # Override version, e.g. "v2.0.8"
)

# --- Resolve version from .csproj if not supplied --------------------------
if ([string]::IsNullOrWhiteSpace($Version)) {
    $csprojVersion = (Select-Xml -Path ".\MarkdownConverter.csproj" -XPath "//Version").Node.InnerText
    if ($csprojVersion) {
        $Version = "v$csprojVersion"
    } else {
        Write-Error "Could not read version from MarkdownConverter.csproj. Pass -Version manually."
        exit 1
    }
}
$verClean = $Version.TrimStart('v')

Write-Host ""
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "   Local Build Script -- v$verClean"               -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

# --- Clean release folder --------------------------------------------------
Write-Host "[1/4] Cleaning release/ folder..." -ForegroundColor Yellow
if (Test-Path ".\release") {
    Remove-Item ".\release\*" -Recurse -Force
} else {
    New-Item -ItemType Directory -Path ".\release" | Out-Null
}

# --- Clean old publish output ----------------------------------------------
Write-Host "[2/4] Cleaning old publish output..." -ForegroundColor Yellow
$publishPath = ".\bin\Release\net10.0\win-x64\publish"
if (Test-Path $publishPath) {
    Remove-Item $publishPath -Recurse -Force
}

# --- dotnet publish --------------------------------------------------------
Write-Host "[3/4] Publishing win-x64 (v$verClean)..." -ForegroundColor Cyan
dotnet publish MarkdownConverter.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:Version=$verClean `
    /p:InformationalVersion=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed!"
    exit 1
}
Write-Host "   dotnet publish succeeded." -ForegroundColor Green

# --- Verify publish output exists ------------------------------------------
$publishExe = ".\bin\Release\net10.0\win-x64\publish\MarkdownConverter.exe"
if (-not (Test-Path $publishExe)) {
    Write-Error "Publish output not found at $publishExe."
    exit 1
}
$fileVer = (Get-Item $publishExe).VersionInfo.ProductVersion
Write-Host "   Built binary version: $fileVer" -ForegroundColor Gray

# --- Inno Setup .exe -------------------------------------------------------
Write-Host "[4/4] Building Windows installer (.exe)..." -ForegroundColor Cyan

# Patch version into .iss
(Get-Content .\packaging\windows\setup.iss) `
    -replace 'AppVersion=.*', "AppVersion=$verClean" |
    Set-Content .\packaging\windows\setup.iss

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if ($null -eq $iscc) {
    Write-Error "iscc (Inno Setup) not found in PATH."
    Write-Error "Install from: https://jrsoftware.org/isdl.php"
    exit 1
}

iscc .\packaging\windows\setup.iss
if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup build failed!"
    exit 1
}
Write-Host "   Created: .\release\MarkdownConverter-Setup-x64.exe" -ForegroundColor Green

# --- Summary ---------------------------------------------------------------
Write-Host ""
Write-Host "===================================================" -ForegroundColor Green
Write-Host "   Build Complete -- v$verClean"                    -ForegroundColor Green
Write-Host "===================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Release file:" -ForegroundColor White
$exe = Get-Item ".\release\MarkdownConverter-Setup-x64.exe"
$size = [math]::Round($exe.Length / 1MB, 1)
Write-Host "   $($exe.Name)  ($size MB)" -ForegroundColor Gray
Write-Host ""
Write-Host ">> When ready to tag and push:" -ForegroundColor Yellow
Write-Host "      git tag -a $Version -m `"Release v$verClean`"" -ForegroundColor DarkGray
Write-Host "      git push origin $Version" -ForegroundColor DarkGray
