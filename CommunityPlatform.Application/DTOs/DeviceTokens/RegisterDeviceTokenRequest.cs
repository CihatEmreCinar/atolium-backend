namespace CommunityPlatform.Application.DTOs.DeviceTokens;

public class RegisterDeviceTokenRequest
{
    public string ExpoPushToken { get; set; } = null!;
    public string Platform { get; set; } = null!; // "ios" | "android"
}
