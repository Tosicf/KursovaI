using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Entities;

namespace StroyControl.App.ViewModels.Editors;

public sealed partial class SupplierEditorViewModel : EditorViewModelBase
{
    private readonly Supplier? _source;

    public SupplierEditorViewModel(Supplier? source)
    {
        _source = source;
        Name = source?.Name ?? string.Empty;
        Unp = source?.Unp ?? string.Empty;
        ContactPerson = source?.ContactPerson ?? string.Empty;
        Phone = source?.Phone ?? string.Empty;
        Email = source?.Email ?? string.Empty;
        Address = source?.Address ?? string.Empty;
        Rating = source?.Rating ?? 5;
        Note = source?.Note;
    }

    public override string Title => _source is null ? "Новый поставщик" : "Редактирование поставщика";

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string unp;

    [ObservableProperty]
    private string contactPerson;

    [ObservableProperty]
    private string phone;

    [ObservableProperty]
    private string email;

    [ObservableProperty]
    private string address;

    [ObservableProperty]
    private int rating;

    [ObservableProperty]
    private string? note;

    protected override string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) return "Укажите наименование поставщика.";
        if (Rating is < 1 or > 5) return "Надёжность оценивается от 1 до 5.";
        if (!string.IsNullOrWhiteSpace(Email) && !Email.Contains('@')) return "Проверьте адрес электронной почты.";
        return null;
    }

    public Supplier BuildEntity() => new()
    {
        Id = _source?.Id ?? 0,
        RowVersion = _source?.RowVersion ?? 0,
        CreatedAt = _source?.CreatedAt ?? DateTime.UtcNow,
        Name = Name.Trim(),
        Unp = (Unp ?? string.Empty).Trim(),
        ContactPerson = (ContactPerson ?? string.Empty).Trim(),
        Phone = (Phone ?? string.Empty).Trim(),
        Email = (Email ?? string.Empty).Trim(),
        Address = (Address ?? string.Empty).Trim(),
        Rating = Rating,
        Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
    };
}
