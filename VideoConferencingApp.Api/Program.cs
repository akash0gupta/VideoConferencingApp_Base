using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using VideoConferencingApp.Api.Middleware;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.Settings;
using VideoConferencingApp.Infrastructure.Extensions;
using VideoConferencingApp.Infrastructure.Messaging.RabbitMq;
using VideoConferencingApp.Infrastructure.Persistence.Migrations;
using VideoConferencingApp.Infrastructure.RealTime;

// --- 1. Configure Serilog ---
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Write logs to the console
    .CreateBootstrapLogger();

Log.Information("Starting up the application...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- Replace the default logger with Serilog ---
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration) // Read settings from appsettings.json
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()); // You can configure sinks here or in appsettings

    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

    builder.Services.AddControllers();
    builder.Services.AddApiVersioning(o =>
    {
        o.DefaultApiVersion = new ApiVersion(1, 0);
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.ReportApiVersions = true;
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddAutoConfigurations(builder.Configuration)
    .AddInfrastructure();

    var app = builder.Build();
    app.RunMigrations();
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        Predicate = _ => true
    });
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.MapControllers();
    app.MapHub<SecureCallHub>("/hubs/call");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.Information("Shutting down the application...");
    Log.CloseAndFlush();
}