namespace StroyControl.Core.Dtos;

/// <summary>Запись ленты последних операций для панели управления.</summary>
public sealed record ActivityItem(DateTime Date, string Title, string Subtitle, string Kind)
{
    public string DateDisplay => Date.ToString("dd.MM.yyyy");

    public string KindDisplay => Kind switch
    {
        "delivery" => "Поступление",
        "usage" => "Списание",
        _ => "Операция"
    };
}
