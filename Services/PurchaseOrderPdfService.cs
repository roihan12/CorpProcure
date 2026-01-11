using CorpProcure.Data;
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
            .Include(p => p.Items)
            .Include(p => p.ManagerApprover)
            .Include(p => p.FinanceApprover)
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.Id == purchaseRequestId);

        if (request == null)
        {
            throw new InvalidOperationException("Purchase request not found");
        }

        if (string.IsNullOrEmpty(request.PoNumber))
        {
            throw new InvalidOperationException("PO has not been generated for this request");
        }

        _logger.LogInformation("Generating PDF for PO {PoNumber}", request.PoNumber);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, request));
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

    private void ComposeContent(IContainer container, Models.PurchaseRequest request)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // PO Header
            column.Item().Background(Colors.Blue.Lighten5).Padding(15).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("PURCHASE ORDER").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingTop(5).Text(request.PoNumber!).FontSize(14).Bold();
                });

                row.ConstantItem(150).AlignRight().Column(col =>
                {
                    col.Item().Text("PO Date:").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(request.PoDate?.ToString("dd MMM yyyy") ?? "-").FontSize(10).Bold();
                });
            });

            column.Item().PaddingTop(20);

            // Vendor Information Section
            if (request.Vendor != null)
            {
                column.Item().Text("Vendor Information").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                column.Item().PaddingTop(10);

                column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(15).Column(vendorCol =>
                {
                    vendorCol.Item().Row(vendorRow =>
                    {
                        vendorRow.RelativeItem().Column(leftCol =>
                        {
                            leftCol.Item().Text(request.Vendor.Name).FontSize(12).Bold();
                            leftCol.Item().PaddingTop(3).Text($"Code: {request.Vendor.Code}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            
                            if (!string.IsNullOrEmpty(request.Vendor.Address))
                            {
                                leftCol.Item().PaddingTop(5).Text(request.Vendor.Address).FontSize(9);
                            }
                            if (!string.IsNullOrEmpty(request.Vendor.City) || !string.IsNullOrEmpty(request.Vendor.Province))
                            {
                                leftCol.Item().Text($"{request.Vendor.City}, {request.Vendor.Province} {request.Vendor.PostalCode}".Trim()).FontSize(9);
                            }
                        });

                        vendorRow.RelativeItem().Column(rightCol =>
                        {
                            if (!string.IsNullOrEmpty(request.Vendor.ContactPerson))
                            {
                                rightCol.Item().Text("Contact:").FontSize(8).FontColor(Colors.Grey.Darken1);
                                rightCol.Item().Text(request.Vendor.ContactPerson).FontSize(9);
                            }
                            if (!string.IsNullOrEmpty(request.Vendor.Phone))
                            {
                                rightCol.Item().PaddingTop(3).Text($"Phone: {request.Vendor.Phone}").FontSize(9);
                            }
                            if (!string.IsNullOrEmpty(request.Vendor.Email))
                            {
                                rightCol.Item().Text($"Email: {request.Vendor.Email}").FontSize(9);
                            }
                            if (!string.IsNullOrEmpty(request.Vendor.TaxId))
                            {
                                rightCol.Item().PaddingTop(5).Text($"NPWP: {request.Vendor.TaxId}").FontSize(9);
                            }
                        });
                    });

                    // Payment Terms
                    vendorCol.Item().PaddingTop(10).Row(paymentRow =>
                    {
                        paymentRow.RelativeItem().Column(ptCol =>
                        {
                            ptCol.Item().Text("Payment Terms:").FontSize(8).FontColor(Colors.Grey.Darken1);
                            var paymentTermText = request.Vendor.PaymentTerms switch
                            {
                                Models.Enums.PaymentTermType.Immediate => "Immediate",
                                Models.Enums.PaymentTermType.Net15 => "Net 15 Days",
                                Models.Enums.PaymentTermType.Net30 => "Net 30 Days",
                                Models.Enums.PaymentTermType.Net45 => "Net 45 Days",
                                Models.Enums.PaymentTermType.Net60 => "Net 60 Days",
                                _ => request.Vendor.PaymentTerms.ToString()
                            };
                            ptCol.Item().Text(paymentTermText).FontSize(9).Bold();
                        });

                        if (!string.IsNullOrEmpty(request.Vendor.BankName))
                        {
                            paymentRow.RelativeItem().Column(bankCol =>
                            {
                                bankCol.Item().Text("Bank Account:").FontSize(8).FontColor(Colors.Grey.Darken1);
                                bankCol.Item().Text($"{request.Vendor.BankName} - {request.Vendor.AccountNumber}").FontSize(9);
                                if (!string.IsNullOrEmpty(request.Vendor.AccountHolderName))
                                {
                                    bankCol.Item().Text($"a/n {request.Vendor.AccountHolderName}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                }
                            });
                        }
                    });
                });

                column.Item().PaddingTop(20);
            }

            // Request Details Section
            column.Item().Text("Request Information").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(10);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(120);
                    columns.RelativeColumn();
                    columns.ConstantColumn(120);
                    columns.RelativeColumn();
                });

                table.Cell().Text("Request Number:").FontSize(9).FontColor(Colors.Grey.Darken1);
                table.Cell().Text(request.RequestNumber).FontSize(10).Bold();
                table.Cell().Text("Request Date:").FontSize(9).FontColor(Colors.Grey.Darken1);
                table.Cell().Text(request.CreatedAt.ToString("dd MMM yyyy")).FontSize(10);

                table.Cell().Text("Requester:").FontSize(9).FontColor(Colors.Grey.Darken1);
                table.Cell().Text(request.Requester?.FullName ?? "-").FontSize(10);
                table.Cell().Text("Department:").FontSize(9).FontColor(Colors.Grey.Darken1);
                table.Cell().Text(request.Department?.Name ?? "-").FontSize(10);
            });

            column.Item().PaddingTop(15);

            // Description
            column.Item().Text("Description:").FontSize(9).FontColor(Colors.Grey.Darken1);
            column.Item().PaddingTop(3).Text(request.Description ?? "-").FontSize(10);

            column.Item().PaddingTop(20);

            // Items Table
            column.Item().Text("Order Items").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(10);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(100);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("#").FontSize(9).FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Item Name").FontSize(9).FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Description").FontSize(9).FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter().Text("Qty").FontSize(9).FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Unit Price").FontSize(9).FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Subtotal").FontSize(9).FontColor(Colors.White).Bold();
                });

                // Rows
                var itemNo = 1;
                foreach (var item in request.Items)
                {
                    var bgColor = itemNo % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(itemNo.ToString()).FontSize(9);
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.ItemName).FontSize(9);
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Description ?? "-").FontSize(9);
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(item.Quantity.ToString()).FontSize(9);
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"Rp {item.UnitPrice:N0}").FontSize(9);
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"Rp {item.Quantity * item.UnitPrice:N0}").FontSize(9);

                    itemNo++;
                }

                // Total row
                table.Cell().ColumnSpan(5).Background(Colors.Blue.Lighten5).Padding(5).AlignRight().Text("TOTAL:").FontSize(10).Bold();
                table.Cell().Background(Colors.Blue.Lighten5).Padding(5).AlignRight().Text($"Rp {request.TotalAmount:N0}").FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
            });

            column.Item().PaddingTop(30);

            // Approval Section
            column.Item().Text("Approvals").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(10);

            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Manager Approval").FontSize(10).Bold();
                    col.Item().PaddingTop(10).Text(request.ManagerApprover?.FullName ?? "-").FontSize(9);
                    col.Item().Text(request.ManagerApprovalDate?.ToString("dd MMM yyyy") ?? "-").FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(20);
                    col.Item().LineHorizontal(0.5f);
                    col.Item().PaddingTop(3).Text("Signature").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(20);

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Finance Approval").FontSize(10).Bold();
                    col.Item().PaddingTop(10).Text(request.FinanceApprover?.FullName ?? "-").FontSize(9);
                    col.Item().Text(request.FinanceApprovalDate?.ToString("dd MMM yyyy") ?? "-").FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(20);
                    col.Item().LineHorizontal(0.5f);
                    col.Item().PaddingTop(3).Text("Signature").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().PaddingTop(30);

            // QR Code Verification Section
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
            var verificationUrl = $"{baseUrl}/Verify/PO/{request.Id}";
            var qrCodeBytes = GenerateQrCode(verificationUrl);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Document Verification").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingTop(5).Text("Scan QR code to verify document authenticity").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(10).Text($"Verification Code: {request.Id.ToString()[..8].ToUpper()}").FontSize(9);
                    col.Item().PaddingTop(3).Text($"PO Number: {request.PoNumber}").FontSize(9);
                    col.Item().PaddingTop(3).Text($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(100).AlignRight().Image(qrCodeBytes);
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
