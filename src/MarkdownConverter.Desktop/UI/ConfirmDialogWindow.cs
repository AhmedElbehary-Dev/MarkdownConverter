using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace MarkdownConverter.Desktop.UI;

public sealed class ConfirmDialogWindow : Window
{
    private bool _result;

    public ConfirmDialogWindow(string title, string message)
    {
        Width = 560;
        MinWidth = 420;
        Height = 260;
        MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = true;
        Title = "Update Available";
        
        var accent = new SolidColorBrush(Color.Parse("#18AEB6"));

        var okButton = new Button
        {
            Content = "Yes",
            MinWidth = 88,
            Margin = new Thickness(0, 0, 12, 0)
        };

        var cancelButton = new Button
        {
            Content = "No",
            MinWidth = 88
        };

        okButton.Click += (_, _) =>
        {
            _result = true;
            Close();
        };

        cancelButton.Click += (_, _) =>
        {
            _result = false;
            Close();
        };

        Content = new Border
        {
            Padding = new Thickness(16),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = accent,
                        FontSize = 18,
                        Margin = new Thickness(0, 0, 0, 12)
                    },
                    new ScrollViewer
                    {
                        [Grid.RowProperty] = 1,
                        Margin = new Thickness(0, 0, 0, 12),
                        Content = new TextBlock
                        {
                            Text = message,
                            TextWrapping = TextWrapping.Wrap
                        }
                    },
                    new StackPanel
                    {
                        [Grid.RowProperty] = 2,
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children =
                        {
                            okButton,
                            cancelButton
                        }
                    }
                }
            }
        };
    }

    public new Task<bool> ShowDialog(Window owner)
    {
        var tcs = new TaskCompletionSource<bool>();
        Closed += (_, _) => tcs.TrySetResult(_result);
        base.ShowDialog(owner);
        return tcs.Task;
    }
}
