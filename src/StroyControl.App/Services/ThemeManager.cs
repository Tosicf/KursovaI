using System.IO;
using System.Text.Json;
using System.Windows;

namespace StroyControl.App.Services;

public enum AppTheme
{
    Light,
    Dark
}

/// <summary>
/// Переключение светлой и тёмной темы: подменяет словарь палитры в ресурсах приложения.
/// Стили используют DynamicResource, поэтому цвета меняются без перезапуска окна.
/// Выбор сохраняется в %APPDATA%\StroyControl\ui.json и применяется при следующем запуске.
/// </summary>
public static class ThemeManager
{
    private static readonly Uri LightUri = new("Resources/Theme/Palette.Light.xaml", UriKind.Relative);
    private static readonly Uri DarkUri = new("Resources/Theme/Palette.Dark.xaml", UriKind.Relative);

    private static readonly string SettingsFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StroyControl");

    private static readonly string SettingsFile = Path.Combine(SettingsFolder, "ui.json");

    public static AppTheme Current { get; private set; } = AppTheme.Light;

    public static bool IsDark => Current == AppTheme.Dark;

    /// <summary>Событие для графиков и других элементов, которые рисуют себя сами.</summary>
    public static event EventHandler? ThemeChanged;

    public static void Toggle() => Apply(IsDark ? AppTheme.Light : AppTheme.Dark);

    public static void Apply(AppTheme theme, bool persist = true)
    {
        var app = Application.Current;
        if (app is null) return;

        var dictionaries = app.Resources.MergedDictionaries;
        var palette = new ResourceDictionary { Source = theme == AppTheme.Dark ? DarkUri : LightUri };

        var existing = dictionaries
            .Select((dictionary, index) => (dictionary, index))
            .FirstOrDefault(pair => pair.dictionary.Source?.OriginalString.Contains("Palette.", StringComparison.OrdinalIgnoreCase) == true);

        if (existing.dictionary is not null)
            dictionaries[existing.index] = palette;
        else
            dictionaries.Insert(0, palette);

        Current = theme;

        if (persist) Save(theme);

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Тема, выбранная в прошлый раз (по умолчанию — светлая).</summary>
    public static AppTheme LoadSavedTheme()
    {
        try
        {
            if (!File.Exists(SettingsFile)) return AppTheme.Light;

            var json = File.ReadAllText(SettingsFile);
            var settings = JsonSerializer.Deserialize<UiSettings>(json);
            return string.Equals(settings?.Theme, nameof(AppTheme.Dark), StringComparison.OrdinalIgnoreCase)
                ? AppTheme.Dark
                : AppTheme.Light;
        }
        catch
        {
            return AppTheme.Light;
        }
    }

    private static void Save(AppTheme theme)
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(new UiSettings { Theme = theme.ToString() }));
        }
        catch
        {
            // Настройка не критична: если записать нельзя (ограниченные права), просто продолжаем.
        }
    }

    private sealed class UiSettings
    {
        public string Theme { get; set; } = nameof(AppTheme.Light);
    }
}
