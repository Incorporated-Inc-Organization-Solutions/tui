using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace tui.Models;

public class TripGuide
{
    [Key]
    public int TripGuideId { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? ZipCode { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    // Navigation properties
    public ICollection<TripPlanning> TripPlannings { get; set; } = [];
}

public class Discount
{
    [Key]
    public int DiscountId { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Precision(5, 2)]
    public decimal Percentage { get; set; }

    // Navigation properties
    public ICollection<Trip> Trips { get; set; } = [];
}

public class Transport
{
    [Key]
    public int TransportId { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Trip> Trips { get; set; } = [];
}

public class Destination
{
    [Key]
    public int DestinationId { get; set; }

    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(100)]
    public string? Climate { get; set; }

    // Navigation properties
    public ICollection<TripDestination> TripDestinations { get; set; } = [];
}

public class Activity
{
    [Key]
    public int ActivityId { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<TripActivity> TripActivities { get; set; } = [];
}

public class Trip
{
    [Key]
    public int TripId { get; set; }

    public int? DiscountId { get; set; }

    public int? TransportId { get; set; }

    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Precision(10, 2)]
    public decimal Amount { get; set; }

    // Navigation properties
    public Discount? Discount { get; set; }
    public Transport? Transport { get; set; }
    public ICollection<TripDestination> TripDestinations { get; set; } = [];
    public ICollection<TripActivity> TripActivities { get; set; } = [];
    public ICollection<TripPlanning> TripPlannings { get; set; } = [];
    public ICollection<Surcharge> Surcharges { get; set; } = [];
    public ICollection<Tariff> Tariffs { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
}

public class TripDestination
{
    [Key]
    public int TripDestinationId { get; set; }

    public int TripId { get; set; }

    public int DestinationId { get; set; }

    public int? StopOrder { get; set; }

    // Navigation properties
    public Trip Trip { get; set; } = null!;
    public Destination Destination { get; set; } = null!;
}

public class TripActivity
{
    public int TripId { get; set; }

    public int ActivityId { get; set; }

    // Navigation properties
    public Trip Trip { get; set; } = null!;
    public Activity Activity { get; set; } = null!;
}

public class TripPlanning
{
    [Key]
    public int TripPlanningId { get; set; }

    public int TripId { get; set; }

    public int? TripGuideId { get; set; }

    public DateTime PlanningDate { get; set; }

    // Navigation properties
    public Trip Trip { get; set; } = null!;
    public TripGuide? TripGuide { get; set; }
}

public class SurchargeType
{
    [Key]
    public int SurchargeTypeId { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Surcharge> Surcharges { get; set; } = [];
}

public class Surcharge
{
    [Key]
    public int SurchargeId { get; set; }

    public int SurchargeTypeId { get; set; }

    public int TripId { get; set; }

    [Precision(10, 2)]
    public decimal Amount { get; set; }

    // Navigation properties
    public SurchargeType SurchargeType { get; set; } = null!;
    public Trip Trip { get; set; } = null!;
}
