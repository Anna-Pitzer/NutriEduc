---
name: bootstrap-admin-por-token
status: implemented
references: V11, V6, ADR-002, ADR-004
---

# Tasks — Bootstrap de administrador por token

## v2 — token único por unidade e token de 30 minutos

### Domínio e persistência

- [x] T15: `Usuario.DefinirPapel` aceita `UnidadeId` para `AdminSecretaria`
  (atribuição, não escopo). `Nutricionista` e `AdminEscola` continuam recusando.
- [x] T16: `ConsumoBootstrap` (unidade única + data) e repositório.
- [x] T17: `TokenDeCadastro` (hash, autor, criação, expiração, uso) com `EstaValido` e
  `MarcarUsado`, e repositório.
- [x] T18: `ConnectionContext` com os dois DbSets, índice único por unidade, índice único
  de hash, FKs e índice `(UnidadeId, ExpiraEm)`.
- [x] T19: migration `BootstrapPorUnidade` — cria as tabelas e remove
  `ConfiguracoesBootstrap`.
- [x] T20: remover `ConfiguracaoBootstrap`, seu repositório, a interface e
  `DefinirBootstrapDTO`.

### Serviço e rotas

- [x] T21: `BootstrapService` v2 — via do token de ambiente (1 uso por unidade, unidade
  pelo nome exato) e via do token temporário (30 min, uso único, unidade do autor).
- [x] T22: emissão e consulta de token do painel, presas à unidade do solicitante;
  `409` quando já existe token válido.
- [x] T23: `TimeProvider` injetado; toda decisão de tempo usa `GetUtcNow()`.
- [x] T24: `BootstrapController` — `GET /status`, `POST /admin`, `GET /token`,
  `POST /token`; `PUT /permitir` removido.
- [x] T25: `IEscolaRepository.BuscarUnidadesPorNome` (exato, sem caixa) e implementação
  no repositório real e no fake.

### Frontend

- [x] T26: `BootstrapApi` — `consultarBootstrap`, `criarAdminBootstrap` com `unidade`,
  `gerarTokenCadastro`, `consultarTokenCadastro`.
- [x] T27: `/bootstrap` com campo **Unidade** (hint: obrigatório só para o token de
  ambiente) e mensagens para `sem-token` e `desativado-ambiente`.
- [x] T28: painel `/secretaria` — gera token de 30 min, exibe o valor uma vez com botão
  copiar, contagem `mm:ss` e botão desabilitado enquanto válido; ao expirar, libera.
- [x] T29: tela `/secretaria/unidades` (renomear, ativar/desativar, excluir) **sem criação**
  e sem seletor de escola: unidade é da rede (ADR-013) e nasce no bootstrap (D17).

### Testes

- [x] T30: `TokenDeCadastroTests` (validade, expiração, uso único, construtor).
- [x] T31: `BootstrapApiTests` v2 — 1 uso por unidade, outra unidade, unidade nova,
  unidade desativada, sem unidade, token inválido, e-mail repetido, sem data, sem token.
- [x] T32: `BootstrapTokenApiTests` — emissão de 30 min, `409` com token ativo, uso único,
  expiração com relógio falso, `401`/`403`/sem unidade.
- [x] T33: `PapeisEEscolasTests` — `AdminSecretaria` com unidade aceito;
  `Nutricionista` com unidade recusado.

### Fechamento

- [x] T34: gates (reauditados em 2026-10-31) — build 0 erros; `dotnet test`
  **251/251** (inclui unidade desativada); `npm run build` OK; `npm run lint` 0 erros e
  5 avisos (baseline).
- [x] T35: `README.md` com as quatro rotas e o novo texto; `DocumentationTests` verde.

## Evidência (v2)

