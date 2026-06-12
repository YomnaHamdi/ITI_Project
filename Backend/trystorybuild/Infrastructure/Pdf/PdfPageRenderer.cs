using Application.Interfaces;
using Microsoft.Extensions.Logging;
using PDFtoImage;
using SkiaSharp;

namespace Infrastructure.Pdf;

public class PdfPageRenderer(ILogger<PdfPageRenderer> logger) : IPdfPageRenderer
{
    public Task<List<string>> RenderPagesAsync(
        string pdfPath,
        string outputDirectory,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var pdfBytes = File.ReadAllBytes(pdfPath);
        var pageCount = Conversion.GetPageCount(pdfBytes, password: null);

        logger.LogInformation("[PDF] Rendering {Count} pages from {Path}", pageCount, pdfPath);

        var paths = new List<string>();

        for (var i = 0; i < pageCount; i++)
        {
            ct.ThrowIfCancellationRequested();

            using var bitmap = Conversion.ToImage(
                pdfBytes,
                page: i,
                options: new(Dpi: 200, WithAnnotations: true, WithFormFill: true));

            var fileName = $"page_{i + 1}.png";
            var fullPath = Path.Combine(outputDirectory, fileName);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(fullPath);
            data.SaveTo(stream);

            paths.Add(fullPath);
            logger.LogInformation("[PDF] Saved page {Page} → {Path}", i + 1, fullPath);
        }

        return Task.FromResult(paths);
    }
}
