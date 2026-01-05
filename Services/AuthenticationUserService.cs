using CorpProcure.DTOs.Auth;
using CorpProcure.Models;
using Microsoft.AspNetCore.Identity;

namespace CorpProcure.Services;

/// <summary>
/// Implementation Authentication Service menggunakan ASP.NET Core Identity
/// </summary>
public class AuthenticationUserService : IAuthenticationUserService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AuthenticationUserService(
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto)
    {
        try
        {
            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Email sudah terdaftar",
                    Errors = new List<string> { "Email sudah digunakan oleh user lain" }
                };
            }

            // Create new user
            var user = new User
            {
                UserName = dto.Email, // Use email as username
                Email = dto.Email,
                FullName = dto.Name,
                DepartmentId = dto.DepartmentId,
                Role = dto.Role,
                PhoneNumber = dto.PhoneNumber,
                IsActive = true,
                EmailConfirmed = true // Auto-confirm untuk development, set false di production
            };

            // Create user with password
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Registrasi gagal",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            // Add custom claim for UserRole
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("UserRole", dto.Role.ToString()));
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("DepartmentId", dto.DepartmentId.ToString()));

            return new AuthResultDto
            {
                Success = true,
                Message = "Registrasi berhasil",
                User = MapToUserDto(user)
            };
        }
        catch (Exception ex)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Terjadi kesalahan saat registrasi",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        try
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Email atau password salah",
                    Errors = new List<string> { "Kredensial tidak valid" }
                };
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Akun Anda tidak aktif",
                    Errors = new List<string> { "Silakan hubungi administrator" }
                };
            }

            // Check if user is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Akun Anda terkunci",
                    Errors = new List<string> { "Terlalu banyak percobaan login gagal. Silakan coba lagi nanti." }
                };
            }

            // Attempt sign in
            var result = await _signInManager.PasswordSignInAsync(
                user,
                dto.Password,
                dto.RememberMe,
                lockoutOnFailure: true // Enable lockout on failed attempts
            );

            if (!result.Succeeded)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Email atau password salah",
                    Errors = new List<string> { "Kredensial tidak valid" }
                };
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return new AuthResultDto
            {
                Success = true,
                Message = "Login berhasil",
                User = MapToUserDto(user)
            };
        }
        catch (Exception ex)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Terjadi kesalahan saat login",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<AuthResultDto> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "User tidak ditemukan",
                    Errors = new List<string> { "User tidak valid" }
                };
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Gagal mengubah password",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            return new AuthResultDto
            {
                Success = true,
                Message = "Password berhasil diubah"
            };
        }
        catch (Exception ex)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Terjadi kesalahan saat mengubah password",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<AuthResultDto> InitiatePasswordResetAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that user doesn't exist (security best practice)
                return new AuthResultDto
                {
                    Success = true,
                    Message = "Jika email terdaftar, link reset password akan dikirim"
                };
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // TODO: Send email with reset token
            // For now, just return success with token (remove token from response in production)

            return new AuthResultDto
            {
                Success = true,
                Message = "Link reset password telah dikirim ke email Anda",
                Token = token // Remove this in production, send via email
            };
        }
        catch (Exception ex)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Terjadi kesalahan saat memproses reset password",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<AuthResultDto> ResetPasswordAsync(string email, string token, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "User tidak ditemukan",
                    Errors = new List<string> { "Email tidak terdaftar" }
                };
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Gagal reset password",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            return new AuthResultDto
            {
                Success = true,
                Message = "Password berhasil direset"
            };
        }
        catch (Exception ex)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Terjadi kesalahan saat reset password",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user != null ? MapToUserDto(user) : null;
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null ? MapToUserDto(user) : null;
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.FullName!,
            Email = user.Email!,
            Role = user.Role,
            DepartmentId = user.DepartmentId,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt
        };
    }
}
