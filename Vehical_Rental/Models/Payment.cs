using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vehical_Rental.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }
        public virtual Booking? Booking { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 100000)]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Credit Card"; // Credit Card, Debit Card, PayPal, Cash

        [Required]
        [MaxLength(30)]
        public string PaymentStatus { get; set; } = "Completed"; // Pending, Completed, Failed, Refunded

        [MaxLength(100)]
        public string TransactionId { get; set; } = Guid.NewGuid().ToString("N")[..12].ToUpper();
    }
}