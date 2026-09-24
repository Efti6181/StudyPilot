using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using StudyPilotApp.Options;

namespace StudyPilotApp.Services;

public sealed class SmtpAccountEmailService : IAccountEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpAccountEmailService> _logger;

    public SmtpAccountEmailService(
        IOptions<EmailOptions> options,
        ILogger<SmtpAccountEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<bool> SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Password reset email was requested, but SMTP is not configured.");
            return false;
        }

        var safeName = HtmlEncoder.Default.Encode(
            string.IsNullOrWhiteSpace(recipientName) ? "StudyPilot member" : recipientName);
        var safeUrl = HtmlEncoder.Default.Encode(resetUrl);

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail.Trim(), _options.FromName.Trim()),
            Subject = "Reset your StudyPilot password",
            IsBodyHtml = true,
            Body = $$"""
                <!doctype html>
                <html lang="en">
                <body style="margin:0;padding:24px;background:#f1f5f9;font-family:Arial,sans-serif;color:#0f172a">
                  <div style="max-width:580px;margin:auto;padding:32px;background:#fff;border:1px solid #e2e8f0;border-radius:18px">
                    <div style="font-size:21px;font-weight:800;color:#2563eb">StudyPilot</div>
                    <h1 style="margin:24px 0 12px;font-size:25px">Reset your password</h1>
                    <p>Hello {{safeName}},</p>
                    <p style="line-height:1.65;color:#475569">A password reset was requested for your StudyPilot account. Use the secure button below within two hours.</p>
                    <p style="margin:28px 0"><a href="{{safeUrl}}" style="display:inline-block;padding:13px 20px;color:#fff;background:#2563eb;border-radius:10px;text-decoration:none;font-weight:700">Reset password</a></p>
                    <p style="line-height:1.65;color:#64748b;font-size:13px">If you did not request this change, ignore this email. Your current password remains unchanged.</p>
                    <hr style="margin:26px 0;border:0;border-top:1px solid #e2e8f0" />
                    <p style="margin:0;color:#94a3b8;font-size:12px">Never share this reset link with another person.</p>
                  </div>
                </body>
                </html>
                """
        };
        message.To.Add(new MailAddress(recipientEmail));

        using var client = new SmtpClient(_options.Host.Trim(), _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.Username.Trim(), _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SmtpException exception)
        {
            _logger.LogWarning(exception, "SMTP could not deliver a StudyPilot password reset email.");
            return false;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected failure while delivering a password reset email.");
            return false;
        }
    }
}
