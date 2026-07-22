using System.ComponentModel.DataAnnotations;

namespace CommunityPlatform.Application.DTOs.Auth;

public class AccountActionTokenRequest
{
    [Required]
    public string Token { get; set; } = null!;
}

public class VerifyEmailRequest : IValidatableObject
{
    public string? Token { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [RegularExpression("^\\d{6}$", ErrorMessage = "Verification code must be 6 digits.")]
    public string? Code { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var usesToken = !string.IsNullOrWhiteSpace(Token);
        var usesOtp = !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Code);

        if (usesToken == usesOtp)
            yield return new ValidationResult(
                "Provide either a token or an email and 6-digit verification code.",
                [nameof(Token), nameof(Email), nameof(Code)]);

        if (usesOtp && (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Code)))
            yield return new ValidationResult(
                "OTP verification requires both an email and a 6-digit code.",
                [nameof(Email), nameof(Code)]);
    }
}

public class RequestPasswordResetRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;
}

public class ResetPasswordRequest : AccountActionTokenRequest
{
    [Required, MinLength(12, ErrorMessage = "Şifre en az 12 karakter olmalı.")]
    public string NewPassword { get; set; } = null!;
}
