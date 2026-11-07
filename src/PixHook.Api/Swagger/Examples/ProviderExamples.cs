using PixHook.Application.Features.Providers;
using PixHook.Domain.Enums;
using PixHook.Infrastructure.Persistence.Seed;
using Swashbuckle.AspNetCore.Filters;

namespace PixHook.Api.Swagger.Examples;

public sealed class ProviderListExample : IExamplesProvider<IReadOnlyList<ProviderDto>>
{
    public IReadOnlyList<ProviderDto> GetExamples() => new[]
    {
        new ProviderDto(SeedData.EfiId, "Efi (Gerencianet)", "efi",
            "Webhook PIX da API Pix Efi (padrão Bacen), com array 'pix' de pagamentos recebidos.", true),
        new ProviderDto(SeedData.MercadoPagoId, "Mercado Pago", "mercado-pago",
            "Notificações de pagamento (tópico 'payment') do Mercado Pago, assinadas via x-signature.", true)
    };
}

public sealed class EventTemplateListExample : IExamplesProvider<IReadOnlyList<EventTemplateDto>>
{
    public IReadOnlyList<EventTemplateDto> GetExamples() => new[]
    {
        new EventTemplateDto(SeedData.EfiPixConfirmadoId, SeedData.EfiId, EventType.PixConfirmado,
            "PIX confirmado", "PIX recebido na chave do recebedor.",
            "{\"pix\":[{\"endToEndId\":\"{{endToEndId}}\",\"txid\":\"{{txid}}\",\"chave\":\"{{chave}}\",\"valor\":\"{{valor}}\",\"horario\":\"{{dataHora}}\"}]}",
            "{\"User-Agent\":\"Efi-Webhook/1.0\"}", null,
            new[] { "endToEndId", "txid", "chave", "valor", "dataHora" })
    };
}
