using StroyControl.Core.Enums;

namespace StroyControl.Core.Dtos;

/// <summary>Сводный результат анализа отклонений по выбранному объекту или всей организации.</summary>
public sealed class DeviationReport
{
    public DeviationReport(string scopeName, IReadOnlyList<DeviationRow> rows)
    {
        ScopeName = scopeName;
        Rows = rows;
    }

    public string ScopeName { get; }

    public IReadOnlyList<DeviationRow> Rows { get; }

    public decimal PlannedTotal => Rows.Sum(r => r.PlannedCost);

    public decimal ActualTotal => Rows.Sum(r => r.ActualCost);

    public decimal CostDeviation => ActualTotal - PlannedTotal;

    public decimal DeviationPercent => PlannedTotal == 0m ? 0m : Math.Round(CostDeviation / PlannedTotal * 100m, 1);

    public DeviationSeverity Severity => DeviationCalculator.Classify(Math.Abs(DeviationPercent));

    public int CriticalCount => Rows.Count(r => r.Severity == DeviationSeverity.Critical);

    public int WarningCount => Rows.Count(r => r.Severity == DeviationSeverity.Warning);

    public int OverrunCount => Rows.Count(r => r.IsOverrun);

    public int EconomyCount => Rows.Count(r => r.CostDeviation < 0m);

    public IReadOnlyList<string> Sections => Rows
        .Select(r => r.Section)
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Distinct()
        .OrderBy(s => s)
        .ToList();
}
