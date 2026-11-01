---
name: bootstrap-admin-por-token
status: created
references: V11, V6, ADR-002, ADR-004, ADR-013, ADR-014
---

# Requirements — Bootstrap de administrador por token (v2)

## Objetivo

Criar `AdminSecretaria` sem editar o banco, com **duas vias de token**:

1. **token de ambiente** (`Bootstrap:Token`) — uso único **por unidade**;
2. **token temporário** — emitido no painel da secretaria, válido por **30 minutos** e
   de uso único.

O administrador criado mantém o papel `AdminSecretaria` (escopo global) e recebe a
**unidade de origem** como atribuição. A unidade não restringe dados.

## Requisitos funcionais

- **RF1 — Status**: `GET /api/bootstrap/status` devolve `{ disponivel, motivo }`, com
  `motivo` em `ok`, `sem-token` ou `desativado-ambiente`. Não revela token, unidades nem
  quantos administradores existem.
- **RF2 — Token de ambiente, uma vez por unidade**: `POST /api/bootstrap/admin` com o
  token de `Bootstrap:Token` exige o **nome exato** de uma unidade cadastrada e ativa.
  A unidade só pode consumir esse token **uma vez**; a segunda tentativa responde `409`.
  Unidades distintas podem usar o mesmo token, cada uma uma vez.
- **RF3 — Unidade criada no bootstrap**: a página `/bootstrap` exige o nome da unidade.
  Se a unidade não existir, ela é **criada** na mesma operação do primeiro administrador;
  se existir, o admin é atribuído a ela. Não existe rota de criação de unidade
  (`POST /api/unidades` removido) — a tela `/secretaria/unidades` só lista, renomeia,
  ativa/desativa e exclui. Nome inexistente deixou de ser `400`; nome já consumido pelo
  token de ambiente continua `409`.
- **RF4 — Token temporário**: `POST /api/bootstrap/token` (só `AdminSecretaria`) emite um
  token de 30 minutos, preso à unidade de quem gerou. Responde `201 { token, expiraEm }`
  **uma única vez**; o banco guarda apenas o hash SHA-256.
- **RF5 — Um token válido por unidade**: com um token ainda válido, novo `POST` responde
  `409`. O painel desabilita o botão e mostra contagem regressiva.
- **RF6 — Expiração**: passados 30 minutos o token deixa de valer (`403` no uso),
  `GET /api/bootstrap/token` informa `ativo: false` e o botão volta a permitir gerar
  outro token.
- **RF7 — Uso único**: um token temporário cria no máximo um administrador; a segunda
  tentativa responde `403`. A unidade do novo admin é a do token — o corpo não a informa.
- **RF8 — Painel**: a tela `/secretaria` mostra o estado do token, gera o token de 30
  minutos, exibe o valor uma única vez com botão de copiar e contagem regressiva, e mantém
  o botão desabilitado enquanto o token estiver válido.
- **RF9 — Trava de ambiente**: `Bootstrap:Permitir=false` nega as duas vias
  (`403 Criação de administrador desativada.`); ausente equivale a `true`; valor não
  booleano equivale a `false`, com erro no log (falha fechada).
- **RF10 — Rota oculta**: sem token de ambiente **e** sem token temporário válido, o
  `POST /api/bootstrap/admin` responde `404 Rota não encontrada.`
- **RF11 — E-mail duplicado**: `409 E-mail já cadastrado.`
- **RF12 — Auditoria**: toda criação, toda emissão de token e toda recusa por token
  inválido registram `LogWarning` sem senha e sem o valor do token.
- **RF13 — Tela de unidades**: a secretaria precisa listar as unidades da rede. A
  **criação** não fica na tela: acontece no bootstrap do primeiro administrador (RF3).

## Requisitos não funcionais

- **RNF1 — Token nunca na URL**: viaja no corpo `POST`; URL aparece em histórico, log de
  acesso e cabeçalho `Referer`.
- **RNF2 — Comparação em tempo constante** para o token de ambiente
  (`CryptographicOperations.FixedTimeEquals`). O token temporário é comparado por hash
  SHA-256 com índice único.
- **RNF3 — Sem segredo no repositório**: o token de ambiente só existe em configuração.
- **RNF4 — Reutiliza o domínio**: `Usuario` valida nome, CPF, data de nascimento, e-mail e
  senha; a rota não tem regra própria de validação.
- **RNF5 — `AdminSecretaria` aceita unidade**: a unidade é atribuição de origem, não escopo
  de autorização. `Nutricionista`, `AdminEscola` e os demais continuam recusando unidade.

## Decisão de risco (explícita)

O token de ambiente **não expira**: vale para sempre, mas só uma vez por unidade. Quem o
obtiver cria um administrador em cada unidade ainda livre. A mitigação operacional é
rotacionar ou remover `Bootstrap__Token` depois de configurar a rede, e usar o token de
30 minutos para os administradores seguintes.

## Critérios de aceitação

- [x] Token de ambiente cria um `AdminSecretaria` com `unidadeId` preenchido e login
      funcional.
- [x] Mesma unidade duas vezes → `409`; outra unidade → `201`.
- [x] Unidade desativada ou sem unidade informada → `400`; unidade nova é criada junto
  com o administrador.
- [x] `POST /api/bootstrap/token` → `201` com expiração de 30 minutos; segundo pedido
      com token válido → `409`.
- [x] Token temporário cria admin na unidade do autor; reutilização → `403`.
- [x] Após 30 minutos o token expira (`403` no uso) e um novo token pode ser gerado.
- [x] `GET /api/bootstrap/token` reflete `ativo` e `expiraEm`.
- [x] `POST /api/bootstrap/token` anônimo → `401`; papel não administrativo → `403`;
      conta sem unidade → `403`.
- [x] `Bootstrap__Permitir=false` → `motivo: desativado-ambiente` e `403` nas duas vias.
- [x] Sem token de ambiente e sem token temporário → `404` na criação.
- [x] Tela de unidades disponível em `/secretaria/unidades`, sem criação e sem vínculo
  com escola.
- [x] `dotnet test` verde; `npm run build` e `npm run lint` sem erro.
