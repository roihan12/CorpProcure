namespace CorpProcure.Configuration;

/// <summary>
/// Strongly-typed configuration for Email/SMTP settings
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// SMTP server host (e.g., smtp.gmail.com)
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (typically 587 for TLS, 465 for SSL)
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SMTP authentication username
    /// </summary>
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>
    /// SMTP authentication password
    /// </summary>
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// Sender email address
    /// </summary>
    public string FromEmail { get; set; } = "noreply@corpprocure.com";

    /// <summary>
    /// Sender display name
    /// </summary>
    public string FromName { get; set; } = "CorpProcure System";

    /// <summary>
    /// Enable SSL/TLS for SMTP connection
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Global toggle to enable/disable email sending
    /// </summary>
    public bool IsEnabled { get; set; } = false;
}
