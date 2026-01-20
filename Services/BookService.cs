
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Entities;
using LibraryManagementSystem.Interfaces;
using LibraryManagementSystem.Responses;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public class BookService : IBookService
{
  
    private readonly AppDbContext _context;

    public BookService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Book>> SearchAsync(string query, int page = 1, int pageSize = 10)
    {       
        var books = new List<Book>();

        if (!string.IsNullOrWhiteSpace(query))
        {
            books = await _context.Books
                            .AsNoTracking()
                            .Where(b => b.Title.Contains(query) || b.Author.Contains(query) || b.ISBN.Contains(query))
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();
        }

        books = await _context.Books
                        .AsNoTracking()
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

        var result = new PagedResult<Book>
        {
            Items = books,
            TotalCount = await _context.Books.CountAsync(),
            PageNumber = page,
            PageSize = pageSize
        };


        return result;
    }


    public async Task<Book> CreateAsync(Book book)
    {
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        return book;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null) 
            return false;

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        return true;
    }
}
