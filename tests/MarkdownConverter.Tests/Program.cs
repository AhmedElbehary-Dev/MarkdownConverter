using MarkdownConverter.Converters;
using MarkdownConverter.Platform;
using MarkdownConverter.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarkdownConverter.Tests;

internal static class Program
{
    private static readonly List<string> Failures = [];

    public static async Task<int> Main()
    {
        await RunAsync(nameof(IsValidMarkdownFile_AcceptsMdAndMarkdown), IsValidMarkdownFile_AcceptsMdAndMarkdown);
        await RunAsync(nameof(TrySetSelectedMarkdownPathFromDrop_RejectsUnsupportedFile), TrySetSelectedMarkdownPathFromDrop_RejectsUnsupportedFile);
        await RunAsync(nameof(OutputPath_UpdatesWithFormatAndFolder), OutputPath_UpdatesWithFormatAndFolder);
        await RunAsync(nameof(CopyOutputCommand_CopiesPathAndShowsToast), CopyOutputCommand_CopiesPathAndShowsToast);
        await RunAsync(nameof(ClearCommand_ResetsExpectedFields), ClearCommand_ResetsExpectedFields);
        await RunAsync(nameof(ConvertCommand_OverwriteProtectionBlocksConversion), ConvertCommand_OverwriteProtectionBlocksConversion);
        await RunAsync(nameof(ConvertCommand_XlsxConversion_SucceedsAndWritesFile), ConvertCommand_XlsxConversion_SucceedsAndWritesFile);
        await RunAsync(nameof(ConvertCommand_PdfConversion_ProducesFileOrDependencyError), ConvertCommand_PdfConversion_ProducesFileOrDependencyError);
        await RunAsync(nameof(BaselineScaffold_FoldersExist), BaselineScaffold_FoldersExist);

        Console.WriteLine();
        if (Failures.Count == 0)
        {
            Console.WriteLine("All test harness checks passed.");
            return 0;
        }

        Console.WriteLine($"FAILED ({Failures.Count}):");
        foreach (var failure in Failures)
        {
            Console.WriteLine($"- {failure}");
        }

        return 1;
    }

    private static async Task RunAsync(string name, Func<Task> test)
    {
        try
        {
            await test();
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            Failures.Add($"{name}: {ex.Message}");
            Console.WriteLine($"FAIL {name}");
        }
    }

    private static Task IsValidMarkdownFile_AcceptsMdAndMarkdown()
    {
        var vm = CreateViewModel(out _);

        Assert(vm.IsValidMarkdownFile("sample.md"), "Expected .md to be valid.");
        Assert(vm.IsValidMarkdownFile("sample.markdown"), "Expected .markdown to be valid.");
        Assert(!vm.IsValidMarkdownFile("sample.txt"), "Expected .txt to be invalid.");

        return Task.CompletedTask;
    }

    private static Task TrySetSelectedMarkdownPathFromDrop_RejectsUnsupportedFile()
    {
        var vm = CreateViewModel(out _);

        var ok = vm.TrySetSelectedMarkdownPathFromDrop("sample.txt");

        Assert(!ok, "Unsupported file should be rejected.");
        Assert(vm.StatusText == "Unsupported file type.", "StatusText should match current behavior.");
        Assert(vm.StatusKind == StatusKind.Error, "StatusKind should be Error.");
        Assert(vm.IsToastVisible, "Toast should be visible.");
        Assert(vm.ToastMessage == "Only .md or .markdown files are supported.", "Toast message should match.");

        return Task.CompletedTask;
    }

    private static Task OutputPath_UpdatesWithFormatAndFolder()
    {
        var vm = CreateViewModel(out _);

        vm.SetSelectedMarkdownPath("/tmp/input/sample.md");
        vm.OutputFolder = "/tmp/output";

        Assert(vm.OutputFilePath.EndsWith(Path.Combine("output", "sample.pdf"), StringComparison.Ordinal),
            "Default output format should be PDF.");

        vm.SelectedFormat = vm.Formats.First(f => f.Extension == "xlsx").Value;

        Assert(vm.OutputFilePath.EndsWith(Path.Combine("output", "sample.xlsx"), StringComparison.Ordinal),
            "Output path should update with selected format.");

        return Task.CompletedTask;
    }

