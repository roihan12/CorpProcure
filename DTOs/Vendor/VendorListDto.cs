using CorpProcure.Models.Enums;

namespace CorpProcure.DTOs.Vendor;

/// <summary>
/// DTO untuk daftar vendor (Index view)
/// </summary>
public class VendorListDto
{
    public Guid Id { get; set; }
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
    public string? City { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
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
    public int Rating { get; set; }
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
    public int TotalOrders { get; set; }
    public decimal TotalOrderValue { get; set; }
}
