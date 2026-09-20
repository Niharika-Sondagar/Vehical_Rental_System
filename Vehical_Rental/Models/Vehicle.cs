using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehical_Rental.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Model { get; set; } = string.Empty;

        [Range(1990, 2030)]
        public int Year { get; set; }

        [Required]
        [MaxLength(20)]
        public string LicensePlate { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Sedan"; // Sedan, SUV, Luxury, Sports, Electric, Van

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(1, 10000)]
        public decimal DailyRate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Transmission { get; set; } = "Automatic"; // Automatic, Manual

        [Required]
        [MaxLength(20)]
        public string FuelType { get; set; } = "Petrol"; // Petrol, Diesel, Electric, Hybrid

        [Range(1, 20)]
        public int Seats { get; set; } = 5;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        [NotMapped]
        public string FullName => $"{Year} {Make} {Model}";

        [NotMapped]
        public double AverageRating => Reviews.Any() ? Math.Round(Reviews.Average(r => r.Rating), 1) : 5.0;
    }
}