namespace CommunityPlatform.Application.DTOs.Media;

public class FileUploadResponse
{
    public string Url { get; set; } = null!;
    public long SizeBytes { get; set; }
}