using auth.Repositories;
using Data;
using Microsoft.EntityFrameworkCore;
using Queries;
using Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AutheDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

// Register the query service
builder.Services.AddScoped<GetUserPasswordQuery>();


// Add authentication and authorization services
builder.Services.AddAuthentication(); // Add authentication if needed
builder.Services.AddAuthorization();  // This fixes the error

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
app.UseAuthentication();  // Make sure authentication is added before authorization
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Endpoint to get user password (with better error handling)
app.MapGet("/user/password/{userId}", async (long userId, GetUserPasswordQuery query) =>
{
    if (userId <= 0)
        return Results.BadRequest("Invalid user ID.");

    try
    {
        var password = await query.ExecuteAsync(userId);
        return password is not null ? Results.Ok(new { Password = password }) : Results.NotFound("User not found");
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
})
.WithName("GetUserPassword")
.WithOpenApi();

app.Run();

// Record type for WeatherForecast
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
