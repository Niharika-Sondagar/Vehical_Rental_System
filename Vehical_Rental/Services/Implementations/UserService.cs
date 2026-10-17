using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vehical_Rental.Data;
using Vehical_Rental.Models;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // ==========================================
        // GET ALL USERS
        // ==========================================
        public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
        {
            var users = await _userManager.Users
                .Include(u => u.Bookings)
                .Include(u => u.Reviews)
                .Include(u => u.OwnedVehicles)
                .ToListAsync();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                user.Role = roles.FirstOrDefault() ?? "Customer";
            }

            return users;
        }

        // ==========================================
        // GET USER BY ID
        // ==========================================
        public async Task<ApplicationUser?> GetByIdAsync(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.Bookings)
                .Include(u => u.Reviews)
                .Include(u => u.OwnedVehicles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                user.Role = roles.FirstOrDefault() ?? "Customer";
            }

            return user;
        }

        // ==========================================
        // GET USER BY EMAIL
        // ==========================================
        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            var user = await _userManager.Users
                .Include(u => u.Bookings)
                .Include(u => u.Reviews)
                .Include(u => u.OwnedVehicles)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                user.Role = roles.FirstOrDefault() ?? "Customer";
            }

            return user;
        }

        // ==========================================
        // REGISTER USER (via UserManager)
        // ==========================================
        public async Task<(bool Success, string Message)> RegisterAsync(
            ApplicationUser user,
            string password,
            string role = "Customer")
        {
            // Ensure unique email
            var existingUser = await _userManager.FindByEmailAsync(user.Email ?? string.Empty);
            if (existingUser != null)
            {
                return (false, "Email is already registered.");
            }

            // Set username to email if not provided
            if (string.IsNullOrEmpty(user.UserName))
            {
                user.UserName = user.Email;
            }

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errorMessages = string.Join("; ", result.Errors.Select(e => e.Description));
                return (false, errorMessages);
            }

            // Ensure role exists in Identity Database
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(user, role);
            user.Role = role;

            return (true, "User registered successfully.");
        }

        // ==========================================
        // LOGIN VERIFICATION (via UserManager)
        // ==========================================
        public async Task<ApplicationUser?> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return null;
            }

            bool passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);
            user.Role = roles.FirstOrDefault() ?? "Customer";

            return user;
        }

        // ==========================================
        // UPDATE USER (via UserManager)
        // ==========================================
        public async Task<(bool Success, string Message)> UpdateAsync(
            ApplicationUser user,
            string? newRole = null)
        {
            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser == null)
            {
                return (false, "User not found.");
            }

            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.UserName = user.Email;
            existingUser.PhoneNumber = user.PhoneNumber;
            existingUser.Address = user.Address;

            var result = await _userManager.UpdateAsync(existingUser);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }

            // Update role if changed
            if (!string.IsNullOrEmpty(newRole))
            {
                var currentRoles = await _userManager.GetRolesAsync(existingUser);
                if (!currentRoles.Contains(newRole))
                {
                    await _userManager.RemoveFromRolesAsync(existingUser, currentRoles);
                    if (!await _roleManager.RoleExistsAsync(newRole))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(newRole));
                    }
                    await _userManager.AddToRoleAsync(existingUser, newRole);
                }
            }

            return (true, "User updated successfully.");
        }

        // ==========================================
        // DELETE USER (via UserManager & EF Core)
        // Cleanly removes user and associated data with safety protections
        // ==========================================
        public async Task<(bool Success, string Message)> DeleteAsync(string id, string? currentUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return (false, "Invalid user identifier specified.");
            }

            // 1. Prevent self-deletion by logged-in admin
            if (!string.IsNullOrEmpty(currentUserId) && string.Equals(currentUserId, id, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Security restriction: You cannot remove your own administrator account while logged in.");
            }

            // 2. Fetch the target user
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return (false, "User account not found.");
            }

            // 3. Prevent removal of the last administrator
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1)
                {
                    return (false, "Safety restriction: Cannot remove the only remaining administrator account from the system.");
                }
            }

            // 4. Perform safe cascade deletion using execution strategy for database resilience
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // A. Remove reviews written by this user
                    var userReviews = await _context.Reviews
                        .Where(r => r.UserId == id)
                        .ToListAsync();
                    if (userReviews.Count != 0)
                    {
                        _context.Reviews.RemoveRange(userReviews);
                    }

                    // B. Remove customer's bookings and associated payments
                    var userBookings = await _context.Bookings
                        .Where(b => b.UserId == id)
                        .ToListAsync();
                    if (userBookings.Count != 0)
                    {
                        var userBookingIds = userBookings.Select(b => b.Id).ToList();
                        var userPayments = await _context.Payments
                            .Where(p => userBookingIds.Contains(p.BookingId))
                            .ToListAsync();
                        if (userPayments.Count != 0)
                        {
                            _context.Payments.RemoveRange(userPayments);
                        }

                        // Restore availability for vehicles if they were booked
                        var bookedVehicleIds = userBookings
                            .Where(b => b.Status == "Confirmed" || b.Status == "Pending")
                            .Select(b => b.VehicleId)
                            .Distinct()
                            .ToList();

                        _context.Bookings.RemoveRange(userBookings);

                        foreach (var vId in bookedVehicleIds)
                        {
                            var vehicle = await _context.Vehicles.FindAsync(vId);
                            if (vehicle != null && !vehicle.IsAvailable)
                            {
                                var otherActive = await _context.Bookings
                                    .AnyAsync(other => other.VehicleId == vId && !userBookingIds.Contains(other.Id) && other.Status == "Confirmed");
                                if (!otherActive)
                                {
                                    vehicle.IsAvailable = true;
                                }
                            }
                        }
                    }

                    // C. If user is a VehicleOwner, clean up all owned vehicles, bookings, reviews, and payments
                    var ownedVehicles = await _context.Vehicles
                        .Where(v => v.OwnerId == id)
                        .ToListAsync();
                    if (ownedVehicles.Count != 0)
                    {
                        var vehicleIds = ownedVehicles.Select(v => v.Id).ToList();

                        // Bookings on these vehicles
                        var vehicleBookings = await _context.Bookings
                            .Where(b => vehicleIds.Contains(b.VehicleId))
                            .ToListAsync();
                        if (vehicleBookings.Count != 0)
                        {
                            var vBookingIds = vehicleBookings.Select(b => b.Id).ToList();
                            var vPayments = await _context.Payments
                                .Where(p => vBookingIds.Contains(p.BookingId))
                                .ToListAsync();
                            if (vPayments.Count != 0)
                            {
                                _context.Payments.RemoveRange(vPayments);
                            }
                            _context.Bookings.RemoveRange(vehicleBookings);
                        }

                        // Reviews on these vehicles
                        var vehicleReviews = await _context.Reviews
                            .Where(r => vehicleIds.Contains(r.VehicleId))
                            .ToListAsync();
                        if (vehicleReviews.Count != 0)
                        {
                            _context.Reviews.RemoveRange(vehicleReviews);
                        }

                        _context.Vehicles.RemoveRange(ownedVehicles);
                    }

                    // Save EF Core changes to clear all foreign key constraints
                    await _context.SaveChangesAsync();

                    // D. Delete Identity User (removes user, claims, roles, tokens)
                    var deleteResult = await _userManager.DeleteAsync(user);
                    if (!deleteResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = string.Join("; ", deleteResult.Errors.Select(e => e.Description));
                        return (false, $"Failed to remove user account: {errors}");
                    }

                    await transaction.CommitAsync();
                    return (true, $"User '{user.FullName}' ({user.Email}) was permanently removed from the system.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, $"An unexpected error occurred while removing user: {ex.Message}");
                }
            });
        }

        // ==========================================
        // GET USER ROLE
        // ==========================================
        public async Task<string> GetUserRoleAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault() ?? "Customer";
        }

        // ==========================================
        // CHECK IF USER IS LOCKED OUT (SYSTEM PROTECTION)
        // ==========================================
        public async Task<bool> IsLockedOutAsync(ApplicationUser user)
        {
            return await _userManager.IsLockedOutAsync(user);
        }

        // ==========================================
        // TOGGLE USER LOCKOUT (SYSTEM PROTECTION)
        // Admin can suspend or restore user accounts to protect the platform
        // ==========================================
        public async Task<(bool Success, string Message)> ToggleLockoutAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return (false, "User not found.");
            }

            var isLocked = await _userManager.IsLockedOutAsync(user);
            if (isLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
                return (true, $"Account for '{user.FullName}' has been unlocked and restored.");
            }
            else
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                await _userManager.UpdateSecurityStampAsync(user);
                return (true, $"Account for '{user.FullName}' has been suspended to protect the system.");
            }
        }
    }
}