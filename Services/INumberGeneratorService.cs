namespace CorpProcure.Services
{
    public interface INumberGeneratorService
    {
        /// <summary>
        /// Generate nomor Purchase Request (Format: PR-YYYYMM-0001)
        /// </summary>
        Task<string> GeneratePurchaseRequestNumberAsync();

        /// <summary>
        /// Generate nomor Purchase Order (Format: PO-YYYYMM-0001)
        /// </summary>
        Task<string> GeneratePurchaseOrderNumberAsync();
    }
}
