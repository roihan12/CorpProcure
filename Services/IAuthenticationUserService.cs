using CorpProcure.DTOs.Auth;

namespace CorpProcure.Services;
/// <summary>
/// Interface untuk authentication service
/// </summary>
public interface IAuthenticationUserService
{
    /// <summary>
    /// Register user baru
    /// </summary>
    Task<AuthResultDto> RegisterAsync(RegisterDto dto);

    /// <summary>
    /// Login user dengan email dan password
    /// </summary>
    Task<AuthResultDto> LoginAsync(LoginDto dto);

    /// <summary>
    /// Logout user yang sedang login
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Ubah password user
    /// </summary>
    Task<AuthResultDto> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

    /// <summary>
    /// Reset password user (generate token untuk email)
    /// </summary>
    Task<AuthResultDto> InitiatePasswordResetAsync(string email);

    /// <summary>
    /// Konfirmasi reset password dengan token
    /// </summary>
    Task<AuthResultDto> ResetPasswordAsync(string email, string token, string newPassword);

    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Get user by email
    /// </summary>
    Task<UserDto?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Update user profile
    /// </summary>
    Task<AuthResultDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
}
