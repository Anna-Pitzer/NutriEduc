<!-- AI-ONLY. Durable cross-session memory. Never shown to end users. Keep entries minimal and factual. -->

# Cross-Session Memory

Decisions, rationale, and progress that must survive across sessions live here;
conversation history alone is not durable memory.

## 2026-10-30 — Estado do planejamento SDD

- `implementacoes.md` tem 128 funcionalidades em cinco blocos (Nutrição 39,
  Merendeiras 24, Pais 18, Secretaria 24, Escolas 23).
- Partição em 11 specs novas: 009 (46), 010 (3 + base para 2 da 017), 011 (10),
  012 (17), 013 (3), 014 (4), 015 (9), 016 (10), 017 (10), 018 (7), 019 (8);
  001 cobre 1. Soma = 128 (L12).
- Todas as 11 têm `requirements.md`, `design.md` e `task.md` com `status: created`.
- Ordem de execução derivada do grafo, não da numeração: `012 → 014 → 013 → 015`
  (L13). Cadeia completa: `009 → 010 → 011 → 012 → 014 → 013 → 015 → 016 → 017 →
  018 → 019`.
- 006–008 têm os três documentos e nenhuma task executada; a implementação segue a
  ordem `006 → 007 → 008` antes da 009.

## Decisões estruturais registradas

- ADR-007: escola/unidade são chaves de autorização; `Aluno.UnidadeId` anulável.
- ADR-008: desativação em vez de exclusão para cadastros referenciados.
- ADR-009: histórico append-only com snapshots de autor e alvo.
- ADR-010: concorrência otimista por `[ConcurrencyCheck]` (SQLite sem rowversion).
- ADR-011: compatibilidade é função pura com severidade única (usada por 013/014/015).
- ADR-012: relatório é tabela; exportadores são plugáveis e não consultam banco.

## Bloqueios conhecidos

- `management/roadmap.md` é a única fonte de verdade do planejamento, com 21 linhas
  `planned`; `PLANEJAMENTO.md` foi descartado. A promoção de status é feita por
  `/spec-new`, `/spec-implement` e `/spec-test`.
- Frontmatter `status: created` é o valor que o próprio `/spec-new` grava
  (`templates/prompts/spec_new.md.ejs:125`); o ciclo de vida vive no roadmap.
- `frontend/node_modules` instalado (51 pacotes) e `frontend/.env` criado localmente
  (ambos ignorados pelo git).

## Estado do código (verificado em 2026-10-31)

- `dotnet test backend.Tests`: 236/236 verdes (156 unitários + 80 de integração HTTP).
- Gates de frontend passam: `npm run build` OK e `npm run lint` com 0 erros.
- 7 controllers com rota (inclui `BootstrapController`) + `RespostaController` base;
  8 migrations (a última é `BootstrapPorUnidade`).
- Specs 001–009 e 020–022: tasks executadas (a 009 com T13 transferida para a 021).
  Especificadas e não implementadas: 010–019.

## Bootstrap v2 (2026-10-31)

- **Unidade é da rede, não da escola (ADR-013).** `UnidadeEscolar.EscolaId` é opcional
  (dado legado) e o nome é único na rede. `/api/escolas/{id}/unidades` foi substituído por
  `GET`/`POST /api/unidades`; a tela `/secretaria/unidades` não pede escola.
- `ConfiguracaoBootstrap` (interruptor booleano) foi **removida**. O painel agora emite um
  token de **30 minutos**, de uso único, preso à unidade de quem gerou (`TokensDeCadastro`).
- O token de ambiente (`Bootstrap__Token`) vale **uma vez por unidade**, registrado em
  `ConsumosBootstrap`; a unidade é informada pelo nome exato e precisa existir.
- `AdminSecretaria` passou a aceitar `UnidadeId` como **atribuição**, não escopo — o papel
  continua global. `Nutricionista` e `AdminEscola` seguem recusando unidade.
