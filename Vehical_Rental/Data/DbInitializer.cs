using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Vehical_Rental.Models;

namespace Vehical_Rental.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles ("Admin", "Customer", "VehicleOwner")
            string[] roleNames = { "Admin", "Customer", "VehicleOwner" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Admin User
            var adminEmail = "admin@rental.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    PhoneNumber = "9876543200",
                    Address = "100 Fleet Center Blvd, Suite 400, Mumbai, India",
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                };

                var createAdminResult = await userManager.CreateAsync(adminUser, "Admin@123");
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else if (adminUser.PhoneNumber != "9876543200")
            {
                adminUser.PhoneNumber = "9876543200";
                await userManager.UpdateAsync(adminUser);
            }

            // 3. Seed Sample Vehicle Owner User
            var ownerEmail = "owner@rental.com";
            var ownerUser = await userManager.FindByEmailAsync(ownerEmail);
            if (ownerUser == null)
            {
                ownerUser = new ApplicationUser
                {
                    UserName = ownerEmail,
                    Email = ownerEmail,
                    EmailConfirmed = true,
                    FullName = "David Miller (Fleet Owner)",
                    PhoneNumber = "9876543210",
                    Address = "742 Evergreen Terrace, Bengaluru, India",
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                };

                var createOwnerResult = await userManager.CreateAsync(ownerUser, "Owner@123");
                if (createOwnerResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(ownerUser, "VehicleOwner");
                }
            }
            else if (ownerUser.PhoneNumber != "9876543210")
            {
                ownerUser.PhoneNumber = "9876543210";
                await userManager.UpdateAsync(ownerUser);
            }

            // 4. Seed Sample Customer Users
            var user1Email = "john@example.com";
            var user1 = await userManager.FindByEmailAsync(user1Email);
            if (user1 == null)
            {
                user1 = new ApplicationUser
                {
                    UserName = user1Email,
                    Email = user1Email,
                    EmailConfirmed = true,
                    FullName = "John Doe",
                    PhoneNumber = "9876543220",
                    Address = "456 Market St, New Delhi, India",
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                };

                var createUser1Result = await userManager.CreateAsync(user1, "User@123");
                if (createUser1Result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user1, "Customer");
                }
            }
            else if (user1.PhoneNumber != "9876543220")
            {
                user1.PhoneNumber = "9876543220";
                await userManager.UpdateAsync(user1);
            }

            var user2Email = "sarah@example.com";
            var user2 = await userManager.FindByEmailAsync(user2Email);
            if (user2 == null)
            {
                user2 = new ApplicationUser
                {
                    UserName = user2Email,
                    Email = user2Email,
                    EmailConfirmed = true,
                    FullName = "Sarah Connor",
                    PhoneNumber = "9876543230",
                    Address = "789 Sunset Blvd, Pune, India",
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                };

                var createUser2Result = await userManager.CreateAsync(user2, "User@123");
                if (createUser2Result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user2, "Customer");
                }
            }
            else if (user2.PhoneNumber != "9876543230")
            {
                user2.PhoneNumber = "9876543230";
                await userManager.UpdateAsync(user2);
            }

            // 5. Seed Vehicles if none exist
            if (context.Vehicles.Any())
            {
                // Ensure existing vehicles have an OwnerId assigned
                var unownedVehicles = context.Vehicles.Where(v => v.OwnerId == null).ToList();
                if (unownedVehicles.Any() && ownerUser != null)
                {
                    foreach (var v in unownedVehicles)
                    {
                        v.OwnerId = ownerUser.Id;
                    }
                    await context.SaveChangesAsync();
                }

                // Ensure existing vehicles have realistic coordinates assigned if they are 0, 0
                var unlocatedVehicles = context.Vehicles.Where(v => v.Latitude == 0 && v.Longitude == 0).ToList();
                if (unlocatedVehicles.Any())
                {
                    var sampleCoords = new[]
                    {
                        (37.7749, -122.4194), // Civic Center / Market St
                        (37.7915, -122.4010), // Financial District
                        (37.8080, -122.4177), // Fisherman's Wharf
                        (37.7599, -122.4148), // Mission District
                        (37.7989, -122.4410), // Marina District
                        (37.7810, -122.3990), // SoMa / Oracle Park
                        (37.7930, -122.4150), // Nob Hill
                        (37.7690, -122.4830)  // Golden Gate Park
                    };
                    for (int i = 0; i < unlocatedVehicles.Count; i++)
                    {
                        var coord = sampleCoords[i % sampleCoords.Length];
                        unlocatedVehicles[i].Latitude = coord.Item1;
                        unlocatedVehicles[i].Longitude = coord.Item2;
                    }
                    await context.SaveChangesAsync();
                }

                // If existing vehicles have USD rates (less than 300), convert them to realistic INR rates
                var lowRateVehicles = context.Vehicles.Where(v => v.DailyRate < 500).ToList();
                if (lowRateVehicles.Any())
                {
                    foreach (var car in lowRateVehicles)
                    {
                        car.DailyRate = car.Category switch
                        {
                            "Sports" => 12000.00m,
                            "Electric" => 4500.00m,
                            "SUV" => 7500.00m,
                            "Sedan" => 6000.00m,
                            _ => 3500.00m
                        };
                    }
                    await context.SaveChangesAsync();
                }

                return; // Vehicles and sample bookings already seeded
            }

            var vehicles = new List<Vehicle>
            {
                new()
                {
                    Make = "Tesla",
                    Model = "Model 3 Long Range",
                    Year = 2024,
                    LicensePlate = "EV-990-TX",
                    Category = "Electric",
                    DailyRate = 4500.00m,
                    Transmission = "Automatic",
                    FuelType = "Electric",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1560958089-b8a1929cea89?auto=format&fit=crop&w=1000&q=80",
                    Description = "Dual-motor all-wheel drive, quick acceleration, Autopilot, 350+ miles range on a single charge.",
                    IsAvailable = true,
                    Latitude = 37.7749,
                    Longitude = -122.4194
                },
                new()
                {
                    Make = "BMW",
                    Model = "X5 xDrive40i",
                    Year = 2023,
                    LicensePlate = "BMW-450-NY",
                    Category = "SUV",
                    DailyRate = 7500.00m,
                    Transmission = "Automatic",
                    FuelType = "Petrol",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1555215695-3004980ad54e?auto=format&fit=crop&w=1000&q=80",
                    Description = "Luxury midsize SUV blending athletic dynamics with plush comfort and high-tech cabin.",
                    IsAvailable = true,
                    Latitude = 37.7915,
                    Longitude = -122.4010
                },
                new()
                {
                    Make = "Mercedes-Benz",
                    Model = "C-Class C300",
                    Year = 2024,
                    LicensePlate = "MB-300-FL",
                    Category = "Sedan",
                    DailyRate = 6500.00m,
                    Transmission = "Automatic",
                    FuelType = "Petrol",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?auto=format&fit=crop&w=1000&q=80",
                    Description = "Elegance meets everyday performance. Premium leather interior, ambient lighting, and Burmester audio.",
                    IsAvailable = true,
                    Latitude = 37.8080,
                    Longitude = -122.4177
                },
                new()
                {
                    Make = "Ford",
                    Model = "Mustang GT Premium",
                    Year = 2023,
                    LicensePlate = "GT-500-CA",
                    Category = "Sports",
                    DailyRate = 5500.00m,
                    Transmission = "Automatic",
                    FuelType = "Petrol",
                    Seats = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1584345604476-8ec5e12e42dd?auto=format&fit=crop&w=1000&q=80",
                    Description = "Iconic 5.0L V8 American muscle car offering thrilling exhaust sound and timeless styling.",
                    IsAvailable = true,
                    Latitude = 37.7599,
                    Longitude = -122.4148
                },
                new()
                {
                    Make = "Toyota",
                    Model = "RAV4 Hybrid XSE",
                    Year = 2024,
                    LicensePlate = "TY-880-WA",
                    Category = "SUV",
                    DailyRate = 3500.00m,
                    Transmission = "Automatic",
                    FuelType = "Hybrid",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1581540222194-0def2dda95b8?auto=format&fit=crop&w=1000&q=80",
                    Description = "Exceptional fuel economy (40+ MPG), spacious cargo room, and Toyota Safety Sense suite.",
                    IsAvailable = true,
                    Latitude = 37.7989,
                    Longitude = -122.4410
                },
                new()
                {
                    Make = "Audi",
                    Model = "A6 Premium Plus",
                    Year = 2023,
                    LicensePlate = "AU-620-IL",
                    Category = "Sedan",
                    DailyRate = 6000.00m,
                    Transmission = "Automatic",
                    FuelType = "Petrol",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6?auto=format&fit=crop&w=1000&q=80",
                    Description = "Audi Quattro all-wheel drive, dual touchscreens, virtual cockpit, and exceptionally smooth ride.",
                    IsAvailable = true,
                    Latitude = 37.7810,
                    Longitude = -122.3990
                },
                new()
                {
                    Make = "Porsche",
                    Model = "911 Carrera S",
                    Year = 2024,
                    LicensePlate = "PR-911-NV",
                    Category = "Sports",
                    DailyRate = 12000.00m,
                    Transmission = "Automatic",
                    FuelType = "Petrol",
                    Seats = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1000&q=80",
                    Description = "The benchmark sports car. Twin-turbo flat-six engine delivering precision handling and unmatched road feel.",
                    IsAvailable = true,
                    Latitude = 37.7930,
                    Longitude = -122.4150
                },
                new()
                {
                    Make = "Hyundai",
                    Model = "Ioniq 5 Limited",
                    Year = 2024,
                    LicensePlate = "HY-550-CO",
                    Category = "Electric",
                    DailyRate = 4000.00m,
                    Transmission = "Automatic",
                    FuelType = "Electric",
                    Seats = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1617814076367-b759c7d7e738?auto=format&fit=crop&w=1000&q=80",
                    Description = "Futuristic design, ultra-fast 800V charging, relaxing interior lounge seats, and whisper quiet ride.",
                    IsAvailable = true,
                    Latitude = 37.7690,
                    Longitude = -122.4830
                }
            };

            if (ownerUser != null)
            {
                foreach (var v in vehicles)
                {
                    v.OwnerId = ownerUser.Id;
                }
            }

            await context.Vehicles.AddRangeAsync(vehicles);
            await context.SaveChangesAsync();

            // 5. Seed Sample Bookings & Payments
            var booking1 = new Booking
            {
                UserId = user1.Id,
                VehicleId = vehicles[0].Id, // Tesla Model 3
                StartDate = DateTime.UtcNow.AddDays(2).Date,
                EndDate = DateTime.UtcNow.AddDays(5).Date,
                TotalPrice = 3 * 4500.00m,
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
                TotalPrice = 4 * 7500.00m,
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
                TotalPrice = 2 * 5500.00m,
                Status = "Pending",
                BookingDate = DateTime.UtcNow.AddDays(-1),
                Notes = "Weekend getaway trip."
            };

            await context.Bookings.AddRangeAsync(booking1, booking2, booking3);
            await context.SaveChangesAsync();

            // 6. Seed Payments
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

            await context.Payments.AddRangeAsync(payment1, payment2);
            await context.SaveChangesAsync();

            // 7. Seed Reviews
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

            await context.Reviews.AddRangeAsync(review1, review2);
            await context.SaveChangesAsync();
        }
    }
}
