using System.ComponentModel.DataAnnotations;

namespace CommunityPlatform.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;
    [Required, MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalı.")]
    public string Password { get; set; } = null!;
    [Required]
    public string FirstName { get; set; } = null!;
    [Required]
    public string LastName { get; set; } = null!;
    public string Role { get; set; } = "employee"; // employer | employee | cafe — admin bu uçtan asla atanamaz
    public Guid? CityId { get; set; }
    public Guid? DistrictId { get; set; }
}

