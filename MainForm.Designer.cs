using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MarkdownConverter;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Text = "Markdown Converter Pro";

        var iconPath = Path.Combine(AppContext.BaseDirectory, "img", "md_converter.ico");
        if (File.Exists(iconPath))
        {
            Icon = new Icon(iconPath);
        }
    }

    #endregion
}
