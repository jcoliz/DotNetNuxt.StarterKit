using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "Application Backend";
    options.Description = "Application boundary between .NET backend and ListsWebApp frontend.";
});

var app = builder.Build();

app.UseOpenApi();
app.UseSwaggerUi();

app.MapControllers();

app.Run();
