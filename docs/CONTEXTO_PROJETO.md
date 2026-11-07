# PixHook — Contexto do Projeto (para retomar em conversas futuras)

> Este arquivo existe só para guardar contexto entre sessões de conversa com o Claude.
> Atualize-o sempre que fizer uma rodada grande de mudanças.

## O que é o PixHook

Ferramenta para simular e testar o envio de webhooks de PIX de diversos provedores de
pagamento, permitindo escolher um provedor + evento, apontar para uma URL local de destino
(da aplicação do usuário) e disparar o payload simulado — útil para testar o lado receptor
de webhooks sem depender do provedor real em produção/sandbox.

Projeto em: `C:\Users\Ruan9\source\repos\PixHook` (Visual Studio 2022, solução `PixHook.sln`).

## Estrutura da solução

- `PixHook.Api` — API principal (porta `http://localhost:5180` / `https://localhost:7180`).
- `PixHook.Web` — front-end Blazor (porta `http://localhost:5050`), consome a API via
  `ApiBaseUrl` no `appsettings.json`. **Precisa da API rodando junto** — configurar
  "Projetos de Inicialização Múltiplos" na solução (API + Web, e opcionalmente o
  SampleReceiver) em vez de só definir a Web como projeto de inicialização único.
- `PixHook.SampleReceiver` — app de exemplo que recebe os webhooks disparados, zero
  dependências de NuGet (de propósito), valida assinaturas (Mercado Pago e Pagar.me).
- `PixHook.Application`, `PixHook.Infrastructure`, `PixHook.Domain` — camadas internas.
- `PixHook.Tests.Unit`, `PixHook.Tests.Integration` — suíte de testes (56 testes, todos
  verdes na última execução).

## Provedores suportados (6, após remoção da Stone)

Cada provedor tem 3 "cenários" padronizados para facilitar comparação entre provedores:
**PIX confirmado**, **PIX expirado**, **PIX valor divergente**. A estrutura real de cada
payload segue a documentação oficial de cada provedor (nomes de campo, nesting, tipos),
então os payloads NÃO são iguais entre provedores — só a categoria do evento é padronizada.

| Provedor | Status da validação (nov/2026) | Observações importantes |
|---|---|---|
| **Mercado Pago** | Aprovado com ressalva corrigida | Não assina o corpo cru: assina o manifest `id:{data.id};request-id:{x-request-id};ts:{ts};` em HMAC-SHA256, publicado como `ts=...,v1=...` no header `x-signature`. Payload real é minimalista (só `data.id`); dados ricos (valor, status) ficam em bloco `pixhook_simulation`. |
| **Efí (Gerencianet)** | Aprovado com ressalva | "PIX confirmado" real, mas `gnExtras.pagador` exige opt-in na conta. "PIX expirado" é **simulação pura** (Efí não emite esse webhook de verdade) — deixado explícito no nome/descrição do template. |
| **Pagar.me** | Aprovado com ressalva corrigida | `amount` em **centavos** (inteiro), não decimal em reais. Assinatura HMAC-SHA1 **sem prefixo** no header `X-Hub-Signature` (único esquema documentado publicamente, API v1; v5 atual não tem esquema confirmado publicamente). |
| **Stone** | **Reprovada e removida** | Payload estava totalmente inventado, sem base em doc pública. Removida do seed por decisão do usuário — não recolocar sem documentação real confirmada. |
| **Asaas** | Aprovado com ressalva corrigida | Evento correto é `PAYMENT_OVERDUE` (não `PIX_QRCODE_EXPIRED`, que era inventado). Campo `qrCode.payload` removido por não confirmado. |
| **PagBank** | Aprovado com ressalva corrigida | Envia o objeto `Order` completo (não um resumo fabricado), com `charges[]` e `qr_codes[]`. |
| **Cielo** | Reprovada e corrigida | Payload real é minimalista: `{PaymentId, ChangeType}` — exige `GET /1/sales/{PaymentId}` depois para status real. Números reais dos status da Cielo não são públicos; bloco `pixhook_simulation` tem nota avisando disso. |

## Decisões importantes já tomadas pelo usuário

- Stone: remover do seed por enquanto (não substituir por outro provedor nem inventar payload).
- Aplicar todas as correções de uma vez (não pediu diff prévio) e já sincronizar/rebuildar/rodar testes.

## Lições operacionais (para não repetir erros)

- **`device_commit_files` pode reportar sucesso (`"written"`) sem persistir de fato** no
  arquivo do Windows. Sempre que uma correção crítica não aparentar ter efeito (ex.: teste
  continua falhando do mesmo jeito), confirmar com `device_stage_files` + `Read` puxando o
  conteúdo atual do arquivo no device antes de assumir que o código está desatualizado — e
  se precisar, re-commitar com `force: true`.
- Visual Studio 2022 via computer-use está só no tier de clique (screenshot + clique
  esquerdo); nada de digitar/atalhos/clique direito. Toda edição de código é via
  Bash/Edit/Write local + sync para o device, nunca digitando na janela do VS.

## Pendências / próximos passos sugeridos

- [ ] Atualizar o `README.md` do projeto documentando as features novas e a validação dos
      6 provedores (foi oferecido ao usuário, ainda não iniciado).
- [ ] Usuário sinalizou que vai pedir mais melhorias numa próxima conversa — usar este
      arquivo como ponto de partida de contexto.

## Como rodar localmente

1. Abrir `PixHook.sln` no Visual Studio 2022.
2. Configurar "Projetos de Inicialização Múltiplos": `PixHook.Api` + `PixHook.Web`
   (+ `PixHook.SampleReceiver` se for testar recebimento real).
3. F5. A Web abre em `http://localhost:5050`, consumindo a API em `http://localhost:5180`.
4. Testes: Gerenciador de Testes → "Executar Todos os Testes" (56 testes esperados, todos
   verdes na última validação, nov/2026).
