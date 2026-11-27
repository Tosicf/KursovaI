using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace StroyControl.Core.Services;

/// <summary>Стиль ячейки в книге Excel.</summary>
public enum XlsxStyle
{
    Default = 0,
    Bold = 1,
    Title = 2,
    Header = 3,
    Text = 4,
    Money = 5,
    Section = 6,
    Subtotal = 7,
    Center = 8
}

/// <summary>Ячейка книги: значение, стиль и (для заголовков разделов) объединение столбцов.</summary>
public readonly record struct XlsxCell(string? Text, decimal? Number, XlsxStyle Style, int Span)
{
    public static XlsxCell Of(string? text, XlsxStyle style = XlsxStyle.Text) => new(text, null, style, 1);

    public static XlsxCell Of(decimal number, XlsxStyle style = XlsxStyle.Money) => new(null, number, style, 1);

    /// <summary>Текст, растянутый на несколько столбцов (заголовок раздела, итог).</summary>
    public static XlsxCell Spanning(string? text, int span, XlsxStyle style = XlsxStyle.Section) => new(text, null, style, span);
}

/// <summary>
/// Минимальная книга Excel (.xlsx) без сторонних библиотек: собирается как набор
/// OOXML-частей в zip-архиве. Одного листа и девяти стилей хватает на все отчёты
/// приложения (смета, отклонения, счёт-фактура).
/// </summary>
public sealed class XlsxBuilder
{
    private readonly StringBuilder _rows = new();
    private readonly List<string> _merges = new();
    private readonly List<double> _widths;
    private readonly string _sheetName;
    private readonly int _frozenRows;
    private int _row;

    public XlsxBuilder(string sheetName, IReadOnlyList<double> widths, int frozenRows = 0)
    {
        if (string.IsNullOrWhiteSpace(sheetName)) throw new ArgumentException("Имя листа не задано.", nameof(sheetName));

        _sheetName = sheetName.Length > 31 ? sheetName[..31] : sheetName;
        _widths = widths.ToList();
        _frozenRows = frozenRows;
    }

    public int RowCount => _row;

    public void AddRow(params XlsxCell[] cells) => AddRow((IEnumerable<XlsxCell>)cells);

    public void AddRow(IEnumerable<XlsxCell> cells)
    {
        var list = cells as IReadOnlyList<XlsxCell> ?? cells.ToList();

        _row++;
        var column = 0;
        var xml = new StringBuilder();

        foreach (var cell in list)
        {
            var span = Math.Max(1, cell.Span);
            var reference = $"{ColumnName(column)}{_row}";

            if (cell.Number.HasValue)
            {
                xml.Append($"<c r=\"{reference}\" s=\"{(int)cell.Style}\"><v>" +
                           cell.Number.Value.ToString("0.00", CultureInfo.InvariantCulture) + "</v></c>");
            }
            else
            {
                xml.Append($"<c r=\"{reference}\" s=\"{(int)cell.Style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">" +
                           Xml(cell.Text) + "</t></is></c>");
            }

            if (span > 1)
            {
                _merges.Add($"{reference}:{ColumnName(column + span - 1)}{_row}");

                // Пустые ячейки внутри объединения нужны, чтобы границы выглядели ровно.
                for (var i = 1; i < span; i++)
                {
                    xml.Append($"<c r=\"{ColumnName(column + i)}{_row}\" s=\"{(int)cell.Style}\"/>");
                }
            }

            column += span;
        }

        var height = list.Any(c => c.Style is XlsxStyle.Section or XlsxStyle.Subtotal)
            ? " ht=\"20\" customHeight=\"1\""
            : string.Empty;

        _rows.Append($"<row r=\"{_row}\"{height}>{xml}</row>");
    }

    /// <summary>Пустая строка-разделитель.</summary>
    public void AddEmptyRow() => AddRow(Array.Empty<XlsxCell>());

    public byte[] Build()
    {
        using var buffer = new MemoryStream();

        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "[Content_Types].xml", ContentTypes());
            AddEntry(zip, "_rels/.rels", RootRels());
            AddEntry(zip, "xl/workbook.xml", Workbook());
            AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
            AddEntry(zip, "xl/styles.xml", Styles());
            AddEntry(zip, "xl/worksheets/sheet1.xml", Worksheet());
        }

        return buffer.ToArray();
    }

    // ------------------------------------------------------------------

    private string Worksheet()
    {
        var cols = new StringBuilder();
        for (var i = 0; i < _widths.Count; i++)
        {
            cols.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{_widths[i].ToString(CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
        }

        var mergeXml = new StringBuilder();
        if (_merges.Count > 0)
        {
            mergeXml.Append($"<mergeCells count=\"{_merges.Count}\">");
            foreach (var merge in _merges)
                mergeXml.Append($"<mergeCell ref=\"{merge}\"/>");
            mergeXml.Append("</mergeCells>");
        }

        var frozen = _frozenRows > 0
            ? $"<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"{_frozenRows}\" topLeftCell=\"A{_frozenRows + 1}\" " +
              "activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>"
            : "<sheetViews><sheetView workbookViewId=\"0\"/></sheetViews>";

        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
               "<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>" + frozen +
               "<sheetFormatPr defaultRowHeight=\"15\"/>" +
               $"<cols>{cols}</cols><sheetData>{_rows}</sheetData>{mergeXml}" +
               "<pageMargins left=\"0.5\" right=\"0.4\" top=\"0.5\" bottom=\"0.5\" header=\"0.3\" footer=\"0.3\"/>" +
               "</worksheet>";
    }

    private static string ContentTypes() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
        "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
        "</Types>";

    private static string RootRels() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>";

    private string Workbook() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
        $"<sheets><sheet name=\"{Xml(_sheetName)}\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";

    private static string WorkbookRels() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
        "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
        "</Relationships>";

    private static string Styles() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"#,##0.00\"/></numFmts>" +
        "<fonts count=\"5\">" +
        "<font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
        "<font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
        "<font><b/><sz val=\"14\"/><name val=\"Calibri\"/></font>" +
        "<font><b/><sz val=\"10.5\"/><name val=\"Calibri\"/></font>" +
        "<font><sz val=\"10\"/><name val=\"Calibri\"/></font>" +
        "</fonts>" +
        "<fills count=\"4\">" +
        "<fill><patternFill patternType=\"none\"/></fill>" +
        "<fill><patternFill patternType=\"gray125\"/></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFEEE8FD\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFD9CCFA\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
        "</fills>" +
        "<borders count=\"2\">" +
        "<border><left/><right/><top/><bottom/><diagonal/></border>" +
        "<border><left style=\"thin\"><color rgb=\"FF93A1B5\"/></left><right style=\"thin\"><color rgb=\"FF93A1B5\"/></right>" +
        "<top style=\"thin\"><color rgb=\"FF93A1B5\"/></top><bottom style=\"thin\"><color rgb=\"FF93A1B5\"/></bottom><diagonal/></border>" +
        "</borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"9\">" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
        "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"3\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\">" +
        "<alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"164\" fontId=\"1\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"164\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\"/></xf>" +
        "</cellXfs>" +
        "</styleSheet>";

    private static void AddEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    /// <summary>Имя столбца по индексу: 0 → A, 25 → Z, 26 → AA.</summary>
    private static string ColumnName(int index)
    {
        var name = string.Empty;
        index++;
        while (index > 0)
        {
            var remainder = (index - 1) % 26;
            name = (char)('A' + remainder) + name;
            index = (index - 1) / 26;
        }

        return name;
    }

    private static string Xml(string? value) => (value ?? string.Empty)
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");
}
