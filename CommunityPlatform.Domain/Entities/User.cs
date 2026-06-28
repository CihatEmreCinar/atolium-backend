namespace CommunityPlatform.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Role { get; set; } = null!; // employer | employee | admin
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? City { get; set; }
    public string? AdminLevel { get; set; } // super | mid | support
    public bool IsVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int XpPoints { get; set; } = 0;
    public int RankLevel { get; set; } = 1;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigation
    public EmployerProfile? EmployerProfile { get; set; }
    public EmployeeProfile? EmployeeProfile { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<UserBadge> UserBadges { get; set; } = [];
    public ICollection<Wishlist> Wishlists { get; set; } = [];
    public ICollection<UserFollow> Followers { get; set; } = [];   // beni takip edenler
    public ICollection<UserFollow> Following { get; set; } = [];   // benim takip ettiklerim
}