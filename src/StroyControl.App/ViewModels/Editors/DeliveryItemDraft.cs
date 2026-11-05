using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Entities;

namespace StroyControl.App.ViewModels.Editors;

/// <summary>Редактируемая строка поступления (до сохранения документа).</summary>
public sealed partial class DeliveryItemDraft : ObservableObject
{
    [ObservableProperty]
    private Material? material;

    [ObservableProperty]
    private decimal quantity;

    [ObservableProperty]
    private decimal unitPrice;

    public decimal Cost => Quantity * UnitPrice;

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(Cost));

    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(Cost));

    partial void OnMaterialChanged(Material? value)
    {
        if (value is not null && UnitPrice == 0m)
            UnitPrice = value.Price;
    }
}
