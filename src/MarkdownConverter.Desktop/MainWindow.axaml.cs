using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using MarkdownConverter.Converters;
using MarkdownConverter.Desktop.Services;
using MarkdownConverter.Desktop.Platform;
using MarkdownConverter.ViewModels;
using System;
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
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void DropZone_PointerReleased(object? sender, PointerReleasedEventArgs e)
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

        e.DragEffects = GetDragDropEffects(viewModel, e);
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

    private static DragDropEffects GetDragDropEffects(MainViewModel viewModel, DragEventArgs e)
    {
        if (!TryGetDroppedFile(e, out var filePath))
        {
            return DragDropEffects.None;
        }

        return viewModel.IsValidMarkdownFile(filePath) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private static bool TryGetDroppedFile(DragEventArgs e, out string filePath)
    {
        filePath = string.Empty;
        var files = e.Data.GetFiles();
        var first = files?.FirstOrDefault();
        var localPath = GetLocalPath(first);
        if (string.IsNullOrWhiteSpace(localPath))
        {
            return false;
        }

        filePath = localPath;
        return true;
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

    private static string? GetLocalPath(Avalonia.Platform.Storage.IStorageItem? item)
    {
        if (item?.Path is null)
        {
            return null;
        }

        return item.Path.IsFile
            ? item.Path.LocalPath
            : item.Path.LocalPath;
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        if (_linuxDesktopIdentityApplied)
        {
            return;
        }

        _linuxDesktopIdentityApplied = true;
        LinuxDesktopIntegration.TryApplyRuntimeIdentity(this);
    }
}
