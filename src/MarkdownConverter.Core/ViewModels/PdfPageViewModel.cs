using System;
using MarkdownConverter.Models;

namespace MarkdownConverter.ViewModels
{
    public class PdfPageViewModel : ViewModelBase
    {
        private bool _isSelected;
        private object? _uiThumbnail;

        public int PageNumber { get; }
        public PdfPageImage Image { get; }

        public object? UiThumbnail 
        { 
            get => _uiThumbnail; 
            set => SetProperty(ref _uiThumbnail, value); 
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public PdfPageViewModel(PdfPageImage image)
        {
            Image = image;
            PageNumber = image.PageNumber;
        }
    }
}
