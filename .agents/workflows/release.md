---
description: How to trigger a high-quality release with automated descriptions.
---

1. **Prepare the Release**: 
   Run the preparation script. It will automatically calculate the next version (e.g., v2.0.7 -> v2.0.8) and bump version numbers across all project files.
   ```powershell
   ./scripts/prepare-release.ps1
   ```
   *Note: If you want to force a specific version, use `./scripts/prepare-release.ps1 -Version v3.0.0`.*

2. **Review & Refine**: 
   Open `RELEASE_NOTES_TMP.md`. The script generates a base description, but the AI assistant (or you) should refine it to make it "Good" (as per user preference for professional wording).

3. **Rebuild Local Installers**:
   Run the local build script to clean the stale `release/` folder and rebuild all Windows installers with the new version. This ensures the local artifacts match the current version before tagging.
   ```powershell
   ./scripts/build-local.ps1
   ```
   *Note: Requires Inno Setup (`iscc`) and WiX (`wix`) to be installed. Skips gracefully if not found.*

4. **Tag and Push**: 
   Once the description is finalized and local build looks good, create the tag using the description as the message. Use the exact version calculated by the prepare script.
   ```powershell
   $desc = Get-Content RELEASE_NOTES_TMP.md | Out-String
   git tag -a v2.0.8 -m "$desc"
   git push origin v2.0.8
   ```

5. **Cleanup**: 
   Remove the temporary file.
   ```powershell
   Remove-Item RELEASE_NOTES_TMP.md
   ```

6. **GitHub Sync**: 
   The `release.yml` workflow will automatically pick up the tag and build all 7 platform assets (3 Windows + 4 Linux) and publish them to the GitHub release.
