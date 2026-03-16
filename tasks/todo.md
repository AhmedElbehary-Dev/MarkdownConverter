# Adding Automated GitHub Release

- [x] Identify failure: The `v2.0.0` and `v2.0.1` tags were pushed, but no release was created because we lacked a GitHub Actions workflow to create one, and `dotnet-desktop.yml` does not trigger on tags.
- [ ] Create `.github/workflows/release.yml` triggered by `push` on `tags: ['v*']`
- [ ] Define jobs to build the single-file standalone executable for Windows using the corrected `dotnet publish` command
- [ ] Add a step to create the GitHub Release and upload the built binary alongside the release notes using `softprops/action-gh-release`
- [ ] Commit and push the new workflow
- [ ] Delete remote tag `v2.0.1` and re-push it to trigger the new release workflow
- [ ] Update `tasks/lessons.md` with the new learning.
