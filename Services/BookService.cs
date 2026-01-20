using LibraryManagementSystem.Data;
using LibraryManagementSystem.Dto;
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
    private readonly ICacheService _cache;

    public BookService(AppDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
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
        string cacheKey = $"books:{searchParams}:{page}:{pageSize}";
        var cached = _cache.Get<PagedResult<Book>>(cacheKey);
        if (cached != null)
            return cached;

        IQueryable<Book> queryable = _context.Books.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchParams))
        {
            queryable = queryable.Where(b => b.Title.Contains(searchParams) || 
                                b.Author.Contains(searchParams) || 
                                b.ISBN.Contains(searchParams));
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
        _cache.Set(cacheKey, result);
        return result;
    }


    /// <summary>
    /// Creates a new book if ISBN does not exist.
    /// </summary>
    /// <param name="payload">Book to create</param>
    /// <returns>Created book or existing book if ISBN already exists</returns>
    public async Task<Book> CreateAsync(BookDto payload)
    {
        var existingBook = await _context.Books.FirstOrDefaultAsync(b => b.ISBN == payload.ISBN);
        if (existingBook != null)
        {
            // Book already exists, return it
            return existingBook;
        }
        var newBook = new Book
        {
            Title =  payload.Title,
            Author = payload.Author,
            ISBN = payload.ISBN,
            PublishedDate = payload.PublishedDate
        };
        _context.Books.Add(newBook);
        await _context.SaveChangesAsync();
        return newBook;
    }
        public async Task<Book?> GetByIdAsync(int id)
    {
        // Try cache first
        string cacheKey = $"book:id:{id}";
        var cached = _cache.Get<Book>(cacheKey);
        if (cached != null)
            return cached;
        var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (book != null)
            _cache.Set(cacheKey, book);
        return book;
    }

    public async Task<Book?> GetByIsbnAsync(string isbn)
    {
        // Try cache first
        string cacheKey = $"book:isbn:{isbn}";
        var cached = _cache.Get<Book>(cacheKey);
        if (cached != null)
            return cached;
        var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.ISBN == isbn);
        if (book != null)
            _cache.Set(cacheKey, book);
        return book;
    }

    /// <summary>
    /// Updates an existing book.
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <param name="payload">Updated book data</param>
    /// <returns>Updated book or null if not found</returns>
    public async Task<Book?> UpdateAsync(int id, Dto.BookDto payload)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
            return null;
        book.Title = payload.Title;
        book.Author = payload.Author;
        book.ISBN = payload.ISBN;
        book.PublishedDate = payload.PublishedDate;
        _context.Books.Update(book);
        await _context.SaveChangesAsync();
        // Update cache
        _cache.Set($"book:id:{id}", book);
        _cache.Set($"book:isbn:{book.ISBN}", book);
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
