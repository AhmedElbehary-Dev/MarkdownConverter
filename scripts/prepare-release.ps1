param (
    [string]$Version = ""
)

# 1. Automatic Version Selection logic
$lastTag = git describe --tags --abbrev=0 2>$null
if ($null -eq $lastTag) { $lastTag = "v2.0.7" }

if ([string]::IsNullOrWhiteSpace($Version)) {
    if ($lastTag -match 'v(\d+)\.(\d+)\.(\d+)') {
        $major = [int]$Matches[1]
        $minor = [int]$Matches[2]
        $patch = [int]$Matches[3]
        $patch++
        if ($patch -gt 9) {
            $minor++
            $patch = 0
        }
        $Version = "v$major.$minor.$patch"
    } else {
        $Version = "v2.0.8"
    }
}

Write-Host "Preparing Release: $lastTag -> $Version" -ForegroundColor Cyan
if ($null -eq $lastTag) {
    $changes = git log --oneline -n 10
} else {
    $changes = git log "$($lastTag)..HEAD" --oneline
}

$verClean = $Version.TrimStart('v')
$today = Get-Date -Format "yyyy-MM-dd"

$features = ""
if ($changes) {
    $features = ($changes | ForEach-Object { "- " + $_.Substring(8) }) -join "`n"
} else {
    $features = "- Stabilization and minor tweaks."
}

$mdContent = @"
## Release v$verClean ($today)

This release of **Markdown Converter Pro** includes the following updates:

### Features and Improvements
$features

### Platform Support
- **Windows**: .exe installer with improved version checking.
- **Linux**: Debian (.deb), RPM (.rpm), Flatpak, Snap, and AppImage.

---
**Full Changelog**: https://github.com/AhmedElbehary-Dev/MarkdownConverter/compare/$($lastTag)...$Version
"@

$mdContent | Set-Content RELEASE_NOTES_TMP.md
Write-Host "Generated RELEASE_NOTES_TMP.md for version $Version" -ForegroundColor Green

Write-Host "Updating version numbers across project files to $verClean..." -ForegroundColor Cyan

# Update all .csproj
Get-ChildItem -Path ".\src", ".\" -Filter *.csproj -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName
    if ($content -match '<Version>.*?</Version>') {
        $newContent = $content -replace '<Version>.*?</Version>', "<Version>$verClean</Version>"
        $newContent | Set-Content $_.FullName
    }
}

# Update Windows Installer (.iss)
$issPath = ".\packaging\windows\setup.iss"
if (Test-Path $issPath) {
    $content = Get-Content $issPath
    $newContent = $content -replace 'AppVersion=.*', "AppVersion=$verClean"
    $newContent | Set-Content $issPath
}

# Update Flatpak manifest
$flatpakPath = ".\packaging\linux\com.markdownconverter.app.yml"
if (Test-Path $flatpakPath) {
    $content = Get-Content $flatpakPath
    $newContent = $content -replace "version:.+", "version: '$verClean'"
    $newContent | Set-Content $flatpakPath
}

# Update Snap snapcraft.yaml
$snapCraftPath = ".\packaging\linux\snap\snapcraft.yaml"
if (Test-Path $snapCraftPath) {
    $content = Get-Content $snapCraftPath
    $newContent = $content -replace "version:.+", "version: '$verClean'"
    $newContent | Set-Content $snapCraftPath
}

Write-Host ""
Write-Host "Version bumped to $Version across all project files." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "   1. Review RELEASE_NOTES_TMP.md" -ForegroundColor White
Write-Host "   2. Commit changes: git add -A && git commit -m 'Release $Version'" -ForegroundColor White
Write-Host "   3. Tag and push: git tag -a $Version -m 'Release $Version' && git push origin $Version" -ForegroundColor White
