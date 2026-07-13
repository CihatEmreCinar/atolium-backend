namespace CommunityPlatform.Application.DTOs.Tickets;

public class TicketResponse
{
    public Guid TicketId { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid WorkshopId { get; set; }
    public string WorkshopTitle { get; set; } = null!;
    public string WorkshopLocationType { get; set; } = null!;
    public string? WorkshopLocationDetail { get; set; }
    public DateTime WorkshopStartAt { get; set; }
    public DateTime WorkshopEndAt { get; set; }
    public string EmployerName { get; set; } = null!;
    public string ParticipantName { get; set; } = null!;
    public string EnrollmentStatus { get; set; } = null!;
    public string AttendanceStatus { get; set; } = null!;

    // QR'a gömülecek imzalı, opak payload — "{ticketId:N}.{signature}"
    // Hiçbir kişisel veya iş verisi (workshopId, userId, enrollmentId) içermez.
    public string QrPayload { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

public class VerifyTicketRequest
{
    public string QrPayload { get; set; } = null!;
}

public class TicketPreviewResponse
{
    public Guid TicketId { get; set; }
    public Guid EnrollmentId { get; set; }
    public string ParticipantName { get; set; } = null!;
    public string WorkshopTitle { get; set; } = null!;
    public string AttendanceStatus { get; set; } = null!;
    public bool AlreadyUsed { get; set; }
    public DateTime? UsedAt { get; set; }
}

public class CheckInResponse
{
    public Guid EnrollmentId { get; set; }
    public string ParticipantName { get; set; } = null!;
    public string AttendanceStatus { get; set; } = null!;
    public DateTime AttendedAt { get; set; }
}
