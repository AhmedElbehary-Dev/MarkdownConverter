using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using MarkdownConverter.Desktop.Services;
using MarkdownConverter.Services;
using MarkdownConverter.ViewModels;
using System;
using System.Collections.Specialized;

namespace MarkdownConverter.Desktop.UI
{
    public partial class ImageToPdfWindow : Window
    {
        public ImageToPdfWindow()
        {
            InitializeComponent();
            
            var platformServices = new AvaloniaUiPlatformServices(() => this);
            var imageToPdfService = new ImageToPdfService();
            var viewModel = new ImageToPdfViewModel(imageToPdfService, platformServices);
            
            viewModel.Images.CollectionChanged += Images_CollectionChanged;
            
            DataContext = viewModel;
        }

        private void Images_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ImageItemViewModel item in e.NewItems)
                {
                    if (item.UiThumbnail == null && !string.IsNullOrEmpty(item.FilePath))
                    {
                        try
                        {
                            using var stream = System.IO.File.OpenRead(item.FilePath);
                            item.UiThumbnail = Bitmap.DecodeToWidth(stream, 300);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load thumbnail: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
