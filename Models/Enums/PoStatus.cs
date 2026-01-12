using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models.Enums;

/// <summary>
/// Status Purchase Order
/// </summary>
public enum PoStatus
{
    [Display(Name = "Draft")]
    Draft = 0,
    
    [Display(Name = "Issued")]
    Issued = 1,
    
    [Display(Name = "Acknowledged")]
    Acknowledged = 2,
    
    [Display(Name = "Partial Received")]
    PartialReceived = 3,
    
    [Display(Name = "Received")]
    Received = 4,

    [Display(Name = "Invoiced")]
    Invoiced = 5,
    
    [Display(Name = "Closed")]
    Closed = 6,
    
    [Display(Name = "Cancelled")]
    Cancelled = 99
}
