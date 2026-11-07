using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using PixHook.Application.Abstractions;
using PixHook.Domain.Repositories;
using PixHook.Infrastructure.Dispatching;
using PixHook.Infrastructure.Persistence;
using PixHook.Infrastructure.Repositories;
using PixHook.Infrastructure.Signing;
using PixHook.Infrastructure.Time;

namespace PixHook.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Para migrar para PostgreSQL: trocar UseSqlite por UseNpgsql (Npgsql.EntityFrameworkCore.PostgreSQL)
        // e gerar novas migrations. Repositórios e Application não mudam.
        var connectionString = configuration.GetConnectionString("PixHook") ?? "Data Source=pixhook.db";
        services.AddDbContext<PixHookDbContext>(o => o.UseSqlite(connectionString));

        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IDispatchRepository, DispatchRepository>();

        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<ISignatureStrategy, HmacSha256SignatureStrategy>();
        services.AddSingleton<ISignatureStrategy, MercadoPagoSignatureStrategy>();
        services.AddSingleton<ISignatureStrategyResolver, SignatureStrategyResolver>();
        services.AddSingleton<IWebhookSecretProvider, ConfigurationWebhookSecretProvider>();

        services.Configure<DispatcherOptions>(configuration.GetSection(DispatcherOptions.SectionName));
        services.AddScoped<IWebhookDispatcher, HttpWebhookDispatcher>();

        services.AddHttpClient(HttpWebhookDispatcher.HttpClientName, c =>
                c.Timeout = Timeout.InfiniteTimeSpan) // timeout controlado pela política Polly
            .AddPolicyHandler((sp, _) =>
            {
                var opts = sp.GetRequiredService<IOptions<DispatcherOptions>>().Value;
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<Polly.Timeout.TimeoutRejectedException>()
                    .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                    .WaitAndRetryAsync(opts.RetryCount, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)));
            })
            .AddPolicyHandler((sp, _) =>
            {
                var opts = sp.GetRequiredService<IOptions<DispatcherOptions>>().Value;
                return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(opts.TimeoutSeconds));
            });

        return services;
    }
}
