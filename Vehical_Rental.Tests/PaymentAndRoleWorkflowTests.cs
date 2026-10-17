using Microsoft.EntityFrameworkCore;
using Vehical_Rental.Data;
using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Implementations;
using Vehical_Rental.Services.Implementations;
using Xunit;

namespace Vehical_Rental.Tests
{
    public class PaymentAndRoleWorkflowTests
    {
        private ApplicationDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
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
        public async Task FullEndToEndWorkflow_OwnerRentingOtherOwnersVehicle_To_Payment()
        {
            // Scenario:
            // 1. Owner A ("owner-alice") lists a Tesla (Vehicle 1)
            // 2. Owner B ("owner-bob") lists a Mustang (Vehicle 2)
            // 3. Owner A tries to rent Vehicle 1 (their own) -> MUST BE BLOCKED
            // 4. Owner A rents Vehicle 2 (belonging to Owner B) -> Creates Pending booking
            // 5. Owner B reviews booking requests and accepts Owner A's booking -> Status becomes Accepted
            // 6. Owner A makes payment for the Accepted booking -> Status becomes Confirmed and Payment record saved

            var db = GetInMemoryDbContext(nameof(FullEndToEndWorkflow_OwnerRentingOtherOwnersVehicle_To_Payment));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);

            var ownerAlice = await CreateUserAsync(db, "owner-alice", "alice@rental.com", "VehicleOwner");
            var ownerBob = await CreateUserAsync(db, "owner-bob", "bob@rental.com", "VehicleOwner");

            // Seed Vehicles
            var car1 = new Vehicle
            {
                Id = 1,
                Make = "Tesla",
                Model = "Model Y",
                Year = 2024,
                OwnerId = ownerAlice.Id,
                DailyRate = 5000m,
                IsAvailable = true
            };
            var car2 = new Vehicle
            {
                Id = 2,
                Make = "Ford",
                Model = "Mustang GT",
                Year = 2024,
                OwnerId = ownerBob.Id,
                DailyRate = 6000m,
                IsAvailable = true
            };
            await db.Vehicles.AddRangeAsync(car1, car2);
            await db.SaveChangesAsync();

            // Step 3: Owner Alice tries to rent car1 (her own)
            var attemptRentOwn = new Booking
            {
                VehicleId = car1.Id,
                UserId = ownerAlice.Id,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(4)
            };
            var ownResult = await bookingService.CreateBookingAsync(attemptRentOwn);
            Assert.False(ownResult.Success);
            Assert.Contains("Vehicle Owners cannot book their own vehicles", ownResult.Message);

            // Step 4: Owner Alice rents car2 (belonging to Bob)
            var validRental = new Booking
            {
                VehicleId = car2.Id,
                UserId = ownerAlice.Id,
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(5) // 3 days
            };
            var rentalResult = await bookingService.CreateBookingAsync(validRental);
            Assert.True(rentalResult.Success);
            Assert.Equal("Pending", validRental.Status);
            Assert.Equal(3 * 6000m, validRental.TotalPrice);

            // Step 5: Owner Bob reviews requests in his fleet inbox and accepts
            var bobFleetRequests = await bookingRepo.GetByOwnerIdAsync(ownerBob.Id);
            var bobRequest = Assert.Single(bobFleetRequests);
            Assert.Equal(validRental.Id, bobRequest.Id);
            Assert.Equal(ownerAlice.Id, bobRequest.UserId);

            var acceptResult = await bookingService.AcceptBookingAsync(validRental.Id, ownerBob.Id, isAdmin: false);
            Assert.True(acceptResult.Success);

            var acceptedBooking = await bookingRepo.GetByIdAsync(validRental.Id);
            Assert.Equal("Accepted", acceptedBooking!.Status);

            // Step 6: Process payment in INR (Simulating PaymentController.Process)
            var payment = new Payment
            {
                BookingId = acceptedBooking.Id,
                Amount = acceptedBooking.TotalPrice,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "UPI",
                PaymentStatus = "Completed",
                TransactionId = "TXN" + Guid.NewGuid().ToString("N")[..8].ToUpper()
            };
            await db.Payments.AddAsync(payment);
            acceptedBooking.Status = "Confirmed";
            await bookingRepo.UpdateAsync(acceptedBooking);

            // Verify final state
            var finalBooking = await bookingRepo.GetByIdAsync(validRental.Id);
            Assert.Equal("Confirmed", finalBooking!.Status);
            Assert.NotNull(finalBooking.Payment);
            Assert.Equal(18000m, finalBooking.Payment.Amount);
            Assert.Equal("UPI", finalBooking.Payment.PaymentMethod);
            Assert.Equal("Completed", finalBooking.Payment.PaymentStatus);

            // Verify that Alice can see this in her customer rentals
            var aliceRentals = await bookingRepo.GetByCustomerIdAsync(ownerAlice.Id);
            var aliceBooking = Assert.Single(aliceRentals);
            Assert.Equal("Confirmed", aliceBooking.Status);
        }

        [Fact]
        public async Task VehicleService_CRUD_And_DistanceCalculation()
        {
            var db = GetInMemoryDbContext(nameof(VehicleService_CRUD_And_DistanceCalculation));
            var vehicleRepo = new VehicleRepository(db);
            var vehicleService = new VehicleService(vehicleRepo);

            var owner = await CreateUserAsync(db, "owner-test", "testowner@test.com", "VehicleOwner");

            // Create
            var newVehicle = new Vehicle
            {
                Make = "Hyundai",
                Model = "Creta",
                Year = 2024,
                LicensePlate = "GJ-05-AA-1111",
                Category = "SUV",
                DailyRate = 3000m,
                OwnerId = owner.Id,
                Latitude = 21.1702, // Surat
                Longitude = 72.8311,
                IsAvailable = true
            };
            await vehicleService.AddAsync(newVehicle);

            var retrieved = await vehicleService.GetByIdAsync(newVehicle.Id);
            Assert.NotNull(retrieved);
            Assert.Equal("2024 Hyundai Creta", retrieved.FullName);

            // Calculate distance between Surat and Mumbai (approx 260-280 km)
            double distanceKm = VehicleRepository.CalculateDistance(21.1702, 72.8311, 19.0760, 72.8777);
            Assert.True(distanceKm > 200 && distanceKm < 350, $"Distance was {distanceKm}");

            // Filter nearby: Within 50km of Surat should find it
            var nearbyVehicles = await vehicleService.GetNearbyVehiclesAsync(21.1702, 72.8311, 50);
            Assert.Contains(nearbyVehicles, v => v.Id == newVehicle.Id);

            // Within 10km of London should NOT find it
            var farVehicles = await vehicleService.GetNearbyVehiclesAsync(51.5074, -0.1278, 50);
            Assert.DoesNotContain(farVehicles, v => v.Id == newVehicle.Id);

            // Update
            retrieved.DailyRate = 3500m;
            retrieved.IsAvailable = false;
            await vehicleService.UpdateAsync(retrieved);

            var updated = await vehicleService.GetByIdAsync(newVehicle.Id);
            Assert.Equal(3500m, updated!.DailyRate);
            Assert.False(updated.IsAvailable);

            // Delete
            await vehicleService.DeleteAsync(newVehicle.Id);
            var deleted = await vehicleService.GetByIdAsync(newVehicle.Id);
            Assert.Null(deleted);
        }
    }
}
