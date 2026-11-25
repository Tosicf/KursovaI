namespace StroyControl.App.ViewModels;

/// <summary>
/// Ячейка сетки месяцев в окне выбора периода: месяц, его подпись, доступность
/// (будущие месяцы выбрать нельзя) и признак текущего выбора.
/// </summary>
public sealed class PeriodMonthCell
{
    public PeriodMonthCell(int month, string title, bool isEnabled, bool isSelected)
    {
        Month = month;
        Title = title;
        IsEnabled = isEnabled;
        IsSelected = isSelected;
    }

    /// <summary>Номер месяца (1–12).</summary>
    public int Month { get; }

    /// <summary>Подпись ячейки: «Янв», «Фев», …</summary>
    public string Title { get; }

    /// <summary>Месяц можно выбрать (не в будущем и год выбран).</summary>
    public bool IsEnabled { get; }

    /// <summary>Месяц входит в текущий период.</summary>
    public bool IsSelected { get; }

    public override string ToString() => Title;
}
