using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;

namespace StroyControl.Core.Services;

/// <summary>
/// Хранит данные вошедшего пользователя и его права доступа.
///
/// Матрица прав (по возрастанию специализации):
/// <list type="bullet">
/// <item><b>Admin</b> — полный доступ, включая сотрудников.</item>
/// <item><b>Engineer (ИТО)</b> — сметы, справочники, документы, отчёты.</item>
/// <item><b>SupplyManager (снабженец)</b> — поставщики и поступления, видит бюджет по смете.</item>
/// <item><b>Foreman (прораб)</b> — только факт: поступления и списания.</item>
/// <item><b>Accountant (бухгалтер)</b> — просмотр финансов и экспорт отчётов.</item>
/// </list>
/// </summary>
public sealed class SessionService : ISessionService
{
    public User? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public event EventHandler? Changed;

    public void SignIn(User user)
    {
        CurrentUser = user;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        CurrentUser = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool IsInRole(params UserRole[] roles) =>
        CurrentUser is not null && roles.Contains(CurrentUser.Role);

    // Справочник материалов ведут ИТО и администратор: снабженец закупает по уже
    // заведённой номенклатуре, прораб на объекте её не меняет.
    public bool CanEditMaterials =>
        IsInRole(UserRole.Admin, UserRole.Engineer);

    // Поставщиков заводит снабженец — это его зона ответственности наряду с поступлениями.
    public bool CanEditSuppliers =>
        IsInRole(UserRole.Admin, UserRole.Engineer, UserRole.SupplyManager);

    public bool CanEditProjects =>
        IsInRole(UserRole.Admin, UserRole.Engineer);

    // Смету (план) меняет только ИТО: прораб и снабженец работают с фактом.
    public bool CanEditEstimates =>
        IsInRole(UserRole.Admin, UserRole.Engineer);

    // Поступления оформляют снабженец и прораб (приёмка материала на объекте).
    public bool CanEditDeliveries =>
        IsInRole(UserRole.Admin, UserRole.Engineer, UserRole.SupplyManager, UserRole.Foreman);

    // Списания — факт расхода на объекте: прораб и ИТО.
    public bool CanEditUsages =>
        IsInRole(UserRole.Admin, UserRole.Engineer, UserRole.Foreman);

    public bool CanManageUsers => IsInRole(UserRole.Admin);

    // Бюджет видят все роли: снабженцу нужно понимать остаток по смете,
    // бухгалтеру и прорабу — фактическую стоимость работ.
    public bool CanViewFinance =>
        IsInRole(UserRole.Admin, UserRole.Engineer, UserRole.SupplyManager, UserRole.Foreman, UserRole.Accountant);

    // Экспорт — для тех, кто формирует отчётность: ИТО, бухгалтер, администратор.
    public bool CanExportReports =>
        IsInRole(UserRole.Admin, UserRole.Engineer, UserRole.Accountant);
}
