namespace StroyControl.Core.Entities;

/// <summary>
/// Строка акта выполненных работ: конкретная позиция-работа сметы, фактический объём
/// и расценка на дату акта. Остаток склада строка не меняет — работы выполняются
/// рабочими, а материалы списываются отдельными строками акта.
/// </summary>
public class WorkCompletionItem : EntityBase
{
    public int WorkCompletionId { get; set; }

    public WorkCompletion? WorkCompletion { get; set; }

    /// <summary>Позиция-работа сметы, к которой относится фактический объём.</summary>
    public int EstimateItemId { get; set; }

    public EstimateItem? EstimateItem { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Расценка на дату акта: снимок плановой цены позиции сметы.</summary>
    public decimal UnitPrice { get; set; }

    public decimal Cost => Quantity * UnitPrice;

    public string WorkName => EstimateItem?.NameDisplay ?? "—";

    public string UnitDisplay => EstimateItem?.UnitDisplay ?? string.Empty;

    public string SectionDisplay => EstimateItem?.SectionDisplay ?? "Без раздела";

    public decimal PlannedQuantity => EstimateItem?.PlannedQuantity ?? 0m;

    public string QuantityDisplay => Quantity.ToString("N2");

    public string CostDisplay => Cost.ToString("N2");

    public override string ToString() => $"{WorkName}: {Quantity:N2} × {UnitPrice:N2}";
}
