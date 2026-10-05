using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Implementations;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Controllers
{
    public class VehicleController : Controller
    {
        private readonly IVehicleService _vehicleService;
        private readonly IHttpClientFactory _httpClientFactory;

        public VehicleController(IVehicleService vehicleService, IHttpClientFactory httpClientFactory)
        {
            _vehicleService = vehicleService;
            _httpClientFactory = httpClientFactory;
        }

        // ==========================================
        // GET: Vehicle (Public fleet browsing)
        // ==========================================
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var vehicles = await _vehicleService.GetAllAsync();
            return View(vehicles);
        }

        // ==========================================
        // GET: Vehicle/Nearby
        // Map-based vehicle exploration using OpenStreetMap
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Nearby()
        {
            return View();
        }

        // ==========================================
        // GET: Vehicle/NearbyVehicles
        // Searches vehicles near user coordinates with radius and category filters
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> NearbyVehicles(
            double latitude,
            double longitude,
            double radiusKm = 25,
            string? category = null)
        {
            IEnumerable<Vehicle> vehicles;

            if (radiusKm <= 0 || radiusKm >= 20000)
            {
                var all = await _vehicleService.GetAllAsync();
                vehicles = all.Where(v => v.IsAvailable);
            }
            else
            {
                vehicles = await _vehicleService.GetNearbyVehiclesAsync(latitude, longitude, radiusKm);
            }

            if (!string.IsNullOrEmpty(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                vehicles = vehicles.Where(v => v.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            // Project to clean anonymous DTO with calculated distance to avoid JSON cycle issues
            var result = vehicles.Select(v =>
            {
                double dist = VehicleRepository.CalculateDistance(latitude, longitude, v.Latitude, v.Longitude);
                return new
                {
                    id = v.Id,
                    make = v.Make,
                    model = v.Model,
                    year = v.Year,
                    fullName = v.FullName,
                    licensePlate = v.LicensePlate,
                    category = v.Category,
                    dailyRate = v.DailyRate,
                    transmission = v.Transmission,
                    fuelType = v.FuelType,
                    seats = v.Seats,
                    imageUrl = string.IsNullOrEmpty(v.ImageUrl)
                        ? "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?auto=format&fit=crop&w=600&q=80"
                        : v.ImageUrl,
                    description = v.Description,
                    isAvailable = v.IsAvailable,
                    latitude = v.Latitude,
                    longitude = v.Longitude,
                    distanceKm = Math.Round(dist, 2),
                    averageRating = v.AverageRating,
                    ownerName = v.Owner?.FullName ?? "Fleet Partner"
                };
            })
            .OrderBy(v => v.distanceKm)
            .ToList();

            return Json(result);
        }

        // ==========================================
        // GET: Vehicle/SearchLocation
        // Proxy for OpenStreetMap Nominatim Search API
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SearchLocation(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = "Search query is required." });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("User-Agent", "RentDrive-VehicleRental/1.0 (contact@rental.com)");
                var encoded = Uri.EscapeDataString(query.Trim());
                var url = $"https://nominatim.openstreetmap.org/search?format=json&q={encoded}&limit=5&addressdetails=1";
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { message = "OpenStreetMap search failed." });
                }

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error querying OpenStreetMap: " + ex.Message });
            }
        }

        // ==========================================
        // GET: Vehicle/ReverseGeocode
        // Proxy for OpenStreetMap Nominatim Reverse Geocoding API
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ReverseGeocode(double latitude, double longitude)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("User-Agent", "RentDrive-VehicleRental/1.0 (contact@rental.com)");
                var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&zoom=16&addressdetails=1";
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { message = "OpenStreetMap reverse geocode failed." });
                }

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reverse geocoding: " + ex.Message });
            }
        }

        // ==========================================
        // POST: Vehicle/SeedDemoVehiclesNear
        // Relocates / sets sample vehicles near user coordinates for testing
        // ==========================================
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SeedDemoVehiclesNear([FromBody] DemoLocationRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Coordinates required." });
            }

            var allVehicles = (await _vehicleService.GetAllAsync()).ToList();
            if (!allVehicles.Any())
            {
                return NotFound(new { message = "No vehicles found in database." });
            }

            var offsets = new[]
            {
                (0.005, 0.006),
                (-0.008, 0.007),
                (0.009, -0.008),
                (-0.006, -0.009),
                (0.013, 0.002),
                (-0.012, 0.004),
                (0.002, 0.014),
                (-0.004, -0.013)
            };

            for (int i = 0; i < allVehicles.Count; i++)
            {
                var offset = offsets[i % offsets.Length];
                allVehicles[i].Latitude = request.Latitude + offset.Item1;
                allVehicles[i].Longitude = request.Longitude + offset.Item2;
                allVehicles[i].IsAvailable = true;
                await _vehicleService.UpdateAsync(allVehicles[i]);
            }

            return Ok(new { message = $"{allVehicles.Count} demo vehicles stationed near your location successfully." });
        }

        // ==========================================
        // GET: Vehicle/Details/5 (Public)
        // ==========================================
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _vehicleService.GetByIdAsync(id.Value);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        // ==========================================
        // GET: Vehicle/MyVehicles (Vehicle Owner Only)
        // Dedicated dashboard for Vehicle Owners to manage their fleet
        // ==========================================
        [Authorize(Roles = "VehicleOwner")]
        public async Task<IActionResult> MyVehicles()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            var ownerVehicles = await _vehicleService.GetByOwnerIdAsync(currentUserId);
            return View(ownerVehicles);
        }

        // ==========================================
        // GET: Vehicle/Create (Vehicle Owner Only)
        // ==========================================
        [Authorize(Roles = "VehicleOwner")]
        public IActionResult Create()
        {
            return View();
        }

        // ==========================================
        // POST: Vehicle/Create (Vehicle Owner Only)
        // Any vehicle is only added by a registered vehicle owner
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "VehicleOwner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vehicle vehicle)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            // Assign vehicle ownership to the logged-in Vehicle Owner
            vehicle.OwnerId = currentUserId;

            if (ModelState.IsValid)
            {
                await _vehicleService.AddAsync(vehicle);
                TempData["Success"] = $"Vehicle '{vehicle.FullName}' was successfully added to your fleet.";
                return RedirectToAction(nameof(MyVehicles));
            }

            return View(vehicle);
        }

        // ==========================================
        // GET: Vehicle/Edit/5 (Vehicle Owner Only - Own Vehicles)
        // ==========================================
        [Authorize(Roles = "VehicleOwner")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _vehicleService.GetByIdAsync(id.Value);
            if (vehicle == null)
            {
                return NotFound();
            }

            // Security check: Only the vehicle owner can edit their own vehicle
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (vehicle.OwnerId != currentUserId)
            {
                TempData["Error"] = "Access Denied: You can only edit vehicles that belong to your owner account.";
                return Forbid();
            }

            return View(vehicle);
        }

        // ==========================================
        // POST: Vehicle/Edit/5 (Vehicle Owner Only - Own Vehicles)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "VehicleOwner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Vehicle vehicle)
        {
            if (id != vehicle.Id)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingVehicle = await _vehicleService.GetByIdAsync(id);
            if (existingVehicle == null)
            {
                return NotFound();
            }

            // Security check: Ensure vehicle belongs to logged-in Vehicle Owner
            if (existingVehicle.OwnerId != currentUserId)
            {
                TempData["Error"] = "Access Denied: You can only edit vehicles that belong to your owner account.";
                return Forbid();
            }

            // Preserve owner id
            vehicle.OwnerId = currentUserId;

            if (ModelState.IsValid)
            {
                await _vehicleService.UpdateAsync(vehicle);
                TempData["Success"] = $"Vehicle '{vehicle.FullName}' was updated successfully.";
                return RedirectToAction(nameof(MyVehicles));
            }

            return View(vehicle);
        }

        // ==========================================
        // GET: Vehicle/Delete/5 (Vehicle Owner Only - Own Vehicles)
        // ==========================================
        [Authorize(Roles = "VehicleOwner")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _vehicleService.GetByIdAsync(id.Value);
            if (vehicle == null)
            {
                return NotFound();
            }

            // Security check: Only the vehicle owner can delete their own vehicle
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (vehicle.OwnerId != currentUserId)
            {
                TempData["Error"] = "Access Denied: You can only delete vehicles that belong to your owner account.";
                return Forbid();
            }

            return View(vehicle);
        }

        // ==========================================
        // POST: Vehicle/Delete/5 (Vehicle Owner Only - Own Vehicles)
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "VehicleOwner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vehicle = await _vehicleService.GetByIdAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            // Security check: Only the vehicle owner can delete their own vehicle
            if (vehicle.OwnerId != currentUserId)
            {
                TempData["Error"] = "Access Denied: You can only delete vehicles that belong to your owner account.";
                return Forbid();
            }

            await _vehicleService.DeleteAsync(id);
            TempData["Success"] = $"Vehicle '{vehicle.FullName}' was removed from your fleet successfully.";
            return RedirectToAction(nameof(MyVehicles));
        }

        // ==========================================
        // POST: Vehicle/ToggleAvailability (Vehicle Owner Only)
        // Allows owner to quickly toggle vehicle availability
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "VehicleOwner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vehicle = await _vehicleService.GetByIdAsync(id);
            if (vehicle == null || vehicle.OwnerId != currentUserId)
            {
                return Forbid();
            }

            vehicle.IsAvailable = !vehicle.IsAvailable;
            await _vehicleService.UpdateAsync(vehicle);
            TempData["Success"] = $"Vehicle '{vehicle.FullName}' availability set to {(vehicle.IsAvailable ? "Available" : "Unavailable")}.";
            return RedirectToAction(nameof(MyVehicles));
        }
    }

    public class DemoLocationRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}