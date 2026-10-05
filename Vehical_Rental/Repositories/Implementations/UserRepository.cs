using Microsoft.EntityFrameworkCore;
using Vehical_Rental.Data;
using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Interfaces;

namespace Vehical_Rental.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Bookings)
                .Include(u => u.Reviews)
                .ToListAsync();
        }

        public async Task<ApplicationUser?> GetByIdAsync(string id)
        {
            return await _context.Users
                .Include(u => u.Bookings)
                .Include(u => u.Reviews)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Bookings)
                .Include(u => u.Reviews)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AddAsync(ApplicationUser user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ApplicationUser user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user != null)
            {
                var reviews = await _context.Reviews.Where(r => r.UserId == id).ToListAsync();
                if (reviews.Count != 0) _context.Reviews.RemoveRange(reviews);

                var bookings = await _context.Bookings.Where(b => b.UserId == id).ToListAsync();
                if (bookings.Count != 0)
                {
                    var bIds = bookings.Select(b => b.Id).ToList();
                    var payments = await _context.Payments.Where(p => bIds.Contains(p.BookingId)).ToListAsync();
                    if (payments.Count != 0) _context.Payments.RemoveRange(payments);
                    _context.Bookings.RemoveRange(bookings);
                }

                var vehicles = await _context.Vehicles.Where(v => v.OwnerId == id).ToListAsync();
                if (vehicles.Count != 0)
                {
                    var vIds = vehicles.Select(v => v.Id).ToList();
                    var vBookings = await _context.Bookings.Where(b => vIds.Contains(b.VehicleId)).ToListAsync();
                    if (vBookings.Count != 0)
                    {
                        var vbIds = vBookings.Select(b => b.Id).ToList();
                        var vPayments = await _context.Payments.Where(p => vbIds.Contains(p.BookingId)).ToListAsync();
                        if (vPayments.Count != 0) _context.Payments.RemoveRange(vPayments);
                        _context.Bookings.RemoveRange(vBookings);
                    }
                    var vReviews = await _context.Reviews.Where(r => vIds.Contains(r.VehicleId)).ToListAsync();
                    if (vReviews.Count != 0) _context.Reviews.RemoveRange(vReviews);
                    _context.Vehicles.RemoveRange(vehicles);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}