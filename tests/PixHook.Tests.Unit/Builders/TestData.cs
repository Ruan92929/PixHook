using Bogus;
using PixHook.Domain.Entities;
using PixHook.Domain.Enums;

namespace PixHook.Tests.Unit.Builders;

internal static class TestData
{
    private static readonly Faker Faker = new("pt_BR");

    public static WebhookProvider Provider() =>
        new(Guid.NewGuid(), Faker.Company.CompanyName(), Faker.Internet.DomainWord(), Faker.Lorem.Sentence());

    public static WebhookEventTemplate Template(
        Guid? providerId = null,
        string payload = """{"txid":"{{txid}}","valor":"{{valor}}","horario":"{{dataHora}}"}""",
        string headers = """{"User-Agent":"Teste/1.0"}""",
        string? signatureKey = null) =>
        new(Guid.NewGuid(), providerId ?? Guid.NewGuid(), Faker.PickRandom<EventType>(),
            Faker.Lorem.Word(), Faker.Lorem.Sentence(), payload, headers, signatureKey);

    public static string Url() => $"http://localhost:{Faker.Random.Int(5000, 9000)}/webhooks/{Faker.Lorem.Word()}";
}
