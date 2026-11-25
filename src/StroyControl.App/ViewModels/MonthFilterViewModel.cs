using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Dtos;
using StroyControl.Core.Services;

namespace StroyControl.App.ViewModels;

/// <summary>
/// Фильтр по месяцу для списков и главной панели. Хранит выбранный период,
/// отдаёт список месяцев «от нового к старому» и сообщает странице о смене
/// периода через callback — так сортировка по месяцам работает одинаково везде.
/// </summary>
public sealed partial class MonthFilterViewModel : ObservableObject
{
    private readonly Action _changed;
    private bool _suspended = true;

    /// <param name="changed">Действие страницы при смене периода (обычно — обновление фильтра).</param>
    public MonthFilterViewModel(Action changed) => _changed = changed;

    /// <summary>Пункты списка: «Все месяцы» и месяцы, за которые есть документы.</summary>
    public ObservableCollection<MonthOption> Options { get; } = new();

    // Свойство допускает null: при очистке списка WPF-привязка ComboBox кратковременно
    // записывает null, и это не должно ломать выбор периода.
    [ObservableProperty]
    private MonthOption? selected = MonthOption.All;

    partial void OnSelectedChanged(MonthOption? value)
    {
        OnPropertyChanged(nameof(Month));
        OnPropertyChanged(nameof(IsAll));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(FilterCaption));
        OnPropertyChanged(nameof(BalanceCaption));

        if (_suspended) return;
        _changed();
    }

    /// <summary>Выбранный месяц; null — «Все месяцы».</summary>
    public DateTime? Month => Selected?.Month;

    /// <summary>Период не ограничен.</summary>
    public bool IsAll => Selected?.Month is null;

    /// <summary>Подпись выбранного периода: «Все месяцы» или «Ноябрь 2026».</summary>
    public string Title => Selected?.Title ?? MonthPeriod.AllTitle;

    /// <summary>Подпись для заголовков блоков: «за ноябрь 2026» / «за всё время».</summary>
    public string FilterCaption => Selected?.Caption ?? MonthOption.All.Caption;

    /// <summary>Подпись для накопленных показателей: «по состоянию на ноябрь 2026».</summary>
    public string BalanceCaption => Selected?.Month is null
        ? MonthOption.All.Caption
        : "по состоянию на " + Selected.Title.ToLowerInvariant();

    /// <summary>
    /// Перестраивает список месяцев по датам документов, сохраняя выбранный период.
    /// Если выбранного месяца в данных больше нет — возвращается «Все месяцы».
    /// </summary>
    public void Rebuild(IEnumerable<DateTime?> dates)
    {
        var previous = Selected;
        var options = MonthPeriod.Options(dates);

        _suspended = true;
        Options.Clear();
        foreach (var option in options)
            Options.Add(option);

        Selected = Options.FirstOrDefault(o => o.Month == previous?.Month) ?? MonthOption.All;
        _suspended = false;

        // Даже если выбор не изменился, страница должна перечитать список месяцев.
        OnPropertyChanged(nameof(Options));
    }

    /// <summary>Выбирает месяц программно (null — «Все месяцы»).</summary>
    public void Select(DateTime? month)
    {
        var match = Options.FirstOrDefault(o => o.Month == month);
        if (match is not null && !ReferenceEquals(match, Selected))
            Selected = match;
    }

    /// <summary>Дата относится к выбранному периоду.</summary>
    public bool Matches(DateTime date) => MonthPeriod.Matches(date, Month);
}
