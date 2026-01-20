
using LibraryManagementSystem.Entities;
using LibraryManagementSystem.Responses;

namespace LibraryManagementSystem.Interfaces;

public interface IBookService
{
    Task<PagedResult<Book>> SearchAsync(string query, int page = 1, int pageSize = 10);
    Task<Book> CreateAsync(Book book);
    Task<bool> DeleteAsync(int id);
}
