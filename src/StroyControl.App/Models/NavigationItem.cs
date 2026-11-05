using System.Windows.Input;
using System.Windows.Media;

namespace StroyControl.App.Models;

/// <summary>Пункт бокового меню.</summary>
public sealed class NavigationItem
{
    public NavigationItem(string key, string title, Geometry? icon, Func<Task>? activate = null)
    {
        Key = key;
        Title = title;
        Icon = icon;
        Activate = activate;
    }

    public string Key { get; }

    public string Title { get; }

    public Geometry? Icon { get; }

    public Func<Task>? Activate { get; }
}
