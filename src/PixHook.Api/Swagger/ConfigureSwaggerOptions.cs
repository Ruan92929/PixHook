using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PixHook.Api.Swagger;

/// <summary>Gera um documento Swagger por versão de API descoberta.</summary>
internal sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => _provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "PixHook API",
                Version = description.ApiVersion.ToString(),
                Description = "Simulador de webhooks de pagamento PIX (Mercado Pago, Efi, ...) para desenvolvedores. " +
                              "Escolha um provedor e um evento, aponte para sua URL local e dispare — com histórico e replay." +
                              (description.IsDeprecated ? " **Esta versão está obsoleta.**" : string.Empty),
                Contact = new OpenApiContact { Name = "PixHook" }
            });
        }
    }
}
