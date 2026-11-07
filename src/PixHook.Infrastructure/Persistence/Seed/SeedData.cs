using PixHook.Domain.Entities;
using PixHook.Domain.Enums;
using PixHook.Infrastructure.Signing;

namespace PixHook.Infrastructure.Persistence.Seed;

/// <summary>
/// Dados iniciais: 6 provedores (Mercado Pago, Efi, Pagar.me, Asaas, PagBank, Cielo) com 3 eventos PIX
/// cada. IDs fixos garantem seed idempotente e exemplos estáveis no Swagger.
/// Para adicionar um provedor/evento novo basta acrescentar registros aqui (dados, não código).
///
/// Validado manualmente contra a documentação oficial de cada provedor (nov/2026). Duas observações
/// valem para todos os templates:
///
/// 1) Os payloads de "valor divergente" não correspondem a um evento real documentado por nenhum
///    provedor (esse cenário normalmente não gera um webhook dedicado) — por isso reaproveitam o
///    formato do evento de confirmação e acrescentam um bloco "pixhook_simulation" explicando a
///    simulação, em vez de fingir um campo oficial que não existe.
/// 2) Quando a notificação REAL de um provedor é minimalista e exige uma consulta (GET) subsequente
///    para saber status/valor (caso do Mercado Pago e da Cielo), o template reproduz fielmente esse
///    envelope minimalista e coloca os dados "ricos" dentro de "pixhook_simulation" — já que não há
///    como simular aqui o GET de consulta que o integrador faria de verdade.
///
/// Um provedor que estava no seed original (Stone) foi removido: o formato usado não correspondia a
/// nenhuma API real e pública da Stone (a API real de conta digital da Stone usa payload criptografado
/// JWE/JWT com eventos como "pix_inbound_payment_received", bem diferente do que havia aqui).
/// </summary>
public static class SeedData
{
    public static readonly Guid MercadoPagoId = Guid.Parse("4f1c2a10-0000-4000-8000-000000000001");
    public static readonly Guid EfiId = Guid.Parse("4f1c2a10-0000-4000-8000-000000000002");
    public static readonly Guid PagarmeId = Guid.Parse("4f1c2a10-0000-4000-8000-000000000003");
    public static readonly Guid AsaasId = Guid.Parse("4f1c2a10-0000-4000-8000-000000000005");
    public static readonly Guid PagBankId = Guid.Parse("4f1c2a10-0000-4000-8000-000000000006");
    public static readonly Guid CieloId = Guid.Parse("4f1c2a10-0000-4000-8000-000000000007");

    public static readonly Guid MpPixConfirmadoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000101");
    public static readonly Guid MpPixExpiradoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000102");
    public static readonly Guid MpPixValorDivergenteId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000103");

    public static readonly Guid EfiPixConfirmadoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000201");
    public static readonly Guid EfiPixExpiradoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000202");
    public static readonly Guid EfiPixValorDivergenteId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000203");

    public static readonly Guid PagarmePixConfirmadoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000301");
    public static readonly Guid PagarmePixExpiradoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000302");
    public static readonly Guid PagarmePixValorDivergenteId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000303");

    public static readonly Guid AsaasPixConfirmadoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000501");
    public static readonly Guid AsaasPixExpiradoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000502");
    public static readonly Guid AsaasPixValorDivergenteId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000503");

    public static readonly Guid PagBankPixConfirmadoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000601");
    public static readonly Guid PagBankPixExpiradoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000602");
    public static readonly Guid PagBankPixValorDivergenteId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000603");

    public static readonly Guid CieloPixConfirmadoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000701");
    public static readonly Guid CieloPixExpiradoId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000702");
    public static readonly Guid CieloPixValorDivergenteId = Guid.Parse("7a3e9b20-0000-4000-8000-000000000703");

    public static readonly Guid ScenarioPixDuplicadoId = Guid.Parse("5c3f8e40-0000-4000-8000-000000000001");
    public static readonly Guid ScenarioPixCorrecaoValorId = Guid.Parse("5c3f8e40-0000-4000-8000-000000000002");
    public static readonly Guid ScenarioPixForaDeOrdemId = Guid.Parse("5c3f8e40-0000-4000-8000-000000000003");

