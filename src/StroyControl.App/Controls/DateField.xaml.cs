using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using StroyControl.Core.Services;

namespace StroyControl.App.Controls;

/// <summary>
/// Поле даты с календарём в цветах темы. Заменяет стандартный <c>DatePicker</c>:
/// у того выпадающий календарь рисуется системными стилями WPF, из-за чего в тёмной
/// теме заголовок месяца и числа становились нечитаемыми.
/// Ввод с клавиатуры — ДД.ММ.ГГГГ, выбор мышью — из календаря; будущие даты
/// блокируются, если задано <see cref="MaxDate"/>.
/// </summary>
public partial class DateField : UserControl
{
    private const string DateFormat = "dd.MM.yyyy";

    private static readonly string[] WeekDays = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };

    private DateTime _displayMonth = MonthPeriod.Start(DateTime.Today);
    private bool _textSuppressed;

    public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register(
        nameof(SelectedDate), typeof(DateTime?), typeof(DateField),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((DateField)d).OnSelectedDateUpdated()));

    public static readonly DependencyProperty MaxDateProperty = DependencyProperty.Register(
        nameof(MaxDate), typeof(DateTime?), typeof(DateField),
        new FrameworkPropertyMetadata(null, (d, _) => ((DateField)d).RefreshCalendar()));

    public DateField()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            BuildWeekHeader();
            SyncText();
            RefreshCalendar();
        };

        Input.GotFocus += (_, _) => SetBorderBrush("AccentBrush");
        Input.LostFocus += (_, _) => SetBorderBrush("BorderBrush");
    }

    /// <summary>Выбранная дата; null — поле пустое.</summary>
    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    /// <summary>Верхняя граница: даты позже недоступны (null — без ограничения).</summary>
    public DateTime? MaxDate
    {
        get => (DateTime?)GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value);
    }

    private DateTime Limit => MaxDate?.Date ?? DateTime.MaxValue.Date;

    private void OnSelectedDateUpdated()
    {
        if (SelectedDate is { } date)
            _displayMonth = MonthPeriod.Start(date);

        SyncText();
        RefreshCalendar();
    }

    private void BuildWeekHeader()
    {
        if (WeekHeader.Children.Count > 0) return;

        foreach (var day in WeekDays)
        {
            WeekHeader.Children.Add(new TextBlock
            {
                Text = day,
                Style = (Style)FindResource("WeekDayStyle")
            });
        }
    }

    private void SyncText()
    {
        _textSuppressed = true;
        Input.Text = SelectedDate?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? string.Empty;
        _textSuppressed = false;
    }

    private void RefreshCalendar()
    {
        if (DayGrid is null || MonthTitle is null) return;

        MonthTitle.Text = MonthPeriod.Title(_displayMonth);
        DayGrid.Children.Clear();

        var first = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);

        // Неделя начинается с понедельника: 0 — понедельник … 6 — воскресенье.
        var offset = ((int)first.DayOfWeek + 6) % 7;
        var start = first.AddDays(-offset);

        for (var index = 0; index < 42; index++)
        {
            var date = start.AddDays(index);
            var inMonth = date.Month == _displayMonth.Month;
            var allowed = inMonth && date <= Limit;

            var button = new Button
            {
                Content = date.Day.ToString(CultureInfo.InvariantCulture),
                Tag = date,
                Style = (Style)FindResource("DayCellStyle"),
                IsEnabled = allowed,
                IsTabStop = false
            };

            if (SelectedDate is { } selected && selected.Date == date)
            {
                button.Background = FindBrush("AccentBrush");
                button.BorderBrush = FindBrush("AccentBrush");
                button.BorderThickness = new Thickness(1);
                button.Foreground = Brushes.White;
                button.FontWeight = FontWeights.SemiBold;
            }
            else if (date == DateTime.Today)
            {
                button.Foreground = FindBrush("AccentBrush");
                button.FontWeight = FontWeights.SemiBold;
            }

            if (!inMonth) button.Opacity = 0.35;

            button.Click += OnDayClick;
            DayGrid.Children.Add(button);
        }
    }

    private void OnDayClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DateTime date }) return;
        if (date > Limit) return;

        SelectedDate = date;
        CalendarPopup.IsOpen = false;
    }

    private void OnToggleClick(object sender, RoutedEventArgs e)
    {
        if (CalendarPopup.IsOpen)
        {
            CalendarPopup.IsOpen = false;
            return;
        }

        _displayMonth = MonthPeriod.Start(SelectedDate ?? DateTime.Today);
        RefreshCalendar();
        CalendarPopup.IsOpen = true;
    }

    private void OnPreviousMonthClick(object sender, RoutedEventArgs e)
    {
        _displayMonth = _displayMonth.AddMonths(-1);
        RefreshCalendar();
    }

    private void OnNextMonthClick(object sender, RoutedEventArgs e)
    {
        _displayMonth = _displayMonth.AddMonths(1);
        RefreshCalendar();
    }

    private void OnPreviousYearClick(object sender, RoutedEventArgs e)
    {
        _displayMonth = _displayMonth.AddYears(-1);
        RefreshCalendar();
    }

    private void OnNextYearClick(object sender, RoutedEventArgs e)
    {
        _displayMonth = _displayMonth.AddYears(1);
        RefreshCalendar();
    }

    private void OnTodayClick(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;

        if (today > Limit)
        {
            _displayMonth = MonthPeriod.Start(Limit);
            RefreshCalendar();
            return;
        }

        SelectedDate = today;
        CalendarPopup.IsOpen = false;
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                CommitInput();
                e.Handled = true;
                break;
            case Key.Escape:
                SyncText();
                e.Handled = true;
                break;
        }
    }

    private void OnInputLostFocus(object sender, RoutedEventArgs e)
    {
        if (!_textSuppressed) CommitInput();
    }

    /// <summary>
    /// Разбирает введённый текст: «25.11.2026» (и привычные варианты через точку,
    /// дефис или слэш). Пустое или некорректное значение возвращает прежнюю дату.
    /// </summary>
    private void CommitInput()
    {
        var text = Input.Text?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            SyncText();
            return;
        }

        var parsed = TryParse(text);
        if (parsed is null || parsed.Value.Date > Limit)
        {
            SyncText();
            return;
        }

        SelectedDate = parsed.Value.Date;
        _displayMonth = MonthPeriod.Start(parsed.Value);
        SyncText();
        RefreshCalendar();
    }

    private static DateTime? TryParse(string text)
    {
        string[] formats =
        {
            "dd.MM.yyyy", "d.M.yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy", "ddMMyyyy"
        };

        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact;

        return DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var loose)
            ? loose
            : null;
    }

    private void SetBorderBrush(string brushKey)
    {
        if (TryFindResource(brushKey) is Brush brush)
            FieldBorder.BorderBrush = brush;
    }

    private Brush FindBrush(string key) =>
        TryFindResource(key) as Brush ?? Brushes.Transparent;
}
