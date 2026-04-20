using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MarkdownConverter.ViewModels;

namespace MarkdownConverter.UI;

public sealed class MessageDialogWindow : Window
{
    public MessageDialogWindow(string message, ToastKind kind)
    {
        Width = 420;
        Height = 240;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        Background = Brushes.Transparent;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };

        var isError = kind == ToastKind.Error;
        var accentColor = isError ? Color.Parse("#FF453A") : Color.Parse("#30D158");
        var accentBrush = new SolidColorBrush(accentColor);
        var iconText = isError ? "✕" : "✓";
        var titleText = isError ? "Error" : "Success";

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = accentBrush,
            Foreground = Brushes.White,
            FontWeight = FontWeight.Medium,
            FontSize = 14,
            Padding = new Thickness(32, 8),
            CornerRadius = new CornerRadius(8),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        okButton.Click += (_, _) => Close();

        var copyButton = new Button
        {
            Content = "📋 Copy",
            Background = new SolidColorBrush(Color.Parse("#3A3A3C")),
            Foreground = Brushes.White,
            FontWeight = FontWeight.Medium,
            FontSize = 14,
            Padding = new Thickness(16, 8),
            CornerRadius = new CornerRadius(8),
            Cursor = new Cursor(StandardCursorType.Hand),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        copyButton.Click += async (_, _) => 
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(message);
                var originalContent = copyButton.Content;
                copyButton.Content = "✓ Copied";
                await System.Threading.Tasks.Task.Delay(2000);
                copyButton.Content = originalContent;
            }
        };

        var messageText = new SelectableTextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#EBEBF5"), 0.6),
            FontSize = 14,
            LineHeight = 22
        };

        Content = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1C1C1E")),
            CornerRadius = new CornerRadius(16),
            BorderBrush = new SolidColorBrush(Color.Parse("#3A3A3C")),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(16),
            Padding = new Thickness(24, 24, 24, 20),
            BoxShadow = BoxShadows.Parse("0 12 30 0 #40000000"),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto, *, Auto"),
                Children =
                {
                    // Header
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 0, 0, 16),
                        Spacing = 12,
                        Children =
                        {
                            new Border
                            {
                                Width = 36,
                                Height = 36,
                                CornerRadius = new CornerRadius(18),
                                Background = new SolidColorBrush(accentColor, 0.15),
                                Child = new TextBlock
                                {
                                    Text = iconText,
                                    Foreground = accentBrush,
                                    FontSize = 18,
                                    FontWeight = FontWeight.Bold,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center
                                }
                            },
                            new TextBlock
                            {
                                Text = titleText,
                                FontWeight = FontWeight.SemiBold,
                                Foreground = Brushes.White,
                                FontSize = 20,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    },
                    // Message
                    new ScrollViewer
                    {
                        [Grid.RowProperty] = 1,
                        Margin = new Thickness(0, 0, 0, 16),
                        Content = messageText
                    },
                    // Buttons
                    new Grid
                    {
                        [Grid.RowProperty] = 2,
                        Children =
                        {
                            copyButton,
                            okButton
                        }
                    }
                }
            }
        };

        if (Content is Border { Child: Grid grid })
        {
            // Enable dragging the custom window
            grid.PointerPressed += (sender, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    BeginMoveDrag(e);
                }
            };
        }
    }
}
