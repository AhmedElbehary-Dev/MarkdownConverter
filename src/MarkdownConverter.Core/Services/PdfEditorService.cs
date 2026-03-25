using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Docnet.Core;
using Docnet.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using MarkdownConverter.Models;

namespace MarkdownConverter.Services
{
    public class PdfEditorService
    {
        static PdfEditorService()
        {
            // PdfSharp requires this for .NET Core / .NET 5+ to handle older encodings
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public int GetPageCount(string filePath)
        {
            using var docReader = DocLib.Instance.GetDocReader(filePath, new PageDimensions(1080, 1920));
            return docReader.GetPageCount();
        }

        public PdfPageImage GetPageThumbnail(string filePath, int pageIndex)
        {
            // Use Docnet.Core to read the page and generate a thumbnail
            // Use a reasonable dimension to constrain memory for thumbnails
            using var docReader = DocLib.Instance.GetDocReader(filePath, new PageDimensions(800, 800));
            using var pageReader = docReader.GetPageReader(pageIndex);
            
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();
            var rawBytes = pageReader.GetImage(); // Returns BGRA32 byte array

            return new PdfPageImage
            {
                PageNumber = pageIndex + 1,
                BgraPixels = rawBytes,
                Width = width,
                Height = height
            };
        }

        public string DeletePagesAndExport(string inputFilePath, IEnumerable<int> pagesToDelete1Based)
        {
            var deleteSet = new HashSet<int>(pagesToDelete1Based);
            
            if (deleteSet.Count == 0)
            {
                throw new ArgumentException("No pages selected for deletion.");
            }

            var directory = Path.GetDirectoryName(inputFilePath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputFilePath);
            var ext = Path.GetExtension(inputFilePath);

            var deletedPagesStr = string.Join("_", deleteSet.OrderBy(p => p));
            if (deletedPagesStr.Length > 50) 
            {
                // Truncate to avoid too long file names
                deletedPagesStr = deletedPagesStr.Substring(0, 50) + "_etc";
            }

            var newFileName = $"{fileNameWithoutExt}_deleted_{deletedPagesStr}{ext}";
            var outputFilePath = Path.Join(directory, newFileName);

            using (var inputDocument = PdfReader.Open(inputFilePath, PdfDocumentOpenMode.Import))
            using (var outputDocument = new PdfDocument())
            {
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    int pageNumber1Based = i + 1;
                    if (!deleteSet.Contains(pageNumber1Based))
                    {
                        outputDocument.AddPage(inputDocument.Pages[i]);
                    }
                }
                
                outputDocument.Save(outputFilePath);
            }

            return outputFilePath;
        }
    }
}
