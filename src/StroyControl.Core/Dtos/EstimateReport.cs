using StroyControl.Core.Entities;

namespace StroyControl.Core.Dtos;

/// <summary>
/// Строка отчёта по смете: материал или работа с наценкой, прибылью и зарплатой рабочих.
/// Расчёт выполняется в <see cref="EstimateReportBuilder"/> — без обращения к базе данных,
/// поэтому логику легко покрыть модульными тестами.
/// </summary>
public sealed class EstimateReportRow
{
    public int Number { get; init; }

    public string Section { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    /// <summary>Цена из сметы (плановая цена за единицу).</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Цена с наценкой: <see cref="UnitPrice"/> × (1 + наценка / 100).</summary>
    public decimal PriceWithMarkup { get; init; }

    /// <summary>Итого по позиции: количество × цена с наценкой.</summary>
    public decimal Total { get; init; }

    public decimal Profit { get; init; }

    /// <summary>Зарплата рабочих: только для работ, доля от стоимости позиции.</summary>
    public decimal LaborCost { get; init; }

    public bool IsWork { get; init; }

    public bool IsMaterial => !IsWork;

    public string KindDisplay => IsWork ? "Работа" : "Материал";
}

/// <summary>Раздел сметы с итогами по работам и материалам.</summary>
public sealed class EstimateReportGroup
{
    public string Section { get; init; } = string.Empty;

    /// <summary>Все позиции раздела в порядке вывода: сначала работы, затем материалы.</summary>
    public IReadOnlyList<EstimateReportRow> Rows { get; init; } = Array.Empty<EstimateReportRow>();

    public IReadOnlyList<EstimateReportRow> Works { get; init; } = Array.Empty<EstimateReportRow>();

    public IReadOnlyList<EstimateReportRow> Materials { get; init; } = Array.Empty<EstimateReportRow>();

    public decimal WorksTotal { get; init; }

    public decimal MaterialsTotal { get; init; }

    public decimal Total => WorksTotal + MaterialsTotal;

    public decimal Profit { get; init; }

    public decimal LaborCost { get; init; }

    public bool HasWorks => Works.Count > 0;

    public bool HasMaterials => Materials.Count > 0;
}

/// <summary>Готовый отчёт по смете: шапка, разделы и итоги (в том числе на 1 м²).</summary>
public sealed class EstimateReport
{
    public string Number { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string StatusDisplay { get; init; } = string.Empty;

    public string ProjectName { get; init; } = string.Empty;

    public string ProjectAddress { get; init; } = string.Empty;

    public string Customer { get; init; } = string.Empty;

    public DateTime GeneratedAt { get; init; } = DateTime.Now;

    public decimal Area { get; init; }

    public decimal MarkupPercent { get; init; }

    public decimal ProfitPercent { get; init; }

    public decimal LaborSharePercent { get; init; }

    public IReadOnlyList<EstimateReportGroup> Groups { get; init; } = Array.Empty<EstimateReportGroup>();

    public IReadOnlyList<EstimateReportRow> Rows { get; init; } = Array.Empty<EstimateReportRow>();

    public decimal MaterialsCost { get; init; }

    public decimal WorksCost { get; init; }

    /// <summary>Прямые затраты: материалы + работы (без прибыли).</summary>
    public decimal DirectCost => MaterialsCost + WorksCost;

    public decimal LaborCost { get; init; }

    public decimal Profit { get; init; }

    /// <summary>Всего затрат с прибылью — «цена» сметы.</summary>
    public decimal TotalCost => DirectCost + Profit;

    public decimal CostPerSquareMeter => Area <= 0m ? 0m : Round(TotalCost / Area);

    public decimal ProfitPerSquareMeter => Area <= 0m ? 0m : Round(Profit / Area);

    public int PositionsCount => Rows.Count;

    public bool HasRows => Rows.Count > 0;

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Считает отчёт по смете «как в специализированных программах»: цена с наценкой,
/// итого по позиции, прибыль и зарплата рабочих, подытоги по разделу и по смете.
/// </summary>
public static class EstimateReportBuilder
{
    public static EstimateReport Build(
        Estimate estimate,
        decimal? area = null,
        decimal? markupPercent = null,
        decimal? profitPercent = null,
        decimal? laborSharePercent = null)
    {
        ArgumentNullException.ThrowIfNull(estimate);

        var markup = markupPercent ?? estimate.MarkupPercent;
        var profit = profitPercent ?? estimate.ProfitPercent;
        var laborShare = laborSharePercent ?? estimate.LaborSharePercent;
        var square = area ?? estimate.Project?.TotalArea ?? 0m;

        var items = (estimate.Items ?? Array.Empty<EstimateItem>())
            .OrderBy(i => string.IsNullOrWhiteSpace(i.Section) ? "Я" : i.Section, StringComparer.CurrentCulture)
            .ThenByDescending(i => i.IsWork)
            .ThenBy(i => i.NameDisplay, StringComparer.CurrentCulture)
            .ToList();

        var rows = new List<EstimateReportRow>(items.Count);
        var groups = new List<EstimateReportGroup>();

        foreach (var sectionItems in items.GroupBy(i => i.SectionDisplay))
        {
            var sectionRows = new List<EstimateReportRow>();
            var number = 1;

            foreach (var item in sectionItems)
            {
                var quantity = item.PlannedQuantity;
                var unitPrice = item.PlannedUnitPrice;
                var priceWithMarkup = Round(unitPrice * (1m + markup / 100m));
                var total = Round(quantity * priceWithMarkup);
                var profitValue = Round(total * profit / 100m);
                var laborValue = item.IsWork ? Round(total * laborShare / 100m) : 0m;

                var row = new EstimateReportRow
                {
                    Number = number++,
                    Section = sectionItems.Key,
                    Name = item.NameDisplay,
                    Unit = item.UnitDisplay,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    PriceWithMarkup = priceWithMarkup,
                    Total = total,
                    Profit = profitValue,
                    LaborCost = laborValue,
                    IsWork = item.IsWork
                };

                sectionRows.Add(row);
                rows.Add(row);
            }

            var works = sectionRows.Where(r => r.IsWork).ToList();
            var materials = sectionRows.Where(r => r.IsMaterial).ToList();

            groups.Add(new EstimateReportGroup
            {
                Section = sectionItems.Key,
                Rows = sectionRows,
                Works = works,
                Materials = materials,
                WorksTotal = works.Sum(r => r.Total),
                MaterialsTotal = materials.Sum(r => r.Total),
                Profit = sectionRows.Sum(r => r.Profit),
                LaborCost = sectionRows.Sum(r => r.LaborCost)
            });
        }

        return new EstimateReport
        {
            Number = estimate.Number,
            Name = estimate.Name,
            StatusDisplay = estimate.StatusDisplay,
            ProjectName = estimate.ProjectName,
            ProjectAddress = estimate.Project?.AddressDisplay ?? string.Empty,
            Customer = estimate.Project?.Customer ?? string.Empty,
            GeneratedAt = DateTime.Now,
            Area = square,
            MarkupPercent = markup,
            ProfitPercent = profit,
            LaborSharePercent = laborShare,
            Groups = groups,
            Rows = rows,
            MaterialsCost = rows.Where(r => r.IsMaterial).Sum(r => r.Total),
            WorksCost = rows.Where(r => r.IsWork).Sum(r => r.Total),
            LaborCost = rows.Sum(r => r.LaborCost),
            Profit = rows.Sum(r => r.Profit)
        };
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