- Efeito colateral em **PEND-024**: sem `Aluno.EscolaId`, o escopo de aluno de
  `AdminEscola` ficou limitado a unidades legadas com escola; os testes de vínculo passaram
  a usar `AdminSecretaria`.
- **Unidade nasce no bootstrap (2026-10-31).** `POST /api/unidades` e
  `EscolaService.CriarUnidade` foram removidos: o token de ambiente cria a unidade junto
  com o primeiro administrador (`ResolverOuCriarUnidade`). A tela `/secretaria/unidades`
  só lista, renomeia, ativa/desativa e exclui.
- **Exclusão definitiva (ADR-014).** `DELETE /api/escolas/{id}`, `/api/unidades/{id}` e
  `/api/usuarios/{id}`, sempre com bloqueio por referência (`409`): escola com usuários ou
  unidade legada; unidade com usuário, aluno, token ou consumo de bootstrap; usuário com
  própria conta, último `AdminSecretaria`, vínculo de responsável ou token emitido. A
  checagem vive em `ExclusaoService`.
- `TimeProvider` é injetado no backend; o relógio falso dos testes parte da hora real
  porque o cookie de sessão também usa `TimeProvider`.
- Limitação conhecida: a criação grava usuário e consumo em dois `SaveChanges` (sem
  transação única), registrada no `design.md` da 022.

## Spec 016 — auditoria técnica concluída; validação manual pendente

- Backend + frontend de ocorrências implementados. Gates: `dotnet test` 703/703,
  `npm run build` OK, `npm run lint` 0 erros (6 avisos de react-hooks pré-existentes).
