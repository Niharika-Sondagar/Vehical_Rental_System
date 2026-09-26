using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Interfaces;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;

        public ReviewService(IReviewRepository reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        // ==========================================
        // GET ALL REVIEWS
        // ==========================================

        public async Task<IEnumerable<Review>> GetAllAsync()
        {
            return await _reviewRepository.GetAllAsync();
        }


        // ==========================================
        // GET REVIEW BY ID
        // ==========================================

        public async Task<Review?> GetByIdAsync(int id)
        {
            return await _reviewRepository.GetByIdAsync(id);
        }


        // ==========================================
        // GET REVIEWS BY VEHICLE
        // ==========================================

        public async Task<IEnumerable<Review>> GetByVehicleIdAsync(int vehicleId)
        {
            return await _reviewRepository.GetByVehicleIdAsync(vehicleId);
        }


        // ==========================================
        // ADD REVIEW
        // ==========================================

        public async Task<(bool Success, string Message)> AddAsync(Review review)
        {
            // Check rating
            if (review.Rating < 1 || review.Rating > 5)
            {
                return (false, "Rating must be between 1 and 5.");
            }

            // Add review
            await _reviewRepository.AddAsync(review);

            return (true, "Review added successfully.");
        }


        // ==========================================
        // UPDATE REVIEW
        // ==========================================

        public async Task UpdateAsync(Review review)
        {
            await _reviewRepository.UpdateAsync(review);
        }


        // ==========================================
        // DELETE REVIEW
        // ==========================================

        public async Task DeleteAsync(int id)
        {
            await _reviewRepository.DeleteAsync(id);
        }
    }
}