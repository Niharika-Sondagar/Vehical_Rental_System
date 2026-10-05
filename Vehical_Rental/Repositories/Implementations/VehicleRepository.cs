using Microsoft.EntityFrameworkCore;
using Vehical_Rental.Data;
using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Interfaces;

namespace Vehical_Rental.Repositories.Implementations
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly ApplicationDbContext _context;

        public VehicleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET ALL VEHICLES
        // ==========================================
        public async Task<IEnumerable<Vehicle>> GetAllAsync()
        {
            return await _context.Vehicles
                .Include(v => v.Owner)
                .Include(v => v.Reviews)
                .ToListAsync();
        }

        // ==========================================
        // GET NEARBY VEHICLES
        // ==========================================
        public async Task<IEnumerable<Vehicle>> GetNearbyVehiclesAsync(
            double latitude,
            double longitude,
            double radiusKm)
        {
            // Get only available vehicles from database
            var vehicles = await _context.Vehicles
                .Where(v => v.IsAvailable)
                .Include(v => v.Owner)
                .Include(v => v.Reviews)
                .ToListAsync();

            // Calculate distance and return vehicles
            // within the requested radius
            return vehicles.Where(v =>
                CalculateDistance(
                    latitude,
                    longitude,
                    v.Latitude,
                    v.Longitude
                ) <= radiusKm
            );
        }

        // ==========================================
        // CALCULATE DISTANCE BETWEEN TWO LOCATIONS
        // Uses Haversine Formula
        // ==========================================
        public static double CalculateDistance(
            double lat1,
            double lon1,
            double lat2,
            double lon2)
        {
            const double earthRadiusKm = 6371.0;

            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);

            double a =
                Math.Sin(dLat / 2) *
                Math.Sin(dLat / 2) +

                Math.Cos(DegreesToRadians(lat1)) *
                Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a)
            );

            return earthRadiusKm * c;
        }

        // ==========================================
        // CONVERT DEGREES TO RADIANS
        // ==========================================
        public static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        // ==========================================
        // GET VEHICLE BY ID
        // ==========================================
        public async Task<Vehicle?> GetByIdAsync(int id)
        {
            return await _context.Vehicles
                .Include(v => v.Owner)
                .Include(v => v.Reviews)
                    .ThenInclude(r => r.User)
                .Include(v => v.Bookings)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        // ==========================================
        // GET VEHICLES BY OWNER
        // ==========================================
        public async Task<IEnumerable<Vehicle>> GetByOwnerIdAsync(string ownerId)
        {
            return await _context.Vehicles
                .Include(v => v.Owner)
                .Include(v => v.Reviews)
                .Include(v => v.Bookings)
                .Where(v => v.OwnerId == ownerId)
                .ToListAsync();
        }

        // ==========================================
        // ADD VEHICLE
        // ==========================================
        public async Task AddAsync(Vehicle vehicle)
        {
            await _context.Vehicles.AddAsync(vehicle);
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // UPDATE VEHICLE
        // ==========================================
        public async Task UpdateAsync(Vehicle vehicle)
        {
            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // DELETE VEHICLE
        // ==========================================
        public async Task DeleteAsync(int id)
        {
            // With SqlServerRetryingExecutionStrategy enabled,
            // use CreateExecutionStrategy
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    var vehicle = await _context.Vehicles
                        .FirstOrDefaultAsync(v => v.Id == id);

                    if (vehicle == null)
                    {
                        return;
                    }

                    // 1. Find all bookings associated with this vehicle
                    var bookings = await _context.Bookings
                        .Where(b => b.VehicleId == id)
                        .ToListAsync();

                    if (bookings.Any())
                    {
                        var bookingIds = bookings
                            .Select(b => b.Id)
                            .ToList();

                        // Delete payments related to these bookings first
                        var payments = await _context.Payments
                            .Where(p => bookingIds.Contains(p.BookingId))
                            .ToListAsync();

                        if (payments.Any())
                        {
                            _context.Payments.RemoveRange(payments);
                            await _context.SaveChangesAsync();
                        }

                        // Delete bookings
                        _context.Bookings.RemoveRange(bookings);
                        await _context.SaveChangesAsync();
                    }

                    // 2. Find and delete reviews
                    var reviews = await _context.Reviews
                        .Where(r => r.VehicleId == id)
                        .ToListAsync();

                    if (reviews.Any())
                    {
                        _context.Reviews.RemoveRange(reviews);
                        await _context.SaveChangesAsync();
                    }

                    // 3. Delete vehicle
                    _context.Vehicles.Remove(vehicle);
                    await _context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}