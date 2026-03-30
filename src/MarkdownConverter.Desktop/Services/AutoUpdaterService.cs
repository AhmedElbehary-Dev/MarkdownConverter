using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using MarkdownConverter.Platform;

namespace MarkdownConverter.Desktop.Services;

public class AutoUpdaterService
{
    private readonly IUiPlatformServices _uiServices;
    private const string RepoUrl = "https://api.github.com/repos/AhmedElbehary-Dev/MarkdownConverter/releases/latest";

    public AutoUpdaterService(IUiPlatformServices uiServices)
    {
        _uiServices = uiServices;
    }

    public async Task CheckForUpdatesBackgroundAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MarkdownConverter", "1.0"));
            
            var response = await client.GetAsync(RepoUrl);
            if (!response.IsSuccessStatusCode)
                return;

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            
            var root = document.RootElement;
            if (!root.TryGetProperty("tag_name", out var tagElement))
                return;

            var tagName = tagElement.GetString();
            if (string.IsNullOrWhiteSpace(tagName))
                return;

            var currentVersionText = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
            if (!Version.TryParse(currentVersionText, out var currentVersion))
                return;

            var cleanTag = tagName.TrimStart('v');
            if (!Version.TryParse(cleanTag, out var releaseVersion))
                return;

            if (releaseVersion <= currentVersion)
                return;

            // Newer version available. Find asset.
            if (!root.TryGetProperty("assets", out var assetsElement) || assetsElement.ValueKind != JsonValueKind.Array)
                return;

            string? targetExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : 
                                     RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? ".deb" : null;

            if (targetExtension == null)
                return; // Unsupported OS for auto-update

            string? downloadUrl = null;
            string fileName = $"MarkdownConverter_Update_{releaseVersion}{targetExtension}";

            foreach (var asset in assetsElement.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                if (name != null && name.EndsWith(targetExtension, StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            if (downloadUrl == null)
                return;

            // Silently download the update in the background
            var tempPath = Path.Join(Path.GetTempPath(), fileName);
            
            var downloadResponse = await client.GetAsync(downloadUrl);
            if (!downloadResponse.IsSuccessStatusCode)
                return;

            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await downloadResponse.Content.CopyToAsync(fs);
            }

            // Prompt user that update is ready
            var promptResult = await _uiServices.ShowConfirmAsync("Update Ready", 
                $"Version {releaseVersion} of Markdown Converter has been downloaded.\n\nWould you like to close the application and install it now?");

            if (promptResult)
            {
                // Run the newly downloaded installer
                var startInfo = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                Environment.Exit(0);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or IOException or UnauthorizedAccessException)
        {
            // Fail silently since it's a background update task
        }
    }
}
