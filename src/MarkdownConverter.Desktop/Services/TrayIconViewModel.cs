using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Windows.Input;

namespace MarkdownConverter.Desktop.Services;

public class TrayIconViewModel
{
    public ICommand ToggleWindowCommand { get; }
    public ICommand ShowWindowCommand { get; }
    public ICommand QuickPasteCommand { get; }
    public ICommand ExitCommand { get; }

    public TrayIconViewModel()
    {
        ToggleWindowCommand = new SimpleCommand(ToggleWindow);
        ShowWindowCommand = new SimpleCommand(ShowWindow);
        QuickPasteCommand = new SimpleCommand(OpenQuickPaste);
        ExitCommand = new SimpleCommand(ExitApp);
    }

    private void ToggleWindow()
    {
        if (GetMainWindow() is { } window)
        {
            if (window.IsVisible)
            {
                window.Hide();
            }
            else
            {
                window.Show();
                window.WindowState = WindowState.Normal;
                window.Activate();
            }
        }
    }

    private void ShowWindow()
    {
        if (GetMainWindow() is { } window)
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
        }
    }

    private void OpenQuickPaste()
    {
        ShowWindow(); // Ensure main window is visible first
        if (GetMainWindow() is MainWindow mainWin)
        {
            var services = new AvaloniaUiPlatformServices(() => mainWin);
            var storage = new MarkdownConverter.Services.QuickPasteStorageService();
            var vm = new MarkdownConverter.ViewModels.QuickPasteViewModel(
                new MarkdownConverter.Converters.ConversionService(), services, storage);
            var qpWindow = new UI.QuickPasteWindow(vm);
            qpWindow.Show(mainWin);
        }
    }

    private void ExitApp()
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Signal the main window to allow close instead of hiding to tray
            if (desktop.MainWindow is MainWindow mainWindow)
            {
                mainWindow.IsShuttingDown = true;
            }

            desktop.TryShutdown();
        }
    }

    private static Window? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
    }
}

/// <summary>Simple ICommand implementation for tray menu items.</summary>
public class SimpleCommand : ICommand
{
    private readonly Action _execute;

    public SimpleCommand(Action execute) => _execute = execute;

#pragma warning disable CS0067 // Required by ICommand interface
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}
