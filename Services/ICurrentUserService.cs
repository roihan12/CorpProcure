namespace CorpProcure.Services;

/// <summary>
/// Interface untuk mendapatkan informasi current user yang sedang login
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Get User ID dari current authenticated user
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Get User Name (email) dari current authenticated user
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Check apakah user sudah authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}
