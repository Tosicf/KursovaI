using System.Globalization;
using System.IO.Compression;
using System.Text;
using StroyControl.Core.Dtos;

namespace StroyControl.Core.Services;

/// <summary>
/// Экспорт отчёта по смете в Excel (.xlsx) и CSV без сторонних библиотек:
/// книга собирается как минимальный набор OOXML-частей (zip-архив).
/// </summary>
public sealed class EstimateReportExporter
{
    private const int StyleDefault = 0;
    private const int StyleBold = 1;
    private const int StyleTitle = 2;
    private const int StyleHeader = 3;
    private const int StyleText = 4;
    private const int StyleMoney = 5;
    private const int StyleSection = 6;
    private const int StyleSubtotal = 7;
    private const int StyleCenter = 8;

    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    /// <summary>Имя файла по умолчанию: «Смета_СМ-24-03_20261105_1830.xlsx».</summary>
    public string DefaultFileName(EstimateReport report, string extension = "xlsx") =>
        $"Смета_{Sanitize(report.Number)}_{report.GeneratedAt:yyyyMMdd_HHmm}.{extension}";

    /// <summary>Табличный CSV: открывается в Excel, разделитель «;», кодировка UTF-8 с BOM.</summary>
    public string BuildCsv(EstimateReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Расчёт количества и стоимости работ и материалов;{Esc(report.Number)};{Esc(report.Name)}");
        sb.AppendLine($"Объект;{Esc(report.ProjectName)};{Esc(report.ProjectAddress)}");
        sb.AppendLine($"Сформировано;{report.GeneratedAt:dd.MM.yyyy HH:mm}");

        sb.AppendLine($"Всего затрат;{Num(report.TotalCost)}");
        sb.AppendLine($"Стоимость материалов;{Num(report.MaterialsCost)}");
        sb.AppendLine($"Стоимость работ;{Num(report.WorksCost)}");
        sb.AppendLine($"Зарплата рабочих;{Num(report.LaborCost)}");
        sb.AppendLine($"Прибыль;{Num(report.Profit)}");
        sb.AppendLine($"S строения общая, м2;{Num(report.Area)}");
        sb.AppendLine($"Стоимость 1 м2;{Num(report.CostPerSquareMeter)}");
        sb.AppendLine($"Прибыль от 1 м2;{Num(report.ProfitPerSquareMeter)}");
        sb.AppendLine($"Наценка;{Num(report.MarkupPercent)}");
        sb.AppendLine($"Прибыль, %;{Num(report.ProfitPercent)}");
        sb.AppendLine();

        sb.AppendLine("№;Наименование работ и затрат;ед. изм.;кол-во;Цена ед. изм.;Цена с наценкой;Итого;Прибыль;ЗП рабочих");

        var order = 1;
        foreach (var group in report.Groups)
        {
            sb.AppendLine($"{order++};;;;;;{Num(group.Total)};{Num(group.Profit)};{Num(group.LaborCost)}");

            if (group.HasWorks)
            {
                sb.AppendLine(";Работы;;;;;;;");
                foreach (var row in group.Works) sb.AppendLine(RowLine(row));
                sb.AppendLine($";Итого работы;;;;;{Num(group.WorksTotal)};;{Num(group.Works.Sum(r => r.Profit))};{Num(group.LaborCost)}");
            }

            if (group.HasMaterials)
            {
                sb.AppendLine(";Материалы;;;;;;;");
                foreach (var row in group.Materials) sb.AppendLine(RowLine(row));
                sb.AppendLine($";Итого материалы;;;;;{Num(group.MaterialsTotal)};;{Num(group.Materials.Sum(r => r.Profit))};0,00");
            }
        }

        sb.AppendLine();
        sb.AppendLine($";Итого по смете;;;;;{Num(report.DirectCost)};;{Num(report.Profit)};{Num(report.LaborCost)}");
        return sb.ToString();
    }

