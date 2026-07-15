namespace NotificationService.Email.Models;

/// <summary>Birincil (ve opsiyonel ikincil) CTA butonunu temsil eder.</summary>
public sealed class EmailButtonModel
{
    public required string PrimaryText { get; init; }
    public required string PrimaryUrl { get; init; }
    public string? SecondaryText { get; init; }
    public string? SecondaryUrl { get; init; }
}
