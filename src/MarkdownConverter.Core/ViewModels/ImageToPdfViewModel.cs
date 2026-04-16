using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MarkdownConverter.Services;
using MarkdownConverter.Platform;

namespace MarkdownConverter.ViewModels
{
    public class ImageToPdfViewModel : ViewModelBase
    {
        private readonly ImageToPdfService _imageToPdfService;
        private readonly IUiPlatformServices _platformServices;
        
        private string _statusText = "Ready";
        private bool _isProcessing;

        public ObservableCollection<ImageItemViewModel> Images { get; } = new ObservableCollection<ImageItemViewModel>();

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public AsyncRelayCommand BrowseImagesCommand { get; }
        public RelayCommand RemoveSelectedCommand { get; }
        public RelayCommand ClearAllCommand { get; }
        public RelayCommand MoveUpCommand { get; }
        public RelayCommand MoveDownCommand { get; }
        public AsyncRelayCommand CombineToPdfCommand { get; }

        public ImageToPdfViewModel(ImageToPdfService imageToPdfService, IUiPlatformServices platformServices)
        {
            _imageToPdfService = imageToPdfService;
            _platformServices = platformServices;

            BrowseImagesCommand = new AsyncRelayCommand(BrowseImagesAsync, () => !IsProcessing);
            RemoveSelectedCommand = new RelayCommand(_ => RemoveSelected(), _ => !IsProcessing && Images.Any(i => i.IsSelected));
            ClearAllCommand = new RelayCommand(_ => ClearAll(), _ => !IsProcessing && Images.Count > 0);
            MoveUpCommand = new RelayCommand(_ => MoveUp(), _ => CanMoveUp());
            MoveDownCommand = new RelayCommand(_ => MoveDown(), _ => CanMoveDown());
            CombineToPdfCommand = new AsyncRelayCommand(CombineToPdfAsync, () => !IsProcessing && Images.Count > 0);
        }

        private void RaiseCommandStates()
        {
            BrowseImagesCommand.RaiseCanExecuteChanged();
            RemoveSelectedCommand.RaiseCanExecuteChanged();
            ClearAllCommand.RaiseCanExecuteChanged();
            MoveUpCommand.RaiseCanExecuteChanged();
            MoveDownCommand.RaiseCanExecuteChanged();
            CombineToPdfCommand.RaiseCanExecuteChanged();
        }

        public void HookItemPropertyChanged(ImageItemViewModel item)
        {
            item.PropertyChanged -= Item_PropertyChanged;
            item.PropertyChanged += Item_PropertyChanged;
        }

        private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImageItemViewModel.IsSelected))
            {
                RaiseCommandStates();
            }
        }

        private async Task BrowseImagesAsync()
        {
            var files = await _platformServices.PickImageFilesAsync();
            if (files != null && files.Length > 0)
            {
                foreach (var file in files)
                {
                    var vm = new ImageItemViewModel(file);
                    HookItemPropertyChanged(vm);
                    Images.Add(vm);
                }
                StatusText = $"Added {files.Length} images. Total: {Images.Count}";
                RaiseCommandStates();
            }
        }

        private void RemoveSelected()
        {
            var selected = Images.Where(i => i.IsSelected).ToList();
            foreach (var item in selected)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                Images.Remove(item);
            }
            StatusText = $"Removed {selected.Count} images. Total: {Images.Count}";
            RaiseCommandStates();
        }

        private void ClearAll()
        {
            foreach(var item in Images)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
            Images.Clear();
            StatusText = "Cleared all images.";
            RaiseCommandStates();
        }

        private bool CanMoveUp()
        {
            if (IsProcessing) return false;
            var selected = Images.Where(i => i.IsSelected).ToList();
            if (selected.Count != 1) return false;
            return Images.IndexOf(selected[0]) > 0;
        }

        private void MoveUp()
        {
            var item = Images.Single(i => i.IsSelected);
            int idx = Images.IndexOf(item);
            if (idx > 0)
            {
                Images.Move(idx, idx - 1);
            }
            RaiseCommandStates();
        }

        private bool CanMoveDown()
        {
            if (IsProcessing) return false;
            var selected = Images.Where(i => i.IsSelected).ToList();
            if (selected.Count != 1) return false;
            return Images.IndexOf(selected[0]) < Images.Count - 1;
        }

        private void MoveDown()
        {
            var item = Images.Single(i => i.IsSelected);
            int idx = Images.IndexOf(item);
            if (idx < Images.Count - 1)
            {
                Images.Move(idx, idx + 1);
            }
            RaiseCommandStates();
        }

        private async Task CombineToPdfAsync()
        {
            try
            {
                var initialName = Images.FirstOrDefault()?.FileName;
                if (!string.IsNullOrEmpty(initialName))
                {
                    initialName = Path.GetFileNameWithoutExtension(initialName) + "_combined";
                }
                else
                {
                    initialName = "combined";
                }

                var outputPath = await _platformServices.SavePdfFileAsync(initialName);
                if (string.IsNullOrEmpty(outputPath)) return; // Canceled

                IsProcessing = true;
                StatusText = "Combining images to PDF...";

                await Task.Run(() =>
                {
                    _imageToPdfService.CombineImagesToPdf(Images.Select(i => i.FilePath), outputPath);
                });

                StatusText = $"Saved PDF to: {outputPath}";
                await _platformServices.ShowMessageAsync("PDF successfully created!", ToastKind.Success);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                StatusText = $"Error: {ex.Message}";
                await _platformServices.ShowMessageAsync($"Failed to create PDF:\n{ex.Message}", ToastKind.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