    private static Task CopyOutputCommand_CopiesPathAndShowsToast()
    {
        var vm = CreateViewModel(out var ui);

        vm.OutputFolder = "/tmp";
        vm.SetSelectedMarkdownPath("/tmp/input.md");
        vm.CopyOutputCommand.Execute(null);

        Assert(ui.ClipboardText == vm.OutputFilePath, "Clipboard should receive output file path.");
        Assert(vm.ToastMessage == "Output path copied.", "Success toast text should match.");
        Assert(vm.ToastKind == ToastKind.Success, "Toast kind should be success.");

        return Task.CompletedTask;
    }

    private static Task ClearCommand_ResetsExpectedFields()
    {
        var vm = CreateViewModel(out _);
        vm.OutputFolder = "/tmp";
        vm.SetSelectedMarkdownPath("/tmp/input.md");
        vm.TrySetSelectedMarkdownPathFromDrop("/tmp/input.md");

        vm.ClearCommand.Execute(null);

        Assert(vm.SelectedMarkdownPath == string.Empty, "SelectedMarkdownPath should clear.");
        Assert(vm.OutputFilePath == string.Empty, "OutputFilePath should clear.");
        Assert(vm.Progress == 0, "Progress should reset.");
        Assert(vm.StatusText == "Ready", "StatusText should reset.");
        Assert(vm.StatusKind == StatusKind.Neutral, "StatusKind should reset.");

        return Task.CompletedTask;
    }

    private static Task ConvertCommand_OverwriteProtectionBlocksConversion()
    {
        using var tempDir = new TempDir();
        var inputPath = Path.Combine(tempDir.Path, "input.md");
        var outputPath = Path.Combine(tempDir.Path, "input.xlsx");
        File.WriteAllText(inputPath, "# Title");
        File.WriteAllText(outputPath, "existing");

        var vm = CreateViewModel(out _);
        vm.SetSelectedMarkdownPath(inputPath);
        vm.OutputFolder = tempDir.Path;
        vm.SelectedFormat = vm.Formats.First(f => f.Extension == "xlsx").Value;
        vm.OverwriteIfExists = false;

        vm.ConvertCommand.Execute(null);

        Assert(vm.StatusText == "Output file already exists.", "Overwrite protection message should match.");
        Assert(vm.StatusKind == StatusKind.Error, "StatusKind should be Error.");
        Assert(vm.IsBusy == false, "ViewModel should not remain busy.");

        return Task.CompletedTask;
    }

    private static async Task ConvertCommand_XlsxConversion_SucceedsAndWritesFile()
    {
        using var tempDir = new TempDir();
        var inputPath = Path.Combine(tempDir.Path, "input.md");
        File.WriteAllText(inputPath, "# Title\n\n| A | B |\n|---|---|\n| 1 | 2 |\n");

        var vm = CreateViewModel(out _);
        vm.SetSelectedMarkdownPath(inputPath);
        vm.OutputFolder = tempDir.Path;
        vm.SelectedFormat = vm.Formats.First(f => f.Extension == "xlsx").Value;
        vm.OpenAfterConvert = false;
        vm.OverwriteIfExists = true;

        vm.ConvertCommand.Execute(null);

        await WaitForAsync(() => !vm.IsBusy, TimeSpan.FromSeconds(20));

        Assert(vm.StatusText == "Complete", "Successful conversion should set Complete.");
        Assert(vm.StatusKind == StatusKind.Success, "StatusKind should be Success.");
        Assert(File.Exists(vm.OutputFilePath), "Output file should exist.");
    }

