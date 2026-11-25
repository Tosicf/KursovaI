using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StroyControl.App.ViewModels;

namespace StroyControl.App.Controls;

/// <summary>
/// Выбор периода одной кнопкой: по нажатию открывается небольшое окно, устроенное как
/// календарь — шапка с годом и стрелками перехода и сетка месяцев. Текущий выбор
/// подсвечен, будущие месяцы недоступны, кнопки «Весь год» и «Всё время» снимают
/// ограничение. Данными управляет <see cref="PeriodFilterViewModel"/>.
/// </summary>
public partial class PeriodPicker : UserControl
{
    /// <summary>Момент закрытия окна: щелчок по кнопке не должен открывать его заново.</summary>
    private DateTime _closedAt = DateTime.MinValue;

    public PeriodPicker()
    {
        InitializeComponent();

        // StaysOpen=False: при щелчке по самой кнопке окно закрывается раньше,
        // чем сработает Click, и без этой отметки оно тут же открылось бы снова.
        PeriodPopup.Closed += (_, _) => _closedAt = DateTime.UtcNow;
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if ((DateTime.UtcNow - _closedAt).TotalMilliseconds < 200)
        {
            PeriodPopup.IsOpen = false;
            return;
        }

        PeriodPopup.IsOpen = !PeriodPopup.IsOpen;
    }

    private void OnPreviousYearClick(object sender, RoutedEventArgs e) => Period?.SelectPreviousYear();

    private void OnNextYearClick(object sender, RoutedEventArgs e) => Period?.SelectNextYear();

    private void OnMonthClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PeriodMonthCell cell } || Period is not { } period) return;

        period.SelectMonth(cell.Month);
        PeriodPopup.IsOpen = false;
    }

    private void OnWholeYearClick(object sender, RoutedEventArgs e)
    {
        Period?.SelectWholeYear();
        PeriodPopup.IsOpen = false;
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        Period?.Reset();
        PeriodPopup.IsOpen = false;
    }

    private PeriodFilterViewModel? Period => DataContext as PeriodFilterViewModel;
}