| Cenário | Requisição | Resultado |
| --- | --- | --- |
| Token de ambiente, unidade existente | `POST /api/bootstrap/admin` | `201 { id }`; `/me` com `unidadeId` |
| Mesma unidade novamente | segundo `POST` | `409 Esta unidade já usou o token de criação.` |
| Outra unidade | `POST` com outro nome | `201` |
| Unidade nova | `POST` com nome ainda não cadastrado | `201`; a unidade é criada junto (RF3/D17) |
| Unidade ausente no corpo | `POST` sem `unidade` | `400 Informe a unidade de destino.` |
| Gerar token | `POST /api/bootstrap/token` | `201 { token, expiraEm }` com 30 min |
| Gerar de novo com token ativo | `POST /api/bootstrap/token` | `409 Já existe um token válido...` |
| Estado do token | `GET /api/bootstrap/token` | `{ ativo: true, expiraEm }` |
| Criar com token temporário | `POST /api/bootstrap/admin` sem `unidade` | `201`; admin herda a unidade do autor |
| Reutilizar token temporário | segundo `POST` | `403 Token inválido ou expirado.` |
| Após 31 minutos | `POST` com o token expirado | `403`; `GET /token` → `ativo: false`; novo `POST /token` → `201` |
| Sem sessão | `POST /api/bootstrap/token` | `401` |
| Papel não administrativo | `POST /api/bootstrap/token` | `403` |
| Conta sem unidade | `POST /api/bootstrap/token` | `403` |
| Trava de ambiente | `Bootstrap__Permitir=false` | `motivo: desativado-ambiente`; `403` nas duas vias |
| Nenhuma via configurada | `POST /api/bootstrap/admin` | `404 Rota não encontrada.` |
| Unidade por nome, sem escola | `POST /api/bootstrap/admin { unidade: "Nova" }` | `201`; `GET /api/unidades` mostra a unidade criada |
| Nome de unidade repetido na rede | segundo bootstrap com o mesmo nome | `409 Esta unidade já usou o token de criação.` |

## Desvios e decisões (v2)

| # | Item | Detalhe |
| --- | --- | --- |
| D9 | Unidade pelo nome exato, sem listagem pública | `/bootstrap` é anônima; listar unidades vazaria a rede |
| D10 | Nome de unidade ambíguo é erro | Nome é único por escola; escolher uma escola em silêncio seria arbitrário |
| D11 | Um token válido por unidade (`409`) | Só o hash é guardado; emitir outro invalidaria o anterior sem aviso |
| D12 | Consumo registrado apenas na via do token de ambiente | O token temporário não pode queimar o token de configuração da unidade |
| D13 | Relógio falso ancorado na hora real | O cookie de sessão também usa `TimeProvider`; data fixa no passado invalidava o ticket |
| D14 | `AdminSecretaria` aceita unidade | Atribuição de origem, não escopo; restringir exigiria papel novo (Spec 009) |
| D15 | Edição de usuário permite vincular unidade | O admin legado não tinha unidade e não conseguiria gerar token; a tela `/secretaria/usuarios` passou a oferecer a unidade (só para `AdminSecretaria`) e força novo login ao alterar a própria conta, porque papel e unidade viram claims no login |
| D16 | Unidade é da rede, não da escola (ADR-013) | Nome único na rede, `EscolaId` opcional como dado legado e `/api/escolas/{id}/unidades` substituído por `/api/unidades`. Efeito colateral registrado em PEND-024: `AdminEscola` perde o escopo de aluno derivado da unidade |
| D17 | Unidade nasce no bootstrap (decisão de 2026-10-31) | `ResolverOuCriarUnidade` cria a unidade no fluxo do token de ambiente; `POST /api/unidades` e `EscolaService.CriarUnidade` foram removidos. A tela de unidades ficou somente leitura na criação. O e-mail é validado antes, para não deixar unidade órfã em caso de `409` |

## Histórico v1 (substituído pela v2)

- [x] T1–T3: DTO, serviço com token de `Bootstrap:Token` e controller com `status`/`admin`.
- [x] T4–T5: DI e `ApiFactory` com `Bootstrap:Token` + `ApiFactorySemBootstrap`.
- [x] T6–T8: `BootstrapApi`, página `/bootstrap` e rota pública.
- [x] T9–T10: gates da v1 (209/209) e documentação no `README.md`.
- [x] T11: interruptor `Bootstrap:Permitir` mantido na v2 como trava dura.
- [x] T12–T14: interruptor em banco (`ConfiguracaoBootstrap`) — **removido na v2**;
  substituído pelo token de 30 minutos.
- D1–D8 da v1 continuam válidos, exceto D1 (rota válida para sempre) e D5 (interruptor em
  banco), substituídos por D11 e pelo token temporário.

## Segurança operacional

- O token de ambiente **não** está no repositório: só em `Bootstrap__Token`.
- Toda criação, toda emissão e toda recusa por token inválido geram `LogWarning` sem senha
  e sem o valor do token.
- Rotacionar ou remover `Bootstrap__Token` impede novas criações pela via de ambiente.
- O token temporário nunca é persistido em claro: apenas o hash SHA-256.