    public static IReadOnlyList<WebhookProvider> Providers() => new[]
    {
        new WebhookProvider(MercadoPagoId, "Mercado Pago", "mercado-pago",
            "Notificações de pagamento (tópico 'payment') do Mercado Pago, assinadas via x-signature."),
        new WebhookProvider(EfiId, "Efi (Gerencianet)", "efi",
            "Webhook PIX da API Pix Efi (padrão Bacen), com array 'pix' de pagamentos recebidos."),
        new WebhookProvider(PagarmeId, "Pagar.me", "pagarme",
            "Eventos de charge (charge.*) da API v5 do Pagar.me, assinados via X-Hub-Signature (HMAC-SHA1)."),
        new WebhookProvider(AsaasId, "Asaas", "asaas",
            "Eventos de pagamento (PAYMENT_*) do Asaas, autenticados por token fixo no header."),
        new WebhookProvider(PagBankId, "PagBank", "pagbank",
            "Notificações de transação (charges[]) do PagBank/PagSeguro (API de Pedidos)."),
        new WebhookProvider(CieloId, "Cielo", "cielo",
            "Notificações de mudança de status de pagamento (ChangeType) da Cielo E-commerce.")
    };

    private const string MpHeaders = """
        {"User-Agent":"MercadoPago WebHook v1.0 payment","x-request-id":"{{requestId}}"}
        """;

    private const string EfiHeaders = """
        {"User-Agent":"Efi-Webhook/1.0"}
        """;

    private const string PagarmeHeadersPaid = """
        {"User-Agent":"pagarme-webhook/1.0","X-Pagarme-Event":"charge.paid"}
        """;

    private const string PagarmeHeadersFailed = """
        {"User-Agent":"pagarme-webhook/1.0","X-Pagarme-Event":"charge.payment_failed"}
        """;

    private const string AsaasHeaders = """
        {"User-Agent":"Asaas-Webhook/1.0","asaas-access-token":"pixhook-demo-token"}
        """;

    private const string PagBankHeaders = """
        {"User-Agent":"PagBank-Notification/1.0"}
        """;

    private const string CieloHeaders = """
        {"User-Agent":"CieloEcommerce-Webhook/1.0"}
        """;

    public static IReadOnlyList<DispatchScenario> Scenarios() => new[]
    {
        new DispatchScenario(ScenarioPixDuplicadoId, "PIX confirmado duplicado", "pix-confirmado-duplicado",
            "O mesmo evento de confirmação chega duas vezes, 3s depois — testa se o seu sistema trata o " +
            "reenvio como o mesmo pagamento (idempotência) em vez de processá-lo de novo."),

        new DispatchScenario(ScenarioPixCorrecaoValorId, "PIX confirmado seguido de correção de valor", "pix-correcao-valor",
            "Confirmação normal seguida, 5s depois, de um aviso de que o valor pago foi diferente do " +
            "cobrado — testa se o seu sistema consegue corrigir o pedido em vez de só ignorar o segundo evento."),

        new DispatchScenario(ScenarioPixForaDeOrdemId, "PIX expira depois de já confirmado", "pix-fora-de-ordem",
            "A confirmação chega primeiro, mas um webhook de expiração do mesmo pagamento chega atrasado " +
            "2s depois — testa se o seu sistema ignora eventos desatualizados em vez de reverter um pedido já pago. " +
            "Nota: a Efí não tem um evento de expiração real (ver ressalva no template 'PIX expirado' da Efí); " +
            "o cenário continua útil para testar entrega fora de ordem mesmo assim.")
    };

