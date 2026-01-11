using CorpProcure.Models.Enums;

namespace CorpProcure.DTOs.Vendor;

/// <summary>
/// DTO untuk detail vendor (Details view)
/// </summary>
public class VendorDetailDto
{
    public Guid Id { get; set; }

    #region Basic Information
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public VendorCategory Category { get; set; }
    public string CategoryDisplay => Category switch
    {
        VendorCategory.Goods => "Barang",
        VendorCategory.Services => "Jasa",
        VendorCategory.Both => "Barang & Jasa",
        _ => "-"
    };
    public string? Description { get; set; }
    #endregion

    #region Contact Information
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string FullAddress => string.Join(", ", 
        new[] { Address, City, Province, PostalCode }
        .Where(s => !string.IsNullOrEmpty(s)));
    public string? ContactPerson { get; set; }
    public string? ContactTitle { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    #endregion

    #region Legal & Tax Information
    public string? TaxId { get; set; }
    public string? BusinessLicense { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }
    public bool IsLicenseExpired => LicenseExpiryDate.HasValue && LicenseExpiryDate.Value < DateTime.UtcNow;
    public bool IsLicenseExpiringSoon => LicenseExpiryDate.HasValue && 
        LicenseExpiryDate.Value >= DateTime.UtcNow && 
        LicenseExpiryDate.Value <= DateTime.UtcNow.AddMonths(3);
    #endregion

    #region Banking Information
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountHolderName { get; set; }
    #endregion

    #region Payment Terms
    public PaymentTermType PaymentTerms { get; set; }
    public string PaymentTermsDisplay => PaymentTerms switch
    {
        PaymentTermType.Immediate => "Immediate",
        PaymentTermType.Net15 => "Net 15",
        PaymentTermType.Net30 => "Net 30",
        PaymentTermType.Net45 => "Net 45",
        PaymentTermType.Net60 => "Net 60",
        _ => "-"
    };
    public decimal? CreditLimit { get; set; }
    #endregion

    #region Performance & Status
    public int Rating { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalOrderValue { get; set; }
    public VendorStatus Status { get; set; }
    public string StatusDisplay => Status switch
    {
        VendorStatus.PendingReview => "Pending Review",
        VendorStatus.Active => "Aktif",
        VendorStatus.Inactive => "Tidak Aktif",
        VendorStatus.Blacklisted => "Blacklist",
        _ => "-"
    };
    public string StatusBadgeClass => Status switch
    {
        VendorStatus.PendingReview => "bg-warning",
        VendorStatus.Active => "bg-success",
        VendorStatus.Inactive => "bg-secondary",
        VendorStatus.Blacklisted => "bg-danger",
        _ => "bg-secondary"
    };
    public string? StatusReason { get; set; }
    public DateTime? StatusChangedAt { get; set; }
    #endregion

    #region Contract Information
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public bool IsContractActive => ContractStartDate.HasValue && ContractEndDate.HasValue &&
        DateTime.UtcNow >= ContractStartDate.Value && DateTime.UtcNow <= ContractEndDate.Value;
    public string? Notes { get; set; }
    #endregion

    #region Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    #endregion
}