- **RNF3 (snapshot de nomes):** `Ocorrencia` grava `AlunoNome`, `CardapioNome`,
  `RefeicaoNome` e `AlimentoNome` no momento do registro (ADR-009 — "cópia dos nomes
  relevantes do alvo"). Renomear/excluir aluno, cardápio, refeição ou alimento não
  reescreve o que a ocorrência exibe; a FK vira `null` e o nome é preservado. O
  `design.md` da 016 omite esses campos (omissão, não conflito).
- **Fuso do cancelamento (RF9):** "mesmo dia" = dia civil de `America/Sao_Paulo`
  (UTC-3), decidido pelo dono. `Ocorrencia.Cancelar` usa `TimeZoneInfo`; `OcorrenciaService`
  injeta `TimeProvider`. Testes cobrem a fronteira UTC×local.
- **Status:** permanece `created` no roadmap. **Não promovido a `audited`** porque
  (1) validação manual no navegador está pendente (delegada a outro integrante) e
  (2) **PEND-028** bloqueia o motor SDD (`normalizeSpecNumber`/`parseRoadmap` comparam
  a célula crua `"016"` com `"16"`); a correção é do binário `dscode.exe`, externa ao
  repositório. Não alterei o status manualmente.
- **Próximo passo:** validação manual por papel (checklist no relatório de auditoria) e,
  após a correção do PEND-028, promover via `/spec-implement` → `/spec-test`.

## Spec 033 — backend implementado; frontend e validação manual pendentes

- **Base Git:** `branch-pedro` fez fast-forward para `origin/main` (`86d1842`); a 018 já
  estava mergeada (PR #13).
- **Backend completo (2026-11-11):** `SolicitacaoDeAcesso` + `StatusSolicitacaoAcesso` +
  `TipoCadastroSolicitado` + `PoliticaDeAprovacao` (pura), `RegistrarAcessoService`,
  `AprovarAcessoService`, `SolicitacaoAcessoController` (`/api/solicitacoes-acesso`),
  migration `20261111004628_SolicitacoesDeAcesso` (índice único filtrado de pendente +
  índice de fila + `Status` como token de concorrência). `UsuarioController.PostAdd` agora
  aceita `CadastroEntrada` (`tipo`,`unidadeId`,`crn`). Login de conta sem papel virou
  `401` genérico ("Email ou senha incorretos."). CPF: 11 dígitos no domínio + unicidade no
  serviço (409), **sem índice no banco** (decisão D7).
- **Decisões do dono (2026-11-11):** CPF único no serviço (sem índice); cadastro público
  anônimo, fora da matriz (a matriz mantém só `SolicitacaoDeAcesso → AdminSecretaria`).
- **Gates:** `dotnet test` **775/777** (41 testes novos: domínio, serviço, integração com
  rollback e concorrência). As 2 falhas restantes são pré-existentes e de fronteira de
  fuso — `OcorrenciaApiTests.Cancelar_NoMesmoDia_PeloAutor_200` e
  `Cancelar_NoDiaSeguinte_409` — falham perto da meia-noite UTC×`America/Sao_Paulo`;
  não têm relação com a 033. `npm run build` OK; `npm run lint` 0 erros (6 avisos
  pré-existentes). Workaround `env APPDATA='C:\Users\pv994\AppData\Roaming'
  ProgramFiles='C:\Program Files' dotnet ...` resolve o `path1` do NuGet (PEND-016).
- **Decisão do dono (2026-11-11, 2ª rodada):** aprovada a consulta pública mínima
  `GET /api/unidades/ativas` (anônima, apenas `id` e `nome`, só ativas) para o cadastro; a
  rota administrativa `GET /api/unidades` continua autenticada. O backend revalida a unidade
  (existente e ativa) no `RegistrarAcessoService`.
- **Frontend entregue (2026-11-11):** cadastro em 2 etapas (`login.tsx` + `authContext`),
  fila `SolicitacoesAcesso.tsx` (listar/aprovar/recusar) + rota `/secretaria/solicitacoes-acesso`
  + aba no menu, e remoção da sugestão `Merendeira` (`papelInicial`) em `Usuarios.tsx`.
  Reconsideração: endpoint `POST /api/solicitacoes-acesso` existe; a UI de reconsideração
  fica para a validação manual.
- **Correção fora da 033 (016):** `Ocorrencia.CriadoEmUtc`/`AtualizadoEmUtc` eram relidos do
  SQLite como `DateTimeKind.Unspecified`; `NormalizarUtc` tratava como hora local e deslocava
  a data na fronteira 00:00–03:00 UTC. Corrigido com `HasConversion(... DateTimeKind.Utc)` no
  `ConnectionContext` (mesmo padrão de `TrilhaAcao`). Os 2 testes de cancelamento passaram a
  ser determinísticos sem enfraquecer a regra `America/Sao_Paulo`.
- **Auditoria final (2026-11-11):** **T13 parcialmente indisponível** — reconsideração
  (`POST /api/solicitacoes-acesso`) entregue e testada; o diagnóstico de legado global
  (`GET /api/usuarios/diagnostico-sem-solicitacao`) foi **desabilitado** porque a leitura
  **global** de nome+e-mail de órfãos conflita com ADR-021/028 ("não existe perfil global").
  Rota removida de `UsuarioAdminController` (→ 4xx); preservados
  `AprovarAcessoService.DiagnosticarSemSolicitacao`, `ListarSemPapel` e `ExistePorUsuario`
  para reabilitação futura. Conflito registrado como **PEND-046** (decisão pendente do grupo).
  Adicionados testes: reconsideração (nova solicitação após recusa + pendente duplicada 409),
  concorrência de responsável sem vínculo duplicado, rollback transacional da aprovação
  (usuário/solicitação/papel/vínculo/trilha/notificação) e regressão determinística do fuso
  (round-trip SQLite em outro contexto + cancelamento na fronteira UTC/`America/Sao_Paulo`).
  `dotnet test` **787/787**; `npm run build`/`lint` OK.
- **Limitação documentada (CPF/D7):** unicidade só no serviço, sem índice no banco. SQLite
  serializa escritas, então duas operações que consultam antes de gravar podem ambas passar a
  checagem — não há garantia de unicidade em corrida real.
- **Não promovido** (`created` no roadmap); status formal é do motor SDD (`/spec-test`), não
  da edição manual. Validação manual no navegador é de outro integrante.
