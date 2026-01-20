
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Entities;
using LibraryManagementSystem.Interfaces;
using LibraryManagementSystem.Responses;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

/// <summary>
/// Service for managing books in the library.
/// </summary>
public class BookService : IBookService
{
  
    private readonly AppDbContext _context;

    public BookService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Searches for books by title, author, or ISBN.
    /// </summary>
    /// <param name="searchParams">Search term</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paged result of books</returns>
    public async Task<PagedResult<Book>> SearchAsync(string? searchParams, int page = 1, int pageSize = 10)
    {
        IQueryable<Book> queryable = _context.Books.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchParams))
        {
            queryable = queryable.Where(b => b.Title.Contains(searchParams) || b.Author.Contains(searchParams) || b.ISBN.Contains(searchParams));
        }
        var totalCount = await queryable.CountAsync();
        var books = await queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<Book>
        {
            Items = books,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
        return result;
    }


    /// <summary>
    /// Creates a new book if ISBN does not exist.
    /// </summary>
    /// <param name="book">Book to create</param>
    /// <returns>Created book or existing book if ISBN already exists</returns>
    public async Task<Book> CreateAsync(Book book)
    {
        var existingBook = await _context.Books.FirstOrDefaultAsync(b => b.ISBN == book.ISBN);
        if (existingBook != null)
        {
            // Book already exists, return it
            return existingBook;
        }
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        return book;
    }

    /// <summary>
    /// Deletes a book by ID.
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <returns>True if deleted, false if not found</returns>
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
