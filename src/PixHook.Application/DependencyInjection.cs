using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PixHook.Application.Behaviors;
using PixHook.Application.Services;

namespace PixHook.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IPlaceholderFactory, DefaultPlaceholderFactory>();
        services.AddScoped<IWebhookRequestBuilder, WebhookRequestBuilder>();

        return services;
    }
}
