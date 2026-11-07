using PixHook.Domain.Entities;
using PixHook.Domain.Enums;
using PixHook.Infrastructure.Signing;

namespace PixHook.Infrastructure.Persistence.Seed;

/// <summary>
/// Dados iniciais: Mercado Pago e Efi com 3 eventos PIX cada.
/// IDs fixos garantem seed idempotente e exemplos estáveis no Swagger.
/// Para adicionar um provedor/evento novo basta acrescentar registros aqui (dados, não código).
/// </summary>
public static class SeedData
{
    public static readonly Guid MercadoPagoId = Guid.Parse("4f1c2a10-0000-4000-8000-000000000001");
    public static readonly Guid EfiId = Guid.Parse("4f1c2a10-0000-4000-8000-000000000002");

    public static readonly Guid MpPixConfirmadoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000101");
    public static readonly Guid MpPixExpiradoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000102");
    public static readonly Guid MpPixValorDivergenteId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000103");

    public static readonly Guid EfiPixConfirmadoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000201");
    public static readonly Guid EfiPixExpiradoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000202");
    public static readonly Guid EfiPixValorDivergenteId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000203");

    public static IReadOnlyList<WebhookProvider> Providers() => new[]
    {
        new WebhookProvider(MercadoPagoId, "Mercado Pago", "mercado-pago",
            "Notificações de pagamento (tópico 'payment') do Mercado Pago, assinadas via x-signature."),
        new WebhookProvider(EfiId, "Efi (Gerencianet)", "efi",
            "Webhook PIX da API Pix Efi (padrão Bacen), com array 'pix' de pagamentos recebidos.")
    };

    private const string MpHeaders = """
        {"User-Agent":"MercadoPago WebHook v1.0 payment","x-request-id":"{{requestId}}"}
        """;

    private const string EfiHeaders = """
        {"User-Agent":"Efi-Webhook/1.0"}
        """;

    public static IReadOnlyList<WebhookEventTemplate> Templates() => new[]
    {
        new WebhookEventTemplate(MpPixConfirmadoId, MercadoPagoId, EventType.PixConfirmado,
            "PIX confirmado", "Pagamento PIX aprovado (status approved).",
            """
            {
              "action": "payment.updated",
              "api_version": "v1",
              "data": { "id": "{{paymentId}}" },
              "date_created": "{{dataHora}}",
              "id": {{paymentId}},
              "live_mode": false,
              "type": "payment",
              "user_id": "123456789",
              "pixhook_simulation": {
                "status": "approved",
                "status_detail": "accredited",
                "payment_method_id": "pix",
                "transaction_amount": {{valor}},
                "external_reference": "{{externalReference}}"
              }
            }
            """, MpHeaders, MercadoPagoSignatureStrategy.StrategyKey),

        new WebhookEventTemplate(MpPixExpiradoId, MercadoPagoId, EventType.PixExpirado,
            "PIX expirado", "QR Code PIX expirou sem pagamento (status cancelled/expired).",
            """
            {
              "action": "payment.updated",
              "api_version": "v1",
              "data": { "id": "{{paymentId}}" },
              "date_created": "{{dataHora}}",
              "id": {{paymentId}},
              "live_mode": false,
              "type": "payment",
              "user_id": "123456789",
              "pixhook_simulation": {
                "status": "cancelled",
                "status_detail": "expired",
                "payment_method_id": "pix",
                "transaction_amount": {{valor}},
                "date_of_expiration": "{{dataExpiracao}}",
                "external_reference": "{{externalReference}}"
              }
            }
            """, MpHeaders, MercadoPagoSignatureStrategy.StrategyKey),

        new WebhookEventTemplate(MpPixValorDivergenteId, MercadoPagoId, EventType.PixValorDivergente,
            "PIX com valor divergente", "Pagamento recebido com valor diferente do cobrado.",
            """
            {
              "action": "payment.updated",
              "api_version": "v1",
              "data": { "id": "{{paymentId}}" },
              "date_created": "{{dataHora}}",
              "id": {{paymentId}},
              "live_mode": false,
              "type": "payment",
              "user_id": "123456789",
              "pixhook_simulation": {
                "status": "approved",
                "status_detail": "partially_paid",
                "payment_method_id": "pix",
                "transaction_amount": {{valorOriginal}},
                "transaction_details": { "total_paid_amount": {{valorPago}} },
                "external_reference": "{{externalReference}}"
              }
            }
            """, MpHeaders, MercadoPagoSignatureStrategy.StrategyKey),

        new WebhookEventTemplate(EfiPixConfirmadoId, EfiId, EventType.PixConfirmado,
            "PIX confirmado", "PIX recebido na chave do recebedor.",
            """
            {
              "pix": [
                {
                  "endToEndId": "{{endToEndId}}",
                  "txid": "{{txid}}",
                  "chave": "{{chave}}",
                  "valor": "{{valor}}",
                  "horario": "{{dataHora}}",
                  "infoPagador": "Pagamento pedido {{externalReference}}",
                  "gnExtras": { "pagador": { "nome": "{{pagadorNome}}", "cpf": "{{pagadorCpf}}" } }
                }
              ]
            }
            """, EfiHeaders),

        new WebhookEventTemplate(EfiPixExpiradoId, EfiId, EventType.PixExpirado,
            "PIX expirado", "Cobrança imediata (cob) expirada e removida pelo PSP.",
            """
            {
              "cobs": [
                {
                  "txid": "{{txid}}",
                  "status": "REMOVIDA_PELO_PSP",
                  "chave": "{{chave}}",
                  "valor": { "original": "{{valor}}" },
                  "calendario": { "criacao": "{{dataExpiracao}}", "expiracao": 3600 },
                  "horario": "{{dataHora}}"
                }
              ]
            }
            """, EfiHeaders),

        new WebhookEventTemplate(EfiPixValorDivergenteId, EfiId, EventType.PixValorDivergente,
            "PIX com valor divergente", "PIX recebido com valor diferente do original da cobrança.",
            """
            {
              "pix": [
                {
                  "endToEndId": "{{endToEndId}}",
                  "txid": "{{txid}}",
                  "chave": "{{chave}}",
                  "valor": "{{valorPago}}",
                  "horario": "{{dataHora}}",
                  "gnExtras": { "valorOriginalCobranca": "{{valorOriginal}}" }
                }
              ]
            }
            """, EfiHeaders)
    };
}
