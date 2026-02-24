using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MarkdownConverter.Platform;
using MarkdownConverter.UI;
using MarkdownConverter.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MarkdownConverter.Desktop.Services;

public sealed class AvaloniaUiPlatformServices : IUiPlatformServices
{
    private readonly Func<Window?> _getOwner;

    public AvaloniaUiPlatformServices(Func<Window?> getOwner)
    {
        _getOwner = getOwner ?? throw new ArgumentNullException(nameof(getOwner));
    }

    public async Task<string?> PickMarkdownFileAsync()
    {
        var owner = _getOwner();
        if (owner?.StorageProvider is null)
        {
            return null;
        }

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Markdown File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Markdown Files")
                {
                    Patterns = ["*.md", "*.markdown"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*"]
                }
            ]
        });

        return ToLocalPath(files.FirstOrDefault());
    }

    public async Task<string?> PickOutputFolderAsync(string? initialPath)
    {
        var owner = _getOwner();
        if (owner?.StorageProvider is null)
        {
            return null;
        }

        IStorageFolder? suggested = null;
        if (!string.IsNullOrWhiteSpace(initialPath))
        {
            try
            {
                suggested = await owner.StorageProvider.TryGetFolderFromPathAsync(initialPath);
            }
            catch
            {
                suggested = null;
            }
        }

        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select output folder",
            AllowMultiple = false,
            SuggestedStartLocation = suggested
        });

        return ToLocalPath(folders.FirstOrDefault());
    }

    public void OpenPathWithShell(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    public void SetClipboardText(string text)
    {
        var owner = _getOwner();
        if (owner?.Clipboard is null)
        {
            return;
        }

        owner.Clipboard.SetTextAsync(text).GetAwaiter().GetResult();
    }

    public async Task ShowMessageAsync(string message, ToastKind kind)
    {
        var owner = _getOwner();
        if (owner is null)
        {
            return;
        }

        if (!Dispatcher.UIThread.CheckAccess())
        {
            var tcs = new TaskCompletionSource<object?>();
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    await ShowMessageAsync(message, kind);
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            await tcs.Task;
            return;
        }

        var dialog = new MessageDialogWindow(message, kind);
        await dialog.ShowDialog(owner);
    }

    public IUiTimer CreateTimer(TimeSpan interval, Action tick)
    {
        return new AvaloniaUiTimer(interval, tick);
    }

    public void PostToUi(Action action)
    {
        Dispatcher.UIThread.Post(action);
    }

    private static string? ToLocalPath(IStorageItem? item)
    {
        return item?.Path?.LocalPath;
    }
}
