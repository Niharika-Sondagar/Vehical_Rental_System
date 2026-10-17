using System.ComponentModel.DataAnnotations;

namespace Vehical_Rental.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }
        public virtual Vehicle? Vehicle { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [Required]
        [MaxLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;
    }
}