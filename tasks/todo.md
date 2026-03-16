# Installer Generation & Deployment Plan

## 1. Preparation
- [x] Review repository structure and existing packaging files.
- [x] Determine packaging approach (Inno Setup for `.exe`, WiX for `.msi`, `dpkg-deb` for `.deb`).

## 2. Implementation: Local Build Scripts
- [x] Create `packaging/windows/setup.iss` (Inno Setup script for `.exe` installer).
- [x] Create `packaging/windows/setup.wxs` (WiX toolset script for `.msi` installer).
- [x] Create `packaging/linux/build-deb.sh` (Bash script for `.deb` package generation).
- [x] Create `build-installers.ps1` to orchestrate building the Windows installers locally.

## 3. Implementation: GitHub Actions CI/CD
- [x] Create or update `.github/workflows/release.yml`.
- [x] Add job `build-windows-exe-msi`: Checkout, Setup .NET SDK, Publish `win-x64`, Run Inno Setup, Run WiX.
- [x] Add job `build-linux-deb`: Checkout, Setup .NET SDK, Publish `linux-x64`, Run `build-deb.sh`.
- [x] Add job `release`: Download artifacts, create GitHub Release via `softprops/action-gh-release`.

## 4. Verification
- [x] Verify Windows installer scripts build locally (`.\build-installers.ps1` runs WiX and Inno Setup).
- [x] Both `.msi` and `.exe` artifacts exist in `./release`.
- [ ] Instruct user to test auto-uninstall capability by running the new installer while the old version is installed.
- [ ] Instruct user to trigger CI/CD actions to verify GitHub Actions workflow runs correctly.
- [ ] Verify released artifacts are correctly installed and launched.
