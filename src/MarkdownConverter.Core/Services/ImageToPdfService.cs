using System;
using System.Collections.Generic;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace MarkdownConverter.Services
{
    public class ImageToPdfService
    {
        static ImageToPdfService()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public void CombineImagesToPdf(IEnumerable<string> imagePaths, string outputPath)
        {
            using var document = new PdfDocument();

            foreach (var imagePath in imagePaths)
            {
                var page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                page.Orientation = PdfSharp.PageOrientation.Portrait;

                using var xImage = XImage.FromFile(imagePath);
                using var gfx = XGraphics.FromPdfPage(page);

                double width = xImage.PixelWidth;
                double height = xImage.PixelHeight;
                double pageW = page.Width.Point;
                double pageH = page.Height.Point;

                double scale = Math.Min(pageW / width, pageH / height);

                double drawWidth = width * scale;
                double drawHeight = height * scale;

                double x = (pageW - drawWidth) / 2;
                double y = (pageH - drawHeight) / 2;

                gfx.DrawImage(xImage, x, y, drawWidth, drawHeight);
            }

            document.Save(outputPath);
        }
    }
}
