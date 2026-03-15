# Lessons

## PublishSingleFile Error at Solution Level
When running `dotnet publish -p:PublishSingleFile=true` at the solution level, MSBuild applies this property to all projects in the solution. This causes build errors for non-executable projects (e.g., class libraries and test projects) with `error NETSDK1099: Publishing to a single-file is only supported for executable applications.`

**Solution**:
Add `<IsPublishable>false</IsPublishable>` to the `.csproj` files of any projects that aren't meant to be standalone executables (e.g. `MarkdownConverter.Core` and `MarkdownConverter.Tests`). This ensures `dotnet publish` only attempts to pack the main desktop executable project.
