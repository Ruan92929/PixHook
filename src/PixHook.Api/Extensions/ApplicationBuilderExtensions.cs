using Asp.Versioning.ApiExplorer;

namespace PixHook.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>Swagger UI em /swagger com seletor de versão de API.</summary>
    public static WebApplication UseVersionedSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            foreach (var description in provider.ApiVersionDescriptions.Reverse())
            {
                o.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                    $"PixHook API {description.GroupName.ToUpperInvariant()}");
            }
            o.RoutePrefix = "swagger";
            o.DocumentTitle = "PixHook API";
            o.DisplayRequestDuration();
        });
        return app;
    }
}
