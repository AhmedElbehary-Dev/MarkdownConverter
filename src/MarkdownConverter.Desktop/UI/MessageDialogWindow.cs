using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MarkdownConverter.ViewModels;

namespace MarkdownConverter.UI;

public sealed class MessageDialogWindow : Window
{
    public MessageDialogWindow(string message, ToastKind kind)
    {
        Width = 560;
        MinWidth = 420;
        Height = 260;
        MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = true;
        Title = kind == ToastKind.Error ? "Error" : "Success";

        var accent = kind == ToastKind.Error
            ? new SolidColorBrush(Color.Parse("#B91C1C"))
            : new SolidColorBrush(Color.Parse("#15803D"));

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
                        Text = Title,
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
                            new Button
                            {
                                Content = "OK",
                                MinWidth = 88
                            }
                        }
                    }
                }
            }
        };

        if (Content is Border { Child: Grid grid } &&
            grid.Children.Count > 2 &&
            grid.Children[2] is StackPanel { Children.Count: > 0 } buttonPanel &&
            buttonPanel.Children[0] is Button okButton)
        {
            okButton.Click += (_, _) => Close();
        }
    }
}
