using System.Windows.Markup;

namespace StroyControl.App.Localization;

/// <summary>
/// Разметочное расширение для локализации текста в XAML: русская строка служит ключом
/// перевода, поэтому разметка остаётся читаемой.
/// <code>&lt;TextBlock Text="{loc:Tr 'Сметы'}" /&gt;</code>
/// Строка переводится в момент создания элемента, поэтому после смены языка оболочка
/// приложения перестраивается (см. <see cref="LocalizationService.LanguageChanged"/>).
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TrExtension : MarkupExtension
{
    public TrExtension()
    {
    }

    public TrExtension(string text) => Text = text;

    /// <summary>Русский текст — ключ перевода.</summary>
    public string Text { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        LocalizationService.Instance.T(Text);
}

/// <summary>Короткая запись локализации для кода: <c>L.T("Списания")</c>.</summary>
public static class L
{
    public static string T(string text) => LocalizationService.Instance.T(text);

    public static string F(string text, params object?[] args) => LocalizationService.Instance.F(text, args);
}
