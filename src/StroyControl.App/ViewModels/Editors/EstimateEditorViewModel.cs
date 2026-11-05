using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;

namespace StroyControl.App.ViewModels.Editors;

public sealed partial class EstimateEditorViewModel : EditorViewModelBase
{
    private readonly Estimate? _source;

    public EstimateEditorViewModel(Estimate? source, IReadOnlyList<Project> projects)
    {
        _source = source;
        Projects = projects;

        Number = source?.Number ?? $"СМ-{DateTime.Now:yy}-{DateTime.Now:MMdd}";
        Name = source?.Name ?? string.Empty;
        SelectedProject = projects.FirstOrDefault(p => p.Id == source?.ProjectId) ?? projects.FirstOrDefault();
        SelectedStatus = Statuses.FirstOrDefault(s => s.Value == (source?.Status ?? EstimateStatus.Draft)) ?? Statuses[0];
        Note = source?.Note;
    }

    public override string Title => _source is null ? "Новая смета" : "Редактирование сметы";

    public IReadOnlyList<Project> Projects { get; }

    public IReadOnlyList<EnumOption<EstimateStatus>> Statuses { get; } = EnumExtensions.ToOptions<EstimateStatus>();

    [ObservableProperty]
    private string number;

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private Project? selectedProject;

    [ObservableProperty]
    private EnumOption<EstimateStatus> selectedStatus;

    [ObservableProperty]
    private string? note;

    protected override string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Number)) return "Укажите номер сметы.";
        if (string.IsNullOrWhiteSpace(Name)) return "Укажите наименование сметы.";
        if (SelectedProject is null) return "Выберите строительный объект.";
        return null;
    }

    public Estimate BuildEntity() => new()
    {
        Id = _source?.Id ?? 0,
        RowVersion = _source?.RowVersion ?? 0,
        CreatedAt = _source?.CreatedAt ?? DateTime.UtcNow,
        Number = Number.Trim(),
        Name = Name.Trim(),
        ProjectId = SelectedProject?.Id ?? 0,
        Status = SelectedStatus.Value,
        Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
    };
}
