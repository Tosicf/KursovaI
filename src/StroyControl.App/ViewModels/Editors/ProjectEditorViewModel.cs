using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;

namespace StroyControl.App.ViewModels.Editors;

public sealed partial class ProjectEditorViewModel : EditorViewModelBase
{
    private readonly Project? _source;

    public ProjectEditorViewModel(Project? source)
    {
        _source = source;
        Code = source?.Code ?? string.Empty;
        Name = source?.Name ?? string.Empty;
        Address = source?.Address ?? string.Empty;
        Customer = source?.Customer ?? string.Empty;
        ManagerName = source?.ManagerName ?? string.Empty;
        ContractSum = source?.ContractSum ?? 0m;
        StartDate = source?.StartDate ?? DateTime.Today;
        PlannedEndDate = source?.PlannedEndDate ?? DateTime.Today.AddMonths(12);
        SelectedStatus = Statuses.FirstOrDefault(s => s.Value == (source?.Status ?? ProjectStatus.Planning)) ?? Statuses[0];
        ProgressPercent = source?.ProgressPercent ?? 0;
        Note = source?.Note;
    }

    public override string Title => _source is null ? "Новый строительный объект" : "Редактирование объекта";

    public IReadOnlyList<EnumOption<ProjectStatus>> Statuses { get; } = EnumExtensions.ToOptions<ProjectStatus>();

    [ObservableProperty]
    private string code;

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string address;

    [ObservableProperty]
    private string customer;

    [ObservableProperty]
    private string managerName;

    [ObservableProperty]
    private decimal contractSum;

    [ObservableProperty]
    private DateTime startDate;

    [ObservableProperty]
    private DateTime plannedEndDate;

    [ObservableProperty]
    private EnumOption<ProjectStatus> selectedStatus;

    [ObservableProperty]
    private int progressPercent;

    [ObservableProperty]
    private string? note;

    protected override string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Code)) return "Укажите шифр объекта.";
        if (string.IsNullOrWhiteSpace(Name)) return "Укажите наименование объекта.";
        if (ContractSum < 0m) return "Договорная стоимость не может быть отрицательной.";
        if (PlannedEndDate <= StartDate) return "Плановая дата окончания должна быть позже даты начала.";
        if (ProgressPercent is < 0 or > 100) return "Готовность объекта — от 0 до 100 %.";
        return null;
    }

    public Project BuildEntity() => new()
    {
        Id = _source?.Id ?? 0,
        RowVersion = _source?.RowVersion ?? 0,
        CreatedAt = _source?.CreatedAt ?? DateTime.UtcNow,
        Code = Code.Trim(),
        Name = Name.Trim(),
        Address = (Address ?? string.Empty).Trim(),
        Customer = (Customer ?? string.Empty).Trim(),
        ManagerName = (ManagerName ?? string.Empty).Trim(),
        ContractSum = ContractSum,
        StartDate = StartDate.Date,
        PlannedEndDate = PlannedEndDate.Date,
        Status = SelectedStatus.Value,
        ProgressPercent = ProgressPercent,
        Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
    };
}
