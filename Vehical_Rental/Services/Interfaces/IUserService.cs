using Vehical_Rental.Models;

namespace Vehical_Rental.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<ApplicationUser>> GetAllAsync();

        Task<ApplicationUser?> GetByIdAsync(string id);

        Task<ApplicationUser?> GetByEmailAsync(string email);

        Task<(bool Success, string Message)> RegisterAsync(ApplicationUser user, string password, string role = "Customer");

        Task<ApplicationUser?> LoginAsync(string email, string password);

        Task<(bool Success, string Message)> UpdateAsync(ApplicationUser user, string? newRole = null);

        Task<(bool Success, string Message)> DeleteAsync(string id, string? currentUserId = null);

        Task<string> GetUserRoleAsync(ApplicationUser user);

        Task<bool> IsLockedOutAsync(ApplicationUser user);

        Task<(bool Success, string Message)> ToggleLockoutAsync(string id);
    }
}