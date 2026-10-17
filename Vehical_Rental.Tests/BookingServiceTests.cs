using Microsoft.EntityFrameworkCore;
using Vehical_Rental.Data;
using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Implementations;
using Vehical_Rental.Services.Implementations;
using Xunit;

namespace Vehical_Rental.Tests
{
    public class BookingServiceTests
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
        public async Task Owner_CannotRentTheirOwnVehicle()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(Owner_CannotRentTheirOwnVehicle));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);

            var owner = await CreateUserAsync(db, "owner-123", "owner1@rental.com", "VehicleOwner");

            var vehicle = new Vehicle
            {
                Id = 1,
                Make = "Tesla",
                Model = "Model 3",
                Year = 2024,
                LicensePlate = "EV-100",
                Category = "Electric",
                DailyRate = 4500m,
                OwnerId = owner.Id,
                IsAvailable = true
            };
            await db.Vehicles.AddAsync(vehicle);
            await db.SaveChangesAsync();

            var booking = new Booking
            {
                VehicleId = vehicle.Id,
                UserId = owner.Id, // Same as OwnerId!
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(4)
            };

            // Act
            var result = await bookingService.CreateBookingAsync(booking);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Vehicle Owners cannot book their own vehicles", result.Message);
        }

        [Fact]
        public async Task Owner_CanRentAnotherOwnersVehicle()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(Owner_CanRentAnotherOwnersVehicle));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);

            var ownerA = await CreateUserAsync(db, "owner-A", "ownerA@rental.com", "VehicleOwner");
            var ownerB = await CreateUserAsync(db, "owner-B", "ownerB@rental.com", "VehicleOwner");

            var vehicleOfB = new Vehicle
            {
                Id = 2,
                Make = "BMW",
                Model = "X5",
                Year = 2024,
                LicensePlate = "BMW-200",
                Category = "SUV",
                DailyRate = 6000m,
                OwnerId = ownerB.Id,
                IsAvailable = true
            };
            await db.Vehicles.AddAsync(vehicleOfB);
            await db.SaveChangesAsync();

            var bookingByA = new Booking
            {
                VehicleId = vehicleOfB.Id,
                UserId = ownerA.Id, // Owner A renting Owner B's vehicle
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(5)
            };

            // Act
            var result = await bookingService.CreateBookingAsync(bookingByA);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Pending", bookingByA.Status);
            Assert.Equal(3 * 6000m, bookingByA.TotalPrice);

            // Verify in repository
            var savedBooking = await bookingRepo.GetByIdAsync(bookingByA.Id);
            Assert.NotNull(savedBooking);
            Assert.Equal("Pending", savedBooking.Status);
            Assert.Equal(ownerA.Id, savedBooking.UserId);
            Assert.Equal(ownerB.Id, savedBooking.Vehicle?.OwnerId);
        }

        [Fact]
        public async Task Customer_CanRentAnyAvailableVehicle()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(Customer_CanRentAnyAvailableVehicle));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);

            var customer = await CreateUserAsync(db, "customer-john", "john@example.com", "Customer");
            var owner = await CreateUserAsync(db, "fleet-owner", "owner@rental.com", "VehicleOwner");

            var vehicle = new Vehicle
            {
                Id = 3,
                Make = "Toyota",
                Model = "RAV4",
                Year = 2024,
                LicensePlate = "TY-300",
                Category = "SUV",
                DailyRate = 3500m,
                OwnerId = owner.Id,
                IsAvailable = true
            };
            await db.Vehicles.AddAsync(vehicle);
            await db.SaveChangesAsync();

            var booking = new Booking
            {
                VehicleId = vehicle.Id,
                UserId = customer.Id,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(3)
            };

            // Act
            var result = await bookingService.CreateBookingAsync(booking);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Pending", booking.Status);
            Assert.Equal(2 * 3500m, booking.TotalPrice);
        }

        [Fact]
        public async Task Customer_CannotBookOverlappingDates()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(Customer_CannotBookOverlappingDates));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);

            var customer = await CreateUserAsync(db, "customer-john", "john@example.com", "Customer");
            var owner = await CreateUserAsync(db, "car-owner", "owner@rental.com", "VehicleOwner");

            var car1 = new Vehicle { Id = 10, Make = "Tesla", Model = "Model 3", DailyRate = 4000m, OwnerId = owner.Id, IsAvailable = true };
            var car2 = new Vehicle { Id = 11, Make = "BMW", Model = "M3", DailyRate = 5000m, OwnerId = owner.Id, IsAvailable = true };
            await db.Vehicles.AddRangeAsync(car1, car2);

            // First active booking for Oct 20 to Oct 25
            var booking1 = new Booking
            {
                Id = 101,
                VehicleId = car1.Id,
                UserId = customer.Id,
                StartDate = DateTime.Today.AddDays(5),
                EndDate = DateTime.Today.AddDays(10),
                Status = "Confirmed"
            };
            await db.Bookings.AddAsync(booking1);
            await db.SaveChangesAsync();

            // Customer attempts second booking on car2 overlapping Oct 22 to Oct 27
            var overlappingBooking = new Booking
            {
                VehicleId = car2.Id,
                UserId = customer.Id,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(12)
            };

            // Act
            var result = await bookingService.CreateBookingAsync(overlappingBooking);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("already have an active rental booking for the selected dates", result.Message);
        }

        [Fact]
        public async Task Vehicle_CannotBeDoubleBookedOnOverlappingDates()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(Vehicle_CannotBeDoubleBookedOnOverlappingDates));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);

            var customer1 = await CreateUserAsync(db, "customer-1", "c1@test.com", "Customer");
            var customer2 = await CreateUserAsync(db, "customer-2", "c2@test.com", "Customer");
            var owner = await CreateUserAsync(db, "car-owner", "owner@rental.com", "VehicleOwner");

            var car = new Vehicle { Id = 20, Make = "Audi", Model = "A6", DailyRate = 5000m, OwnerId = owner.Id, IsAvailable = true };
            await db.Vehicles.AddAsync(car);

            // Customer 1 books car for Oct 10 to Oct 15
            var booking1 = new Booking
            {
                Id = 201,
                VehicleId = car.Id,
                UserId = customer1.Id,
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(7),
                Status = "Confirmed"
            };
            await db.Bookings.AddAsync(booking1);
            await db.SaveChangesAsync();

            // Customer 2 attempts to book same car for overlapping dates
            var booking2 = new Booking
            {
                VehicleId = car.Id,
                UserId = customer2.Id,
                StartDate = DateTime.Today.AddDays(4),
                EndDate = DateTime.Today.AddDays(9)
            };

            // Act
            var result = await bookingService.CreateBookingAsync(booking2);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("not available for the selected dates", result.Message);
        }

        [Fact]
        public async Task Owner_CanAcceptBooking_ForTheirVehicle()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(Owner_CanAcceptBooking_ForTheirVehicle));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);

            var owner = await CreateUserAsync(db, "owner-correct", "owner@test.com", "VehicleOwner");
            var customer = await CreateUserAsync(db, "customer-sarah", "sarah@test.com", "Customer");

            var car = new Vehicle { Id = 30, Make = "Porsche", Model = "911", OwnerId = owner.Id, DailyRate = 10000m, IsAvailable = true };
            await db.Vehicles.AddAsync(car);

            var booking = new Booking
            {
                Id = 301,
                VehicleId = car.Id,
                UserId = customer.Id,
                StartDate = DateTime.Today.AddDays(3),
                EndDate = DateTime.Today.AddDays(6),
                Status = "Pending"
            };
            await db.Bookings.AddAsync(booking);
            await db.SaveChangesAsync();

            // Act
            var result = await bookingService.AcceptBookingAsync(booking.Id, owner.Id, isAdmin: false);

            // Assert
            Assert.True(result.Success);
            var updated = await db.Bookings.FindAsync(booking.Id);
            Assert.Equal("Accepted", updated!.Status);
        }

        [Fact]
        public async Task Owner_CannotAcceptBooking_ForAnotherOwnersVehicle()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(Owner_CannotAcceptBooking_ForAnotherOwnersVehicle));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);

            var actualOwner = await CreateUserAsync(db, "owner-real", "real@test.com", "VehicleOwner");
            var rogueOwner = await CreateUserAsync(db, "owner-rogue", "rogue@test.com", "VehicleOwner");
            var customer = await CreateUserAsync(db, "customer-sarah", "sarah@test.com", "Customer");

            var car = new Vehicle { Id = 40, Make = "Ferrari", Model = "Roma", OwnerId = actualOwner.Id, DailyRate = 15000m, IsAvailable = true };
            await db.Vehicles.AddAsync(car);

            var booking = new Booking
            {
                Id = 401,
                VehicleId = car.Id,
                UserId = customer.Id,
                StartDate = DateTime.Today.AddDays(3),
                EndDate = DateTime.Today.AddDays(6),
                Status = "Pending"
            };
            await db.Bookings.AddAsync(booking);
            await db.SaveChangesAsync();

            // Act
            var result = await bookingService.AcceptBookingAsync(booking.Id, rogueOwner.Id, isAdmin: false);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Access Denied", result.Message);
            var updated = await db.Bookings.FindAsync(booking.Id);
            Assert.Equal("Pending", updated!.Status);
        }

        [Fact]
        public async Task Owner_CanRejectBooking_ForTheirVehicle()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(Owner_CanRejectBooking_ForTheirVehicle));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);

            var owner = await CreateUserAsync(db, "owner-50", "owner50@test.com", "VehicleOwner");
            var customer = await CreateUserAsync(db, "customer-mike", "mike@test.com", "Customer");

            var car = new Vehicle { Id = 50, Make = "Honda", Model = "Civic", OwnerId = owner.Id, DailyRate = 3000m, IsAvailable = true };
            await db.Vehicles.AddAsync(car);

            var booking = new Booking
            {
                Id = 501,
                VehicleId = car.Id,
                UserId = customer.Id,
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(4),
                Status = "Pending"
            };
            await db.Bookings.AddAsync(booking);
            await db.SaveChangesAsync();

            // Act
            var result = await bookingService.RejectBookingAsync(booking.Id, owner.Id, isAdmin: false);

            // Assert
            Assert.True(result.Success);
            var updated = await db.Bookings.FindAsync(booking.Id);
            Assert.Equal("Rejected", updated!.Status);
        }
    }
}
