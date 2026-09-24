namespace StudyPilotApp.Services;

public interface IAccountEmailService
{
    bool IsConfigured { get; }

    Task<bool> SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        CancellationToken cancellationToken = default);
}
