using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehical_Rental.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string Role { get; set; } = "Customer";

        // Navigation properties
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Vehicle> OwnedVehicles { get; set; } = new List<Vehicle>();
    }
}
