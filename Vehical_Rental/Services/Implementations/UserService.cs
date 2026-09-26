using BCrypt.Net;
using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Interfaces;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }


        // ==========================================
        // GET ALL USERS
        // ==========================================

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _userRepository.GetAllAsync();
        }


        // ==========================================
        // GET USER BY ID
        // ==========================================

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }


        // ==========================================
        // GET USER BY EMAIL
        // ==========================================

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }


        // ==========================================
        // REGISTER USER
        // ==========================================

        public async Task<(bool Success, string Message)> RegisterAsync(User user)
        {
            var existingUser = await _userRepository.GetByEmailAsync(user.Email);

            if (existingUser != null)
            {
                return (false, "Email is already registered.");
            }

            // Every user registering normally is a Customer
            user.Role = "Customer";

            // Hash the password before storing it
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                user.PasswordHash
            );

            await _userRepository.AddAsync(user);

            return (true, "User registered successfully.");
        }
        // ==========================================
        // LOGIN USER
        // ==========================================

        public async Task<User?> LoginAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                return null;
            }

            bool passwordValid = BCrypt.Net.BCrypt.Verify(
                password,
                user.PasswordHash
            );

            if (!passwordValid)
            {
                return null;
            }

            return user;
        }


        // ==========================================
        // UPDATE USER
        // ==========================================

        public async Task UpdateAsync(User user)
        {
            await _userRepository.UpdateAsync(user);
        }


        // ==========================================
        // DELETE USER
        // ==========================================

        public async Task DeleteAsync(int id)
        {
            await _userRepository.DeleteAsync(id);
        }
    }
}