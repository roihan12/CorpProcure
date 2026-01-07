namespace CorpProcure.Services;

/// <summary>
/// Service interface untuk generate Purchase Order PDF
/// </summary>
public interface IPurchaseOrderPdfService
{
    /// <summary>
    /// Generate PDF bytes for a Purchase Order
    /// </summary>
    /// <param name="purchaseRequestId">ID of the approved purchase request</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> GeneratePdfAsync(Guid purchaseRequestId);
}
