using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vehical_Rental.Controllers;
using Vehical_Rental.Data;
using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Implementations;
using Vehical_Rental.Services.Implementations;
using Vehical_Rental.Services.Interfaces;
using Xunit;

namespace Vehical_Rental.Tests
{
    public class ControllerTests
    {
        private ApplicationDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ApplicationDbContext(options);
        }

        private ControllerContext CreateControllerContext(string userId, string role, string userName = "TestUser")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Email, $"{userName.ToLower()}@test.com"),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            return new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task BookingController_GET_Create_WithOwnVehicle_RedirectsWithError()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(BookingController_GET_Create_WithOwnVehicle_RedirectsWithError));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);
            var vehicleService = new VehicleService(vehicleRepo);

            var ownerId = "owner-1";
            var car = new Vehicle
            {
                Id = 10,
                Make = "Tesla",
                Model = "Model 3",
                DailyRate = 4500m,
                OwnerId = ownerId,
                IsAvailable = true
            };
            await db.Vehicles.AddAsync(car);
            await db.SaveChangesAsync();

            var controller = new BookingController(bookingService, vehicleService)
            {
                ControllerContext = CreateControllerContext(ownerId, "VehicleOwner", "Owner1"),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };

            // Act: Owner 1 attempts to open create page for their own vehicle
            var result = await controller.Create(car.Id, DateTime.Today, DateTime.Today.AddDays(3));

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Vehicle", redirectResult.ControllerName);
            Assert.Contains("You cannot rent your own vehicle", controller.TempData["Error"]?.ToString());
        }

        [Fact]
        public async Task BookingController_GET_Create_WithOtherOwnersVehicle_ReturnsView()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(BookingController_GET_Create_WithOtherOwnersVehicle_ReturnsView));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);
            var vehicleService = new VehicleService(vehicleRepo);

            var owner1Id = "owner-1";
            var owner2Id = "owner-2";

            var carOfOwner2 = new Vehicle
            {
                Id = 20,
                Make = "BMW",
                Model = "X5",
                DailyRate = 6000m,
                OwnerId = owner2Id,
                IsAvailable = true
            };
            await db.Vehicles.AddAsync(carOfOwner2);
            await db.SaveChangesAsync();

            var controller = new BookingController(bookingService, vehicleService)
            {
                ControllerContext = CreateControllerContext(owner1Id, "VehicleOwner", "Owner1"),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };

            // Act: Owner 1 opens create page for Owner 2's vehicle
            var result = await controller.Create(carOfOwner2.Id, DateTime.Today.AddDays(1), DateTime.Today.AddDays(4));

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Booking>(viewResult.Model);
            Assert.Equal(carOfOwner2.Id, model.VehicleId);
        }

        [Fact]
        public async Task BookingController_POST_Create_WithOwnVehicle_ReturnsModelError()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(BookingController_POST_Create_WithOwnVehicle_ReturnsModelError));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);
            var vehicleService = new VehicleService(vehicleRepo);

            var ownerId = "owner-1";
            var car = new Vehicle
            {
                Id = 30,
                Make = "Mercedes",
                Model = "C300",
                DailyRate = 5500m,
                OwnerId = ownerId,
                IsAvailable = true
            };
            await db.Vehicles.AddAsync(car);
            await db.SaveChangesAsync();

            var controller = new BookingController(bookingService, vehicleService)
            {
                ControllerContext = CreateControllerContext(ownerId, "VehicleOwner", "Owner1"),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };

            var booking = new Booking
            {
                VehicleId = car.Id,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(4)
            };

            // Act: Owner 1 submits booking on their own car
            var result = await controller.Create(booking);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState.Values.SelectMany(v => v.Errors), e => e.ErrorMessage.Contains("Vehicle owners cannot book their own vehicles"));
        }

        [Fact]
        public async Task BookingController_POST_Create_WithOtherOwnersVehicle_Succeeds()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(BookingController_POST_Create_WithOtherOwnersVehicle_Succeeds));
            var bookingRepo = new BookingRepository(db);
            var vehicleRepo = new VehicleRepository(db);
            var bookingService = new BookingService(bookingRepo, vehicleRepo);
            var vehicleService = new VehicleService(vehicleRepo);

            var owner1Id = "owner-1";
            var owner2Id = "owner-2";

            var car = new Vehicle
            {
                Id = 40,
                Make = "Audi",
                Model = "A6",
                DailyRate = 6000m,
                OwnerId = owner2Id,
                IsAvailable = true
            };
            await db.Vehicles.AddAsync(car);
            await db.SaveChangesAsync();

            var controller = new BookingController(bookingService, vehicleService)
            {
                ControllerContext = CreateControllerContext(owner1Id, "VehicleOwner", "Owner1"),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };

            var booking = new Booking
            {
                VehicleId = car.Id,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(4)
            };

            // Act: Owner 1 submits booking on Owner 2's car
            var result = await controller.Create(booking);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Contains("Your booking request has been sent", controller.TempData["Success"]?.ToString());
        }

        [Fact]
        public async Task VehicleController_UnauthorizedOwnerCannotEditAnotherOwnersCar()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(VehicleController_UnauthorizedOwnerCannotEditAnotherOwnersCar));
            var vehicleRepo = new VehicleRepository(db);
            var vehicleService = new VehicleService(vehicleRepo);
            var httpClientFactory = Mock.Of<IHttpClientFactory>();

            var car = new Vehicle
            {
                Id = 50,
                Make = "Porsche",
                Model = "911",
                DailyRate = 12000m,
                OwnerId = "owner-real",
                IsAvailable = true
            };
            await db.Vehicles.AddAsync(car);
            await db.SaveChangesAsync();

            var controller = new VehicleController(vehicleService, httpClientFactory)
            {
                ControllerContext = CreateControllerContext("owner-intruder", "VehicleOwner", "Intruder"),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };

            // Act: Intruder tries to GET edit for car owned by "owner-real"
            var result = await controller.Edit(car.Id);

            // Assert: Must return Forbid
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task VehicleController_UnauthorizedOwnerCannotDeleteAnotherOwnersCar()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(VehicleController_UnauthorizedOwnerCannotDeleteAnotherOwnersCar));
            var vehicleRepo = new VehicleRepository(db);
            var vehicleService = new VehicleService(vehicleRepo);
            var httpClientFactory = Mock.Of<IHttpClientFactory>();

            var car = new Vehicle
            {
                Id = 60,
                Make = "Ferrari",
                Model = "F8",
                DailyRate = 20000m,
                OwnerId = "owner-real",
                IsAvailable = true
            };
            await db.Vehicles.AddAsync(car);
            await db.SaveChangesAsync();

            var controller = new VehicleController(vehicleService, httpClientFactory)
            {
                ControllerContext = CreateControllerContext("owner-intruder", "VehicleOwner", "Intruder"),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };

            // Act: Intruder tries to Delete car owned by "owner-real"
            var result = await controller.DeleteConfirmed(car.Id);

            // Assert: Must return Forbid
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task VehicleController_OwnerCanToggleTheirOwnVehicleAvailability()
        {
            // Arrange
            var db = GetInMemoryDbContext(nameof(VehicleController_OwnerCanToggleTheirOwnVehicleAvailability));
            var vehicleRepo = new VehicleRepository(db);
            var vehicleService = new VehicleService(vehicleRepo);
            var httpClientFactory = Mock.Of<IHttpClientFactory>();

            var ownerId = "owner-valid";
            var car = new Vehicle
            {
                Id = 70,
                Make = "Toyota",
                Model = "Camry",
                DailyRate = 3500m,
                OwnerId = ownerId,
                IsAvailable = true
            };
            await db.Vehicles.AddAsync(car);
            await db.SaveChangesAsync();

            var controller = new VehicleController(vehicleService, httpClientFactory)
            {
                ControllerContext = CreateControllerContext(ownerId, "VehicleOwner", "ValidOwner"),
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };

            // Act: Toggle from Available -> Unavailable
            var result = await controller.ToggleAvailability(car.Id);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyVehicles", redirect.ActionName);

            var updatedCar = await vehicleService.GetByIdAsync(car.Id);
            Assert.False(updatedCar!.IsAvailable);
        }
    }
}
