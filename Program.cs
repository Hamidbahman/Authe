using auth.Interfaces;
using auth.Repositories;
using Auth;
using Data;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Queries;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers(); // ✅ Add support for Controllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AutheDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddScoped<IApplicationRepository,ApplicationRepository>();
builder.Services.AddScoped<IUserRepository,UserRepository>();
builder.Services.AddScoped<OAuthService>();



// Register the query service
// Add authentication and authorization services
builder.Services.AddAuthentication(); 
builder.Services.AddAuthorization();  

var app = builder.Build();

// Enforce HTTPS before anything else
app.UseHttpsRedirection();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ensure security middleware is before endpoints
app.UseAuthentication();
app.UseAuthorization();

// ✅ Map Controllers (Important for Swagger)
app.MapControllers(); 

app.Run();
