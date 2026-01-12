using CorpProcure.Data;
using CorpProcure.Models;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Services;

public interface IFileUploadService
{
    /// <summary>
    /// Upload a file for PurchaseRequest
    /// </summary>
    Task<Result<Attachment>> UploadAsync(
        IFormFile file,
        Guid purchaseRequestId,
        AttachmentType type,
        string? description,
        Guid userId);

    /// <summary>
    /// Upload a file for PurchaseOrder
    /// </summary>
    Task<Result<Attachment>> UploadForPurchaseOrderAsync(
        IFormFile file,
        Guid purchaseOrderId,
        AttachmentType type,
        string? description,
        Guid userId);

    /// <summary>
    /// Get all attachments for a PurchaseRequest
    /// </summary>
    Task<Result<List<Attachment>>> GetByPurchaseRequestIdAsync(Guid purchaseRequestId);

    /// <summary>
    /// Get all attachments for a PurchaseOrder
    /// </summary>
    Task<Result<List<Attachment>>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId);

    /// <summary>
    /// Delete an attachment (file + record)
    /// </summary>
    Task<Result<bool>> DeleteAsync(Guid attachmentId, Guid userId);

    /// <summary>
    /// Get attachment by ID
    /// </summary>
    Task<Result<Attachment>> GetByIdAsync(Guid id);
}

public class FileUploadService : IFileUploadService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<FileUploadService> _logger;

    // Allowed file extensions and max size
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public FileUploadService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        IAuditLogService auditLogService,
        ILogger<FileUploadService> logger)
    {
        _context = context;
        _environment = environment;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result<Attachment>> UploadAsync(
        IFormFile file,
        Guid purchaseRequestId,
        AttachmentType type,
        string? description,
        Guid userId)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
                return Result<Attachment>.Fail("No file uploaded.");

            if (file.Length > MaxFileSizeBytes)
                return Result<Attachment>.Fail($"File size exceeds limit of {MaxFileSizeBytes / (1024 * 1024)}MB.");

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
                return Result<Attachment>.Fail($"File type {extension} is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");

            // Check if PR exists
            var prExists = await _context.PurchaseRequests.AnyAsync(pr => pr.Id == purchaseRequestId);
            if (!prExists)
                return Result<Attachment>.Fail("Purchase Request not found.");

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "attachments");

            // Ensure directory exists
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var relativePath = Path.Combine("uploads", "attachments", uniqueFileName).Replace("\\", "/");

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create attachment record
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                FileName = uniqueFileName,
                OriginalFileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType,
                FileSize = file.Length,
                FilePath = relativePath,
                Description = description,
                Type = type,
                PurchaseRequestId = purchaseRequestId
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId,
                "UPLOAD",
                "Attachment",
                attachment.Id.ToString(),
                $"Uploaded {attachment.OriginalFileName} for PR {purchaseRequestId}");

            return Result<Attachment>.Ok(attachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return Result<Attachment>.Fail($"Error uploading file: {ex.Message}");
        }
    }

    public async Task<Result<Attachment>> UploadForPurchaseOrderAsync(
        IFormFile file,
        Guid purchaseOrderId,
        AttachmentType type,
        string? description,
        Guid userId)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
                return Result<Attachment>.Fail("No file uploaded.");

            if (file.Length > MaxFileSizeBytes)
                return Result<Attachment>.Fail($"File size exceeds limit of {MaxFileSizeBytes / (1024 * 1024)}MB.");

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
                return Result<Attachment>.Fail($"File type {extension} is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");

            // Check if PO exists
            var poExists = await _context.PurchaseOrders.AnyAsync(po => po.Id == purchaseOrderId);
            if (!poExists)
                return Result<Attachment>.Fail("Purchase Order not found.");

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "attachments");

            // Ensure directory exists
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var relativePath = Path.Combine("uploads", "attachments", uniqueFileName).Replace("\\", "/");

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create attachment record
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                FileName = uniqueFileName,
                OriginalFileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType,
                FileSize = file.Length,
                FilePath = relativePath,
                Description = description,
                Type = type,
                PurchaseOrderId = purchaseOrderId
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId,
                "UPLOAD",
                "Attachment",
                attachment.Id.ToString(),
                $"Uploaded {attachment.OriginalFileName} for PO {purchaseOrderId}");

            return Result<Attachment>.Ok(attachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for PO");
            return Result<Attachment>.Fail($"Error uploading file: {ex.Message}");
        }
    }

    public async Task<Result<List<Attachment>>> GetByPurchaseRequestIdAsync(Guid purchaseRequestId)
    {
        var attachments = await _context.Attachments
            .Where(a => a.PurchaseRequestId == purchaseRequestId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Result<List<Attachment>>.Ok(attachments);
    }

    public async Task<Result<List<Attachment>>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId)
    {
        var attachments = await _context.Attachments
            .Where(a => a.PurchaseOrderId == purchaseOrderId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Result<List<Attachment>>.Ok(attachments);
    }

    public async Task<Result<bool>> DeleteAsync(Guid attachmentId, Guid userId)
    {
        try
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null)
                return Result<bool>.Fail("Attachment not found.");

            // Delete physical file
            var fullPath = Path.Combine(_environment.WebRootPath, attachment.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // Delete record (soft-delete will be handled by interceptor)
            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId,
                "DELETE",
                "Attachment",
                attachmentId.ToString(),
                $"Deleted attachment {attachment.OriginalFileName}");

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {Id}", attachmentId);
            return Result<bool>.Fail($"Error deleting attachment: {ex.Message}");
        }
    }

    public async Task<Result<Attachment>> GetByIdAsync(Guid id)
    {
        var attachment = await _context.Attachments.FindAsync(id);
        if (attachment == null)
            return Result<Attachment>.Fail("Attachment not found.");

        return Result<Attachment>.Ok(attachment);
    }
}
