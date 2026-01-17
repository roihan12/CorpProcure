namespace CorpProcure.DTOs.Export;

public class ExportFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? VendorId { get; set; }
    public string? Status { get; set; }
}
