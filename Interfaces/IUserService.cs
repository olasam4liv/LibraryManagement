
using LibraryManagementSystem.Entities;

namespace LibraryManagementSystem.Interfaces
{
    public interface IUserService
    {
        Task<User?> RegisterAsync(string fullName, string email, string password);
        Task<User?> LoginAsync(string email, string password);
        Task<User?> GetByRefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
    }
}