    public async Task ExportCsvAsync(EstimateReport report, string filePath, CancellationToken cancellationToken = default)
    {
        var csv = BuildCsv(report);
        await File.WriteAllTextAsync(filePath, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), cancellationToken);
    }

    /// <summary>Сохраняет книгу Excel: шапка с итогами, разделы, подытоги, колонки отчёта.</summary>
    public async Task ExportXlsxAsync(EstimateReport report, string filePath, CancellationToken cancellationToken = default)
    {
        var workbook = BuildWorkbook(report);
        await File.WriteAllBytesAsync(filePath, workbook, cancellationToken);
    }

    /// <summary>Собирает .xlsx в памяти (архив OOXML).</summary>
    public byte[] BuildWorkbook(EstimateReport report)
    {
        var sheetXml = BuildSheetXml(report, out var columnWidths, out var merges);

        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "[Content_Types].xml", ContentTypes());
            AddEntry(zip, "_rels/.rels", RootRels());
            AddEntry(zip, "xl/workbook.xml", Workbook());
            AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
            AddEntry(zip, "xl/styles.xml", Styles());
            AddEntry(zip, "xl/worksheets/sheet1.xml", Worksheet(sheetXml, columnWidths, merges));
        }

        return stream.ToArray();
    }

    // ------------------------------------------------------------------
    //  Лист книги
    // ------------------------------------------------------------------

    private static string BuildSheetXml(EstimateReport report, out List<double> widths, out List<string> merges)
    {
        var sb = new StringBuilder();
        merges = new List<string> { "A1:I1", "A2:I2", "A3:I3", "A4:I4" };

        AppendRow(sb, 1, Text("A", 1, "РАСЧЁТ КОЛИЧЕСТВА И СТОИМОСТИ РАБОТ И МАТЕРИАЛОВ", StyleTitle));
        AppendRow(sb, 2, Text("A", 2, $"Смета {report.Number} · {report.Name}", StyleBold));
        AppendRow(sb, 3, Text("A", 3, string.Join(", ", new[] { report.ProjectName, report.ProjectAddress }.Where(s => !string.IsNullOrWhiteSpace(s))), StyleText));
        AppendRow(sb, 4, Text("A", 4, $"Сформировано {report.GeneratedAt:dd.MM.yyyy HH:mm} · {report.StatusDisplay}", StyleText));

        // Шапка с итогами: слева затраты, справа площадь и удельные показатели.
        AppendRow(sb, 6,
            Text("A", 6, "Всего затрат, BYN", StyleBold),
            Money("B", 6, report.TotalCost),
            Text("D", 6, "S строения общая, м²", StyleBold),
            Num("E", 6, report.Area));

        AppendRow(sb, 7,
            Text("A", 7, "Стоимость материалов, BYN", StyleText), Money("B", 7, report.MaterialsCost),
            Text("D", 7, "Стоимость 1 м², BYN", StyleText), Money("E", 7, report.CostPerSquareMeter));

        AppendRow(sb, 8,
            Text("A", 8, "Стоимость работ, BYN", StyleText), Money("B", 8, report.WorksCost),
            Text("D", 8, "Прибыль от 1 м², BYN", StyleText), Money("E", 8, report.ProfitPerSquareMeter));

        AppendRow(sb, 9,
            Text("A", 9, "Зарплата рабочих, BYN", StyleText), Money("B", 9, report.LaborCost),
            Text("D", 9, "Наценка, %", StyleText), Num("E", 9, report.MarkupPercent));

        AppendRow(sb, 10,
            Text("A", 10, "Прибыль, BYN", StyleText), Money("B", 10, report.Profit),
            Text("D", 10, "Прибыль, %", StyleText), Num("E", 10, report.ProfitPercent));

        AppendRow(sb, 11,
            Text("A", 11, $"Позиций в смете: {report.PositionsCount}", StyleText),
            Text("D", 11, "Доля ЗП рабочих, %", StyleText), Num("E", 11, report.LaborSharePercent));

        // Заголовок таблицы.
        const int headerRow = 13;
        AppendRow(sb, headerRow,
            Text("A", headerRow, "№", StyleHeader),
            Text("B", headerRow, "Наименование работ и затрат", StyleHeader),
            Text("C", headerRow, "ед. изм.", StyleHeader),
            Text("D", headerRow, "кол-во", StyleHeader),
            Text("E", headerRow, "Цена ед. изм.", StyleHeader),
            Text("F", headerRow, "Цена с наценкой", StyleHeader),
            Text("G", headerRow, "Итого", StyleHeader),
            Text("H", headerRow, "Прибыль", StyleHeader),
            Text("I", headerRow, "ЗП рабочих", StyleHeader));

        var current = headerRow + 1;
        var sectionNumber = 1;

        foreach (var group in report.Groups)
        {
            AppendRow(sb, current,
                Text("A", current, $"{sectionNumber++}. {group.Section}", StyleSection),
                Money("G", current, group.Total, StyleSection),
                Money("H", current, group.Profit, StyleSection),
                Money("I", current, group.LaborCost, StyleSection));
            merges.Add($"A{current}:F{current}");
            current++;

            if (group.HasWorks)
            {
                AppendRow(sb, current, Text("A", current, "Работы", StyleSubtotal));
                merges.Add($"A{current}:I{current}");
                current++;

                foreach (var item in group.Works)
                {
                    AppendRow(sb, current, RowCells(item, current));
                    current++;
                }
            }

            if (group.HasMaterials)
            {
                AppendRow(sb, current, Text("A", current, "Материалы", StyleSubtotal));
                merges.Add($"A{current}:I{current}");
                current++;

                foreach (var item in group.Materials)
                {
                    AppendRow(sb, current, RowCells(item, current));
                    current++;
                }
            }

            AppendRow(sb, current,
                Text("A", current, $"Итого по разделу «{group.Section}»", StyleSubtotal),
                Money("G", current, group.Total, StyleSubtotal),
                Money("H", current, group.Profit, StyleSubtotal),
                Money("I", current, group.LaborCost, StyleSubtotal));
            merges.Add($"A{current}:F{current}");
            current++;
        }

        AppendRow(sb, current,
            Text("A", current, "ИТОГО ПО СМЕТЕ", StyleSection),
            Money("G", current, report.DirectCost, StyleSection),
            Money("H", current, report.Profit, StyleSection),
            Money("I", current, report.LaborCost, StyleSection));
        merges.Add($"A{current}:F{current}");

        widths = new List<double> { 6, 52, 10, 10, 14, 16, 16, 14, 14 };
        return sb.ToString();
    }

    private static string RowCells(EstimateReportRow item, int row) =>
        string.Concat(
            Text("A", row, item.Number.ToString(Russian), StyleCenter),
            Text("B", row, item.Name, StyleText),
            Text("C", row, item.Unit, StyleCenter),
            Num("D", row, item.Quantity),
            Money("E", row, item.UnitPrice),
            Money("F", row, item.PriceWithMarkup),
            Money("G", row, item.Total),
            Money("H", row, item.Profit),
            Money("I", row, item.LaborCost));

    private static void AppendRow(StringBuilder sb, int rowNumber, params string[] cells)
    {
        sb.Append($"<row r=\"{rowNumber}\">");
        foreach (var cell in cells)
            sb.Append(cell);
        sb.Append("</row>");
    }

    // ------------------------------------------------------------------
    //  Ячейки
    // ------------------------------------------------------------------

    private static string Text(string column, int row, string? value, int style)
    {
        var reference = $"{column}{row}";
        return $"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Xml(value)}</t></is></c>";
    }

    private static string Money(string column, int row, decimal value, int style = StyleMoney) => Num(column, row, value, style);

    private static string Num(string column, int row, decimal value, int style = StyleMoney)
    {
        var reference = $"{column}{row}";
        return $"<c r=\"{reference}\" s=\"{style}\"><v>{value.ToString("0.00", CultureInfo.InvariantCulture)}</v></c>";
    }

    private static string RowLine(EstimateReportRow row) => string.Join(';',
        row.Number.ToString(Russian),
        Esc(row.Name),
        Esc(row.Unit),
        Num(row.Quantity),
        Num(row.UnitPrice),
        Num(row.PriceWithMarkup),
        Num(row.Total),
        Num(row.Profit),
        Num(row.LaborCost));

    // ------------------------------------------------------------------
    //  Части OOXML
    // ------------------------------------------------------------------

    private static void AddEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string Worksheet(string rows, IReadOnlyList<double> widths, IReadOnlyList<string> merges)
    {
        var cols = new StringBuilder();
        for (var i = 0; i < widths.Count; i++)
            cols.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{widths[i].ToString(CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");

        var mergeXml = new StringBuilder();
        if (merges.Count > 0)
        {
            mergeXml.Append($"<mergeCells count=\"{merges.Count}\">");
            foreach (var merge in merges)
                mergeXml.Append($"<mergeCell ref=\"{merge}\"/>");
            mergeXml.Append("</mergeCells>");
        }

        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
               "<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>" +
               "<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"13\" topLeftCell=\"A14\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" +
               "<sheetFormatPr defaultRowHeight=\"15\"/>" +
               $"<cols>{cols}</cols><sheetData>{rows}</sheetData>{mergeXml}" +
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

    private static string Workbook() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
        "<sheets><sheet name=\"Смета\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";

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
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFCFE0F5\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFEEF2F8\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
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
        "<xf numFmtId=\"0\" fontId=\"3\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\">" +
        "<alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"164\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"164\" fontId=\"1\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\"/></xf>" +
        "</cellXfs>" +
        "</styleSheet>";

    // ------------------------------------------------------------------
    //  Вспомогательное
    // ------------------------------------------------------------------

    private static string Xml(string? value) => (value ?? string.Empty)
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");

    private static string Num(decimal value) => value.ToString("0.00", Russian);

    private static string Esc(string? value) => (value ?? string.Empty)
        .Replace(';', ',')
        .Replace('\n', ' ')
        .Replace('\r', ' ');

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "смета";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
        return cleaned.Trim('_');
    }
}
