using System.Threading.Tasks;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Entities;
using LibraryManagementSystem.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> RegisterAsync(string fullName, string email, string password)
        {
            if (await _context.Users.AnyAsync(x => x.Email == email))
                return null;
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            var user = await _context.Users.SingleOrDefaultAsync(x => x.Email == email);
            if (user == null)
                return null;
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;
            return user;
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users.SingleOrDefaultAsync(x => x.RefreshToken == refreshToken && x.RefreshTokenExpiry > DateTime.UtcNow);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var user = await _context.Users.SingleOrDefaultAsync(x => x.RefreshToken == refreshToken);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = DateTime.MinValue;
                await _context.SaveChangesAsync();
            }
        }
    }
}
