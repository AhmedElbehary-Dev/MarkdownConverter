using System.IO;

namespace MarkdownConverter.ViewModels
{
    public class ImageItemViewModel : ViewModelBase
    {
        private bool _isSelected;
        private object? _uiThumbnail;

        public string FilePath { get; }
        public string FileName { get; }

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

        public ImageItemViewModel(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
        }
    }
}
