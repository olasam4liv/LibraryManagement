# Library Management System API

A minimal ASP.NET Core Web API for managing a library's books and users, featuring JWT authentication, in-memory caching, and FluentValidation.

## Features
- Register, login, and manage users with JWT authentication
- CRUD operations for books
- Search books by title, author, or ISBN
- In-memory caching for book queries
- FluentValidation for input validation
- Swagger UI for API documentation and testing

## Prerequisites
- [.NET 8 SDK or later](https://dotnet.microsoft.com/download)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)

## Getting Started

### 1. Clone the repository
```
git clone <repo-url>
cd LibraryManagementSystem
```

### 2. Configure the Database
- The default connection string in `appsettings.json` uses LocalDB:
  ```json
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\mssqllocaldb;Database=LibraryDb;Trusted_Connection=True;"
  }
  ```
- You can change this to your preferred SQL Server instance if needed.

### 3. Build and Run the API
```
dotnet build
dotnet run
```
- The API will be available at `https://localhost:5001` or `http://localhost:5000` by default.

### 4. Access Swagger UI
- Open your browser and go to: [https://localhost:5001](https://localhost:5001) or [http://localhost:5000](http://localhost:5000)
- Swagger UI provides interactive documentation and allows you to test all endpoints.

## Default User Credentials
- The database is seeded with a default user:
  - **Email:** `admin@library.com`
  - **Password:** `Admin@123`

## Sample Payloads

### Register
```json
{
  "fullName": "John Doe",
  "email": "john.doe@example.com",
  "password": "Password123!"
}
```

### Login
```json
{
  "email": "admin@library.com",
  "password": "Admin@123"
}
```

### Add Book
```json
{
  "title": "The Great Gatsby",
  "author": "F. Scott Fitzgerald",
  "isbn": "9780743273565",
  "publishedDate": "1925-04-10T00:00:00Z"
}
```

## Useful Endpoints
- `POST /api/auth/register` — Register a new user
- `POST /api/auth/login` — Login and receive JWT tokens
- `POST /api/auth/refresh` — Refresh JWT token
- `POST /api/auth/logout` — Logout user
- `GET /api/books/search` — Search for books (query, page, pageSize)
- `GET /api/books/{id}` — Get book by ID
- `GET /api/books/isbn/{isbn}` — Get book by ISBN
- `POST /api/books` — Add a new book
- `DELETE /api/books/{id}` — Delete a book by ID

## Notes
- All book endpoints require authentication. Use the JWT token from the login response in the `Authorization: Bearer <token>` header.
- The API uses in-memory caching for book search and fetch endpoints for improved performance.
- Validation errors and unauthorized access return clear JSON messages.

## Troubleshooting
- If you encounter database errors, ensure LocalDB is installed and running, or update the connection string.
- For any issues, check the logs in the `logs/` directory (not tracked by git).

---

Happy coding!
