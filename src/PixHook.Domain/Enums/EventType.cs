namespace PixHook.Domain.Enums;

/// <summary>
/// Tipos de evento disparáveis. Extensível: novos meios de pagamento (boleto, cartão)
/// entram como novos valores, sem alterar o pipeline de disparo.
/// </summary>
public enum EventType
{
    PixConfirmado = 1,
    PixExpirado = 2,
    PixValorDivergente = 3

    // Reservado para o futuro (não implementado na v1):
    // BoletoPago = 100, BoletoVencido = 101,
    // CartaoAprovado = 200, CartaoRecusado = 201
}
