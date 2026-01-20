
namespace LibraryManagementSystem.Entities;
public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }
}
