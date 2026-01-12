using CorpProcure.Data;
using CorpProcure.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace CorpProcure.Services;

/// <summary>
/// Service implementation untuk generate Purchase Order PDF
/// </summary>
public class PurchaseOrderPdfService : IPurchaseOrderPdfService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PurchaseOrderPdfService> _logger;
    private readonly IConfiguration _configuration;

    public PurchaseOrderPdfService(
        ApplicationDbContext context,
        ILogger<PurchaseOrderPdfService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        
        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePdfAsync(Guid purchaseRequestId)
    {
        var request = await _context.PurchaseRequests
            .Include(p => p.Requester)
            .Include(p => p.Department)
            .Include(p => p.ManagerApprover)
            .Include(p => p.FinanceApprover)
            .Include(p => p.PurchaseOrders)
                .ThenInclude(po => po.Vendor)
            .Include(p => p.PurchaseOrders)
                .ThenInclude(po => po.Items)
            .FirstOrDefaultAsync(p => p.Id == purchaseRequestId);

        if (request == null)
        {
            throw new InvalidOperationException("Purchase request not found");
        }

        var po = request.PurchaseOrders.OrderByDescending(x => x.GeneratedAt).FirstOrDefault();

        if (po == null)
        {
            throw new InvalidOperationException("PO has not been generated for this request");
        }

        _logger.LogInformation("Generating PDF for PO {PoNumber}", po.PoNumber);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, request, po));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(innerCol =>
                {
                    innerCol.Item().Text("CORP PROCURE")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    innerCol.Item().Text("Purchase Order Document")
                        .FontSize(12)
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(100).AlignRight().Text(text =>
                {
                    text.Span("Generated: ").FontSize(8);
                    text.Span(DateTime.Now.ToString("dd MMM yyyy")).FontSize(8).Bold();
                });
            });

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
        });
    }

    private void ComposeContent(IContainer container, PurchaseRequest request, PurchaseOrder po)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // 1. PO Header & Title
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("PURCHASE ORDER").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"PO No: {po.PoNumber}").FontSize(12).Bold();
                    col.Item().PaddingTop(5).Text($"Status: {po.Status}").FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"Date: {po.PoDate:dd MMM yyyy}").FontSize(10).Bold();
                    if (!string.IsNullOrEmpty(po.QuotationReference))
                        col.Item().Text($"Ref: {po.QuotationReference}").FontSize(10);
                });
            });

            column.Item().PaddingTop(20);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(20);

            // 2. Addresses (Vendor vs Shipping/Billing)
            column.Item().Row(row =>
            {
                // Vendor (Left)
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("VENDOR").FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text(po.Vendor.Name).FontSize(11).Bold();
                    col.Item().Text(po.Vendor.Address ?? "-").FontSize(10);
                    if (!string.IsNullOrEmpty(po.Vendor.City))
                        col.Item().Text($"{po.Vendor.City} {po.Vendor.PostalCode}").FontSize(10);
                    
                    col.Item().PaddingTop(5);
                    col.Item().Text($"Attn: {po.Vendor.ContactPerson ?? "-"}").FontSize(9);
                    col.Item().Text($"Phone: {po.Vendor.Phone ?? "-"}").FontSize(9);
                    col.Item().Text($"Email: {po.Vendor.Email ?? "-"}").FontSize(9);
                });

                row.ConstantItem(20); // Spacer

                // Ship To / Bill To (Right)
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("SHIP TO").FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text(po.ShippingAddress).FontSize(10);
                    
                    col.Item().PaddingTop(10);
                    col.Item().Text("BILL TO").FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text(po.BillingAddress).FontSize(10);
                });
            });

            column.Item().PaddingTop(20);

            // 3. PO Details Grid
            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c => { c.Item().Text("Payment Terms").FontSize(8).FontColor(Colors.Grey.Darken1); c.Item().Text(po.PaymentTerms.ToString()).FontSize(9).Bold(); });
                row.RelativeItem().Column(c => { c.Item().Text("Incoterms").FontSize(8).FontColor(Colors.Grey.Darken1); c.Item().Text(po.Incoterms ?? "-").FontSize(9).Bold(); });
                row.RelativeItem().Column(c => { c.Item().Text("Currency").FontSize(8).FontColor(Colors.Grey.Darken1); c.Item().Text(po.Currency).FontSize(9).Bold(); });
                row.RelativeItem().Column(c => { c.Item().Text("Expected Delivery").FontSize(8).FontColor(Colors.Grey.Darken1); c.Item().Text(po.ExpectedDeliveryDate?.ToString("dd MMM yyyy") ?? "-").FontSize(9).Bold(); });
            });

            column.Item().PaddingTop(20);

            // 4. Items Table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(3); // Name & Desc
                    columns.ConstantColumn(60); // Qty
                    columns.RelativeColumn(); // Unit Price
                    columns.RelativeColumn(); // Total
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("#").FontSize(9).FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Description").FontSize(9).FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter().Text("Qty").FontSize(9).FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Unit Price").FontSize(9).FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Total").FontSize(9).FontColor(Colors.White).Bold();
                });

                // Rows
                var i = 1;
                foreach (var item in po.Items)
                {
                    var bg = i % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(i.ToString()).FontSize(9);
                    
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Column(c => 
                    {
                        c.Item().Text(item.ItemName).FontSize(9).Bold();
                        if(!string.IsNullOrEmpty(item.Description)) c.Item().Text(item.Description).FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                    });

                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{item.Quantity} {item.UoM}").FontSize(9);
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{po.Currency} {item.UnitPrice:N0}").FontSize(9);
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{po.Currency} {item.TotalPrice:N0}").FontSize(9);
                    i++;
                }
            });

            // 5. Financial Summary (Right Aligned)
            column.Item().PaddingTop(10).Row(row =>
            {
                // Left side: Notes
                row.RelativeItem(2).Column(col =>
                {
                    col.Item().Text("Notes / Special Instructions:").FontSize(9).Bold();
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(po.Notes ?? "-").FontSize(9);
                    
                    col.Item().PaddingTop(10).Text("Amount in Words:").FontSize(9).Bold();
                    // Optional: Add NumberToWords converter logic here if available
                    col.Item().Text("-").FontSize(9).Italic();
                });

                row.ConstantItem(20);

                // Right side: Totals
                row.RelativeItem(1).Column(col =>
                {
                    col.Item().Row(r => { r.RelativeItem().Text("Subtotal:").FontSize(9); r.RelativeItem().AlignRight().Text($"{po.Currency} {po.Subtotal:N0}").FontSize(9); });
                    
                    if(po.Discount > 0)
                        col.Item().Row(r => { r.RelativeItem().Text("Discount:").FontSize(9).FontColor(Colors.Red.Medium); r.RelativeItem().AlignRight().Text($"- {po.Currency} {po.Discount:N0}").FontSize(9).FontColor(Colors.Red.Medium); });
                    
                    col.Item().Row(r => { r.RelativeItem().Text($"Tax ({po.TaxRate:0.##}%):").FontSize(9); r.RelativeItem().AlignRight().Text($"{po.Currency} {po.TaxAmount:N0}").FontSize(9); });
                    
                    if(po.ShippingCost > 0)
                        col.Item().Row(r => { r.RelativeItem().Text("Shipping:").FontSize(9); r.RelativeItem().AlignRight().Text($"{po.Currency} {po.ShippingCost:N0}").FontSize(9); });

                    col.Item().PaddingVertical(5).LineHorizontal(1);
                    
                    col.Item().Row(r => { r.RelativeItem().Text("Grand Total:").FontSize(11).Bold(); r.RelativeItem().AlignRight().Text($"{po.Currency} {po.GrandTotal:N0}").FontSize(11).Bold(); });
                });
            });

            column.Item().PaddingTop(30);

            // 6. Signatures
            column.Item().Row(row =>
            {
                // Generated By
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Prepared By,").FontSize(9).AlignCenter();
                    c.Item().PaddingTop(40);
                    c.Item().LineHorizontal(1);
                    c.Item().PaddingTop(2).Text(po.GeneratedByUser?.FullName ?? "System").FontSize(9).Bold().AlignCenter();
                    c.Item().Text("Procurement Officer").FontSize(8).AlignCenter();
                });
                
                row.ConstantItem(20);

                // Authorized By
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Authorized By,").FontSize(9).AlignCenter();
                    c.Item().PaddingTop(40);
                    c.Item().LineHorizontal(1);
                    c.Item().PaddingTop(2).Text("Manager / Director").FontSize(9).Bold().AlignCenter();
                    c.Item().Text(po.Vendor.Name).FontSize(8).FontColor(Colors.Transparent); // Spacer
                });
                
                row.ConstantItem(20);

                // Vendor Acceptance
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Accepted By (Vendor),").FontSize(9).AlignCenter();
                    c.Item().PaddingTop(40);
                    c.Item().LineHorizontal(1);
                    c.Item().PaddingTop(2).Text("Name & Stamp").FontSize(9).Bold().AlignCenter();
                    c.Item().Text(DateTime.Now.ToString("dd MMM yyyy")).FontSize(8).AlignCenter();
                });
            });
            
             // QR Verification match
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
            var verificationUrl = $"{baseUrl}/Verify/PO/{request.Id}"; 
            var qrCodeBytes = GenerateQrCode(verificationUrl);
            
            column.Item().PaddingTop(20).AlignRight().Row(r => 
            {
                 r.AutoItem().Column(c => 
                 {
                     c.Item().Text("Scan to Verify").FontSize(8).AlignRight();
                     c.Item().Width(50).Image(qrCodeBytes);
                 });
            });
        });
    }

    private byte[] GenerateQrCode(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(5);
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Generated by CorpProcure System").FontSize(8).FontColor(Colors.Grey.Darken1);
            });

            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(8);
                text.CurrentPageNumber().FontSize(8);
                text.Span(" of ").FontSize(8);
                text.TotalPages().FontSize(8);
            });
        });
    }
}