    private static async Task ConvertCommand_PdfConversion_ProducesFileOrDependencyError()
    {
        using var tempDir = new TempDir();
        var inputPath = Path.Combine(tempDir.Path, "input.md");
        File.WriteAllText(inputPath, "# Title\n\nHello PDF.\n");

        var vm = CreateViewModel(out var ui);
        vm.SetSelectedMarkdownPath(inputPath);
        vm.OutputFolder = tempDir.Path;
        vm.SelectedFormat = vm.Formats.First(f => f.Extension == "pdf").Value;
        vm.OpenAfterConvert = false;
        vm.OverwriteIfExists = true;

        vm.ConvertCommand.Execute(null);

        await WaitForAsync(() => !vm.IsBusy, TimeSpan.FromSeconds(20));

        if (File.Exists(vm.OutputFilePath))
        {
            Assert(vm.StatusKind == StatusKind.Success, "PDF success should set StatusKind=Success.");
            return;
        }

        if (HasAnyCommand(["google-chrome", "google-chrome-stable", "chromium-browser", "chromium", "msedge", "microsoft-edge"]))
        {
            if (ui.Dialogs.Count > 0)
            {
                Console.WriteLine($"PDF fallback error: {ui.Dialogs[^1].Message.Replace(Environment.NewLine, " | ")}");
            }
            throw new InvalidOperationException("Expected PDF conversion to succeed because a Chromium-based browser is available on PATH.");
        }

        Assert(vm.StatusKind == StatusKind.Error, "PDF failure should surface an error state.");
        Assert(ui.Dialogs.Count > 0, "PDF failure should show an error dialog.");
        var message = ui.Dialogs[^1].Message;
        Assert(message.Contains("wkhtmltopdf", StringComparison.OrdinalIgnoreCase)
            || message.Contains("wkhtmltox", StringComparison.OrdinalIgnoreCase),
            "PDF dependency errors should mention wkhtmltopdf/wkhtmltox.");
    }

    private static Task BaselineScaffold_FoldersExist()
    {
        var root = FindRepoRoot();
        Assert(Directory.Exists(Path.Combine(root, "tests", "Baselines")), "tests/Baselines should exist.");
        Assert(Directory.Exists(Path.Combine(root, "tests", "Fixtures")), "tests/Fixtures should exist.");
        Assert(File.Exists(Path.Combine(root, "tests", "Baselines", "README.md")), "Baseline README scaffold should exist.");
        return Task.CompletedTask;
    }

    private static MainViewModel CreateViewModel(out FakeUiPlatformServices ui)
    {
        ui = new FakeUiPlatformServices();
        return new MainViewModel(new ConversionService(), ui);
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - start > timeout)
            {
                throw new TimeoutException("Condition timed out.");
            }

            await Task.Delay(50);
        }
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && current is not null; i++)
        {
            var candidate = current.FullName;
            if (File.Exists(Path.Combine(candidate, "MarkdownConverter.sln")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        // Fallback for local debugging if the above path walk misses.
        return Directory.GetCurrentDirectory();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static bool HasAnyCommand(IEnumerable<string> commandNames)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return false;
        }

        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        var pathDirs = pathValue.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [string.Empty];

        foreach (var command in commandNames)
        {
            foreach (var dir in pathDirs)
            {
                foreach (var ext in extensions)
                {
                    if (File.Exists(Path.Combine(dir, command + ext)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private sealed class FakeUiPlatformServices : IUiPlatformServices
    {
        private readonly List<FakeTimer> _timers = [];

        public string? ClipboardText { get; private set; }

        public string? NextMarkdownPath { get; set; }

        public string? NextOutputFolder { get; set; }

        public List<(string Message, ToastKind Kind)> Dialogs { get; } = [];

        public List<string> OpenedPaths { get; } = [];

        public Task<string?> PickMarkdownFileAsync() => Task.FromResult(NextMarkdownPath);

        public Task<string?> PickOutputFolderAsync(string? initialPath) => Task.FromResult(NextOutputFolder);

        public void OpenPathWithShell(string path) => OpenedPaths.Add(path);

        public void SetClipboardText(string text) => ClipboardText = text;

        public Task ShowMessageAsync(string message, ToastKind kind)
        {
            Dialogs.Add((message, kind));
            return Task.CompletedTask;
        }

        public IUiTimer CreateTimer(TimeSpan interval, Action tick)
        {
            var timer = new FakeTimer(interval, tick);
            _timers.Add(timer);
            return timer;
        }

        public void PostToUi(Action action) => action();
    }

    private sealed class FakeTimer : IUiTimer
    {
        private readonly Action _tick;
        public FakeTimer(TimeSpan interval, Action tick)
        {
            _tick = tick;
            Interval = interval;
        }

        public TimeSpan Interval { get; }
        public bool IsRunning { get; private set; }

        public void Start() => IsRunning = true;
        public void Stop() => IsRunning = false;
        public void Dispose() => Stop();

        public void Fire()
        {
            _tick();
        }
    }

    private sealed class TempDir : IDisposable
    {
        public TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MarkdownConverter.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup failures in test harness.
            }
        }
    }
}
