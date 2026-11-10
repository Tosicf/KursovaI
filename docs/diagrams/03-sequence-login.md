# Диаграмма последовательности: вход в систему

![Диаграмма последовательности](img/03-sequence-login.png)

```mermaid
sequenceDiagram
    actor U as Пользователь
    participant LW as LoginWindow
    participant VM as LoginViewModel
    participant AUTH as AuthService
    participant REPO as UserRepository
    participant DB as SQL Server
    participant S as SessionService
    participant MW as MainWindow

    U->>LW: логин и пароль
    LW->>VM: LoginCommand
    VM->>AUTH: SignInAsync(login, password)
    AUTH->>REPO: FindByLoginAsync(login)
    REPO->>DB: SELECT … FROM dbo.users
    DB-->>REPO: строка пользователя
    REPO-->>AUTH: User
    AUTH->>AUTH: Verify(password, salt, hash)
    alt пароль неверен
        AUTH-->>VM: «Неверный логин или пароль»
    else пароль верен
        AUTH->>REPO: UpdateLastLoginAsync(now)
        REPO->>DB: UPDATE dbo.users
        AUTH->>S: SignIn(user)
        AUTH-->>VM: User
        VM-->>LW: DialogResult = true
        LW->>MW: открыть главное окно
    end
```

Пароль не передаётся и не хранится в открытом виде: `AuthService` получает из базы хеш и соль, а проверка выполняется по схеме PBKDF2-HMAC-SHA256 (100 000 итераций). После успешного входа `SessionService` сохраняет пользователя и вычисляет права роли.
