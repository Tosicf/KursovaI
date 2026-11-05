using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Services;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.ViewModels;

/// <summary>Справочник поставщиков строительных материалов.</summary>
public sealed partial class SuppliersViewModel : ViewModelBase
{
    private readonly ISupplierRepository _suppliers;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;

    public SuppliersViewModel(ISupplierRepository suppliers, IDialogService dialogs, IToastService toasts, ISessionService session)
    {
        _suppliers = suppliers;
        _dialogs = dialogs;
        _toasts = toasts;
        _session = session;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;
    }

    public override string Title => "Поставщики";

    public ObservableCollection<Supplier> Items { get; } = new();

    public ICollectionView ItemsView { get; }

    public bool CanEdit => _session.CanEdit;

    public bool IsEmpty => Items.Count == 0;

    public bool IsFilteredEmpty => Items.Count > 0 && VisibleCount == 0;

    public int TotalCount => Items.Count;

    public int LinkedMaterialsCount => Items.Sum(s => s.MaterialsCount);

    public string AverageRating => Items.Count == 0
        ? "—"
        : Items.Average(s => s.Rating).ToString("N1") + " / 5";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int visibleCount;

    [ObservableProperty]
    private Supplier? selectedSupplier;

    partial void OnSearchTextChanged(string value)
    {
        ItemsView.Refresh();
        VisibleCount = ItemsView.Cast<object>().Count();
        OnPropertyChanged(nameof(IsFilteredEmpty));
    }

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () =>
        {
            var suppliers = await _suppliers.GetAllWithMaterialsAsync();
            Items.Clear();
            foreach (var supplier in suppliers)
                Items.Add(supplier);

            UpdateStatistics();
        }, "Не удалось загрузить список поставщиков.");

        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Поставщики", ErrorMessage);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (!CanEdit)
        {
            _toasts.Info("Недостаточно прав", "Текущая роль работает в режиме просмотра.");
            return;
        }

        var entity = _dialogs.EditSupplier(null);
        if (entity is null) return;

        Items.Insert(0, entity);
        UpdateStatistics();

        try
        {
            await _suppliers.AddAsync(entity);
            _toasts.Success("Поставщик добавлен", entity.Name);
        }
        catch (Exception ex)
        {
            Items.Remove(entity);
            UpdateStatistics();
            _toasts.Error("Не удалось сохранить поставщика", ex.Message);
        }
    }

    [RelayCommand]
    private async Task EditAsync(Supplier? supplier)
    {
        if (!CanEdit || supplier is null) return;

        var updated = _dialogs.EditSupplier(supplier);
        if (updated is null) return;

        var index = Items.IndexOf(supplier);
        if (index < 0) return;

        Items[index] = updated;
        UpdateStatistics();

        try
        {
            await _suppliers.UpdateAsync(updated);

            updated.Materials = supplier.Materials;
            Items[index] = updated;

            _toasts.Success("Изменения сохранены", updated.Name);
        }
        catch (Exception ex)
        {
            Items[index] = supplier;
            UpdateStatistics();
            _toasts.Error("Не удалось изменить поставщика", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Supplier? supplier)
    {
        if (!CanEdit || supplier is null) return;

        var confirmed = _dialogs.Confirm(
            "Удалить поставщика?",
            $"Поставщик «{supplier.Name}» будет удалён. Материалы останутся в справочнике, но потеряют привязку.",
            "Удалить");

        if (!confirmed) return;

        var index = Items.IndexOf(supplier);
        Items.Remove(supplier);
        UpdateStatistics();

        try
        {
            await _suppliers.DeleteAsync(supplier.Id);
            _toasts.Success("Поставщик удалён", supplier.Name);
        }
        catch (Exception ex)
        {
            if (index >= 0 && index <= Items.Count)
                Items.Insert(index, supplier);

            UpdateStatistics();
            _toasts.Error("Не удалось удалить поставщика", ex.Message);
        }
    }

    private bool FilterItem(object item)
    {
        if (item is not Supplier supplier) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var term = SearchText.Trim();
        return supplier.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
               || supplier.Unp.Contains(term, StringComparison.OrdinalIgnoreCase)
               || supplier.ContactPerson.Contains(term, StringComparison.OrdinalIgnoreCase)
               || supplier.Address.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateStatistics()
    {
        VisibleCount = ItemsView.Cast<object>().Count();
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(LinkedMaterialsCount));
        OnPropertyChanged(nameof(AverageRating));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsFilteredEmpty));
    }
}
