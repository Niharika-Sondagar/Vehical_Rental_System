using Microsoft.EntityFrameworkCore;
using Vehical_Rental.Data;
using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Interfaces;

namespace Vehical_Rental.Repositories.Implementations
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public ReviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET ALL REVIEWS
        // ==========================================

        public async Task<IEnumerable<Review>> GetAllAsync()
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                .ToListAsync();
        }


        // ==========================================
        // GET REVIEW BY ID
        // ==========================================

        public async Task<Review?> GetByIdAsync(int id)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == id);
        }


        // ==========================================
        // GET REVIEWS BY VEHICLE
        // ==========================================

        public async Task<IEnumerable<Review>> GetByVehicleIdAsync(int vehicleId)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                .Where(r => r.VehicleId == vehicleId)
                .ToListAsync();
        }


        // ==========================================
        // ADD REVIEW
        // ==========================================

        public async Task AddAsync(Review review)
        {
            await _context.Reviews.AddAsync(review);

            await _context.SaveChangesAsync();
        }


        // ==========================================
        // UPDATE REVIEW
        // ==========================================

        public async Task UpdateAsync(Review review)
        {
            _context.Reviews.Update(review);

            await _context.SaveChangesAsync();
        }


        // ==========================================
        // DELETE REVIEW
        // ==========================================

        public async Task DeleteAsync(int id)
        {
            var review = await _context.Reviews.FindAsync(id);

            if (review != null)
            {
                _context.Reviews.Remove(review);

                await _context.SaveChangesAsync();
            }
        }
    }
}