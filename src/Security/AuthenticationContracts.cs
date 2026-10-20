using System.Threading.Tasks;

namespace PaternityTestSystem.Portfolio.Security
{
    /// <summary>
    /// Minimal user data required by the portfolio authentication flow.
    /// Production user repositories and database models are not included.
    /// </summary>
    public class StoredUser
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class AuthenticatedUser
    {
        public string Id { get; }
        public string DisplayName { get; }

        public AuthenticatedUser(
            string id,
            string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }
    }

    public class LoginResult
    {
        public bool Success { get; }
        public string Message { get; }
        public AuthenticatedUser? User { get; }

        public LoginResult(
            bool success,
            string message,
            AuthenticatedUser? user)
        {
            Success = success;
            Message = message;
            User = user;
        }
    }

    public enum AuthenticationAction
    {
        Login,
        Logout
    }

    public interface IUserLookup
    {
        Task<StoredUser?> FindByNameAsync(
            string username);
    }

    public interface IAuthenticationAuditWriter
    {
        Task WriteAsync(
            string userId,
            AuthenticationAction action);
    }
}
