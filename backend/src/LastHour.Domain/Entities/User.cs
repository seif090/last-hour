using LastHour.Domain.Common;

namespace LastHour.Domain.Entities;

public class UserProfile : BaseEntity
{
    public string IdentityId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }

    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}

public class Address : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string? Building { get; set; }
    public string? Apartment { get; set; }
    public string? Floor { get; set; }
    public string? Landmark { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsDefault { get; set; }
}

public class Favorite : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? StoreId { get; set; }
    public string? OfferId { get; set; }
}

public class RefreshToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime ExpiryDate { get; set; }
}
