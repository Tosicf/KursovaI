using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StroyControl.App.Services;

/// <summary>
/// Сохранение печатной формы в PDF без сторонних библиотек: страницы FlowDocument
/// отрисовываются в изображения (200 dpi) и упаковываются в PDF собственной сборкой.
/// В отличие от печати через «Microsoft Print to PDF» файл пишется сразу по выбранному
/// пути, поэтому имя файла подставляется автоматически и лишних диалогов нет.
/// </summary>
public static class PdfExporter
{
    private const double RenderDpi = 200d;

    private const double PointsPerInch = 72d;
    private const double DipPerInch = 96d;

    /// <summary>Сохраняет документ в PDF. Возвращает false и текст ошибки при сбое.</summary>
    public static bool TryExport(FlowDocument document, string filePath, out string? error)
    {
        error = null;

        try
        {
            var bytes = Build(document);
            File.WriteAllBytes(filePath, bytes);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Рендерит документ в PDF и возвращает его содержимое.</summary>
    public static byte[] Build(FlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (double.IsNaN(document.PageWidth) || document.PageWidth <= 0) document.PageWidth = 794;
        if (double.IsNaN(document.PageHeight) || document.PageHeight <= 0) document.PageHeight = 1123;

        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        paginator.PageSize = new Size(document.PageWidth, document.PageHeight);
        paginator.ComputePageCount();

        var pageCount = Math.Max(1, paginator.PageCount);
        var pages = new List<byte[]>(pageCount);
        var pixelWidth = 0;
        var pixelHeight = 0;

        for (var i = 0; i < pageCount; i++)
        {
            var page = paginator.GetPage(i);
            var image = RenderPage(page.Visual, document.PageWidth, document.PageHeight, out pixelWidth, out pixelHeight);
            pages.Add(image);
        }

        return WritePdf(pages, pixelWidth, pixelHeight, document.PageWidth, document.PageHeight);
    }

    // ------------------------------------------------------------------
    //  Растеризация страницы
    // ------------------------------------------------------------------

    private static byte[] RenderPage(Visual page, double width, double height, out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = Math.Max(1, (int)Math.Ceiling(width * RenderDpi / DipPerInch));
        pixelHeight = Math.Max(1, (int)Math.Ceiling(height * RenderDpi / DipPerInch));

        var target = new RenderTargetBitmap(pixelWidth, pixelHeight, RenderDpi, RenderDpi, PixelFormats.Pbgra32);

        var canvas = new DrawingVisual();
        using (var context = canvas.RenderOpen())
        {
            var area = new Rect(0, 0, width, height);
            context.DrawRectangle(Brushes.White, null, area);

            // Страница кладётся на белый фон 1:1 — иначе прозрачность уйдёт в чёрный.
            var brush = new VisualBrush(page)
            {
                Stretch = Stretch.Fill,
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = area,
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = area
            };

            context.DrawRectangle(brush, null, area);
        }

        target.Render(canvas);

        var encoder = new JpegBitmapEncoder { QualityLevel = 88 };
        encoder.Frames.Add(BitmapFrame.Create(target));

        using var buffer = new MemoryStream();
        encoder.Save(buffer);
        return buffer.ToArray();
    }

    // ------------------------------------------------------------------
    //  Сборка PDF
    // ------------------------------------------------------------------

    private static byte[] WritePdf(IReadOnlyList<byte[]> pages, int pixelWidth, int pixelHeight,
        double pageWidthDip, double pageHeightDip)
    {
        var pageWidth = pageWidthDip / DipPerInch * PointsPerInch;
        var pageHeight = pageHeightDip / DipPerInch * PointsPerInch;

        var count = pages.Count;
        var firstPageObject = 3;
        var firstContentObject = firstPageObject + count;
        var firstImageObject = firstContentObject + count;

        using var stream = new MemoryStream();
        var offsets = new List<long> { 0 }; // объект 0 — свободный

        void Write(string text) => stream.Write(Encoding.ASCII.GetBytes(text));

        void BeginObject(int number)
        {
            while (offsets.Count < number) offsets.Add(0);
            offsets.Add(stream.Position);
            Write($"{number} 0 obj\n");
        }

        // Маркер бинарного файла: без него часть просмотрщиков считает PDF текстовым.
        Write("%PDF-1.4\n");
        stream.Write(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A });

        BeginObject(1);
        Write("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        var kids = new StringBuilder();
        for (var i = 0; i < count; i++)
            kids.Append($"{firstPageObject + i} 0 R ");

        BeginObject(2);
        Write($"<< /Type /Pages /Kids [{kids.ToString().TrimEnd()}] /Count {count} >>\nendobj\n");

        for (var i = 0; i < count; i++)
        {
            BeginObject(firstPageObject + i);
            Write($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Num(pageWidth)} {Num(pageHeight)}] " +
                  $"/Resources << /XObject << /Im0 {firstImageObject + i} 0 R >> /ProcSet [/PDF /ImageC] >> " +
                  $"/Contents {firstContentObject + i} 0 R >>\nendobj\n");
        }

        var content = Encoding.ASCII.GetBytes($"q {Num(pageWidth)} 0 0 {Num(pageHeight)} 0 0 cm /Im0 Do Q\n");
        for (var i = 0; i < count; i++)
        {
            BeginObject(firstContentObject + i);
            Write($"<< /Length {content.Length} >>\nstream\n");
            stream.Write(content);
            Write("endstream\nendobj\n");
        }

        for (var i = 0; i < count; i++)
        {
            var image = pages[i];
            BeginObject(firstImageObject + i);
            Write($"<< /Type /XObject /Subtype /Image /Width {pixelWidth} /Height {pixelHeight} " +
                  "/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode " +
                  $"/Length {image.Length} >>\nstream\n");
            stream.Write(image);
            Write("\nendstream\nendobj\n");
        }

        var xrefOffset = stream.Position;
        var total = firstImageObject + count;

        Write($"xref\n0 {total}\n");
        Write("0000000000 65535 f \n");
        for (var i = 1; i < total; i++)
            Write($"{offsets[i]:D10} 00000 n \n");

        Write($"trailer\n<< /Size {total} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");

        return stream.ToArray();
    }

    private static string Num(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}
