using StroyControl.App.ViewModels.Editors;
using StroyControl.Core.Dtos;
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

    /// <summary>
    /// Диалог поступления. Если передан <paramref name="suggested"/> (материалы ниже минимума),
    /// строки нового документа заполняются автоматически с рекомендованным количеством.
    /// </summary>
    Delivery? EditDelivery(
        Delivery? delivery,
        IReadOnlyList<Supplier> suppliers,
        IReadOnlyList<Project> projects,
        IReadOnlyList<Material> materials,
        IReadOnlyList<Material>? suggested = null,
        string? suggestedNumber = null);

    MaterialUsage? EditUsage(MaterialUsage? usage, IReadOnlyList<Project> projects, IReadOnlyList<Material> materials, IReadOnlyList<Estimate> estimates, int? currentUserId);

    UserEditorViewModel? EditUser(User? user);

    /// <summary>Показывает отчёт по смете (печать, Excel, CSV).</summary>
    void ShowEstimateReport(Estimate estimate);

    /// <summary>Предпросмотр и печать счёта-фактуры по поступлению материалов.</summary>
    void ShowPurchaseReceipt(Delivery delivery);

    /// <summary>Предпросмотр и печать акта списания материалов.</summary>
    void ShowUsageAct(MaterialUsage usage);

    /// <summary>
    /// Акт выполненных работ: работы по смете и списанные на них материалы.
    /// <paramref name="estimateId"/> и <paramref name="estimateItemId"/> позволяют открыть
    /// форму сразу по нужной смете и позиции — так факт вводится из карточки сметы.
    /// </summary>
    WorkCompletion? EditWorkCompletion(
        WorkCompletion? completion,
        IReadOnlyList<Estimate> estimates,
        IReadOnlyList<Material> materials,
        int? currentUserId,
        int? estimateId = null,
        int? estimateItemId = null,
        IReadOnlyList<string>? usedNumbers = null);

    void ShowWorkCompletionAct(WorkCompletion completion);

    /// <summary>Печатный отчёт по анализу отклонений: предпросмотр, печать, CSV и PDF.</summary>
    void ShowDeviationReport(DeviationReport report);
}
