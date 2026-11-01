---
name: bootstrap-admin-por-token
status: implemented
references: V11, V6, ADR-002, ADR-004
---

# Design — Bootstrap de administrador por token (v2)

## Arquivos

| Arquivo | Responsabilidade |
| --- | --- |
| `backend/Domain/Entities/ConsumoBootstrap.cs` | Registra que uma unidade já usou o token de ambiente |
| `backend/Domain/Entities/TokenDeCadastro.cs` | Token de 30 min: hash, expiração e uso único |
| `backend/Domain/Entities/Usuario.cs` | `AdminSecretaria` passa a aceitar `UnidadeId` como atribuição |
| `backend/Domain/Interface/IConsumoBootstrapRepository.cs` | Checagem prévia de consumo por unidade |
| `backend/Domain/Interface/ITokenDeCadastroRepository.cs` | Busca por hash e por unidade |
| `backend/Domain/Interface/IEscolaRepository.cs` | `BuscarUnidadesPorNome` (nome exato, sem caixa) |
| `backend/Infrastructure/Repositories/ConsumoBootstrapRepository.cs` | Persistência do consumo |
| `backend/Infrastructure/Repositories/TokenDeCadastroRepository.cs` | Persistência dos tokens |
| `backend/Infrastructure/DataBase/ConnectionContext.cs` | DbSets, índices únicos e FKs |
| `backend/API/Services/BootstrapService.cs` | Decide as duas vias, emite token e cria o admin |
| `backend/API/Controllers/BootstrapController.cs` | `status`, `admin`, `GET/POST token` |
| `backend/Application/CriarAdminBootstrapDTO.cs` | Entrada, com `Unidade` opcional |
| `backend/Application/TokenDeCadastroDTOs.cs` | `TokenDeCadastroGerado` e `TokenDeCadastroAtual` |
| `backend/Program.cs` | DI das novas dependências e `TimeProvider.System` |
| `frontend/src/services/BootstrapApi.ts` | Status, criação, emissão e consulta de token |
| `frontend/src/pages/comum/BootstrapAdmin.tsx` | Página anônima com token e unidade |
| `frontend/src/pages/secretaria/HomeSecretaria.tsx` | Card que gera o token, com contagem e bloqueio |
| `frontend/src/pages/secretaria/Unidades.tsx` | Lista as unidades da rede (sem criação: nasce no bootstrap) |
| `frontend/src/services/EscolaApi.ts` | Funções de unidade restauradas |
| `backend.Tests/Domain/TokenDeCadastroTests.cs` | Validade e uso único no domínio |
| `backend.Tests/Integration/BootstrapApiTests.cs` | Uma vez por unidade |
| `backend.Tests/Integration/BootstrapTokenApiTests.cs` | Token de 30 min, expiração e uso único |

`ConfiguracaoBootstrap`, `IConfiguracaoBootstrapRepository`,
`ConfiguracaoBootstrapRepository` e `DefinirBootstrapDTO` foram **removidos** na v2.

## Modelo de dados

```text
ConsumosBootstrap
  Id, UnidadeId (único), ConsumidoEm
  FK UnidadeId → Unidades (Restrict)

TokensDeCadastro
  Id, UnidadeId, Hash (único), CriadoPorUsuarioId,
  CriadoEm, ExpiraEm, UsadoEm (nulo = não usado)
  FK UnidadeId → Unidades (Restrict)
  FK CriadoPorUsuarioId → Usuarios (Restrict)
  índice (UnidadeId, ExpiraEm) para a consulta do painel
```

`Usuario`: `Papel = AdminSecretaria`, `EscolaId = null`, `UnidadeId` da origem. A unidade
não entra em `EscopoUsuario.Global`, portanto não altera visibilidade de dados.

## Fluxo de criação

```text
Bootstrap:Permitir=false?            sim → 403 "Criação de administrador desativada."
token confere com Bootstrap:Token?   sim → via do token de ambiente
  unidade informada?                  não → 400
  unidade existe (nome exato)?        não → CRIA a unidade e segue
  unidade ativa?                      não → 400
  unidade já consumiu?                sim → 409
  e-mail repetido?                    sim → 409
  senão → cria admin + registra consumo → 201

senão → procura hash do token informado
  inexistente, expirado ou usado?     sim → 404 se não há nenhuma via; senão 403
  unidade do token ativa?             não → 403
  e-mail repetido?                    sim → 409
  senão → cria admin na unidade do token + marca uso → 201
```

O `404` só aparece quando não há token de ambiente **e** nenhum token temporário válido:
um deploy sem configuração não revela a existência da rota.

