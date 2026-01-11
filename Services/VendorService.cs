using CorpProcure.Data;
using CorpProcure.DTOs.Vendor;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CorpProcure.Services;

/// <summary>
/// Service implementation untuk Vendor Management
/// </summary>
public class VendorService : IVendorService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public VendorService(
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    #region Read Operations

    public async Task<Result<List<VendorListDto>>> GetAllAsync()
    {
        try
        {
            var vendors = await _context.Vendors
                .OrderBy(v => v.Name)
                .Select(v => new VendorListDto
                {
                    Id = v.Id,
                    Code = v.Code,
                    Name = v.Name,
                    Category = v.Category,
                    City = v.City,
                    ContactPerson = v.ContactPerson,
                    Phone = v.Phone,
                    Email = v.Email,
                    PaymentTerms = v.PaymentTerms,
                    Rating = v.Rating,
                    Status = v.Status,
                    TotalOrders = v.TotalOrders,
                    TotalOrderValue = v.TotalOrderValue
                })
                .ToListAsync();

            return Result<List<VendorListDto>>.Ok(vendors);
        }
        catch (Exception ex)
        {
            return Result<List<VendorListDto>>.Fail($"Gagal mengambil data vendor: {ex.Message}");
        }
    }

    public async Task<Result<(List<VendorListDto> Items, int TotalCount)>> GetAllPaginatedAsync(
        string? searchTerm,
        VendorStatus? statusFilter,
        VendorCategory? categoryFilter,
        int page,
        int pageSize)
    {
        try
        {
            var query = _context.Vendors.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(v =>
                    v.Code.ToLower().Contains(term) ||
                    v.Name.ToLower().Contains(term) ||
                    (v.ContactPerson != null && v.ContactPerson.ToLower().Contains(term)) ||
                    (v.City != null && v.City.ToLower().Contains(term)) ||
                    (v.TaxId != null && v.TaxId.Contains(term)));
            }

            // Apply status filter
            if (statusFilter.HasValue)
            {
                query = query.Where(v => v.Status == statusFilter.Value);
            }

            // Apply category filter
            if (categoryFilter.HasValue)
            {
                query = query.Where(v => v.Category == categoryFilter.Value);
            }

            var totalCount = await query.CountAsync();

            var vendors = await query
                .OrderBy(v => v.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VendorListDto
                {
                    Id = v.Id,
                    Code = v.Code,
                    Name = v.Name,
                    Category = v.Category,
                    City = v.City,
                    ContactPerson = v.ContactPerson,
                    Phone = v.Phone,
                    Email = v.Email,
                    PaymentTerms = v.PaymentTerms,
                    Rating = v.Rating,
                    Status = v.Status,
                    TotalOrders = v.TotalOrders,
                    TotalOrderValue = v.TotalOrderValue
                })
                .ToListAsync();

            return Result<(List<VendorListDto>, int)>.Ok((vendors, totalCount));
        }
        catch (Exception ex)
        {
            return Result<(List<VendorListDto>, int)>.Fail($"Gagal mengambil data vendor: {ex.Message}");
        }
    }

    public async Task<Result<VendorDetailDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var vendor = await _context.Vendors
                .Where(v => v.Id == id)
                .Select(v => new VendorDetailDto
                {
                    Id = v.Id,
                    Code = v.Code,
                    Name = v.Name,
                    Category = v.Category,
                    Description = v.Description,
                    Address = v.Address,
                    City = v.City,
                    Province = v.Province,
                    PostalCode = v.PostalCode,
                    ContactPerson = v.ContactPerson,
                    ContactTitle = v.ContactTitle,
                    Phone = v.Phone,
                    Mobile = v.Mobile,
                    Email = v.Email,
                    Website = v.Website,
                    TaxId = v.TaxId,
                    BusinessLicense = v.BusinessLicense,
                    LicenseExpiryDate = v.LicenseExpiryDate,
                    BankName = v.BankName,
                    BankBranch = v.BankBranch,
                    AccountNumber = v.AccountNumber,
                    AccountHolderName = v.AccountHolderName,
                    PaymentTerms = v.PaymentTerms,
                    CreditLimit = v.CreditLimit,
                    Rating = v.Rating,
                    TotalOrders = v.TotalOrders,
                    TotalOrderValue = v.TotalOrderValue,
                    Status = v.Status,
                    StatusReason = v.StatusReason,
                    StatusChangedAt = v.StatusChangedAt,
                    ContractStartDate = v.ContractStartDate,
                    ContractEndDate = v.ContractEndDate,
                    Notes = v.Notes,
                    CreatedAt = v.CreatedAt,
                    UpdatedAt = v.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (vendor == null)
            {
                return Result<VendorDetailDto>.Fail("Vendor tidak ditemukan");
            }

            return Result<VendorDetailDto>.Ok(vendor);
        }
        catch (Exception ex)
        {
            return Result<VendorDetailDto>.Fail($"Gagal mengambil detail vendor: {ex.Message}");
        }
    }

    public async Task<Result<List<VendorDropdownDto>>> GetForDropdownAsync(bool excludeBlacklisted = true)
    {
        try
        {
            var query = _context.Vendors.AsQueryable();

            if (excludeBlacklisted)
            {
                query = query.Where(v => v.Status != VendorStatus.Blacklisted);
            }

            var vendors = await query
                .Where(v => v.Status == VendorStatus.Active)
                .OrderBy(v => v.Name)
                .Select(v => new VendorDropdownDto
                {
                    Id = v.Id,
                    Code = v.Code,
                    Name = v.Name,
                    Status = v.Status,
                    PaymentTerms = v.PaymentTerms
                })
                .ToListAsync();

            return Result<List<VendorDropdownDto>>.Ok(vendors);
        }
        catch (Exception ex)
        {
            return Result<List<VendorDropdownDto>>.Fail($"Gagal mengambil data vendor: {ex.Message}");
        }
    }

    #endregion

    #region Write Operations

    public async Task<Result<Guid>> CreateAsync(CreateVendorDto dto, Guid userId)
    {
        try
        {
            // Validate unique name
            var existingByName = await _context.Vendors
                .AnyAsync(v => v.Name.ToLower() == dto.Name.ToLower());

            if (existingByName)
            {
                return Result<Guid>.Fail($"Vendor dengan nama '{dto.Name}' sudah ada");
            }

            // Generate code
            var code = await GenerateCodeAsync();

            var vendor = new Vendor
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = dto.Name,
                Category = dto.Category,
                Description = dto.Description,
                Address = dto.Address,
                City = dto.City,
                Province = dto.Province,
                PostalCode = dto.PostalCode,
                ContactPerson = dto.ContactPerson,
                ContactTitle = dto.ContactTitle,
                Phone = dto.Phone,
                Mobile = dto.Mobile,
                Email = dto.Email,
                Website = dto.Website,
                TaxId = dto.TaxId,
                BusinessLicense = dto.BusinessLicense,
                LicenseExpiryDate = dto.LicenseExpiryDate,
                BankName = dto.BankName,
                BankBranch = dto.BankBranch,
                AccountNumber = dto.AccountNumber,
                AccountHolderName = dto.AccountHolderName,
                PaymentTerms = dto.PaymentTerms,
                CreditLimit = dto.CreditLimit,
                Rating = dto.Rating,
                Status = dto.Status,
                ContractStartDate = dto.ContractStartDate,
                ContractEndDate = dto.ContractEndDate,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogActivityAsync(
                userId,
                "System",
                "Create",
                "Vendors",
                JsonSerializer.Serialize(new { vendor.Code, vendor.Name, vendor.Category }),
                vendor.Id,
                "Vendor");

            return Result<Guid>.Ok(vendor.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Fail($"Gagal membuat vendor: {ex.Message}");
        }
    }

    public async Task<Result<bool>> UpdateAsync(UpdateVendorDto dto, Guid userId)
    {
        try
        {
            var vendor = await _context.Vendors.FindAsync(dto.Id);
            if (vendor == null)
            {
                return Result<bool>.Fail("Vendor tidak ditemukan");
            }

            // Check name uniqueness (excluding current vendor)
            var existingByName = await _context.Vendors
                .AnyAsync(v => v.Id != dto.Id && v.Name.ToLower() == dto.Name.ToLower());

            if (existingByName)
            {
                return Result<bool>.Fail($"Vendor dengan nama '{dto.Name}' sudah ada");
            }

            // Store old values for audit
            var oldValues = JsonSerializer.Serialize(new
            {
                vendor.Name,
                vendor.Category,
                vendor.Status
            });

            // Update fields
            vendor.Name = dto.Name;
            vendor.Category = dto.Category;
            vendor.Description = dto.Description;
            vendor.Address = dto.Address;
            vendor.City = dto.City;
            vendor.Province = dto.Province;
            vendor.PostalCode = dto.PostalCode;
            vendor.ContactPerson = dto.ContactPerson;
            vendor.ContactTitle = dto.ContactTitle;
            vendor.Phone = dto.Phone;
            vendor.Mobile = dto.Mobile;
            vendor.Email = dto.Email;
            vendor.Website = dto.Website;
            vendor.TaxId = dto.TaxId;
            vendor.BusinessLicense = dto.BusinessLicense;
            vendor.LicenseExpiryDate = dto.LicenseExpiryDate;
            vendor.BankName = dto.BankName;
            vendor.BankBranch = dto.BankBranch;
            vendor.AccountNumber = dto.AccountNumber;
            vendor.AccountHolderName = dto.AccountHolderName;
            vendor.PaymentTerms = dto.PaymentTerms;
            vendor.CreditLimit = dto.CreditLimit;
            vendor.Rating = dto.Rating;
            vendor.Status = dto.Status;
            vendor.ContractStartDate = dto.ContractStartDate;
            vendor.ContractEndDate = dto.ContractEndDate;
            vendor.Notes = dto.Notes;
            vendor.UpdatedAt = DateTime.UtcNow;
            vendor.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogActivityAsync(
                userId,
                "System",
                "Update",
                "Vendors",
                JsonSerializer.Serialize(new { vendor.Name, vendor.Category, vendor.Status }),
                vendor.Id,
                "Vendor");

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Gagal mengupdate vendor: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
            {
                return Result<bool>.Fail("Vendor tidak ditemukan");
            }

            // Check if vendor has POs
            var hasPOs = await _context.PurchaseRequests
                .AnyAsync(pr => pr.VendorId == id && pr.PoNumber != null);

            if (hasPOs)
            {
                return Result<bool>.Fail("Vendor tidak dapat dihapus karena sudah memiliki PO");
            }

            // Soft delete
            vendor.IsDeleted = true;
            vendor.DeletedAt = DateTime.UtcNow;
            vendor.DeletedBy = userId;

            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogActivityAsync(
                userId,
                "System",
                "Delete",
                "Vendors",
                JsonSerializer.Serialize(new { vendor.Code, vendor.Name }),
                vendor.Id,
                "Vendor");

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Gagal menghapus vendor: {ex.Message}");
        }
    }

    public async Task<Result<bool>> ChangeStatusAsync(Guid id, VendorStatus newStatus, string? reason, Guid userId)
    {
        try
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
            {
                return Result<bool>.Fail("Vendor tidak ditemukan");
            }

            // Require reason for blacklist
            if (newStatus == VendorStatus.Blacklisted && string.IsNullOrWhiteSpace(reason))
            {
                return Result<bool>.Fail("Alasan blacklist wajib diisi");
            }

            var oldStatus = vendor.Status;
            vendor.UpdateStatus(newStatus, reason);
            vendor.UpdatedAt = DateTime.UtcNow;
            vendor.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogActivityAsync(
                userId,
                "System",
                "Update",
                "Vendors",
                JsonSerializer.Serialize(new { OldStatus = oldStatus.ToString(), NewStatus = newStatus.ToString(), Reason = reason }),
                vendor.Id,
                "Vendor");

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Gagal mengubah status vendor: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    public async Task<string> GenerateCodeAsync()
    {
        var lastVendor = await _context.Vendors
            .IgnoreQueryFilters()
            .OrderByDescending(v => v.Code)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastVendor != null && lastVendor.Code.StartsWith("VND-"))
        {
            var numberPart = lastVendor.Code.Substring(4);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"VND-{nextNumber:D4}";
    }

    #endregion
}
