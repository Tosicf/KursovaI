using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StroyControl.App.ViewModels;

namespace StroyControl.App.Controls;

/// <summary>
/// Выбор периода одной кнопкой: по нажатию открывается небольшое окно с годами и
/// месяцами. Текущий выбор (по умолчанию — текущий год и месяц) подсвечен в списках,
/// будущие периоды в них не попадают. Данными управляет <see cref="PeriodFilterViewModel"/>.
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

        // Открываем список на текущем выборе, чтобы не прокручивать его вручную.
        if (PeriodPopup.IsOpen)
        {
            YearsList.ScrollIntoView(YearsList.SelectedItem);
            MonthsList.ScrollIntoView(MonthsList.SelectedItem);
        }
    }

    /// <summary>
    /// Выбор месяца закрывает окно. Обрабатывается именно щелчок мышью: программное
    /// перестроение списков (например, после смены года) SelectionChanged не вызывает
    /// закрытия, иначе окно схлопывалось бы до выбора месяца. После выбора года окно
    /// остаётся открытым — так год и месяц выбираются за один заход.
    /// </summary>
    private void OnOptionClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindItem(source) is null) return;

        var yearChosen = ReferenceEquals(sender, YearsList)
                         && DataContext is PeriodFilterViewModel { CanFilterByMonth: true };

        if (yearChosen)
        {
            MonthsList.Focus();
            return;
        }

        PeriodPopup.IsOpen = false;
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is PeriodFilterViewModel period)
            period.Reset();

        PeriodPopup.IsOpen = false;
    }

    private static ListBoxItem? FindItem(DependencyObject? source)
    {
        while (source is not null && source is not ListBoxItem)
            source = source is System.Windows.Media.Visual visual
                ? System.Windows.Media.VisualTreeHelper.GetParent(visual)
                : LogicalTreeHelper.GetParent(source);

        return source as ListBoxItem;
    }
}
