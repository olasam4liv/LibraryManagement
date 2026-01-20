
using LibraryManagementSystem.Dto;
using LibraryManagementSystem.Entities;
using LibraryManagementSystem.Responses;

namespace LibraryManagementSystem.Interfaces;

public interface IBookService
{
    Task<PagedResult<Book>> SearchAsync(string? searchParams, int page = 1, int pageSize = 10);
    Task<Book> CreateAsync(BookDto book);
    Task<bool> DeleteAsync(int id);
    Task<Book?> GetByIdAsync(int id);
    Task<Book?> GetByIsbnAsync(string isbn);
    Task<Book?> UpdateAsync(int id, BookDto payload);
}
