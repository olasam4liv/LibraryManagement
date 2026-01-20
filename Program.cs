using LibraryManagementSystem.Data;
using LibraryManagementSystem.Dto;
using LibraryManagementSystem.Endpoints;
using LibraryManagementSystem.Entities;
using LibraryManagementSystem.Interfaces;
using LibraryManagementSystem.Middleware;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text;
using Microsoft.OpenApi.Models;




var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File(new Serilog.Formatting.Compact.CompactJsonFormatter(), "logs/log.json", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IUserService, UserService>();

// Register CacheService
builder.Services.AddSingleton<ICacheService, CacheService>();

// Register FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
        o.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new { message = "You are not Authorized" });
                return context.Response.WriteAsync(result);
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
// Enable XML comments for Swagger
var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(xmlPath, true);
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});



var app = builder.Build();

// Log all requests and responses
app.UseMiddleware<LibraryManagementSystem.Middleware.RequestResponseLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DataSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI();
  app.UseSwaggerUI(options =>
  {    
      options.SwaggerEndpoint("/swagger/v1/swagger.json", "Library Management System API V1");
      options.DocExpansion(DocExpansion.None);
      options.EnableTryItOutByDefault();
      options.RoutePrefix = string.Empty;
  });
app.UseAuthentication();
app.UseAuthorization();

// AUTH ENDPOINTS
app.MapPost("/api/auth/register", async (RegisterDto payload, IUserService userService) =>
{
    var user = await userService.RegisterAsync(payload.FullName, payload.Email, payload.Password);
    if (user == null)
        return Results.BadRequest(new { Message = "Email already in use" });
    return Results.Ok(new { message = "User registered successfully", Id = user.Id });
})
.WithTags(Tags.Auth)
.WithOpenApi(op =>
{
    op.Summary = "Register a new user";
    op.Description = "Creates a new user account with the provided details.";
    return op;
});

app.MapPost("/api/auth/login", async (
    LoginDto payload, 
    IUserService userService, 
    TokenService tokenSvc, 
    IConfiguration cfg, 
    AppDbContext _context) =>
{
    var user = await userService.LoginAsync(payload.Email, payload.Password);
    if (user == null)
        return Results.BadRequest(new { Message = "Invalid credentials" });

    user.RefreshToken = tokenSvc.CreateRefreshToken();
    user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(int.Parse(cfg["Jwt:RefreshTokenDays"]!));
    await _context.SaveChangesAsync();

    return Results.Ok(new
    {
        accessToken = tokenSvc.CreateAccessToken(user),
        refreshToken = user.RefreshToken
    });
})
.WithTags(Tags.Auth)
.WithOpenApi(op =>
{
    op.Summary = "Login user";
    op.Description = "Authenticates a user and returns JWT access and refresh tokens.";
    return op;
});

app.MapPost("/api/auth/refresh", async (
    string refreshToken, 
    IUserService userService, 
    TokenService tokenSvc, 
    AppDbContext _context) =>
{
    var user = await userService.GetByRefreshTokenAsync(refreshToken);
    if (user == null)
        return Results.Unauthorized();

    user.RefreshToken = tokenSvc.CreateRefreshToken();
    await _context.SaveChangesAsync();

    return Results.Ok(new
    {
        accessToken = tokenSvc.CreateAccessToken(user),
        refreshToken = user.RefreshToken
    });
})
.WithTags(Tags.Auth)
.WithOpenApi(op =>
{
    op.Summary = "Refresh JWT token";
    op.Description = "Refreshes the JWT access token using a valid refresh token.";
    return op;
});

app.MapPost("/api/auth/logout", async (string refreshToken, IUserService userService) =>
{
    await userService.LogoutAsync(refreshToken);
    return Results.Ok(new { message = "Logged out successfully" });
})
.WithTags(Tags.Auth)
.WithOpenApi(op =>
{
    op.Summary = "Logout user";
    op.Description = "Logs out the user by invalidating the refresh token.";
    return op;
});

// BOOKS 
// Search books endpoint
app.MapGet("/api/books/search", async (IBookService service, string? searchParams,  int page = 1, int pageSize = 10) =>
{
    var result = await service.SearchAsync(searchParams, page, pageSize);
    if (result == null || !result.Items.Any())
    {
        return Results.NotFound(new { message = "No books found matching the query." });
    }
    return Results.Ok(result);
})
.RequireAuthorization()
.WithTags(Tags.Books)
.WithOpenApi(op =>
{
    op.Summary = "Search books";
    op.Description = "Searches for books by title, author, or ISBN. Supports pagination.";
    return op;
});


app.MapPost("/api/books", async (BookDto payload , IBookService service) =>
{    
    var created = await service.CreateAsync(payload);
    return Results.Created($"/api/books/{created.Id}", created);
})
.RequireAuthorization()
.WithTags(Tags.Books)
.WithOpenApi(op =>
{
    op.Summary = "Add a new book";
    op.Description = "Adds a new book to the library. If the ISBN already exists, returns the existing book.";
    return op;
});

// Get book by ID
app.MapGet("/api/books/{id:int}", async (int id, IBookService service) =>
{
    var book = await service.GetByIdAsync(id);
    if (book == null)
        return Results.NotFound(new { message = "Book not found" });
    return Results.Ok(book);
})
.RequireAuthorization()
.WithTags(Tags.Books)
.WithOpenApi(op =>
{
    op.Summary = "Get book by ID";
    op.Description = "Retrieves a book by its unique integer ID.";
    return op;
});

// Get book by ISBN
app.MapGet("/api/books/isbn/{isbn}", async (string isbn, IBookService service) =>
{
    var book = await service.GetByIsbnAsync(isbn);
    if (book == null)
        return Results.NotFound(new { message = "Book not found" });
    return Results.Ok(book);
})
.RequireAuthorization()
.WithTags(Tags.Books)
.WithOpenApi(op =>
{
    op.Summary = "Get book by ISBN";
    op.Description = "Retrieves a book by its ISBN number.";
    return op;
});
// Update book by ID
app.MapPut("/api/books/{id:int}", async (
    int id,
    BookDto payload ,
    IBookService service ) =>
{   
    var updated = await service.UpdateAsync(id, payload);
    if (updated == null)
        return Results.NotFound(new { message = "Book not found" });
    return Results.Ok(updated);
})
.RequireAuthorization()
.WithTags(Tags.Books)
.WithOpenApi(op =>
{
    op.Summary = "Update a book";
    op.Description = "Updates a book's details by its ID.";
    return op;
});
// delete book by ID
app.MapDelete("/api/books/{id:int}", async (int id, IBookService service) =>
{
    var deleted = await service.DeleteAsync(id);
    return deleted ?
        Results.Ok(new { message = "Book deleted successfully" }) :
        Results.NotFound(new { message = "Book not found" });
})
.RequireAuthorization()
.WithTags(Tags.Books)
.WithOpenApi(op =>
{
    op.Summary = "Delete a book";
    op.Description = "Deletes a book from the library by its ID.";
    return op;
});

app.Run();
