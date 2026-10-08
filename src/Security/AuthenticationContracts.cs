namespace PaternityTestSystem.Portfolio.Security;

/// <summary>Infrastructure input only; never return this record to a view.</summary>
public sealed record StoredUser(string Id, string DisplayName, string PasswordHash, bool IsActive);

public sealed record AuthenticatedUser(string Id, string DisplayName);

public sealed record LoginResult(bool Success, string Message, AuthenticatedUser? User);

public enum AuthenticationAction
{
    Login,
    Logout
}

public interface IUserLookup
{
    Task<StoredUser?> FindByNameAsync(string username, CancellationToken cancellationToken = default);
}

public interface IAuthenticationAuditWriter
{
    Task WriteAsync(string userId, AuthenticationAction action, CancellationToken cancellationToken = default);
}
