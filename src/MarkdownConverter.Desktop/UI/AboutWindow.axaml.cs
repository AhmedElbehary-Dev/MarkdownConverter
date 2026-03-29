using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Reflection;

namespace MarkdownConverter.Desktop.UI
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            AvaloniaXamlLoader.Load(this);
            SetVersionText();
        }

        private void SetVersionText()
        {
            var versionBlock = this.FindControl<TextBlock>("VersionText");
            if (versionBlock is null)
            {
                return;
            }

            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            if (string.IsNullOrWhiteSpace(version))
            {
                version = asm.GetName().Version?.ToString(3);
            }
            else
            {
                int plusIndex = version.IndexOf('+');
                if (plusIndex > 0)
                {
                    version = version.Substring(0, plusIndex);
                }
            }

            versionBlock.Text = string.IsNullOrEmpty(version) ? "v1.0.0" : (version.StartsWith("v") ? version : "v" + version);
        }

        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GithubLink_Click(object? sender, RoutedEventArgs e)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/AhmedElbehary-Dev",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
    }
}
