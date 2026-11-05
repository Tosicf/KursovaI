using StroyControl.Core.Entities;
using StroyControl.Core.Enums;

namespace StroyControl.Core.Interfaces;

/// <summary>Текущая сессия пользователя (аутентификация на стороне клиента).</summary>
public interface ISessionService
{
    User? CurrentUser { get; }

    bool IsAuthenticated { get; }

    event EventHandler? Changed;

    void SignIn(User user);

    void SignOut();

    bool IsInRole(params UserRole[] roles);

    /// <summary>Может изменять справочники и документы.</summary>
    bool CanEdit { get; }

    /// <summary>Может управлять учётными записями.</summary>
    bool CanManageUsers { get; }

    /// <summary>Видит финансовые суммы (сметы, отклонения).</summary>
    bool CanViewFinance { get; }
}
