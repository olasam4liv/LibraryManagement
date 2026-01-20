
using LibraryManagementSystem.Entities;
namespace LibraryManagementSystem.Data;
public static class DataSeeder
{
    public static void Seed(AppDbContext context)
    {
        // Seed default books (including Nigerian authors)
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
            var newBooks = books.Where(b => !context.Books.Any(db => db.ISBN == b.ISBN)).ToList();
            if (newBooks.Any())
            {
                context.Books.AddRange(newBooks);
                context.SaveChanges();
            }

        // Seed default user
        var defaultUsers = new List<User>
        {
            new User
            {
                FullName = "Default Admin",
                Email = "admin@library.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                RefreshToken = null,
                RefreshTokenExpiry = DateTime.MinValue
            },
            new User
            {
                FullName = "Samuel Olatunji",
                Email = "olasam4liv@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Samsam1234!"),
                RefreshToken = null,
                RefreshTokenExpiry = DateTime.MinValue
            }
        };
        var newUsers = defaultUsers.Where(u => !context.Users.Any(db => db.Email == u.Email)).ToList();
        if (newUsers.Any())
        {
            context.Users.AddRange(newUsers);
            context.SaveChanges();
        }
    }
}
