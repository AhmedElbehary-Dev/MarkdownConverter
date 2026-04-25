param (
    [string]$Version = ""
)

if ([string]::IsNullOrWhiteSpace($Version)) {
    $csprojVersion = (Select-Xml -Path "..\..\MarkdownConverter.csproj" -XPath "//Version").Node.InnerText
    if ($csprojVersion) {
        $Version = $csprojVersion
    } else {
        $Version = "2.1.0"
    }
}

$verClean = $Version.TrimStart('v')
# MSIX requires a 4-part version number (e.g., 2.1.0.0)
$msixVersion = $verClean
if (($msixVersion -split '\.').Count -eq 3) {
    $msixVersion = "$msixVersion.0"
}

Write-Host "Building MSIX package for version $msixVersion..." -ForegroundColor Cyan

$publishDir = "..\..\bin\Release\net10.0\win-x64\publish"
$msixStagingDir = "..\..\build-msix\AppX"
$outputDir = "..\..\release"

if (-not (Test-Path $publishDir)) {
    Write-Error "Publish directory not found. Run build-local.ps1 first to publish the app."
    exit 1
}

# Clean staging directory
if (Test-Path "..\..\build-msix") {
    Remove-Item "..\..\build-msix\*" -Recurse -Force
}
New-Item -ItemType Directory -Path "$msixStagingDir\Assets" -Force | Out-Null
New-Item -ItemType Directory -Path $outputDir -Force -ErrorAction SilentlyContinue | Out-Null

Write-Host "Copying files to staging directory..."
Copy-Item "$publishDir\*" $msixStagingDir -Recurse -Force

# Copy AppxManifest and inject version
Write-Host "Configuring AppxManifest.xml..."
$manifestContent = Get-Content ".\AppxManifest.xml"
$manifestContent = $manifestContent -replace 'Version="[^"]+"', "Version=`"$msixVersion`""
$manifestContent | Set-Content "$msixStagingDir\AppxManifest.xml"

# Handle Assets
Write-Host "Setting up assets..."
$iconSource = "..\..\src\MarkdownConverter.Desktop\Assets\md_converter.png"
if (Test-Path $iconSource) {
    # In a real build pipeline, you'd use a tool to generate all the required sizes.
    # For now, we copy the single png to all required names to satisfy makeappx.
    Copy-Item $iconSource "$msixStagingDir\Assets\StoreLogo.png"
    Copy-Item $iconSource "$msixStagingDir\Assets\Square150x150Logo.png"
    Copy-Item $iconSource "$msixStagingDir\Assets\Square44x44Logo.png"
    Copy-Item $iconSource "$msixStagingDir\Assets\Wide310x150Logo.png"
} else {
    Write-Warning "Icon not found at $iconSource. You need assets to build an MSIX."
}

# Find makeappx.exe
$makeappx = Get-Command makeappx -ErrorAction SilentlyContinue
if (-not $makeappx) {
    # Check common Windows SDK paths
    $sdkPaths = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin\*" -Directory | Sort-Object Name -Descending
    foreach ($sdk in $sdkPaths) {
        $candidate = "$($sdk.FullName)\x64\makeappx.exe"
        if (Test-Path $candidate) {
            $makeappx = $candidate
            break
        }
    }
}

if (-not $makeappx) {
    Write-Error "makeappx.exe not found. Please install the Windows SDK."
    exit 1
}

$msixFile = "$outputDir\MarkdownConverter-win-x64.msix"
Write-Host "Packing MSIX using $($makeappx)..."
& $makeappx pack /d $msixStagingDir /p $msixFile /o

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Successfully created $msixFile" -ForegroundColor Green
    Write-Host "Note: The package is unsigned. To install it, you must sign it using signtool or install via the Microsoft Store." -ForegroundColor Yellow
} else {
    Write-Error "MSIX packaging failed."
}

# Cleanup
Remove-Item "..\..\build-msix" -Recurse -Force
