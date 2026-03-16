---
description: How to trigger a high-quality release with automated descriptions.
---

1. **Prepare the Release**: 
   Run the preparation script. It will automatically calculate the next version (e.g., v2.0.4 -> v2.0.5).
   ```powershell
   ./scripts/prepare-release.ps1
   ```
   *Note: If you want to force a specific version, use `./scripts/prepare-release.ps1 -Version v3.0.0`.*

2. **Review & Refine**: 
   Open `RELEASE_NOTES_TMP.md`. The script generates a base description, but the AI assistant (or you) should refine it to make it "Good" (as per user preference for professional wording).

3. **Tag and Push**: 
   Once the description is finalized, create the tag using the description as the message.
   ```powershell
   $desc = Get-Content RELEASE_NOTES_TMP.md | Out-String
   git tag -a v2.1.2 -m "$desc"
   git push origin v2.1.2
   ```

4. **Cleanup**: 
   Remove the temporary file.
   ```powershell
   Remove-Item RELEASE_NOTES_TMP.md
   ```

5. **GitHub Sync**: 
   The `release.yml` workflow will automatically pick up the tag and use the same versioning logic to build assets.
