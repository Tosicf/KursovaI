namespace StroyControl.App.Services;

/// <summary>
/// Переход между разделами по запросу страницы: реестр объектов открывает
/// «Анализ отклонений» или «Сметы» с уже выбранным объектом. Страницы объявлены
/// синглтонами, поэтому страница-приёмник запоминает объект до загрузки данных
/// (см. <c>FocusProject</c> в моделях представления).
/// </summary>
public sealed class PageNavigator
{
    private Action<string>? _handler;

    /// <summary>
    /// Регистрирует обработчик активного главного окна; прежний (например, от закрытого
    /// после выхода окна) заменяется, поэтому запросы не выполняются дважды.
    /// </summary>
    public void Attach(Action<string> handler) => _handler = handler;

    /// <summary>Просит главное окно перейти в раздел с указанным ключом.</summary>
    public void Open(string pageKey) => _handler?.Invoke(pageKey);
}
