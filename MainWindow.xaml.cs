using MarkdownConverter.ViewModels;

namespace MarkdownConverter
{
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void Window_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null && viewModel.IsBusy)
            {
                e.Effects = System.Windows.DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = GetDragDropEffects(viewModel, e);
            if (viewModel != null)
            {
                viewModel.IsDropZoneActive = e.Effects == System.Windows.DragDropEffects.Copy;
            }

            e.Handled = true;
        }

        private void Window_PreviewDragLeave(object sender, System.Windows.DragEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.IsDropZoneActive = false;
            }

            e.Handled = true;
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel)
            {
                return;
            }

            if (viewModel.IsBusy)
            {
                return;
            }

            if (TryGetDroppedFile(e, out var filePath))
            {
                viewModel.TrySetSelectedMarkdownPathFromDrop(filePath);
            }
            viewModel.IsDropZoneActive = false;
            e.Handled = true;
        }

        private void DropZone_PreviewDragEnter(object sender, System.Windows.DragEventArgs e)
        {
            HandleDropZoneDragOver(e);
        }

        private void DropZone_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            HandleDropZoneDragOver(e);
        }

        private void DropZone_PreviewDragLeave(object sender, System.Windows.DragEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.IsDropZoneActive = false;
            }

            e.Handled = true;
        }

        private void DropZone_PreviewDrop(object sender, System.Windows.DragEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel)
            {
                return;
            }

            if (viewModel.IsBusy)
            {
                e.Effects = System.Windows.DragDropEffects.None;
                e.Handled = true;
                return;
            }

            if (TryGetDroppedFile(e, out var filePath))
            {
                viewModel.TrySetSelectedMarkdownPathFromDrop(filePath);
            }

            viewModel.IsDropZoneActive = false;
            e.Handled = true;
        }

        private void DropZone_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel || viewModel.IsBusy)
            {
                return;
            }

            if (IsHyperlinkSource(e.OriginalSource))
            {
                return;
            }

            if (viewModel.BrowseCommand.CanExecute(null))
            {
                viewModel.BrowseCommand.Execute(null);
            }

            e.Handled = true;
        }

        private void HandleDropZoneDragOver(System.Windows.DragEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel || viewModel.IsBusy)
            {
                e.Effects = System.Windows.DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = GetDragDropEffects(viewModel, e);
            viewModel.IsDropZoneActive = e.Effects == System.Windows.DragDropEffects.Copy;
            e.Handled = true;
        }

        private static bool TryGetDroppedFile(System.Windows.DragEventArgs e, out string filePath)
        {
            filePath = string.Empty;
            if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                return false;
            }

            if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                filePath = files[0];
                return true;
            }

            return false;
        }

        private static System.Windows.DragDropEffects GetDragDropEffects(MainViewModel? viewModel, System.Windows.DragEventArgs e)
        {
            if (viewModel == null || !TryGetDroppedFile(e, out var filePath))
            {
                return System.Windows.DragDropEffects.None;
            }

            return viewModel.IsValidMarkdownFile(filePath)
                ? System.Windows.DragDropEffects.Copy
                : System.Windows.DragDropEffects.None;
        }

        private static bool IsHyperlinkSource(object? source)
        {
            if (source is not System.Windows.DependencyObject dependencyObject)
            {
                return false;
            }

            var current = dependencyObject;
            while (current != null)
            {
                if (current is System.Windows.Documents.Hyperlink)
                {
                    return true;
                }

                current = System.Windows.LogicalTreeHelper.GetParent(current);
            }

            return false;
        }
    }
}
