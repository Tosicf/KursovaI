using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Services;
using Xunit;

namespace StroyControl.Core.Tests;

/// <summary>
/// Матрица прав проверяется отдельно от UI: разграничение доступа — часть
/// бизнес-логики, поэтому оно должно быть зафиксировано тестами.
/// </summary>
public sealed class SessionServiceTests
{
    private static SessionService ForRole(UserRole role)
    {
        var session = new SessionService();
        session.SignIn(new User { Login = "test", DisplayName = "Тест Тестов", Role = role });
        return session;
    }

    [Fact]
    public void Admin_HasFullAccess()
    {
        var session = ForRole(UserRole.Admin);

        Assert.True(session.CanEditMaterials);
        Assert.True(session.CanEditSuppliers);
        Assert.True(session.CanEditProjects);
        Assert.True(session.CanEditEstimates);
        Assert.True(session.CanEditDeliveries);
        Assert.True(session.CanEditUsages);
        Assert.True(session.CanManageUsers);
        Assert.True(session.CanViewFinance);
        Assert.True(session.CanExportReports);
    }

    [Fact]
    public void Engineer_EditsEstimatesButNotUsers()
    {
        var session = ForRole(UserRole.Engineer);

        Assert.True(session.CanEditEstimates);
        Assert.True(session.CanEditMaterials);
        Assert.True(session.CanEditSuppliers);
        Assert.True(session.CanEditProjects);
        Assert.True(session.CanEditDeliveries);
        Assert.True(session.CanEditUsages);
        Assert.False(session.CanManageUsers);
        Assert.True(session.CanViewFinance);
        Assert.True(session.CanExportReports);
    }

    [Fact]
    public void SupplyManager_SeesBudgetButEditsOnlySuppliersAndDeliveries()
    {
        var session = ForRole(UserRole.SupplyManager);

        Assert.True(session.CanEditSuppliers);
        Assert.True(session.CanEditDeliveries);
        Assert.False(session.CanEditMaterials);
        Assert.False(session.CanEditProjects);
        Assert.False(session.CanEditEstimates);
        Assert.False(session.CanEditUsages);
        Assert.False(session.CanManageUsers);

        // Главное требование: снабженец видит, укладывается ли закупка в смету.
        Assert.True(session.CanViewFinance);
    }

    [Fact]
    public void Foreman_WorksWithFactsOnly()
    {
        var session = ForRole(UserRole.Foreman);

        Assert.True(session.CanEditDeliveries);
        Assert.True(session.CanEditUsages);
        Assert.False(session.CanEditEstimates);
        Assert.False(session.CanEditMaterials);
        Assert.False(session.CanEditSuppliers);
        Assert.False(session.CanEditProjects);
        Assert.False(session.CanManageUsers);
        Assert.True(session.CanViewFinance);
    }

    [Fact]
    public void Accountant_ViewsFinanceAndExportsWithoutEditing()
    {
        var session = ForRole(UserRole.Accountant);

        Assert.False(session.CanEditMaterials);
        Assert.False(session.CanEditSuppliers);
        Assert.False(session.CanEditProjects);
        Assert.False(session.CanEditEstimates);
        Assert.False(session.CanEditDeliveries);
        Assert.False(session.CanEditUsages);
        Assert.False(session.CanManageUsers);
        Assert.True(session.CanViewFinance);
        Assert.True(session.CanExportReports);
    }

    [Fact]
    public void Anonymous_HasNoRights()
    {
        var session = new SessionService();

        Assert.False(session.IsAuthenticated);
        Assert.False(session.CanEditMaterials);
        Assert.False(session.CanEditSuppliers);
        Assert.False(session.CanEditProjects);
        Assert.False(session.CanEditEstimates);
        Assert.False(session.CanEditDeliveries);
        Assert.False(session.CanEditUsages);
        Assert.False(session.CanManageUsers);
        Assert.False(session.CanViewFinance);
        Assert.False(session.CanExportReports);
    }

    [Fact]
    public void SignOut_RevokesRights()
    {
        var session = ForRole(UserRole.Admin);
        Assert.True(session.CanManageUsers);

        session.SignOut();

        Assert.False(session.IsAuthenticated);
        Assert.False(session.CanManageUsers);
        Assert.False(session.CanEditEstimates);
    }
}
