using System.Globalization;
using System.IO;
using System.Text.Json;

namespace StroyControl.App.Localization;

/// <summary>Язык интерфейса приложения.</summary>
public enum AppLanguage
{
    /// <summary>Русский — язык по умолчанию и базовый язык разметки.</summary>
    Ru = 0,

    /// <summary>Английский.</summary>
    En = 1,

    /// <summary>Беларусский.</summary>
    By = 2
}

/// <summary>
/// Локализация интерфейса. Русские строки остаются в разметке как есть и служат ключом
/// перевода: для английского и беларусского языка словари <see cref="LocaleEn"/> и
/// <see cref="LocaleBy"/> сопоставляют им перевод. Если перевода нет, показывается
/// русская строка — интерфейс никогда не остаётся с пустыми подписями.
/// Выбранный язык хранится в профиле пользователя и применяется при следующем запуске.
/// </summary>
public sealed class LocalizationService
{
    private static readonly Lazy<LocalizationService> Lazy = new(() => new LocalizationService());

    private readonly Dictionary<AppLanguage, IReadOnlyDictionary<string, string>> _dictionaries;

    private LocalizationService()
    {
        _dictionaries = new Dictionary<AppLanguage, IReadOnlyDictionary<string, string>>
        {
            [AppLanguage.En] = LocaleEn.Strings,
            [AppLanguage.By] = LocaleBy.Strings
        };
    }

    public static LocalizationService Instance => Lazy.Value;

    /// <summary>Текущий язык интерфейса.</summary>
    public AppLanguage Language { get; private set; } = AppLanguage.Ru;

    /// <summary>Язык изменился: оболочку приложения нужно перестроить.</summary>
    public event EventHandler? LanguageChanged;

    /// <summary>Перевод строки; при отсутствии перевода возвращается исходный текст.</summary>
    public string T(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (Language == AppLanguage.Ru) return text;

        return _dictionaries[Language].TryGetValue(text, out var translated) && !string.IsNullOrWhiteSpace(translated)
            ? translated
            : text;
    }

    /// <summary>Перевод строки формата с подстановкой значений.</summary>
    public string F(string text, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(text), args);

    /// <summary>Переводит текст, только если он ещё не переведён (для отчётов и печатных форм).</summary>
    public string TranslateKey(string key) => T(key);

    /// <summary>Устанавливает язык; при изменении сохраняет выбор и сообщает оболочке.</summary>
    public void SetLanguage(AppLanguage language, bool notify = true)
    {
        if (Language == language) return;

        Language = language;
        Save();

        if (notify)
            LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public static string Code(AppLanguage language) => language switch
    {
        AppLanguage.En => "en",
        AppLanguage.By => "by",
        _ => "ru"
    };

    public static AppLanguage Parse(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        "en" => AppLanguage.En,
        "by" or "be" => AppLanguage.By,
        _ => AppLanguage.Ru
    };

    /// <summary>Название языка для переключателя — всегда на самом языке.</summary>
    public static string NativeName(AppLanguage language) => language switch
    {
        AppLanguage.En => "English",
        AppLanguage.By => "Беларуская",
        _ => "Русский"
    };

    /// <summary>Файл с выбором языка: %LOCALAPPDATA%\StroyControl\ui-settings.json.</summary>
    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StroyControl",
        "ui-settings.json");

    /// <summary>Читает сохранённый язык при запуске. Ошибки чтения не мешают работе.</summary>
    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;

            using var stream = File.OpenRead(SettingsPath);
            var settings = JsonSerializer.Deserialize<UiSettings>(stream);
            Language = Parse(settings?.Language);
        }
        catch
        {
            Language = AppLanguage.Ru;
        }
    }

    /// <summary>Сохраняет выбранный язык. Ошибки записи не прерывают переключение.</summary>
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var settings = new UiSettings { Language = Code(Language) };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch
        {
            // Профиль недоступен для записи — язык применится только на текущий сеанс.
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private sealed class UiSettings
    {
        public string Language { get; set; } = "ru";
    }
}
