using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using MarkdownConverter.Converters;
using MarkdownConverter.Desktop.Services;
using MarkdownConverter.Desktop.Platform;
using MarkdownConverter.ViewModels;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MarkdownConverter.Desktop;

public partial class MainWindow : Window
{
    private static readonly IBrush DropZoneIdleBackground = Brush.Parse("#242B36");
    private static readonly IBrush DropZoneActiveBackground = Brush.Parse("#2B3940");
    private static readonly IBrush DropZoneIdleBorder = Brush.Parse("#4B5565");
    private static readonly IBrush DropZoneActiveBorder = Brush.Parse("#18AEB6");
    private readonly Func<Func<Window?>, MainViewModel> _viewModelFactory;
    private bool _linuxDesktopIdentityApplied;

    public MainWindow()
        : this(static getOwner => new MainViewModel(new ConversionService(), new AvaloniaUiPlatformServices(getOwner)))
    {
    }

    public MainWindow(Func<Func<Window?>, MainViewModel> viewModelFactory)
    {
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));

        InitializeComponent();
        DataContext = _viewModelFactory(() => this);
        ApplyDropZoneVisualState(isActive: false);
        Opened += MainWindow_Opened;
        Closing += MainWindow_Closing;

        // Drag-drop handlers are declared in XAML (DragDrop.DragOver, DragDrop.Drop etc.)
        // We only need to ensure AllowDrop is set programmatically as a safety net.
        if (this.FindControl<Border>("DropZone") is { } dropZone)
        {
            DragDrop.SetAllowDrop(dropZone, true);
        }
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void DropZone_Tapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel is not { IsBusy: false } viewModel)
        {
            return;
        }

        if (e.Source is Button)
        {
            return;
        }

        if (viewModel.BrowseCommand.CanExecute(null))
        {
            viewModel.BrowseCommand.Execute(null);
        }

        e.Handled = true;
    }

    private void DropZone_DragOver(object? sender, DragEventArgs e)
    {
        if (ViewModel is not { IsBusy: false } viewModel)
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.DragEffects = GetDragDropEffects(e);
        viewModel.IsDropZoneActive = e.DragEffects == DragDropEffects.Copy;
        ApplyDropZoneVisualState(viewModel.IsDropZoneActive);
        e.Handled = true;
    }

    private void DropZone_DragLeave(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is MainViewModel viewModel)
        {
            viewModel.IsDropZoneActive = false;
        }

        ApplyDropZoneVisualState(isActive: false);
    }

    private void DropZone_Drop(object? sender, DragEventArgs e)
    {
        if (ViewModel is not { IsBusy: false } viewModel)
        {
            e.Handled = true;
            return;
        }

        if (TryGetDroppedFile(e, out var filePath))
        {
            viewModel.TrySetSelectedMarkdownPathFromDrop(filePath);
        }

        viewModel.IsDropZoneActive = false;
        ApplyDropZoneVisualState(isActive: false);
        e.Handled = true;
    }

    private static DragDropEffects GetDragDropEffects(DragEventArgs e)
    {
        // Use Avalonia 11's recommended API to check for file data.
        // e.Data.GetFiles() returns non-null when files are being dragged,
        // even during DragOver when actual paths may not be accessible yet.
        if (e.Data.GetFiles() != null)
        {
            return DragDropEffects.Copy;
        }

        // Fallback: check via Contains for legacy format strings
        if (e.Data.Contains(DataFormats.Files)
            || e.Data.Contains("FileNames")
            || e.Data.Contains("FileName"))
        {
            return DragDropEffects.Copy;
        }

        return DragDropEffects.None;
    }

    private static bool TryGetDroppedFile(DragEventArgs e, out string filePath)
    {
        filePath = string.Empty;

        // 1. Try Avalonia 11 Storage Items (Recommended)
        var storageItems = e.Data.GetFiles();
        if (storageItems != null)
        {
            var path = storageItems.Select(GetLocalPath).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
            if (path != null)
            {
                filePath = path;
                return true;
            }
        }

        // 2. Try Legacy File Paths list (Strings)
        var legacyFiles = e.Data.Get(DataFormats.Files);
        if (legacyFiles is IEnumerable<string> stringPaths)
        {
            var first = stringPaths.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                filePath = first;
                return true;
            }
        }
        else if (legacyFiles is IEnumerable<object> objectPaths)
        {
            var first = objectPaths.FirstOrDefault()?.ToString();
            if (!string.IsNullOrWhiteSpace(first))
            {
                filePath = first;
                return true;
            }
        }

        // 3. Last ditch effort: Platform-specific keys
        if (e.Data.Get("FileNames") is IEnumerable<string> fileNames)
        {
            var first = fileNames.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                filePath = first;
                return true;
            }
        }

        return false;
    }

    private void ApplyDropZoneVisualState(bool isActive)
    {
        if (this.FindControl<Border>("DropZone") is not { } border)
        {
            return;
        }

        border.Background = isActive ? DropZoneActiveBackground : DropZoneIdleBackground;
        border.BorderBrush = isActive ? DropZoneActiveBorder : DropZoneIdleBorder;
    }

    private static string? GetLocalPath(IStorageItem? item)
    {
        if (item == null) return null;

        // TryGetLocalPath is the most reliable way in Avalonia 11 to get a filesystem path
        var localPath = item.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            return localPath;
        }

        // Fallback to Uri parsing if extension method returns null
        if (item.Path is { IsFile: true } uri)
        {
            return uri.LocalPath;
        }

        return null;
    }

    /// <summary>
    /// Set to true by the tray Exit command so that closing
    /// actually terminates instead of hiding to the tray.
    /// </summary>
    internal bool IsShuttingDown { get; set; }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (IsShuttingDown)
        {
            // App is exiting — allow the window to close normally.
            return;
        }

        // User clicked the X button — hide to system tray instead of closing.
        e.Cancel = true;
        Hide();
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        if (_linuxDesktopIdentityApplied)
        {
            return;
        }

        _linuxDesktopIdentityApplied = true;
        LinuxDesktopIntegration.TryApplyRuntimeIdentity(this);

        // If launched with --minimized (e.g. Windows startup), hide to tray immediately
        if (Program.StartMinimized)
        {
            Hide();
        }

        // Run auto update check in background without blocking UI
        var services = new MarkdownConverter.Desktop.Services.AvaloniaUiPlatformServices(() => this);
        var updateService = new MarkdownConverter.Desktop.Services.AutoUpdaterService(services);
        _ = System.Threading.Tasks.Task.Run(() => updateService.CheckForUpdatesBackgroundAsync());
    }

    private void QuickPaste_Click(object? sender, RoutedEventArgs e)
    {
        var services = new AvaloniaUiPlatformServices(() => MarkdownConverter.Desktop.App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : this);
        var storage = new MarkdownConverter.Services.QuickPasteStorageService();
        var vm = new QuickPasteViewModel(new ConversionService(), services, storage);

        var window = new MarkdownConverter.Desktop.UI.QuickPasteWindow(vm);
        window.Show(this);
    }

    private void PdfEditor_Click(object? sender, RoutedEventArgs e)
    {
        var window = new MarkdownConverter.Desktop.UI.PdfEditorWindow();
        window.Show(this);
    }

    private void About_Click(object? sender, RoutedEventArgs e)
    {
        var window = new MarkdownConverter.Desktop.UI.AboutWindow();
        window.ShowDialog(this);
    }
    }
