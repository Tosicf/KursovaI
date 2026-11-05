using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;

namespace StroyControl.Core.Services;

/// <summary>Хранит данные вошедшего пользователя и его права доступа.</summary>
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

    public bool CanEdit => IsInRole(UserRole.Admin, UserRole.Engineer, UserRole.SupplyManager, UserRole.Foreman);

    public bool CanManageUsers => IsInRole(UserRole.Admin);

    public bool CanViewFinance => IsInRole(UserRole.Admin, UserRole.Engineer, UserRole.Accountant, UserRole.Foreman);
}
