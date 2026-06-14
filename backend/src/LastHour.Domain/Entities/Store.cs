using LastHour.Domain.Common;
using LastHour.Domain.Exceptions;

namespace LastHour.Domain.Entities;

public class Store : BaseAuditableEntity
{
    public string OwnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string Category { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public bool IsOpen { get; set; }
    public string? OpeningHours { get; set; }
    public string? ClosingHours { get; set; }
    public int ActiveOfferCount { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AcceptedPaymentMethods { get; set; }

    public ICollection<Offer> Offers { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}
