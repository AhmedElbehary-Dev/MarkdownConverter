using System.Windows;
using System.Windows.Controls;

namespace MarkdownConverter.Controls
{
    public class Card : HeaderedContentControl
    {
        static Card()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(Card),
                new FrameworkPropertyMetadata(typeof(Card)));
        }
    }
}
