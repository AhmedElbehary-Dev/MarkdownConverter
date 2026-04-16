using MarkdownConverter.ViewModels;
using System;
using System.Threading.Tasks;

namespace MarkdownConverter.Platform;

public interface IUiPlatformServices
{
    Task<string?> PickMarkdownFileAsync();
    Task<string?> PickPdfFileAsync();
    Task<string[]?> PickImageFilesAsync();
    Task<string?> SavePdfFileAsync(string suggestedName);
    Task<string?> PickOutputFolderAsync(string? initialPath);
    void OpenPathWithShell(string path);
    void SetClipboardText(string text);
    Task<string?> GetClipboardTextAsync();
    Task ShowMessageAsync(string message, ToastKind kind);
    Task<bool> ShowConfirmAsync(string title, string message);
    IUiTimer CreateTimer(TimeSpan interval, Action tick);
    void PostToUi(Action action);
}
