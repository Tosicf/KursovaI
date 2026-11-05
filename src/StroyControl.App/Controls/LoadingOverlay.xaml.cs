using System.Windows;
using System.Windows.Controls;

namespace StroyControl.App.Controls;

/// <summary>Полупрозрачный индикатор загрузки, перекрывающий содержимое панели.</summary>
public partial class LoadingOverlay : UserControl
{
    public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
        nameof(IsBusy), typeof(bool), typeof(LoadingOverlay), new PropertyMetadata(false));

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(LoadingOverlay), new PropertyMetadata("Загрузка данных…"));

    public LoadingOverlay()
    {
        InitializeComponent();
    }

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }
}
