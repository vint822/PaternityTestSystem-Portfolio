using System.Diagnostics;

namespace PaternityTestSystem.Portfolio.Security;

/// <summary>
/// Demonstrates lookup, BCrypt verification and audit boundaries. Account storage,
/// session lifecycle, authorization and login throttling belong to the calling app.
/// </summary>
public sealed class AuthenticationService
{
    private readonly IUserLookup users;
    private readonly IAuthenticationAuditWriter audit;

    public AuthenticationService(IUserLookup users, IAuthenticationAuditWriter audit)
    {
        this.users = users ?? throw new ArgumentNullException(nameof(users));
        this.audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<LoginResult> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new LoginResult(false, "Enter a username and password.", null);
        }

        try
        {
            var user = await users.FindByNameAsync(username.Trim(), cancellationToken);
            if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return new LoginResult(false, "Unable to sign in with these credentials.", null);
            }

            // A successful result is returned only after its audit record is written.
            await audit.WriteAsync(user.Id, AuthenticationAction.Login, cancellationToken);
            return new LoginResult(true, "Signed in.", new AuthenticatedUser(user.Id, user.DisplayName));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            return new LoginResult(false, "Sign-in is unavailable. Contact your administrator.", null);
        }
    }

    /// <summary>
    /// Records logout and reports whether the audit write succeeded. The calling app
    /// must clear its session even if this method returns false.
    /// </summary>
    public async Task<bool> LogoutAsync(AuthenticatedUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        try
        {
            await audit.WriteAsync(user.Id, AuthenticationAction.Logout, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            return false;
        }
    }
}
