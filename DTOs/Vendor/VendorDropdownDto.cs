using CorpProcure.Models.Enums;

namespace CorpProcure.DTOs.Vendor;

/// <summary>
/// DTO untuk dropdown vendor selection
/// </summary>
public class VendorDropdownDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayText => $"{Code} - {Name}";
    public VendorStatus Status { get; set; }
    public PaymentTermType PaymentTerms { get; set; }
}
