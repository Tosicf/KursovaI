using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace StroyControl.App.Controls;

/// <summary>Стек всплывающих уведомлений в правом нижнем углу окна.</summary>
public partial class ToastHost : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ToastHost), new PropertyMetadata(null));

    public ToastHost()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
}
