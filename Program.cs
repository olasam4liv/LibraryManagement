
using System.Text;
using BCrypt.Net;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Dto;
using LibraryManagementSystem.Endpoints;
using LibraryManagementSystem.Entities;
using LibraryManagementSystem.Interfaces;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerUI;
using Serilog;



Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IUserService, UserService>();

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
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Swagger to use JWT Bearer Authentication
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


var app = builder.Build();

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
}).WithTags(Tags.Auth);

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
}).WithTags(Tags.Auth);

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
.WithTags(Tags.Auth);

app.MapPost("/api/auth/logout", async (string refreshToken, IUserService userService) =>
{
    await userService.LogoutAsync(refreshToken);
    return Results.Ok(new { message = "Logged out successfully" });
}).WithTags(Tags.Auth);

// BOOKS 
// Search books endpoint
app.MapGet("/api/books/search", async (IBookService service,string query,  int page = 1, int pageSize = 10) =>
{
    var result = await service.SearchAsync(query, page, pageSize);
    if (result == null || !result.Items.Any())
    {
        return Results.NotFound(new { message = "No books found matching the query." });
    }
    return Results.Ok(result);
})
    .RequireAuthorization()
    .WithTags(Tags.Books);


app.MapPost("/api/books", async (BookDto payload , IBookService service) =>
{
    var newBook = new Book
    {
        Title =  payload.Title,
        Author = payload.Author,
        ISBN = payload.ISBN,
        PublishedDate = payload.PublishedDate
    };
    var created = await service.CreateAsync(newBook);
    return Results.Created($"/api/books/{created.Id}", created);
})
.RequireAuthorization()
.WithTags(Tags.Books);

// Get book by ID
app.MapDelete("/api/books/{id:int}", async (int id, IBookService service) =>
{
    var deleted = await service.DeleteAsync(id);
    return deleted ? 
        Results.NotFound(new { message = "Book not found" }) : 
        Results.Ok(new { message = "Book deleted successfully" });
})
.RequireAuthorization()
.WithTags(Tags.Books);

app.Run();
