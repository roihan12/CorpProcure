using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpProcure.Controllers.Api;

/// <summary>
/// API Controller for VendorItem price lookups
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendorItemsController : ControllerBase
{
    private readonly IVendorItemService _vendorItemService;

    public VendorItemsController(IVendorItemService vendorItemService)
    {
        _vendorItemService = vendorItemService;
    }

    /// <summary>
    /// Get contract prices for multiple items from a specific vendor
    /// </summary>
    /// <param name="vendorId">Vendor ID</param>
    /// <param name="request">List of Item IDs</param>
    /// <returns>Dictionary of ItemId -> ContractPrice</returns>
    [HttpPost("prices/{vendorId}")]
    public async Task<IActionResult> GetPricesForItems(Guid vendorId, [FromBody] VendorPricesRequest request)
    {
        if (vendorId == Guid.Empty)
            return BadRequest(new { error = "Invalid vendor ID" });

        if (request?.ItemIds == null || !request.ItemIds.Any())
            return BadRequest(new { error = "Item IDs are required" });

        var result = await _vendorItemService.GetPricesForItemsAsync(vendorId, request.ItemIds);

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { success = true, prices = result.Data });
    }

    /// <summary>
    /// Get contract price for a specific vendor-item combination
    /// </summary>
    [HttpGet("price/{vendorId}/{itemId}")]
    public async Task<IActionResult> GetContractPrice(Guid vendorId, Guid itemId)
    {
        var result = await _vendorItemService.GetContractPriceAsync(vendorId, itemId);

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        if (result.Data == null)
            return Ok(new { success = true, found = false, price = (decimal?)null });

        return Ok(new { 
            success = true, 
            found = true, 
            price = result.Data.ContractPrice,
            itemName = result.Data.ItemName,
            minOrderQty = result.Data.MinOrderQty,
            leadTimeDays = result.Data.LeadTimeDays
        });
    }
}

/// <summary>
/// Request model for getting multiple vendor prices
/// </summary>
public class VendorPricesRequest
{
    public List<Guid> ItemIds { get; set; } = new();
}
