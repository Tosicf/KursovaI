using StroyControl.App.ViewModels.Editors;
using StroyControl.Core.Entities;

namespace StroyControl.App.Services;

/// <summary>Показ модальных диалогов редактирования и подтверждений.</summary>
public interface IDialogService
{
    bool ShowEditor(EditorViewModelBase viewModel);

    bool Confirm(string title, string message, string confirmText = "Удалить");

    /// <summary>Диалог сохранения файла. Возвращает путь или null, если пользователь отменил выбор.</summary>
    string? SaveFile(string title, string suggestedFileName, string filter);

    Material? EditMaterial(Material? material, IReadOnlyList<Supplier> suppliers, IReadOnlyList<string> categories);

    Supplier? EditSupplier(Supplier? supplier);

    Project? EditProject(Project? project);

    Estimate? EditEstimate(Estimate? estimate, IReadOnlyList<Project> projects);

    EstimateItem? EditEstimateItem(EstimateItem? item, int estimateId, IReadOnlyList<Material> materials);

    Delivery? EditDelivery(Delivery? delivery, IReadOnlyList<Supplier> suppliers, IReadOnlyList<Project> projects, IReadOnlyList<Material> materials);

    MaterialUsage? EditUsage(MaterialUsage? usage, IReadOnlyList<Project> projects, IReadOnlyList<Material> materials, IReadOnlyList<Estimate> estimates, int? currentUserId);

    UserEditorViewModel? EditUser(User? user);
}
