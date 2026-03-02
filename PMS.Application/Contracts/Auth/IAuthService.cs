using PMS.Application.Models.Auth;

namespace PMS.Application.Contracts.Auth;

public interface IAuthService
{
    Task<LoginResultDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthSessionDto?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(string accessToken, CancellationToken cancellationToken = default);
}
