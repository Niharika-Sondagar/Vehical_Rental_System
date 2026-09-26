using System.Security.Cryptography;
using System.Text;
using Vehical_Rental.Models;

namespace Vehical_Rental.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Users.Any())
            {
                return; // Database has already been seeded
            }

            // 1. Seed Users
            var admin = new User
            {
                FullName = "System Administrator",
                Email = "admin@rental.com",
                PasswordHash = HashPassword("Admin@123"),
                PhoneNumber = "1-800-555-0100",
                Address = "100 Fleet Center Blvd, Suite 400, Chicago, IL",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            };

            var user1 = new User
            {
                FullName = "John Doe",
                Email = "john@example.com",
                PasswordHash = HashPassword("User@123"),
                PhoneNumber = "1-800-555-0199",
                Address = "456 Market St, San Francisco, CA",
                Role = "Customer",
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            };

            var user2 = new User
            {
                FullName = "Sarah Connor",
                Email = "sarah@example.com",
                PasswordHash = HashPassword("User@123"),
                PhoneNumber = "1-800-555-0245",
                Address = "789 Sunset Blvd, Los Angeles, CA",
                Role = "Customer",
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            };

            context.Users.AddRange(admin, user1, user2);
            context.SaveChanges();

            // 2. Seed Vehicles
            var vehicles = new List<Vehicle>
            {
                new()
                {
                    Make = "Tesla",
                    Model = "Model 3 Long Range",
                    Year = 2024,
                    LicensePlate = "EV-990-TX",
                    Category = "Electric",
                    DailyRate = 89.00m,
                    Transmission = "Automatic",
                    FuelType = "Electric",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1560958089-b8a1929cea89?auto=format&fit=crop&w=1000&q=80",
                    Description = "Dual-motor all-wheel drive, quick acceleration, Autopilot, 350+ miles range on a single charge.",
                    IsAvailable = true
                },
                new()
                {
                    Make = "BMW",
                    Model = "X5 xDrive40i",
                    Year = 2023,
                    LicensePlate = "BMW-450-NY",
                    Category = "SUV",
                    DailyRate = 125.00m,
                    Transmission = "Automatic",
                    FuelType = "Petrol",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1555215695-3004980ad54e?auto=format&fit=crop&w=1000&q=80",
                    Description = "Luxury midsize SUV blending athletic dynamics with plush comfort and high-tech cabin.",
                    IsAvailable = true
                },
                new()
                {
                    Make = "Mercedes-Benz",
                    Model = "C-Class C300",
                    Year = 2024,
                    LicensePlate = "MB-300-FL",
                    Category = "Sedan",
                    DailyRate = 110.00m,
                    Transmission = "Automatic",
                    FuelType = "Petrol",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?auto=format&fit=crop&w=1000&q=80",
                    Description = "Elegance meets everyday performance. Premium leather interior, ambient lighting, and Burmester audio.",
                    IsAvailable = true
                },
                new()
                {
                    Make = "Ford",
                    Model = "Mustang GT Premium",
                    Year = 2023,
                    LicensePlate = "GT-500-CA",
                    Category = "Sports",
                    DailyRate = 95.00m,
                    Transmission = "Automatic",
                    FuelType = "Petrol",
                    Seats = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1584345604476-8ec5e12e42dd?auto=format&fit=crop&w=1000&q=80",
                    Description = "Iconic 5.0L V8 American muscle car offering thrilling exhaust sound and timeless styling.",
                    IsAvailable = true
                },
                new()
                {
                    Make = "Toyota",
                    Model = "RAV4 Hybrid XSE",
                    Year = 2024,
                    LicensePlate = "TY-880-WA",
                    Category = "SUV",
                    DailyRate = 65.00m,
                    Transmission = "Automatic",
                    FuelType = "Hybrid",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1581540222194-0def2dda95b8?auto=format&fit=crop&w=1000&q=80",
                    Description = "Exceptional fuel economy (40+ MPG), spacious cargo room, and Toyota Safety Sense suite.",
                    IsAvailable = true
                },
                new()
                {
                    Make = "Audi",
                    Model = "A6 Premium Plus",
                    Year = 2023,
                    LicensePlate = "AU-620-IL",
                    Category = "Sedan",
                    DailyRate = 105.00m,
                    Transmission = "Automatic",
                    FuelType = "Petrol",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6?auto=format&fit=crop&w=1000&q=80",
                    Description = "Audi Quattro all-wheel drive, dual touchscreens, virtual cockpit, and exceptionally smooth ride.",
                    IsAvailable = true
                },
                new()
                {
                    Make = "Porsche",
                    Model = "911 Carrera S",
                    Year = 2024,
                    LicensePlate = "PR-911-NV",
                    Category = "Sports",
                    DailyRate = 240.00m,
                    Transmission = "Automatic",
                    FuelType = "Petrol",
                    Seats = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1000&q=80",
                    Description = "The benchmark sports car. Twin-turbo flat-six engine delivering precision handling and unmatched road feel.",
                    IsAvailable = true
                },
                new()
                {
                    Make = "Hyundai",
                    Model = "Ioniq 5 Limited",
                    Year = 2024,
                    LicensePlate = "HY-550-CO",
                    Category = "Electric",
                    DailyRate = 75.00m,
                    Transmission = "Automatic",
                    FuelType = "Electric",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1617814076367-b759c7d7e738?auto=format&fit=crop&w=1000&q=80",
                    Description = "Futuristic design, ultra-fast 800V charging, relaxing interior lounge seats, and whisper quiet ride.",
                    IsAvailable = true
                }
            };

            context.Vehicles.AddRange(vehicles);
            context.SaveChanges();

            // 3. Seed Sample Bookings & Payments
            var booking1 = new Booking
            {
                UserId = user1.Id,
                VehicleId = vehicles[0].Id, // Tesla Model 3
                StartDate = DateTime.UtcNow.AddDays(2).Date,
                EndDate = DateTime.UtcNow.AddDays(5).Date,
                TotalPrice = 3 * 89.00m,
                Status = "Confirmed",
                BookingDate = DateTime.UtcNow.AddDays(-2),
                Notes = "Please provide airport pickup if available."
            };

            var booking2 = new Booking
            {
                UserId = user2.Id,
                VehicleId = vehicles[1].Id, // BMW X5
                StartDate = DateTime.UtcNow.AddDays(-10).Date,
                EndDate = DateTime.UtcNow.AddDays(-6).Date,
                TotalPrice = 4 * 125.00m,
                Status = "Completed",
                BookingDate = DateTime.UtcNow.AddDays(-15),
                Notes = "Child safety seat requested."
            };

            var booking3 = new Booking
            {
                UserId = user1.Id,
                VehicleId = vehicles[3].Id, // Mustang GT
                StartDate = DateTime.UtcNow.AddDays(7).Date,
                EndDate = DateTime.UtcNow.AddDays(9).Date,
                TotalPrice = 2 * 95.00m,
                Status = "Pending",
                BookingDate = DateTime.UtcNow.AddDays(-1),
                Notes = "Weekend getaway trip."
            };

            context.Bookings.AddRange(booking1, booking2, booking3);
            context.SaveChanges();

            // 4. Seed Payments
            var payment1 = new Payment
            {
                BookingId = booking1.Id,
                Amount = booking1.TotalPrice,
                PaymentDate = DateTime.UtcNow.AddDays(-2),
                PaymentMethod = "Credit Card",
                PaymentStatus = "Completed",
                TransactionId = "TXN" + Guid.NewGuid().ToString("N")[..10].ToUpper()
            };

            var payment2 = new Payment
            {
                BookingId = booking2.Id,
                Amount = booking2.TotalPrice,
                PaymentDate = DateTime.UtcNow.AddDays(-15),
                PaymentMethod = "Debit Card",
                PaymentStatus = "Completed",
                TransactionId = "TXN" + Guid.NewGuid().ToString("N")[..10].ToUpper()
            };

            context.Payments.AddRange(payment1, payment2);
            context.SaveChanges();

            // 5. Seed Reviews
            var review1 = new Review
            {
                VehicleId = vehicles[1].Id,
                UserId = user2.Id,
                Rating = 5,
                Comment = "The BMW X5 was in pristine condition and handled mountain curves effortlessly. Highly recommend!",
                ReviewDate = DateTime.UtcNow.AddDays(-5)
            };

            var review2 = new Review
            {
                VehicleId = vehicles[0].Id,
                UserId = user1.Id,
                Rating = 5,
                Comment = "Incredible technology! The Tesla made our road trip super enjoyable and charging was effortless.",
                ReviewDate = DateTime.UtcNow.AddDays(-1)
            };

            context.Reviews.AddRange(review1, review2);
            context.SaveChanges();
        }

        public static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
