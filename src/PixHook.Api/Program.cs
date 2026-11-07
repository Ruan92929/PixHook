using PixHook.Api.Extensions;
using PixHook.Api.Middleware;
using PixHook.Application;
using PixHook.Infrastructure;
using PixHook.Infrastructure.Persistence.Seed;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext());

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApiServices();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
        app.UseVersionedSwagger();

    app.MapControllers();
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

    if (app.Configuration.GetValue("Database:MigrateOnStartup", app.Environment.IsDevelopment()))
        await DatabaseInitializer.InitializeAsync(app.Services);

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "PixHook API terminou inesperadamente.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Exposto para WebApplicationFactory nos testes de integração.</summary>
public partial class Program;
