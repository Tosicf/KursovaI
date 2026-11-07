using StroyControl.Core.Entities;
using StroyControl.Core.Enums;

namespace StroyControl.Core.Interfaces;

/// <summary>
/// Текущая сессия пользователя (аутентификация на стороне клиента).
/// Права разбиты по видам деятельности, а не одним флагом «редактирование»:
/// прораб работает с фактом, снабженец — с закупками, ИТО — со сметами.
/// </summary>
public interface ISessionService
{
    User? CurrentUser { get; }

    bool IsAuthenticated { get; }

    event EventHandler? Changed;

    void SignIn(User user);

    void SignOut();

    bool IsInRole(params UserRole[] roles);

    /// <summary>Справочник материалов (номенклатура, цены, минимальные остатки).</summary>
    bool CanEditMaterials { get; }

    /// <summary>Справочник поставщиков.</summary>
    bool CanEditSuppliers { get; }

    /// <summary>Справочник строительных объектов.</summary>
    bool CanEditProjects { get; }

    /// <summary>Сметы и их позиции (план). Меняет только ИТО и администратор.</summary>
    bool CanEditEstimates { get; }

    /// <summary>Поступления материалов.</summary>
    bool CanEditDeliveries { get; }

    /// <summary>Списания материалов (факт).</summary>
    bool CanEditUsages { get; }

    /// <summary>Может управлять учётными записями.</summary>
    bool CanManageUsers { get; }

    /// <summary>Видит финансовые суммы (сметы, отклонения, стоимость склада).</summary>
    bool CanViewFinance { get; }

    /// <summary>Может формировать и выгружать отчёты (CSV/Excel).</summary>
    bool CanExportReports { get; }
}
