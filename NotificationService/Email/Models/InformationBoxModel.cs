namespace NotificationService.Email.Models;

/// <summary>Base.html içindeki gri "bilgi kutusu" alanını temsil eder.</summary>
public sealed class InformationBoxModel
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string IconUrl { get; init; }
}
