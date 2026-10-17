using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vehical_Rental.Data;
using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Implementations;
using Vehical_Rental.Services.Implementations;
using Xunit;

namespace Vehical_Rental.Tests
{
    public class AdminAndSecurityTests
    {
        private ApplicationDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ApplicationDbContext(options);
        }

        private async Task<ApplicationUser> CreateUserAsync(ApplicationDbContext db, string id, string email, string role)
        {
            var user = new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                FullName = email.Split('@')[0],
                Role = role
            };
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task Admin_CanToggleVehicleSafetyStatus()
        {
            var db = GetInMemoryDbContext(nameof(Admin_CanToggleVehicleSafetyStatus));
            var vehicleRepo = new VehicleRepository(db);
            var vehicleService = new VehicleService(vehicleRepo);

            var owner = await CreateUserAsync(db, "owner-safe", "owner@safe.com", "VehicleOwner");

            var car = new Vehicle
            {
                Id = 1,
                Make = "Honda",
                Model = "Accord",
                Year = 2023,
                LicensePlate = "SAFE-01",
                OwnerId = owner.Id,
                IsAvailable = true
            };
            await vehicleService.AddAsync(car);

            // Admin suspends vehicle for passenger safety
            var fetched = await vehicleService.GetByIdAsync(car.Id);
            Assert.NotNull(fetched);
            Assert.True(fetched.IsAvailable);

            fetched.IsAvailable = false;
            await vehicleService.UpdateAsync(fetched);

            var suspended = await vehicleService.GetByIdAsync(car.Id);
            Assert.False(suspended!.IsAvailable);

            // Admin restores vehicle
            suspended.IsAvailable = true;
            await vehicleService.UpdateAsync(suspended);

            var restored = await vehicleService.GetByIdAsync(car.Id);
            Assert.True(restored!.IsAvailable);
        }

        [Fact]
        public async Task ReviewService_CalculatesAverageRatingCorrectly()
        {
            var db = GetInMemoryDbContext(nameof(ReviewService_CalculatesAverageRatingCorrectly));
            var reviewRepo = new ReviewRepository(db);
            var reviewService = new ReviewService(reviewRepo);

            var user1 = await CreateUserAsync(db, "user-1", "u1@test.com", "Customer");
            var user2 = await CreateUserAsync(db, "user-2", "u2@test.com", "Customer");
            var owner = await CreateUserAsync(db, "owner-r", "owner@r.com", "VehicleOwner");

            var car = new Vehicle { Id = 10, Make = "Tesla", Model = "Model S", Year = 2024, LicensePlate = "TS-10", OwnerId = owner.Id };
            await db.Vehicles.AddAsync(car);
            await db.SaveChangesAsync();

            // Add reviews (ratings: 5 and 4 -> Average = 4.5)
            var r1 = new Review { VehicleId = car.Id, UserId = user1.Id, Rating = 5, Comment = "Amazing car!" };
            var r2 = new Review { VehicleId = car.Id, UserId = user2.Id, Rating = 4, Comment = "Very good." };

            var addResult1 = await reviewService.AddAsync(r1);
            var addResult2 = await reviewService.AddAsync(r2);

            Assert.True(addResult1.Success);
            Assert.True(addResult2.Success);

            var reviews = (await reviewService.GetByVehicleIdAsync(car.Id)).ToList();
            Assert.Equal(2, reviews.Count);
            var avg = reviews.Average(r => r.Rating);
            Assert.Equal(4.5, avg);
        }
    }
}
