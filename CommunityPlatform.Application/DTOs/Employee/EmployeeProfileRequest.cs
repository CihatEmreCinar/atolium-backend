namespace CommunityPlatform.Application.DTOs.Employee;

public class EmployeeProfileRequest
{
    public List<string>? Interests { get; set; }
    public List<string>? Hobbies { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public Guid? CityId { get; set; }
    public Guid? DistrictId { get; set; }

    // Atölye keşfi için tercih edilen bölge — kayıt/kişisel şehirden bağımsız, kullanıcı
    // istediği zaman değiştirebilir. GPS izni yoksa nearby fallback zincirinin ilk adımı.
    public Guid? PreferredCityId { get; set; }
    public Guid? PreferredDistrictId { get; set; }
}
