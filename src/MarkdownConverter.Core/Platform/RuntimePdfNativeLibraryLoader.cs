using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MarkdownConverter.Platform;

public sealed class RuntimePdfNativeLibraryLoader : IPdfNativeLibraryLoader
{
    private const string BaseLibraryName = "libwkhtmltox";
    private readonly object _sync = new();
    private bool _loaded;

    public void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        lock (_sync)
        {
            if (_loaded)
            {
                return;
            }

            var libraryFileName = GetNativeLibraryFileName();

            foreach (var candidate in GetCandidatePaths(libraryFileName))
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }

                NativeLibrary.Load(candidate);
                _loaded = true;
                return;
            }

            // Try system lookup last, so deployments can rely on OS package managers.
            if (NativeLibrary.TryLoad(BaseLibraryName, out _))
            {
                _loaded = true;
                return;
            }

            throw new DllNotFoundException(
                $"Unable to load '{BaseLibraryName}'. Expected runtime asset '{libraryFileName}' under " +
                $"'{Path.Combine(AppContext.BaseDirectory, "runtimes", "<rid>", "native")}' or a system-installed library.");
        }
    }

    private static string GetNativeLibraryFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"{BaseLibraryName}.dll";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"{BaseLibraryName}.dylib";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return $"{BaseLibraryName}.so";
        }

        throw new PlatformNotSupportedException("Unsupported OS for wkhtmltopdf native runtime.");
    }

    private static string[] GetCandidatePaths(string libraryFileName)
    {
        var baseDir = AppContext.BaseDirectory;
        var ridCandidates = GetRidCandidates();

        var paths = new string[ridCandidates.Length + 1];
        for (var i = 0; i < ridCandidates.Length; i++)
        {
            paths[i] = Path.Combine(baseDir, "runtimes", ridCandidates[i], "native", libraryFileName);
        }

        // Fallback: app-local native file.
        paths[^1] = Path.Combine(baseDir, libraryFileName);
        return paths;
    }

    private static string[] GetRidCandidates()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new[] { "win-x64" };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new[] { "osx-x64" };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new[] { "linux-x64" };
        }

        return Array.Empty<string>();
    }
}
