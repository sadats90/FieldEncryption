using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Auth01.Data;
using Auth01.Models;

namespace Auth01.Services
{
    public interface IAuthService
    {
        Task<User?> ValidateUserAsync(string email, string password);
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> CreateUserAsync(User user, string password);
        Task<bool> CreateAdminUserAsync(User user, string password);
        bool IsAdmin(User user);
        bool IsUser(User user);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            Console.WriteLine($"=== LOGIN ATTEMPT ===");
            Console.WriteLine($"Email: {email}");
            Console.WriteLine($"Password: {password}");
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null)
            {
                Console.WriteLine("❌ User not found or not active");
                return null;
            }

            Console.WriteLine($"✅ User found: {user.Email}");
            Console.WriteLine($"Stored password: {user.Password}");
            
            // Simple plain text comparison
            if (user.Password == password)
            {
                Console.WriteLine("✅ Password match - Login successful!");
                return user;
            }
            
            Console.WriteLine("❌ Password mismatch - Login failed!");
            return null;
        }

        public async Task<User?> GetUserByIdAsync(int id)
            => await _context.Users.FindAsync(id);

        public async Task<User?> GetUserByEmailAsync(string email)
            => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<bool> CreateUserAsync(User user, string password)
        {
            try
            {
                Console.WriteLine($"=== REGISTRATION ATTEMPT ===");
                Console.WriteLine($"Email: {user.Email}");
                Console.WriteLine($"Password: {password}");
                
                // Store password as plain text (no hashing)
                user.Password = password;
                
                if (string.IsNullOrWhiteSpace(user.Role))
                    user.Role = "User"; // default role

                Console.WriteLine($"Role: {user.Role}");
                Console.WriteLine($"Stored password: {user.Password}");

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                Console.WriteLine("✅ User saved to database successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving user: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateAdminUserAsync(User user, string password)
        {
            user.Role = "Admin"; // force admin role
            return await CreateUserAsync(user, password);
        }

        public bool IsAdmin(User user) => user.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
        public bool IsUser(User user) => user.Role?.Equals("User", StringComparison.OrdinalIgnoreCase) == true;
    }
}
