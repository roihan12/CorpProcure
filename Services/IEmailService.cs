using CorpProcure.Models;

namespace CorpProcure.Services;

/// <summary>
/// Service interface untuk Email Notification
/// </summary>
public interface IEmailService
{
    #region Generic Email

    /// <summary>
    /// Send email to a single recipient
    /// </summary>
    Task<Result<bool>> SendEmailAsync(string to, string subject, string body, bool isHtml = true);

    /// <summary>
    /// Send email to multiple recipients
    /// </summary>
    Task<Result<bool>> SendEmailAsync(List<string> recipients, string subject, string body, bool isHtml = true);

    #endregion

    #region Approval Workflow Notifications

    /// <summary>
    /// Send notification to Manager when PR is submitted
    /// </summary>
    Task SendPrSubmittedToManagerAsync(PurchaseRequest request);

    /// <summary>
    /// Send notification to Finance when PR is approved by Manager
    /// </summary>
    Task SendPrApprovedByManagerAsync(PurchaseRequest request);

    /// <summary>
    /// Send notification to Requester when PR is fully approved
    /// </summary>
    Task SendPrApprovedAsync(PurchaseRequest request);

    /// <summary>
    /// Send notification to Requester when PR is rejected
    /// </summary>
    Task SendPrRejectedAsync(PurchaseRequest request, string rejectorName, string reason);

    /// <summary>
    /// Send notification when auto-approval happens
    /// </summary>
    Task SendAutoApprovalNotificationAsync(PurchaseRequest request, string level, string reason);

    #endregion

    #region Helpers

    /// <summary>
    /// Check if email service is enabled
    /// </summary>
    bool IsEnabled { get; }

    #endregion
}
