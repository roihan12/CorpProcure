namespace CorpProcure.DTOs.Department;
public class DepartmentListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ManagerName { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
}