    public static IReadOnlyList<DispatchScenarioStep> ScenarioSteps() => new[]
    {
        new DispatchScenarioStep(Guid.Parse("5c3f8e41-0000-4000-8000-000000000001"), ScenarioPixDuplicadoId, 0,
            EfiPixConfirmadoId, 0, "Primeiro envio do PIX confirmado."),
        new DispatchScenarioStep(Guid.Parse("5c3f8e41-0000-4000-8000-000000000002"), ScenarioPixDuplicadoId, 1,
            EfiPixConfirmadoId, 3000, "O provedor reenvia o mesmo evento 3s depois (não recebeu 2xx a tempo) — deve ser tratado como o mesmo pagamento."),

        new DispatchScenarioStep(Guid.Parse("5c3f8e41-0000-4000-8000-000000000003"), ScenarioPixCorrecaoValorId, 0,
            MpPixConfirmadoId, 0, "Pagamento confirmado normalmente."),
        new DispatchScenarioStep(Guid.Parse("5c3f8e41-0000-4000-8000-000000000004"), ScenarioPixCorrecaoValorId, 1,
            MpPixValorDivergenteId, 5000, "5s depois, o provedor avisa que o valor pago foi diferente do cobrado."),

        new DispatchScenarioStep(Guid.Parse("5c3f8e41-0000-4000-8000-000000000005"), ScenarioPixForaDeOrdemId, 0,
            EfiPixConfirmadoId, 0, "Confirmação chega dentro do prazo."),
        new DispatchScenarioStep(Guid.Parse("5c3f8e41-0000-4000-8000-000000000006"), ScenarioPixForaDeOrdemId, 1,
            EfiPixExpiradoId, 2000, "2s depois, chega um webhook de expiração atrasado do mesmo pagamento (entrega fora de ordem).")
    };

