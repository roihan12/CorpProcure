namespace CorpProcure.DTOs.Auth;

/// <summary>
/// DTO untuk response hasil autentikasi
/// </summary>
public class AuthResultDto
{
    /// <summary>
    /// Flag sukses/gagal
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Pesan informasi
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// JWT token (jika menggunakan JWT authentication)
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Token expiration (UTC)
    /// </summary>
    public DateTime? TokenExpiration { get; set; }

    /// <summary>
    /// Informasi user yang terautentikasi
    /// </summary>
    public UserDto? User { get; set; }

    /// <summary>
    /// Daftar error messages (jika ada)
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
