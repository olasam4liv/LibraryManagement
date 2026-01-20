
using LibraryManagementSystem.Entities;
namespace LibraryManagementSystem.Data;
public static class DataSeeder
{
    public static void Seed(AppDbContext context)
    {
        // Seed default books (including Nigerian authors)
        if (!context.Books.Any())
        {
            var books = new List<Book>
            {
                
                new Book
                {
                    Title = "Things Fall Apart",
                    Author = "Chinua Achebe",
                    ISBN = "9780385474542",
                    PublishedDate = new DateTime(1958, 6, 17)
                },
                new Book
                {
                    Title = "Half of a Yellow Sun",
                    Author = "Chimamanda Ngozi Adichie",
                    ISBN = "9781400095209",
                    PublishedDate = new DateTime(2006, 9, 12)
                },
                new Book
                {
                    Title = "The Famished Road",
                    Author = "Ben Okri",
                    ISBN = "9780385425131",
                    PublishedDate = new DateTime(1991, 3, 5)
                },
                new Book
                {
                    Title = "Purple Hibiscus",
                    Author = "Chimamanda Ngozi Adichie",
                    ISBN = "9781616202415",
                    PublishedDate = new DateTime(2003, 10, 1)
                },
                new Book
                {
                    Title = "The Secret Lives of Baba Segi's Wives",
                    Author = "Lola Shoneyin",
                    ISBN = "9780061946370",
                    PublishedDate = new DateTime(2010, 5, 4)
                },
                new Book
                {
                    Title = "Stay with Me",
                    Author = "Ayobami Adebayo",
                    ISBN = "9780451494603",
                    PublishedDate = new DateTime(2017, 3, 2)
                }
            };
            context.Books.AddRange(books);
            context.SaveChanges();
        }

        // Seed default user
        if (!context.Users.Any())
        {
            var password = "Password123!";
            var defaultUser = new User
            {
                FullName = "Default Admin",
                Email = "admin@library.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RefreshToken = null,
                RefreshTokenExpiry = DateTime.MinValue
            };
            context.Users.Add(defaultUser);
            context.SaveChanges();
        }
    }
}
