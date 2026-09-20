using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehical_Rental.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public virtual User? User { get; set; }

        [Required]
        public int VehicleId { get; set; }
        public virtual Vehicle? Vehicle { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public int TotalDays => Math.Max(1, (EndDate.Date - StartDate.Date).Days);

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation property for one-to-one or one-to-many payment
        public virtual Payment? Payment { get; set; }
    }
}