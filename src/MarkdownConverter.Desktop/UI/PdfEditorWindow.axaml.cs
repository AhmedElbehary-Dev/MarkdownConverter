using Avalonia.Controls;
using Avalonia.Interactivity;
using MarkdownConverter.Desktop.Services;
using MarkdownConverter.Services;
using MarkdownConverter.ViewModels;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using MarkdownConverter.Models;
using System.Collections.Specialized;
using System;

namespace MarkdownConverter.Desktop.UI
{
    public partial class PdfEditorWindow : Window
    {
        public PdfEditorWindow()
        {
            InitializeComponent();
            
            var platformServices = new AvaloniaUiPlatformServices(() => this);
            var pdfEditorService = new PdfEditorService();
            var viewModel = new PdfEditorViewModel(pdfEditorService, platformServices);
            
            viewModel.Pages.CollectionChanged += Pages_CollectionChanged;
            
            DataContext = viewModel;
        }

        private void Pages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (PdfPageViewModel item in e.NewItems)
                {
                    if (item.Image != null && item.UiThumbnail == null)
                    {
                        try
                        {
                            item.UiThumbnail = CreateBitmapFromBgra(item.Image);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            // In case of any mapping or memory errors
                            System.Diagnostics.Debug.WriteLine($"Failed to create bitmap: {ex.Message}");
                        }
                    }
                }
            }
        }

        private Bitmap CreateBitmapFromBgra(PdfPageImage image)
        {
            var writeableBitmap = new WriteableBitmap(
                new Avalonia.PixelSize(image.Width, image.Height),
                new Avalonia.Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            using (var frameBuffer = writeableBitmap.Lock())
            {
                Marshal.Copy(image.BgraPixels, 0, frameBuffer.Address, image.BgraPixels.Length);
            }
            return writeableBitmap;
        }
    }
}
