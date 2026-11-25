using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Dtos;
using StroyControl.Core.Services;

namespace StroyControl.App.ViewModels;

/// <summary>
/// Фильтр периода для списков и главной панели: сначала выбирается год, затем месяц
/// внутри года. Списки месяцев перестраиваются под год, будущие годы и месяцы в них
/// не попадают — выбрать можно только наступивший период. Смена периода сообщается
/// странице через callback, поэтому сортировка по годам и месяцам работает везде.
/// </summary>
public sealed partial class PeriodFilterViewModel : ObservableObject
{
    private readonly Action _changed;
    private List<DateTime?> _dates = new();
    private bool _suspended = true;
    private bool _defaultApplied;

    /// <param name="changed">Действие страницы при смене периода (обновление фильтра).</param>
    public PeriodFilterViewModel(Action changed) => _changed = changed;

    /// <summary>Годы, за которые есть документы: «Все годы», затем от нового к старому.</summary>
    public ObservableCollection<PeriodOption> Years { get; } = new();

    /// <summary>Месяцы выбранного года: «Все месяцы», затем от нового к старому.</summary>
    public ObservableCollection<PeriodOption> Months { get; } = new();

    // Свойства допускают null: при очистке списка WPF-привязка ComboBox кратковременно
    // записывает null, и это не должно ломать выбор периода.
    [ObservableProperty]
    private PeriodOption? selectedYear = PeriodOption.AllYears;

    [ObservableProperty]
    private PeriodOption? selectedMonth;

    partial void OnSelectedYearChanged(PeriodOption? value)
    {
        // Месяцы зависят от года: пересобираем список и сохраняем выбор, если он возможен.
        var wasSuspended = _suspended;
        _suspended = true;
        RebuildMonths();
        _suspended = wasSuspended;

        RaiseDerived();
        if (_suspended) return;
        _changed();
    }

    partial void OnSelectedMonthChanged(PeriodOption? value)
    {
        RaiseDerived();
        if (_suspended) return;
        _changed();
    }

    /// <summary>Выбранный период (год и месяц).</summary>
    public PeriodFilter Filter => new(SelectedYear?.Year, SelectedMonth?.Month);

    /// <summary>Месяц имеет смысл только внутри выбранного года.</summary>
    public bool CanFilterByMonth => SelectedYear?.Year is not null;

    /// <summary>Подпись для заголовков блоков: «за ноябрь 2026» / «за 2026 год» / «за всё время».</summary>
    public string FilterCaption => Filter.Caption;

    /// <summary>Подпись для накопленных показателей: «по состоянию на ноябрь 2026».</summary>
    public string BalanceCaption => Filter.BalanceCaption;

    /// <summary>Подпись кнопки периода: «Ноябрь 2026» / «2026 год» / «Всё время».</summary>
    public string PickerCaption => Filter.Year switch
    {
        null => "Всё время",
        _ when Filter.Month is null => $"{Filter.Year} год",
        _ => $"{MonthPeriod.MonthName(Filter.Month.Value)} {Filter.Year}"
    };

    /// <summary>Подсказка кнопки периода.</summary>
    public string PickerTooltip =>
        $"Период: {PickerCaption.ToLowerInvariant()}. Нажмите, чтобы выбрать другой год и месяц. "
        + "Будущие периоды недоступны.";

    /// <summary>Сброс периода: показываются все годы и месяцы.</summary>
    public void Reset()
    {
        _suspended = true;
        SelectedYear = PeriodOption.AllYears;

        // Пункты списка сравниваются по ссылке: берём экземпляр из коллекции.
        SelectedMonth = Months.Count > 0 ? Months[0] : null;

        _suspended = false;

        RaiseDerived();
        _changed();
    }

    /// <summary>
    /// Перестраивает списки годов и месяцев по датам документов, сохраняя выбор.
    /// Будущие периоды в списки не попадают.
    /// </summary>
    public void Rebuild(IEnumerable<DateTime?> dates)
    {
        _dates = dates.ToList();
        var beforeYear = SelectedYear?.Year;
        var beforeMonth = SelectedMonth?.Month;

        var previousYear = beforeYear;
        var previousMonth = beforeMonth;

        // При первом построении открывается текущий год и текущий месяц — так на любой
        // странице сразу виден свежий период. Если таких данных нет (например, в архиве
        // только сметы прошлых лет), остаётся «всё время», иначе список был бы пустым.
        if (!_defaultApplied)
        {
            _defaultApplied = true;
            previousYear = DateTime.Today.Year;
            previousMonth = DateTime.Today.Month;
        }

        _suspended = true;

        Years.Clear();
        Years.Add(PeriodOption.AllYears);
        foreach (var year in MonthPeriod.Years(_dates))
            Years.Add(new PeriodOption(year, null, year.ToString()));

        SelectedYear = Years.FirstOrDefault(option => option.Year == previousYear) ?? PeriodOption.AllYears;

        // Текущего года в данных нет — месяц без года не имеет смысла.
        if (SelectedYear?.Year != previousYear) previousMonth = null;

        RebuildMonths(previousMonth);

        _suspended = false;

        RaiseDerived();
        OnPropertyChanged(nameof(Years));

        // Период мог измениться сам (подставлен текущий год и месяц) — сообщаем странице,
        // иначе она осталась бы с данными, загруженными за «всё время».
        if (SelectedYear?.Year != beforeYear || SelectedMonth?.Month != beforeMonth)
            _changed();
    }

    /// <summary>Дата попадает в выбранный период.</summary>
    public bool Matches(DateTime date) => Filter.Contains(date);

    /// <summary>Месяцы пересобираются под выбранный год.</summary>
    /// <param name="preferredMonth">Месяц, который нужно сохранить, если он есть в списке.</param>
    private void RebuildMonths(int? preferredMonth = null)
    {
        var year = SelectedYear?.Year;
        var previousMonth = preferredMonth ?? SelectedMonth?.Month;

        Months.Clear();
        Months.Add(PeriodOption.AllMonthsOf(year));

        if (year is not null)
        {
            foreach (var month in MonthPeriod.Months(year, _dates))
                Months.Add(new PeriodOption(year, month, MonthPeriod.MonthName(month)));
        }

        SelectedMonth = Months.FirstOrDefault(option => option.Month == previousMonth) ?? Months[0];
        OnPropertyChanged(nameof(CanFilterByMonth));
    }

    private void RaiseDerived()
    {
        OnPropertyChanged(nameof(Filter));
        OnPropertyChanged(nameof(FilterCaption));
        OnPropertyChanged(nameof(BalanceCaption));
        OnPropertyChanged(nameof(CanFilterByMonth));
        OnPropertyChanged(nameof(PickerCaption));
        OnPropertyChanged(nameof(PickerTooltip));
    }
}
