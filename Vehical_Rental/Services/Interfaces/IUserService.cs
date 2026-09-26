using Vehical_Rental.Models;

namespace Vehical_Rental.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();

        Task<User?> GetByIdAsync(int id);

        Task<User?> GetByEmailAsync(string email);

        Task<(bool Success, string Message)> RegisterAsync(User user);

        Task<User?> LoginAsync(string email, string password);

        Task UpdateAsync(User user);

        Task DeleteAsync(int id);
    }
}