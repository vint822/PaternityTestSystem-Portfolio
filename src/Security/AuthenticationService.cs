using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PaternityTestSystem.Portfolio.Security
{
    /// <summary>
    /// Authentication service.
    /// Production repositories are intentionally not included
    /// in this portfolio.
    /// </summary>
    public class AuthenticationService
    {
        private readonly IUserLookup _userRepository;
        private readonly IAuthenticationAuditWriter _auditLogRepository;

        public AuthenticationService(
            IUserLookup userRepository,
            IAuthenticationAuditWriter auditLogRepository)
        {
            _userRepository =
                userRepository
                ?? throw new ArgumentNullException(
                    nameof(userRepository));

            _auditLogRepository =
                auditLogRepository
                ?? throw new ArgumentNullException(
                    nameof(auditLogRepository));
        }

        /// <summary>
        /// 驗證使用者登入
        /// </summary>
        public async Task<LoginResult> LoginAsync(
            string username,
            string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                return new LoginResult(
                    false,
                    "請輸入帳號與密碼",
                    null);
            }

            try
            {
                var user =
                    await _userRepository
                        .FindByNameAsync(username);

                if (user == null ||
                    !user.IsActive)
                {
                    return new LoginResult(
                        false,
                        "帳號不存在或已停用",
                        null);
                }

                if (!BCrypt.Net.BCrypt.Verify(
                        password,
                        user.PasswordHash))
                {
                    return new LoginResult(
                        false,
                        "密碼錯誤",
                        null);
                }

                await _auditLogRepository.WriteAsync(
                    user.Id,
                    AuthenticationAction.Login);

                return new LoginResult(
                    true,
                    "登入成功",
                    new AuthenticatedUser(
                        user.Id,
                        user.DisplayName));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                return new LoginResult(
                    false,
                    "登入失敗，請聯絡系統管理員。",
                    null);
            }
        }

        /// <summary>
        /// 記錄登出
        /// </summary>
        public async Task LogoutAsync(
            AuthenticatedUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(
                    nameof(user));
            }

            try
            {
                await _auditLogRepository.WriteAsync(
                    user.Id,
                    AuthenticationAction.Logout);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"記錄登出失敗：{ex.Message}");
            }
        }
    }
}