## Emissão do token de 30 minutos

```text
AdminSecretaria?        não → 403
conta tem unidade?      não → 403
já existe token válido? sim → 409 (o valor antigo não é recuperável: só o hash existe)
senão → 32 bytes aleatórios → base64url
        hash SHA-256 gravado; valor devolvido uma única vez em 201 { token, expiraEm }
```

A entidade revalida no `MarcarUsado`: entre a consulta e a gravação o relógio pode ter
passado da expiração ou outra requisição pode ter consumido o token.

## Comparação e armazenamento

- Token de ambiente: `CryptographicOperations.FixedTimeEquals`, com checagem de tamanho
  antes (vaza apenas o comprimento).
- Token temporário: comparado pelo hash SHA-256 (`Convert.ToHexString`), com índice único.
  O valor em claro existe somente na resposta de emissão.

## Relógio

`TimeProvider` é injetado (`AddSingleton(TimeProvider.System)`) e toda decisão de tempo
usa `GetUtcNow()`. Isso permite testar a expiração de 30 minutos avançando um relógio
falso, sem esperar. O relógio falso dos testes parte da hora real: o cookie de sessão
também usa `TimeProvider` e uma data fixa no passado invalidaria o ticket.

## Frontend

`/bootstrap` continua **fora** do `ProtectedRoutes` e sem item de menu. A página consulta
`/api/bootstrap/status`:

- `disponivel: false` → explica o motivo (`sem-token` ou `desativado-ambiente`) e não
  mostra o formulário;
- `disponivel: true` → formulário com token, unidade (opcional, obrigatória só para o
  token de ambiente) e dados; após sucesso, link para `/login`.

O painel `/secretaria` consulta `GET /api/bootstrap/token`, desabilita o botão enquanto
`ativo` for verdadeiro, mostra a contagem regressiva em `mm:ss` e, ao expirar, libera
novamente — sem nova requisição, porque a expiração é derivada de `expiraEm`.

## Decisões

| # | Decisão | Motivo |
| --- | --- | --- |
| D1 | Unidade informada por nome exato, sem rota pública de listagem | `/bootstrap` é anônima; listar unidades vazaria a rede |
| D2 | Unidade é da rede, nome único (ADR-013) | Nome único por escola tornaria a digitação ambígua e exigiria conhecer a escola |
| D3 | Um token válido por unidade (409 no segundo pedido) | Só o hash é guardado; emitir outro invalidaria o anterior sem o usuário saber |
| D4 | Token temporário é de uso único | "Criação de um usuário"; reduz a janela de abuso a uma conta |
| D5 | `Bootstrap:Permitir=false` mantido como trava dura | Operação precisa desligar tudo sem remover configuração |
| D6 | Consumo só é gravado na via do token de ambiente | O token temporário não pode queimar o token de configuração da unidade |
| D7 | `AdminSecretaria` aceita unidade | É atribuição de origem, não escopo; restringir exigiria papel novo |
| D8 | `Forbid()` evitado no controller | O challenge do cookie trocaria a mensagem do serviço por "Acesso negado." |

## Riscos

| Risco | Mitigação |
| --- | --- |
| Token de ambiente vazado cria um admin por unidade ainda livre | Rotacionar/remover `Bootstrap__Token` após configurar a rede; log de toda criação |
| Token temporário compartilhado fora do prazo | Expira em 30 min, uso único e fica preso à unidade |
| Nome de unidade ambíguo | Erro 400 explícito em vez de escolha silenciosa |
| Deploy esquece o token | Rota responde 404; a página informa o que configurar |
| Admin criado com senha fraca | `Usuario.DefinirSenha` aplica a regra do resto do sistema (400) |

## Limitação conhecida

A criação grava **dois agregados** (usuário e consumo) em dois `SaveChanges`. Não há
transação única: uma falha entre eles cria o admin sem registrar o consumo, permitindo um
segundo admin para a mesma unidade. O índice único impede o inverso (dois consumos). A
correção definitiva é uma unidade de trabalho transacional, fora do escopo desta spec.

Duas bordas relacionadas, aceitas e registradas:
- **Unidade órfã em validação:** na via de ambiente a unidade é criada antes do usuário;
  se a criação do admin falhar por validação do domínio (senha curta, CPF, data), a
  unidade recém-criada permanece. O retry com o mesmo nome a reaproveita.
- **Concorrência no mesmo token de ambiente:** duas requisições simultâneas podem passar
  pela checagem de consumo e criar dois admins antes de a segunda registrar o consumo; a
  gravação duplicada é barrada pelo índice único, mas responde `500` em vez de `409`.
