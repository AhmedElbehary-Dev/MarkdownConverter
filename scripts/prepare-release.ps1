param (
    [string]$Version = ""
)

# 1. Automatic Version Selection logic
$lastTag = git describe --tags --abbrev=0 2>$null
if ($null -eq $lastTag) { $lastTag = "v2.0.4" }

if ([string]::IsNullOrWhiteSpace($Version)) {
    # Extract X.Y.Z
    if ($lastTag -match 'v(\d+)\.(\d+)\.(\d+)') {
        $major = [int]$Matches[1]
        $minor = [int]$Matches[2]
        $patch = [int]$Matches[3]
        
        # User Rule: v2.0.9 -> v2.1.0
        $patch++
        if ($patch -gt 9) {
            $minor++
            $patch = 0
        }
        $Version = "v$major.$minor.$patch"
    } else {
        $Version = "v2.0.5" # Fallback
    }
}

Write-Host "🚀 Preparing Release: $lastTag -> $Version" -ForegroundColor Cyan
if ($null -eq $lastTag) {
    $changes = git log --oneline -n 10
} else {
    $changes = git log "$($lastTag)..HEAD" --oneline
}

$verClean = $Version.TrimStart('v')
$today = Get-Date -Format "yyyy-MM-dd"

$mdContent = @"
## 🚀 Release v$verClean ($today)

This release of **Markdown Converter Pro** includes the following updates:

### ✨ Features & Improvements
$(
    if ($changes) {
        $changes | ForEach-Object { "- " + $_.Substring(8) } | Out-String
    } else {
        "- Stabilization and minor tweaks."
    }
)

### 📱 Platform Support
- **Windows**: Built-in .exe and .msi installers.
- **Linux**: Debian (.deb) package for x64.

---
**Full Changelog**: https://github.com/AhmedElbehary-Dev/MarkdownConverter/compare/$($lastTag)...$Version
"@

$mdContent | Set-Content RELEASE_NOTES_TMP.md
Write-Host "✅ Generated RELEASE_NOTES_TMP.md for version $Version" -ForegroundColor Green

Write-Host "🔄 Updating version numbers across project files to $verClean..." -ForegroundColor Cyan

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

# Update WiX Installer (.wxs)
$wxsPath = ".\packaging\windows\setup.wxs"
if (Test-Path $wxsPath) {
    $content = Get-Content $wxsPath
    $newContent = $content -replace 'Version="[^"]*"', "Version=""$verClean"""
    $newContent | Set-Content $wxsPath
}

Write-Host "Please review the file and changes. Once ready, run your tagging commands."
