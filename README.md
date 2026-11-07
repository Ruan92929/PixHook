# PixHook

Ferramenta de desenvolvedor que **simula webhooks de pagamento PIX** (Mercado Pago, Efi/Gerencianet, …).
Escolha um provedor + tipo de evento, aponte para a sua URL local e dispare um webhook com payload fiel
ao formato real — com histórico e **replay**.

## Arquitetura

```
PixHook.sln
├── src/
│   ├── PixHook.Domain          Entidades, enums, value objects, interfaces de repositório (sem dependências)
│   ├── PixHook.Application     CQRS (MediatR), DTOs, FluentValidation, Result<T>, IWebhookDispatcher, IClock
│   ├── PixHook.Infrastructure  EF Core (SQLite), repositórios, dispatcher HTTP (HttpClientFactory + Polly), seed
│   └── PixHook.Api             ASP.NET Core Web API, Swagger, versionamento, Serilog, ProblemDetails
└── tests/
    ├── PixHook.Tests.Unit         xUnit + FluentAssertions + NSubstitute + Bogus
    └── PixHook.Tests.Integration  WebApplicationFactory + WireMock.Net
```

Dependências: `Api → Application + Infrastructure → Domain`. `Application` só conhece `Domain` (usa interfaces).

Padrões: Clean Architecture · CQRS/MediatR · Repository · Strategy (`ISignatureStrategy`) ·
Result Pattern → ProblemDetails · templates como dados (JSON com placeholders) · DI nativo.

## Rodando localmente

Pré-requisito: .NET 8 SDK.

**Visual Studio:** abra `PixHook.sln`, defina `PixHook.Api` como projeto de inicialização e pressione F5.
O Swagger abre em `https://localhost:7180/swagger`.

**CLI:**

```bash
dotnet run --project src/PixHook.Api
# http://localhost:5180/swagger
```

Em `Development` o banco `pixhook.db` é criado automaticamente (migrations + seed idempotente com
Mercado Pago e Efi, 3 eventos PIX cada).

**Docker:**

```bash
docker compose up --build
# http://localhost:5180/swagger
```

Para atingir uma API rodando no seu host a partir do container use `http://host.docker.internal:<porta>/...`.

## Exemplo de uso

```bash
# 1. provedores
curl http://localhost:5180/api/v1/providers
# 2. eventos de um provedor (Efi)
curl http://localhost:5180/api/v1/providers/4f1c2a10-0000-4000-8000-000000000002/events
# 3. disparo
curl -X POST http://localhost:5180/api/v1/dispatches -H "Content-Type: application/json" -d '{
  "templateId": "7a3e9b20-0000-4000-8000-000000000201",
  "destinationUrl": "http://localhost:5000/webhooks/pix",
  "payloadOverrides": { "valor": "249.90", "txid": "meutxid123" },
  "customHeaders": { "X-Correlation-Id": "teste-1" }
}'
# 4. histórico e replay
curl "http://localhost:5180/api/v1/dispatches?page=1&pageSize=20"
curl -X POST http://localhost:5180/api/v1/dispatches/{id}/replay
```

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/v1/providers` | Provedores ativos |
| GET | `/api/v1/providers/{id}/events` | Templates de evento do provedor |
| POST | `/api/v1/dispatches` | Dispara um webhook |
| GET | `/api/v1/dispatches` | Histórico paginado (`page`, `pageSize`, `providerId`) |
| GET | `/api/v1/dispatches/{id}` | Detalhe de um disparo |
| POST | `/api/v1/dispatches/{id}/replay` | Reenvia um disparo anterior |

Erros seguem RFC 7807 (`application/problem+json`): 400 validação, 404 inexistente, 422 payload inválido.
Falhas no **destino** (HTTP 5xx, timeout, conexão recusada) não são erro da API: o disparo é registrado com
`success: false` e `errorMessage`.

### Placeholders padrão

`txid`, `valor`, `valorOriginal`, `valorPago`, `dataHora`, `dataExpiracao`, `endToEndId`, `chave`,
`pagadorNome`, `pagadorCpf`, `paymentId`, `externalReference`, `requestId`, `timestamp` — todos com valores
realistas gerados a cada disparo e sobrescrevíveis via `payloadOverrides`.

### Configuração (`appsettings.json`)

| Chave | Padrão | Descrição |
|---|---|---|
| `ConnectionStrings:PixHook` | `Data Source=pixhook.db` | Banco SQLite |
| `Database:MigrateOnStartup` | `true` em Development | Migrations + seed no startup |
| `Dispatcher:TimeoutSeconds` / `RetryCount` | `10` / `2` | Polly: timeout por tentativa e retry exponencial |
| `Signing:Secrets:{chave}` | — | Segredo HMAC por estratégia (`mercadopago`, `hmac-sha256`) |

## Testes

```bash
dotnet test
```

Cobertura (Coverlet + ReportGenerator), meta informal de 80%+ em Domain e Application:

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./coveragereport" \
  -reporttypes:"Html;TextSummary" -assemblyfilters:"+PixHook.Domain;+PixHook.Application"
```

Convenção de nomes: `MetodoTestado_CenarioDeEntrada_ResultadoEsperado`.

## Migrations

As migrations ficam versionadas em `src/PixHook.Infrastructure/Persistence/Migrations`.

```bash
dotnet tool install -g dotnet-ef
dotnet ef migrations add NomeDaMigration -p src/PixHook.Infrastructure -s src/PixHook.Api -o Persistence/Migrations
dotnet ef database update -p src/PixHook.Infrastructure -s src/PixHook.Api
```

## Como adicionar um novo provedor/evento

É **dado, não código**:

1. Em `src/PixHook.Infrastructure/Persistence/Seed/SeedData.cs`, adicione um `WebhookProvider` com Id fixo
   e os `WebhookEventTemplate` com o JSON do payload (use `{{placeholder}}`) e os headers.
2. Rode a API — o seed é idempotente e insere apenas o que falta.

Só é preciso código se o provedor tiver um esquema de assinatura novo: implemente `ISignatureStrategy`
em `Infrastructure/Signing`, registre-a em `DependencyInjection.cs` e referencie sua `Key` em
`SignatureStrategyKey` do template. Novo meio de pagamento (boleto/cartão) = novo valor em `EventType` + templates.

## Evolução prevista (fora da v1)

PostgreSQL (trocar `UseSqlite` por `UseNpgsql` na Infrastructure) · multi-tenancy (`TenantId` + middleware) ·
autenticação JWT/Identity na Api · `ITunnelProvider` (túnel embutido) · fila/outbox atrás de
`IWebhookDispatcher` · front-end `PixHook.Web` (Blazor Server) consumindo a API.
