using Microsoft.EntityFrameworkCore;
using Vehical_Rental.Data;
using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Interfaces;

namespace Vehical_Rental.Repositories.Implementations
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ApplicationDbContext _context;

        public BookingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Vehicle)
                    .ThenInclude(v => v!.Owner)
                .Include(b => b.Payment)
                .ToListAsync();
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Vehicle)
                    .ThenInclude(v => v!.Owner)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Booking>> GetByOwnerIdAsync(string ownerId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .Where(b => b.Vehicle != null && b.Vehicle.OwnerId == ownerId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetByCustomerIdAsync(string customerId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Vehicle)
                    .ThenInclude(v => v!.Owner)
                .Include(b => b.Payment)
                .Where(b => b.UserId == customerId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task AddAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsVehicleAvailableAsync(
            int vehicleId,
            DateTime startDate,
            DateTime endDate,
            int? excludeBookingId = null)
        {
            var start = startDate.Date;
            var end = endDate.Date;

            var hasConflict = await _context.Bookings
                .AnyAsync(b =>
                    b.VehicleId == vehicleId &&
                    (excludeBookingId == null || b.Id != excludeBookingId) &&
                    (b.Status == "Pending" || b.Status == "Accepted" || b.Status == "Confirmed") &&
                    b.StartDate.Date < end &&
                    b.EndDate.Date > start);

            return !hasConflict;
        }

        public async Task<bool> HasCustomerActiveBookingOverlapAsync(
            string customerId,
            DateTime startDate,
            DateTime endDate,
            int? excludeBookingId = null)
        {
            if (string.IsNullOrEmpty(customerId)) return false;

            var start = startDate.Date;
            var end = endDate.Date;

            return await _context.Bookings
                .AnyAsync(b =>
                    b.UserId == customerId &&
                    (excludeBookingId == null || b.Id != excludeBookingId) &&
                    (b.Status == "Pending" || b.Status == "Accepted" || b.Status == "Confirmed") &&
                    b.StartDate.Date < end &&
                    b.EndDate.Date > start);
        }

        public async Task UpdateAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
        }
    }
}