    public static IReadOnlyList<WebhookEventTemplate> Templates() => new[]
    {
        // --- Mercado Pago -----------------------------------------------------------------------
        // Confirmado via doc oficial (tópico "payment"): a notificação real só traz action/api_version/
        // data.id/date_created/id/live_mode/type/user_id — nenhum dado de status ou valor. Por isso eles
        // vêm só dentro de "pixhook_simulation" (ver nota de classe acima).
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

        // --- Efí (Gerencianet) -------------------------------------------------------------------
        // Confirmado via dev.efipay.com.br/docs/api-pix/webhooks: envelope {"pix":[...]} com endToEndId/
        // txid/chave/valor/horario/infoPagador/gnExtras estão corretos. "gnExtras.pagador" só é enviado
        // se a conta habilitar "recebimento.webhook.notificacao.pagador" (não é padrão) — mantido aqui
        // como exemplo didático, mas vale saber que por padrão a Efí não manda esses dados do pagador.
        new WebhookEventTemplate(EfiPixConfirmadoId, EfiId, EventType.PixConfirmado,
            "PIX confirmado", "PIX recebido na chave do recebedor. Nota: 'gnExtras.pagador' só é enviado " +
            "se a conta Efí habilitar o envio de dados do pagador (não é o padrão de fábrica).",
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

        // IMPORTANTE: a Efí NÃO envia webhook quando uma cobrança (cob) expira sem pagamento — isso só
        // é percebido consultando GET /v2/cob/{txid} ativamente. O payload abaixo (envelope "cobs") é uma
        // SIMULAÇÃO HIPOTÉTICA do PixHook, sem correspondência com nenhum evento real documentado da
        // Efí. Mantido aqui só para permitir testar como seu sistema reage a esse cenário.
        new WebhookEventTemplate(EfiPixExpiradoId, EfiId, EventType.PixExpirado,
            "PIX expirado (SIMULAÇÃO — sem evento real na Efí)",
            "A Efí não envia webhook de expiração de cobrança; isso só é descoberto por consulta GET " +
            "/v2/cob/{txid}. Este payload é uma simulação hipotética do PixHook para testar esse cenário " +
            "mesmo assim, não um evento documentado oficialmente.",
            """
            {
              "cobs": [
                {
                  "txid": "{{txid}}",
                  "status": "REMOVIDA_PELO_PSP",
                  "chave": "{{chave}}",
                  "valor": { "original": "{{valor}}" },
                  "calendario": { "criacao": "{{dataExpiracao}}", "expiracao": 3600 },
                  "horario": "{{dataHora}}",
                  "pixhook_simulation": {
                    "nota": "Evento hipotético — a Efí não envia webhook de expiração de cobrança."
                  }
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
            """, EfiHeaders),

        // --- Pagar.me (API v5) -------------------------------------------------------------------
        // Confirmado via docs.pagar.me: envelope {id, account, type, created_at, data} e os nomes de
        // evento charge.paid/charge.payment_failed existem de verdade. Ajustes aplicados após a
        // validação: "amount" é inteiro em CENTAVOS (não reais); "pix_provider_tid" não existe de
        // verdade (trocado por "qr_code"/"end_to_end_id", que são os campos reais documentados);
        // status "expired" não existe no enum oficial (trocado para "failed", que é o valor real).
        // A assinatura HMAC-SHA1 sem prefixo é a única documentada publicamente (API v1); não há
        // confirmação pública de um esquema equivalente para a v5, então é a melhor aproximação
        // disponível. O header "X-Pagarme-Event" também não é documentado — mantido só como
        // conveniência didática do simulador, não como garantia de que a Pagar.me o envia de verdade.
        new WebhookEventTemplate(PagarmePixConfirmadoId, PagarmeId, EventType.PixConfirmado,
            "PIX confirmado", "Charge paga via PIX (type charge.paid, status paid). Valor em centavos.",
            """
            {
              "id": "hook_{{requestId}}",
              "account": { "id": "acc_demo" },
              "type": "charge.paid",
              "created_at": "{{dataHora}}",
              "data": {
                "id": "ch_{{paymentId}}",
                "code": "{{externalReference}}",
                "status": "paid",
                "payment_method": "pix",
                "amount": {{valorCentavos}},
                "paid_at": "{{dataHora}}",
                "last_transaction": {
                  "transaction_type": "pix",
                  "end_to_end_id": "{{endToEndId}}",
                  "qr_code": "{{chave}}"
                },
                "customer": { "name": "{{pagadorNome}}", "document": "{{pagadorCpf}}" }
              }
            }
            """, PagarmeHeadersPaid, PagarmeSignatureStrategy.StrategyKey),

        new WebhookEventTemplate(PagarmePixExpiradoId, PagarmeId, EventType.PixExpirado,
            "PIX expirado", "Charge com PIX expirado sem pagamento (type charge.payment_failed, status failed).",
            """
            {
              "id": "hook_{{requestId}}",
              "account": { "id": "acc_demo" },
              "type": "charge.payment_failed",
              "created_at": "{{dataHora}}",
              "data": {
                "id": "ch_{{paymentId}}",
                "code": "{{externalReference}}",
                "status": "failed",
                "payment_method": "pix",
                "amount": {{valorCentavos}},
                "last_transaction": {
                  "transaction_type": "pix",
                  "expires_at": "{{dataExpiracao}}"
                }
              }
            }
            """, PagarmeHeadersFailed, PagarmeSignatureStrategy.StrategyKey),

        new WebhookEventTemplate(PagarmePixValorDivergenteId, PagarmeId, EventType.PixValorDivergente,
            "PIX com valor divergente", "Charge paga com valor diferente do cobrado originalmente (valores em centavos).",
            """
            {
              "id": "hook_{{requestId}}",
              "account": { "id": "acc_demo" },
              "type": "charge.paid",
              "created_at": "{{dataHora}}",
              "data": {
                "id": "ch_{{paymentId}}",
                "code": "{{externalReference}}",
                "status": "paid",
                "payment_method": "pix",
                "amount": {{valorOriginalCentavos}},
                "paid_at": "{{dataHora}}",
                "last_transaction": {
                  "transaction_type": "pix",
                  "end_to_end_id": "{{endToEndId}}",
                  "qr_code": "{{chave}}"
                }
              },
              "pixhook_simulation": { "valor_cobrado_centavos": {{valorOriginalCentavos}}, "valor_pago_centavos": {{valorPagoCentavos}} }
            }
            """, PagarmeHeadersPaid, PagarmeSignatureStrategy.StrategyKey),

        // --- Asaas --------------------------------------------------------------------------------
        // Confirmado via docs.asaas.com: evento PAYMENT_RECEIVED e o envelope {id, event, dateCreated,
        // payment} batem com o exemplo oficial. Ajustes: "PIX_QRCODE_EXPIRED" não existe como evento —
        // o evento real para cobrança PIX vencida é "PAYMENT_OVERDUE" (status continua "OVERDUE"). O
        // campo "qrCode.payload" também não existe dentro de "pixTransaction" na doc oficial (removido).
        new WebhookEventTemplate(AsaasPixConfirmadoId, AsaasId, EventType.PixConfirmado,
            "PIX confirmado", "Cobrança recebida via PIX (event PAYMENT_RECEIVED).",
            """
            {
              "id": "evt_{{requestId}}",
              "event": "PAYMENT_RECEIVED",
              "dateCreated": "{{dataHora}}",
              "payment": {
                "id": "pay_{{paymentId}}",
                "externalReference": "{{externalReference}}",
                "status": "RECEIVED",
                "billingType": "PIX",
                "value": {{valor}},
                "pixTransaction": { "endToEndIdentifier": "{{endToEndId}}" }
              }
            }
            """, AsaasHeaders),

        new WebhookEventTemplate(AsaasPixExpiradoId, AsaasId, EventType.PixExpirado,
            "PIX expirado", "Cobrança PIX vencida sem pagamento (event PAYMENT_OVERDUE).",
            """
            {
              "id": "evt_{{requestId}}",
              "event": "PAYMENT_OVERDUE",
              "dateCreated": "{{dataHora}}",
              "payment": {
                "id": "pay_{{paymentId}}",
                "externalReference": "{{externalReference}}",
                "status": "OVERDUE",
                "billingType": "PIX",
                "value": {{valor}},
                "dueDate": "{{dataExpiracao}}"
              }
            }
            """, AsaasHeaders),

        new WebhookEventTemplate(AsaasPixValorDivergenteId, AsaasId, EventType.PixValorDivergente,
            "PIX com valor divergente", "Cobrança recebida com valor diferente do cobrado.",
            """
            {
              "id": "evt_{{requestId}}",
              "event": "PAYMENT_RECEIVED",
              "dateCreated": "{{dataHora}}",
              "payment": {
                "id": "pay_{{paymentId}}",
                "externalReference": "{{externalReference}}",
                "status": "RECEIVED",
                "billingType": "PIX",
                "value": {{valorOriginal}},
                "pixTransaction": { "endToEndIdentifier": "{{endToEndId}}" }
              },
              "pixhook_simulation": { "valor_cobrado": {{valorOriginal}}, "valor_pago": {{valorPago}} }
            }
            """, AsaasHeaders),

        // --- PagBank (API de Pedidos / Orders) -----------------------------------------------------
        // Confirmado via developer.pagbank.com.br: o webhook real entrega o objeto Order completo (id,
        // reference_id, created_at, charges[], qr_codes[]), não um envelope resumido {notificationType,
        // charges}. Campos de charges[] (status/amount/payment_method.pix.end_to_end_id/paid_at) batem
        // com a doc; "qr_code_expired_at" não existe dentro de charges[] — o campo real de expiração do
        // QR Code fica em "qr_codes[].expiration_date", na raiz do Order.
        new WebhookEventTemplate(PagBankPixConfirmadoId, PagBankId, EventType.PixConfirmado,
            "PIX confirmado", "Order com charge PAID recebida via PIX.",
            """
            {
              "id": "ORDE_{{requestId}}",
              "reference_id": "{{externalReference}}",
              "created_at": "{{dataHora}}",
              "charges": [
                {
                  "id": "CHAR_{{paymentId}}",
                  "reference_id": "{{externalReference}}",
                  "status": "PAID",
                  "payment_method": { "type": "PIX", "pix": { "end_to_end_id": "{{endToEndId}}", "notification_id": "{{requestId}}" } },
                  "amount": { "value": {{valor}}, "currency": "BRL" },
                  "paid_at": "{{dataHora}}"
                }
              ]
            }
            """, PagBankHeaders),

        new WebhookEventTemplate(PagBankPixExpiradoId, PagBankId, EventType.PixExpirado,
            "PIX expirado", "QR Code do Order expirou sem pagamento (status CANCELED).",
            """
            {
              "id": "ORDE_{{requestId}}",
              "reference_id": "{{externalReference}}",
              "created_at": "{{dataHora}}",
              "charges": [
                {
                  "id": "CHAR_{{paymentId}}",
                  "reference_id": "{{externalReference}}",
                  "status": "CANCELED",
                  "payment_method": { "type": "PIX" },
                  "amount": { "value": {{valor}}, "currency": "BRL" }
                }
              ],
              "qr_codes": [
                { "id": "QR_{{requestId}}", "expiration_date": "{{dataExpiracao}}" }
              ]
            }
            """, PagBankHeaders),

        new WebhookEventTemplate(PagBankPixValorDivergenteId, PagBankId, EventType.PixValorDivergente,
            "PIX com valor divergente", "Order com charge paga com valor diferente do cobrado.",
            """
            {
              "id": "ORDE_{{requestId}}",
              "reference_id": "{{externalReference}}",
              "created_at": "{{dataHora}}",
              "charges": [
                {
                  "id": "CHAR_{{paymentId}}",
                  "reference_id": "{{externalReference}}",
                  "status": "PAID",
                  "payment_method": { "type": "PIX", "pix": { "end_to_end_id": "{{endToEndId}}", "notification_id": "{{requestId}}" } },
                  "amount": { "value": {{valorOriginal}}, "currency": "BRL" },
                  "paid_at": "{{dataHora}}"
                }
              ],
              "pixhook_simulation": { "valor_cobrado": {{valorOriginal}}, "valor_pago": {{valorPago}} }
            }
            """, PagBankHeaders),

        // --- Cielo (E-commerce API 3.0) -------------------------------------------------------------
        // Confirmado via docs.cielo.com.br: a notificação REAL de "ChangeType" é extremamente
        // minimalista — só {"PaymentId": "...", "ChangeType": 1} (2/4 quando é recorrência). Não há
        // Status/OrderNumber/objeto Pix/ReceivedAt no corpo real; esses detalhes só vêm de um GET
        // subsequente (ex.: /1/sales/{PaymentId}). O payload abaixo reflete o corpo real minimalista e
        // põe os dados que viriam da consulta dentro de "pixhook_simulation" (mesmo padrão usado para o
        // Mercado Pago), em vez de inventar campos que a notificação não tem.
        new WebhookEventTemplate(CieloPixConfirmadoId, CieloId, EventType.PixConfirmado,
            "PIX confirmado",
            "Notificação real de mudança de status (ChangeType=1). A Cielo não manda status/valor aqui " +
            "— isso só viria de um GET subsequente em /1/sales/{PaymentId}, simulado em 'pixhook_simulation'.",
            """
            {
              "PaymentId": "{{paymentId}}",
              "ChangeType": 1,
              "pixhook_simulation": {
                "nota": "A notificação real da Cielo só traz PaymentId+ChangeType; isto simula o resultado de um GET /1/sales/{PaymentId} seguinte.",
                "status_simulado": "Paid",
                "end_to_end_id_simulado": "{{endToEndId}}",
                "valor_simulado": {{valor}},
                "merchant_order_id_simulado": "{{externalReference}}"
              }
            }
            """, CieloHeaders),

        new WebhookEventTemplate(CieloPixExpiradoId, CieloId, EventType.PixExpirado,
            "PIX expirado",
            "Notificação real de mudança de status (ChangeType=1). Detalhes de expiração simulados em " +
            "'pixhook_simulation' (ver nota do evento 'PIX confirmado' acima).",
            """
            {
              "PaymentId": "{{paymentId}}",
              "ChangeType": 1,
              "pixhook_simulation": {
                "nota": "A notificação real da Cielo só traz PaymentId+ChangeType; isto simula o resultado de um GET /1/sales/{PaymentId} seguinte.",
                "status_simulado": "nao-pago-expirado",
                "status_simulado_nota": "A Cielo retorna um código numérico de status na consulta real; o valor exato não foi confirmado na documentação pública, então usamos um rótulo textual aqui.",
                "expira_em_simulado": "{{dataExpiracao}}",
                "merchant_order_id_simulado": "{{externalReference}}"
              }
            }
            """, CieloHeaders),

        new WebhookEventTemplate(CieloPixValorDivergenteId, CieloId, EventType.PixValorDivergente,
            "PIX com valor divergente",
            "Notificação real de mudança de status (ChangeType=1). Valor divergente simulado em " +
            "'pixhook_simulation' (ver nota do evento 'PIX confirmado' acima).",
            """
            {
              "PaymentId": "{{paymentId}}",
              "ChangeType": 1,
              "pixhook_simulation": {
                "nota": "A notificação real da Cielo só traz PaymentId+ChangeType; isto simula o resultado de um GET /1/sales/{PaymentId} seguinte.",
                "status_simulado": "Paid",
                "end_to_end_id_simulado": "{{endToEndId}}",
                "merchant_order_id_simulado": "{{externalReference}}",
                "valor_cobrado_simulado": {{valorOriginal}},
                "valor_pago_simulado": {{valorPago}}
              }
            }
            """, CieloHeaders)
    };
}
