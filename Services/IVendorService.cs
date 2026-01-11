using CorpProcure.DTOs.Vendor;
using CorpProcure.Models;
using CorpProcure.Models.Enums;

namespace CorpProcure.Services;

/// <summary>
/// Interface untuk Vendor Management Service
/// </summary>
public interface IVendorService
{
    /// <summary>
    /// Mendapatkan semua vendor
    /// </summary>
    Task<Result<List<VendorListDto>>> GetAllAsync();

    /// <summary>
    /// Mendapatkan vendor dengan paginasi dan filter
    /// </summary>
    Task<Result<(List<VendorListDto> Items, int TotalCount)>> GetAllPaginatedAsync(
        string? searchTerm,
        VendorStatus? statusFilter,
        VendorCategory? categoryFilter,
        int page,
        int pageSize);

    /// <summary>
    /// Mendapatkan detail vendor berdasarkan ID
    /// </summary>
    Task<Result<VendorDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// Membuat vendor baru
    /// </summary>
    Task<Result<Guid>> CreateAsync(CreateVendorDto dto, Guid userId);

    /// <summary>
    /// Mengupdate vendor
    /// </summary>
    Task<Result<bool>> UpdateAsync(UpdateVendorDto dto, Guid userId);

    /// <summary>
    /// Menghapus vendor (soft delete)
    /// </summary>
    Task<Result<bool>> DeleteAsync(Guid id, Guid userId);

    /// <summary>
    /// Mengubah status vendor
    /// </summary>
    Task<Result<bool>> ChangeStatusAsync(Guid id, VendorStatus newStatus, string? reason, Guid userId);

    /// <summary>
    /// Mendapatkan daftar vendor untuk dropdown
    /// </summary>
    Task<Result<List<VendorDropdownDto>>> GetForDropdownAsync(bool excludeBlacklisted = true);

    /// <summary>
    /// Generate kode vendor baru
    /// </summary>
    Task<string> GenerateCodeAsync();
}
