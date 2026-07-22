namespace CommunityPlatform.Domain.Entities;

public static class AccountActionTokenPurposes
{
    public const string EmailVerification = "email-verification";
    public const string PasswordReset = "password-reset";
}

public class AccountActionToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public string Purpose { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    // E-mail verification OTP data shares the existing action-token lifecycle.
    // The raw OTP is deliberately never persisted.
    public string? OtpHash { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public int OtpAttemptCount { get; set; }
    public DateTime? OtpUsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
