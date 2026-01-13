using CorpProcure.Configuration;
using CorpProcure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace CorpProcure.Services;

/// <summary>
/// SMTP-based Email Service implementation
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ISystemSettingService _systemSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EmailService(
        IOptions<EmailSettings> settings,
        ISystemSettingService systemSettings,
        ILogger<EmailService> logger,
        IServiceProvider serviceProvider)
    {
        _settings = settings.Value;
        _systemSettings = systemSettings;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public bool IsEnabled => _settings.IsEnabled;

    #region Generic Email

    public async Task<Result<bool>> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {

        return await SendEmailAsync(new List<string> { to }, subject, body, isHtml);
    }

    public async Task<Result<bool>> SendEmailAsync(List<string> recipients, string subject, string body, bool isHtml = true)
    {
        // Check if email is enabled
        if (!_settings.IsEnabled)
        {
            _logger.LogDebug("Email service is disabled. Skipping email to {Recipients}", string.Join(", ", recipients));
            return Result<bool>.Ok(false);
        }

        // Also check system setting
        var systemEnabled = await _systemSettings.GetValueAsync("Email:Enabled", true);
        if (!systemEnabled)
        {
            _logger.LogDebug("Email disabled via system settings. Skipping.");
            return Result<bool>.Ok(false);
        }

        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
                EnableSsl = _settings.EnableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            foreach (var recipient in recipients)
            {
                mailMessage.To.Add(recipient);
            }

            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent successfully to {Recipients}: {Subject}",
                string.Join(", ", recipients), subject);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", recipients));
            return Result<bool>.Fail($"Failed to send email: {ex.Message}");
        }
    }

    #endregion

    #region Approval Workflow Notifications (Fire-and-Forget)

    // These methods run asynchronously in background without blocking the main request
    // They use a new service scope to avoid DbContext disposal issues

    public async Task SendPrSubmittedToManagerAsync(PurchaseRequest request)
    {
        // Capture data before fire-and-forget (to avoid accessing disposed entities)
        var emailData = new EmailData
        {
            RequestNumber = request.RequestNumber,
            Title = request.Title,
            RequesterName = request.Requester?.FullName ?? "",
            DepartmentId = request.DepartmentId,
            DepartmentName = request.Department?.Name ?? "",
            TotalAmount = request.TotalAmount,
            ManagerEmail = request.Department?.Manager?.Email,
            RequesterEmail = request.Requester?.Email,
            CreatedAt = request.CreatedAt
        };

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var settings = scope.ServiceProvider.GetRequiredService<ISystemSettingService>();
                
                var notifyOnSubmit = await settings.GetValueAsync("Email:NotifyOnSubmit", true);
                if (!notifyOnSubmit) return;

                if (string.IsNullOrEmpty(emailData.ManagerEmail))
                {
                    _logger.LogWarning("No manager email for dept {DeptId}", emailData.DepartmentId);
                    return;
                }

                var subject = $"[Action Required] Purchase Request #{emailData.RequestNumber} Needs Your Approval";
                var body = BuildPrSubmittedEmailFromData(emailData);

                await SendEmailDirectAsync(emailData.ManagerEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background email failed: SendPrSubmittedToManagerAsync");
            }
        });

        await Task.CompletedTask;
    }

    public async Task SendPrApprovedByManagerAsync(PurchaseRequest request)
    {
        var emailData = new EmailData
        {
            RequestNumber = request.RequestNumber,
            Title = request.Title,
            RequesterName = request.Requester?.FullName ?? "",
            DepartmentName = request.Department?.Name ?? "",
            TotalAmount = request.TotalAmount,
            ManagerApproverName = request.ManagerApprover?.FullName ?? ""
        };

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var settings = scope.ServiceProvider.GetRequiredService<ISystemSettingService>();
                
                var notifyOnApproval = await settings.GetValueAsync("Email:NotifyOnApproval", true);
                if (!notifyOnApproval) return;

                var financeEmails = await GetFinanceUserEmailsAsync(scope);
                if (!financeEmails.Any())
                {
                    _logger.LogWarning("No finance users found for notification");
                    return;
                }

                var subject = $"[Action Required] Purchase Request #{emailData.RequestNumber} Pending Finance Approval";
                var body = BuildPrPendingFinanceEmailFromData(emailData);

                await SendEmailDirectAsync(financeEmails, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background email failed: SendPrApprovedByManagerAsync");
            }
        });

        await Task.CompletedTask;
    }

    public async Task SendPrApprovedAsync(PurchaseRequest request)
    {
        var emailData = new EmailData
        {
            RequestNumber = request.RequestNumber,
            Title = request.Title,
            RequesterName = request.Requester?.FullName ?? "",
            RequesterEmail = request.Requester?.Email,
            TotalAmount = request.TotalAmount,
            ManagerApproverName = request.ManagerApprover?.FullName ?? "",
            FinanceApproverName = request.FinanceApprover?.FullName ?? ""
        };

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var settings = scope.ServiceProvider.GetRequiredService<ISystemSettingService>();
                
                var notifyOnApproval = await settings.GetValueAsync("Email:NotifyOnApproval", true);
                if (!notifyOnApproval) return;

                if (string.IsNullOrEmpty(emailData.RequesterEmail)) return;

                var subject = $"✅ Purchase Request #{emailData.RequestNumber} Has Been Approved";
                var body = BuildPrApprovedEmailFromData(emailData);

                await SendEmailDirectAsync(emailData.RequesterEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background email failed: SendPrApprovedAsync");
            }
        });

        await Task.CompletedTask;
    }

    public async Task SendPrRejectedAsync(PurchaseRequest request, string rejectorName, string reason)
    {
        var emailData = new EmailData
        {
            RequestNumber = request.RequestNumber,
            Title = request.Title,
            RequesterName = request.Requester?.FullName ?? "",
            RequesterEmail = request.Requester?.Email,
            RejectorName = rejectorName,
            RejectionReason = reason
        };

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var settings = scope.ServiceProvider.GetRequiredService<ISystemSettingService>();
                
                var notifyOnRejection = await settings.GetValueAsync("Email:NotifyOnRejection", true);
                if (!notifyOnRejection) return;

                if (string.IsNullOrEmpty(emailData.RequesterEmail)) return;

                var subject = $"❌ Purchase Request #{emailData.RequestNumber} Has Been Rejected";
                var body = BuildPrRejectedEmailFromData(emailData);

                await SendEmailDirectAsync(emailData.RequesterEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background email failed: SendPrRejectedAsync");
            }
        });

        await Task.CompletedTask;
    }

    public async Task SendAutoApprovalNotificationAsync(PurchaseRequest request, string level, string reason)
    {
        var emailData = new EmailData
        {
            RequestNumber = request.RequestNumber,
            Title = request.Title,
            RequesterName = request.Requester?.FullName ?? "",
            RequesterEmail = request.Requester?.Email,
            TotalAmount = request.TotalAmount,
            AutoApprovalLevel = level,
            AutoApprovalReason = reason
        };

        _ = Task.Run(async () =>
        {
            try
            {
                if (string.IsNullOrEmpty(emailData.RequesterEmail)) return;

                var subject = $"🤖 Purchase Request #{emailData.RequestNumber} Auto-Approved ({level})";
                var body = BuildAutoApprovalEmailFromData(emailData);

                await SendEmailDirectAsync(emailData.RequesterEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background email failed: SendAutoApprovalNotificationAsync");
            }
        });

        await Task.CompletedTask;
    }

    // Direct send without checking system settings (used by background tasks)
    private async Task SendEmailDirectAsync(string to, string subject, string body)
    {
        await SendEmailDirectAsync(new List<string> { to }, subject, body);
    }

    private async Task SendEmailDirectAsync(List<string> recipients, string subject, string body)
    {
        if (!_settings.IsEnabled)
        {
            _logger.LogDebug("Email service disabled. Skipping email to {Recipients}", string.Join(", ", recipients));
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
                EnableSsl = _settings.EnableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            foreach (var recipient in recipients)
            {
                mailMessage.To.Add(recipient);
            }

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Recipients}: {Subject}", string.Join(", ", recipients), subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", recipients));
        }
    }

    #endregion

    #region Email Data Transfer Object

    private class EmailData
    {
        public string RequestNumber { get; set; } = "";
        public string Title { get; set; } = "";
        public string RequesterName { get; set; } = "";
        public string? RequesterEmail { get; set; }
        public string? ManagerEmail { get; set; }
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string ManagerApproverName { get; set; } = "";
        public string FinanceApproverName { get; set; } = "";
        public string RejectorName { get; set; } = "";
        public string RejectionReason { get; set; } = "";
        public string AutoApprovalLevel { get; set; } = "";
        public string AutoApprovalReason { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Email Templates (EmailData-based for background tasks)

    private string BuildPrSubmittedEmailFromData(EmailData data)
    {
        return BuildEmailTemplate(
            "📋 New Purchase Request for Approval",
            "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
            $@"<p>Dear Manager,</p>
            <p>A new purchase request requires your approval:</p>
            
            <div class='detail-row'><span class='label'>Request No:</span><span class='value'><strong>{data.RequestNumber}</strong></span></div>
            <div class='detail-row'><span class='label'>Title:</span><span class='value'>{data.Title}</span></div>
            <div class='detail-row'><span class='label'>Requester:</span><span class='value'>{data.RequesterName}</span></div>
            <div class='detail-row'><span class='label'>Department:</span><span class='value'>{data.DepartmentName}</span></div>
            <div class='detail-row'><span class='label'>Amount:</span><span class='value amount'>Rp {data.TotalAmount:N0}</span></div>
            <div class='detail-row'><span class='label'>Submitted:</span><span class='value'>{data.CreatedAt:dd MMM yyyy HH:mm}</span></div>
            
            <p style='margin-top: 20px;'>Please login to CorpProcure to review and approve this request.</p>");
    }

    private string BuildPrPendingFinanceEmailFromData(EmailData data)
    {
        return BuildEmailTemplate(
            "💰 Purchase Request Pending Finance Approval",
            "linear-gradient(135deg, #11998e 0%, #38ef7d 100%)",
            $@"<p>Dear Finance Team,</p>
            <p>A purchase request has been approved by Manager and requires your review:</p>
            
            <div class='detail-row'><span class='label'>Request No:</span><span class='value'><strong>{data.RequestNumber}</strong></span></div>
            <div class='detail-row'><span class='label'>Title:</span><span class='value'>{data.Title}</span></div>
            <div class='detail-row'><span class='label'>Requester:</span><span class='value'>{data.RequesterName}</span></div>
            <div class='detail-row'><span class='label'>Department:</span><span class='value'>{data.DepartmentName}</span></div>
            <div class='detail-row'><span class='label'>Amount:</span><span class='value amount'>Rp {data.TotalAmount:N0}</span></div>
            <div class='detail-row'><span class='label'>Manager Approval:</span><span class='value'><span class='badge'>✓ Approved</span> by {data.ManagerApproverName}</span></div>
            
            <p style='margin-top: 20px;'>Please login to CorpProcure to review and approve this request.</p>");
    }

    private string BuildPrApprovedEmailFromData(EmailData data)
    {
        return BuildEmailTemplate(
            "✅ Purchase Request Approved",
            "linear-gradient(135deg, #28a745 0%, #20c997 100%)",
            $@"<div class='success-icon'>🎉</div>
            <p>Dear {data.RequesterName},</p>
            <p>Great news! Your purchase request has been <strong>fully approved</strong>:</p>
            
            <div class='detail-row'><span class='label'>Request No:</span><span class='value'><strong>{data.RequestNumber}</strong></span></div>
            <div class='detail-row'><span class='label'>Title:</span><span class='value'>{data.Title}</span></div>
            <div class='detail-row'><span class='label'>Amount:</span><span class='value amount'>Rp {data.TotalAmount:N0}</span></div>
            <div class='detail-row'><span class='label'>Manager:</span><span class='value'>✓ {data.ManagerApproverName}</span></div>
            <div class='detail-row'><span class='label'>Finance:</span><span class='value'>✓ {data.FinanceApproverName}</span></div>
            
            <p style='margin-top: 20px;'>The Procurement team will now process your request and generate a Purchase Order.</p>");
    }

    private string BuildPrRejectedEmailFromData(EmailData data)
    {
        return BuildEmailTemplate(
            "❌ Purchase Request Rejected",
            "linear-gradient(135deg, #dc3545 0%, #c0392b 100%)",
            $@"<p>Dear {data.RequesterName},</p>
            <p>Unfortunately, your purchase request has been rejected:</p>
            
            <div class='detail-row'><span class='label'>Request No:</span><span class='value'><strong>{data.RequestNumber}</strong></span></div>
            <div class='detail-row'><span class='label'>Title:</span><span class='value'>{data.Title}</span></div>
            <div class='detail-row'><span class='label'>Rejected By:</span><span class='value'>{data.RejectorName}</span></div>
            
            <div class='reason-box'>
                <strong>Reason:</strong><br/>
                {data.RejectionReason}
            </div>
            
            <p>Please review the feedback and submit a new request if needed.</p>");
    }

    private string BuildAutoApprovalEmailFromData(EmailData data)
    {
        return BuildEmailTemplate(
            "🤖 Auto-Approval Notification",
            "linear-gradient(135deg, #6f42c1 0%, #e83e8c 100%)",
            $@"<p>Dear {data.RequesterName},</p>
            <p>Your purchase request has been <strong>automatically approved</strong> at the <span class='auto-badge'>{data.AutoApprovalLevel}</span> level:</p>
            
            <div class='detail-row'><span class='label'>Request No:</span><span class='value'><strong>{data.RequestNumber}</strong></span></div>
            <div class='detail-row'><span class='label'>Title:</span><span class='value'>{data.Title}</span></div>
            <div class='detail-row'><span class='label'>Amount:</span><span class='value'>Rp {data.TotalAmount:N0}</span></div>
            <div class='detail-row'><span class='label'>Auto-Approval Reason:</span><span class='value'>{data.AutoApprovalReason}</span></div>
            
            <p style='margin-top: 20px;'>Your request will proceed to the next approval level automatically.</p>");
    }

    private string BuildEmailTemplate(string title, string gradient, string contentHtml)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: {gradient}; color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #ffffff; padding: 20px; border: 1px solid #e0e0e0; border-top: none; }}
        .detail-row {{ display: flex; padding: 8px 0; border-bottom: 1px solid #f0f0f0; }}
        .label {{ font-weight: 600; color: #666; width: 150px; }}
        .value {{ color: #333; }}
        .amount {{ font-size: 20px; font-weight: bold; color: #667eea; }}
        .badge {{ display: inline-block; background: #28a745; color: white; padding: 4px 8px; border-radius: 4px; font-size: 12px; }}
        .auto-badge {{ display: inline-block; background: #6f42c1; color: white; padding: 6px 12px; border-radius: 20px; font-size: 14px; }}
        .reason-box {{ background: #fff3cd; border-left: 4px solid #dc3545; padding: 15px; margin: 15px 0; }}
        .success-icon {{ font-size: 48px; text-align: center; margin-bottom: 15px; }}
        .footer {{ text-align: center; padding: 15px; color: #888; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin:0;'>{title}</h2>
        </div>
        <div class='content'>
            {contentHtml}
        </div>
        <div class='footer'>
            This is an automated message from CorpProcure System.
        </div>
    </div>
</body>
</html>";
    }

    #endregion

    #region Email Templates (Legacy - for non-fire-and-forget usage)

    private string BuildPrSubmittedEmail(PurchaseRequest request)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #ffffff; padding: 20px; border: 1px solid #e0e0e0; border-top: none; }}
        .detail-row {{ display: flex; padding: 8px 0; border-bottom: 1px solid #f0f0f0; }}
        .label {{ font-weight: 600; color: #666; width: 150px; }}
        .value {{ color: #333; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #667eea; }}
        .btn {{ display: inline-block; padding: 12px 24px; background: #667eea; color: white; text-decoration: none; border-radius: 6px; margin-top: 15px; }}
        .footer {{ text-align: center; padding: 15px; color: #888; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin:0;'>📋 New Purchase Request for Approval</h2>
        </div>
        <div class='content'>
            <p>Dear Manager,</p>
            <p>A new purchase request requires your approval:</p>
            
            <div class='detail-row'><span class='label'>Request No:</span><span class='value'><strong>{request.RequestNumber}</strong></span></div>
            <div class='detail-row'><span class='label'>Title:</span><span class='value'>{request.Title}</span></div>
            <div class='detail-row'><span class='label'>Requester:</span><span class='value'>{request.Requester?.FullName}</span></div>
            <div class='detail-row'><span class='label'>Department:</span><span class='value'>{request.Department?.Name}</span></div>
            <div class='detail-row'><span class='label'>Amount:</span><span class='value amount'>Rp {request.TotalAmount:N0}</span></div>
            <div class='detail-row'><span class='label'>Submitted:</span><span class='value'>{request.CreatedAt:dd MMM yyyy HH:mm}</span></div>
            
            <p style='margin-top: 20px;'>Please login to CorpProcure to review and approve this request.</p>
        </div>
        <div class='footer'>
            This is an automated message from CorpProcure System.
        </div>
    </div>
</body>
</html>";
    }

    private string BuildPrPendingFinanceEmail(PurchaseRequest request)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #ffffff; padding: 20px; border: 1px solid #e0e0e0; border-top: none; }}
        .badge {{ display: inline-block; background: #28a745; color: white; padding: 4px 8px; border-radius: 4px; font-size: 12px; }}
        .detail-row {{ display: flex; padding: 8px 0; border-bottom: 1px solid #f0f0f0; }}
        .label {{ font-weight: 600; color: #666; width: 150px; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #11998e; }}
        .footer {{ text-align: center; padding: 15px; color: #888; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin:0;'>💰 Purchase Request Pending Finance Approval</h2>
        </div>
        <div class='content'>
            <p>Dear Finance Team,</p>
            <p>A purchase request has been approved by Manager and requires your review:</p>
            
            <div class='detail-row'><span class='label'>Request No:</span><span class='value'><strong>{request.RequestNumber}</strong></span></div>
            <div class='detail-row'><span class='label'>Title:</span><span class='value'>{request.Title}</span></div>
            <div class='detail-row'><span class='label'>Requester:</span><span class='value'>{request.Requester?.FullName}</span></div>
            <div class='detail-row'><span class='label'>Department:</span><span class='value'>{request.Department?.Name}</span></div>
            <div class='detail-row'><span class='label'>Amount:</span><span class='value amount'>Rp {request.TotalAmount:N0}</span></div>
            <div class='detail-row'><span class='label'>Manager Approval:</span><span class='value'><span class='badge'>✓ Approved</span> by {request.ManagerApprover?.FullName}</span></div>
            
            <p style='margin-top: 20px;'>Please login to CorpProcure to review and approve this request.</p>
        </div>
        <div class='footer'>
            This is an automated message from CorpProcure System.
        </div>
    </div>
</body>
</html>";
    }

    private string BuildPrApprovedEmail(PurchaseRequest request)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #ffffff; padding: 20px; border: 1px solid #e0e0e0; border-top: none; }}
        .success-icon {{ font-size: 48px; text-align: center; margin-bottom: 15px; }}
        .detail-row {{ display: flex; padding: 8px 0; border-bottom: 1px solid #f0f0f0; }}
        .label {{ font-weight: 600; color: #666; width: 150px; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #28a745; }}
        .footer {{ text-align: center; padding: 15px; color: #888; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin:0;'>✅ Purchase Request Approved</h2>
        </div>
        <div class='content'>
            <div class='success-icon'>🎉</div>
            <p>Dear {request.Requester?.FullName},</p>
            <p>Great news! Your purchase request has been <strong>fully approved</strong>:</p>
            
            <div class='detail-row'><span class='label'>Request No:</span><span class='value'><strong>{request.RequestNumber}</strong></span></div>
            <div class='detail-row'><span class='label'>Title:</span><span class='value'>{request.Title}</span></div>
            <div class='detail-row'><span class='label'>Amount:</span><span class='value amount'>Rp {request.TotalAmount:N0}</span></div>
            <div class='detail-row'><span class='label'>Manager:</span><span class='value'>✓ {request.ManagerApprover?.FullName}</span></div>
            <div class='detail-row'><span class='label'>Finance:</span><span class='value'>✓ {request.FinanceApprover?.FullName}</span></div>
            
            <p style='margin-top: 20px;'>The Procurement team will now process your request and generate a Purchase Order.</p>
        </div>
        <div class='footer'>
            This is an automated message from CorpProcure System.
        </div>
    </div>
</body>
</html>";
    }

    private string BuildPrRejectedEmail(PurchaseRequest request, string rejectorName, string reason)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #dc3545 0%, #c0392b 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #ffffff; padding: 20px; border: 1px solid #e0e0e0; border-top: none; }}
        .reason-box {{ background: #fff3cd; border-left: 4px solid #dc3545; padding: 15px; margin: 15px 0; }}
        .detail-row {{ display: flex; padding: 8px 0; border-bottom: 1px solid #f0f0f0; }}
        .label {{ font-weight: 600; color: #666; width: 150px; }}
        .footer {{ text-align: center; padding: 15px; color: #888; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin:0;'>❌ Purchase Request Rejected</h2>
        </div>
        <div class='content'>
            <p>Dear {request.Requester?.FullName},</p>
            <p>Unfortunately, your purchase request has been rejected:</p>
            
            <div class='detail-row'><span class='label'>Request No:</span><span class='value'><strong>{request.RequestNumber}</strong></span></div>
            <div class='detail-row'><span class='label'>Title:</span><span class='value'>{request.Title}</span></div>
            <div class='detail-row'><span class='label'>Rejected By:</span><span class='value'>{rejectorName}</span></div>
            
            <div class='reason-box'>
                <strong>Reason:</strong><br/>
                {reason}
            </div>
            
            <p>Please review the feedback and submit a new request if needed.</p>
        </div>
        <div class='footer'>
            This is an automated message from CorpProcure System.
        </div>
    </div>
</body>
</html>";
    }

    private string BuildAutoApprovalEmail(PurchaseRequest request, string level, string reason)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6f42c1 0%, #e83e8c 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #ffffff; padding: 20px; border: 1px solid #e0e0e0; border-top: none; }}
        .auto-badge {{ display: inline-block; background: #6f42c1; color: white; padding: 6px 12px; border-radius: 20px; font-size: 14px; }}
        .detail-row {{ display: flex; padding: 8px 0; border-bottom: 1px solid #f0f0f0; }}
        .label {{ font-weight: 600; color: #666; width: 150px; }}
        .footer {{ text-align: center; padding: 15px; color: #888; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin:0;'>🤖 Auto-Approval Notification</h2>
        </div>
        <div class='content'>
            <p>Dear {request.Requester?.FullName},</p>
            <p>Your purchase request has been <strong>automatically approved</strong> at the <span class='auto-badge'>{level}</span> level:</p>
            
            <div class='detail-row'><span class='label'>Request No:</span><span class='value'><strong>{request.RequestNumber}</strong></span></div>
            <div class='detail-row'><span class='label'>Title:</span><span class='value'>{request.Title}</span></div>
            <div class='detail-row'><span class='label'>Amount:</span><span class='value'>Rp {request.TotalAmount:N0}</span></div>
            <div class='detail-row'><span class='label'>Auto-Approval Reason:</span><span class='value'>{reason}</span></div>
            
            <p style='margin-top: 20px;'>Your request will proceed to the next approval level automatically.</p>
        </div>
        <div class='footer'>
            This is an automated message from CorpProcure System.
        </div>
    </div>
</body>
</html>";
    }

    #endregion

    #region Helpers

    private async Task<List<string>> GetFinanceUserEmailsAsync(IServiceScope scope)
    {
        var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

        return await context.Users
            .Where(u => u.Role == Models.Enums.UserRole.Finance && u.IsActive && !string.IsNullOrEmpty(u.Email))
            .Select(u => u.Email!)
            .ToListAsync();
    }

    #endregion
}
