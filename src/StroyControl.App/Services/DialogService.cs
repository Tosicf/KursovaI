using System.Windows;
using StroyControl.App.ViewModels;
using StroyControl.App.ViewModels.Editors;
using StroyControl.App.Views.Dialogs;
using StroyControl.Core.Entities;

namespace StroyControl.App.Services;

public sealed class DialogService : IDialogService
{
    public bool ShowEditor(EditorViewModelBase viewModel)
    {
        var window = new EditorWindow { DataContext = viewModel };
        var owner = Application.Current?.MainWindow;
        if (owner is not null && !ReferenceEquals(owner, window) && owner.IsVisible)
            window.Owner = owner;

        return window.ShowDialog() == true;
    }

    public bool Confirm(string title, string message, string confirmText = "Удалить")
    {
        var window = new ConfirmWindow(title, message, confirmText);
        var owner = Application.Current?.MainWindow;
        if (owner is not null && owner.IsVisible)
            window.Owner = owner;

        return window.ShowDialog() == true;
    }

    public string? SaveFile(string title, string suggestedFileName, string filter)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = title,
            FileName = suggestedFileName,
            Filter = filter,
            AddExtension = true,
            OverwritePrompt = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public Material? EditMaterial(Material? material, IReadOnlyList<Supplier> suppliers, IReadOnlyList<string> categories)
    {
        var vm = new MaterialEditorViewModel(material, suppliers, categories);
        return ShowEditor(vm) ? vm.BuildEntity() : null;
    }

    public Supplier? EditSupplier(Supplier? supplier)
    {
        var vm = new SupplierEditorViewModel(supplier);
        return ShowEditor(vm) ? vm.BuildEntity() : null;
    }

    public Project? EditProject(Project? project)
    {
        var vm = new ProjectEditorViewModel(project);
        return ShowEditor(vm) ? vm.BuildEntity() : null;
    }

    public Estimate? EditEstimate(Estimate? estimate, IReadOnlyList<Project> projects)
    {
        var vm = new EstimateEditorViewModel(estimate, projects);
        return ShowEditor(vm) ? vm.BuildEntity() : null;
    }

    public EstimateItem? EditEstimateItem(EstimateItem? item, int estimateId, IReadOnlyList<Material> materials)
    {
        var vm = new EstimateItemEditorViewModel(item, estimateId, materials);
        return ShowEditor(vm) ? vm.BuildEntity() : null;
    }

    public Delivery? EditDelivery(Delivery? delivery, IReadOnlyList<Supplier> suppliers, IReadOnlyList<Project> projects, IReadOnlyList<Material> materials)
    {
        var vm = new DeliveryEditorViewModel(delivery, suppliers, projects, materials);
        return ShowEditor(vm) ? vm.BuildEntity() : null;
    }

    public MaterialUsage? EditUsage(MaterialUsage? usage, IReadOnlyList<Project> projects, IReadOnlyList<Material> materials, IReadOnlyList<Estimate> estimates, int? currentUserId)
    {
        var vm = new UsageEditorViewModel(usage, projects, materials, estimates, currentUserId);
        return ShowEditor(vm) ? vm.BuildEntity() : null;
    }

    public void ShowEstimateReport(Estimate estimate)
    {
        var window = new EstimateReportWindow(new EstimateReportViewModel(estimate));
        var owner = Application.Current?.MainWindow;
        if (owner is not null && owner.IsVisible)
            window.Owner = owner;

        window.ShowDialog();
    }

    public UserEditorViewModel? EditUser(User? user)
    {
        var vm = new UserEditorViewModel(user);
        return ShowEditor(vm) ? vm : null;
    }
}
