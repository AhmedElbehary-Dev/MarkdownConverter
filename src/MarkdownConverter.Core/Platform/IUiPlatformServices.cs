using MarkdownConverter.ViewModels;
using System;
using System.Threading.Tasks;

namespace MarkdownConverter.Platform;

public interface IUiPlatformServices
{
    Task<string?> PickMarkdownFileAsync();
    Task<string?> PickPdfFileAsync();
    Task<string?> PickOutputFolderAsync(string? initialPath);
    void OpenPathWithShell(string path);
    void SetClipboardText(string text);
    Task<string?> GetClipboardTextAsync();
    Task ShowMessageAsync(string message, ToastKind kind);
    IUiTimer CreateTimer(TimeSpan interval, Action tick);
    void PostToUi(Action action);
}
