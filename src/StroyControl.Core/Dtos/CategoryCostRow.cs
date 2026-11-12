namespace StroyControl.Core.Dtos;

/// <summary>Затраты по категории материалов — для кольцевой диаграммы на панели управления.</summary>
public sealed record CategoryCostRow(string Category, decimal Cost, decimal Share)
{
    public string CostDisplay => Cost.ToString("N2") + " BYN";

    public string ShareDisplay => Share.ToString("N1") + " %";
}
