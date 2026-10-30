# Pending Items

Canonical list of open follow-ups that are not planned roadmap specs.
Scope: loose bugs, test flakiness, tech debt, and cross-session follow-ups.

## How to use

- Each item has a stable ID (`PEND-XXX`), status, and observable evidence.
- Status: `open` | `in-progress` | `done` | `discarded`.
- When resolved, move to `done` with commit/evidence — never delete the line.
- Source of truth for specs is `management/roadmap.md`.

## Open

Registrados em 2026-10-27 a partir da revisão técnica (`revisao-codigo.md`). São achados
que **nenhuma spec cobre** ou que atravessam mais de uma spec. Revisados em
2026-10-30, quando 009–019 foram especificadas.

| ID | Date | Status | Title | Evidence | Next action |
|---|---|---|---|---|---|
| PEND-001 | 2026-10-27 | open | IDOR com exposição de CPF: `GET /api/usuario/perfil/{id}` é anônimo e não valida dono | `UsuarioController.cs:33-42` — sem `[Authorize]`; devolve Nome, CPF, DataNascimento, Email, Telefone, Foto via `UsuarioService.ObterUsuarioPorId` | `[Authorize]` + comparar id da rota com id da sessão, como já feito em `AtualizarPerfil` (`:139-156`). Candidatas: Spec 006 e Spec 009 |
| PEND-002 | 2026-10-27 | open | `Usuario` possui construtores que produzem objeto inválido | `Usuario.cs:12-19` — `Usuario(string email)` e vazio público deixam nome, CPF e senha nulos | Spec 009 reduz a um construtor público de criação e torna o vazio privado |
| PEND-003 | 2026-10-27 | open | Criação de usuário não valida domínio; atualização valida — cadastro inválido que nunca pode ser editado | `UsuarioService.cs:14-31` valida só e-mail duplicado e tamanho da senha; `Usuario.cs:35-56` valida nome e formato de e-mail | Mover as invariantes para o construtor (Spec 009) e reutilizá-las em criar e atualizar (ADR-004) |
| PEND-006 | 2026-10-27 | open | `AlunoApi.ts` não envia `credentials: "include"`; quebrará assim que `[Authorize]` entrar | `AlunoApi.ts:21,31,39`; `arch.md` §3 trata a ausência como defeito | Adicionar `credentials` na mesma mudança da Spec 006 |
| PEND-007 | 2026-10-27 | open | `DadosAlunoDTO` serve a entrada e saída e carrega dados de saúde (`Anafilaxia`, `Restricoes`) | `DadosAlunoDTO.cs`; `001/design.md` §Decisões justifica "não contém dado sensível" — justificativa incorreta | Separar DTOs de entrada e saída ao criar contratos novos (Specs 009 e 012) ou abrir exceção explícita em `arch.md` §5 |
| PEND-010 | 2026-10-30 | open | `/colors` (paleta de teste) pública fora do `ProtectedRoutes`; duplicata de `/cardapio` já não existe | `routes.tsx:37` expõe `/colors`; a duplicata de `/cardapio` foi verificada como inexistente em 2026-10-30 | Remover `/colors` das rotas de produção — candidata à Spec 008 |
| PEND-011 | 2026-10-27 | open | `buscarEstatisticas` chama endpoint inexistente e com prefixo errado | `Perfil.ts:13-19` — `/usuarios/${id}/estatisticas` (sem `/api/`) | Remover ou implementar o endpoint correspondente — candidata à Spec 008 |
| PEND-012 | 2026-10-27 | open | Código morto restante no backend | Resolvido em 2026-10-28: `CriarAluno` virou `void` e o branch inalcançável saiu de `AlunoController`; `UpdateAluno` deixou de ser morto com o `PUT /api/aluno/{id}`. Restam `UsuarioService.TrocarSenha` (sem endpoint) e `AlterarSenhaDTO.cs` (sem consumidor) | Remover os dois ou expor o endpoint de troca de senha — candidato à Spec 002 |
| PEND-014 | 2026-10-28 | open | Dois recursos de cardápio ficaram sem UI; estado e handler removidos para destravar o build | `Cardapio.tsx`: `handleExcluirCardapio` nunca era chamado e `setResponsavel` nunca era atribuído — o bloco de responsável técnico era inalcançável e não havia fonte de dados para `Responsavel` | Implementar quando a tela de cardápio for finalizada: botão de excluir refeição e exibição do responsável técnico |
| PEND-018 | 2026-10-28 | open | Persistência SQLite exercitada apenas em memória; o arquivo real não é testado | `ConnectionContextTests` valida só o modelo; a partir de 2026-10-30, `Integration/ApiFactory` abre SQLite em memória e os 12 testes de integração gravam e leem alunos e usuários. Nenhum teste abre `database.db` | Verificar com o app no ar: cadastrar aluno com restrições, reiniciar, ler `GET /api/aluno` e conferir as colunas |

## Resolved

| ID | Date | Status | Title | Evidence |
|---|---|---|---|---|
| PEND-008 | 2026-10-28 | done | `.dscode/` fora do git | `.dscode/` adicionado ao `.gitignore`; `git rm -r --cached .dscode` retirou os 4 arquivos do índice (`git ls-files .dscode` → 0); arquivos preservados no disco. Exposição no histórico: ver PEND-013 |
| PEND-009 | 2026-10-28 | done | `.env.example` versionado | Criado `frontend/.env.example` com `VITE_API_URL=http://localhost:5001` e instrução de cópia para `frontend/.env` |
| PEND-015 | 2026-10-28 | done | `npm run build` do frontend falhava por 5 erros `TS6133` pré-existentes | `Perfil.tsx` (`carregando`, `salvando`, `atualizarCampo`) e `Cardapio.tsx` (`setResponsavel`, `handleExcluirCardapio`). Os três primeiros foram ligados à UI em vez de removidos: `atualizarCampo` substituiu 3 `onChange` duplicados e agora limpa `erro`/`mensagem`; `carregando` corrige o texto "Carregando perfil..." eterno quando a carga falha; `salvando` desabilita o botão e troca o rótulo. `npx tsc -b` → exit 0 |
| PEND-016 | 2026-10-28 | done | `dotnet build`/`dotnet restore` falhavam no shell do agente | Causa raiz: o shell não exporta `APPDATA` nem `ProgramFiles`; o NuGet monta os caminhos de configuração do usuário e da máquina com `Path.Combine`, que lança `ArgumentNullException("path1")` quando a variável falta. Workaround validado em 2026-10-28: `env APPDATA='C:\Users\Kayke\AppData\Roaming' ProgramFiles='C:\Program Files' dotnet test backend.Tests/backend.Tests.csproj` → restore, build e 80 testes verdes |
| PEND-017 | 2026-10-28 | done | Requisições de aluno respondem 400 para payload inválido e 404 no formato `{ erro }` | `AlunoController` envolve `CriarAluno`/`AtualizarAluno` em `catch (ArgumentException)` → `BadRequest(new { erro = erro.Message })`; `backend.dll` recompilado sem erro CS em 2026-10-28 20:20; README atualizado |
| PEND-013 | 2026-10-28 | done | Chave de API exposta no histórico do GitHub | Histórico reescrito: `627e302` → `d431349`, sem `.dscode`; `branch-kayke` e `main` atualizados por `--force-with-lease` e conferidos (`git log --all -- .dscode` vazio). Chave antiga revogada e substituída no provedor pelo dono em 2026-10-28. O objeto antigo pode sobreviver em clones já sincronizados até o GC do GitHub, mas a credencial está inválida |
| PEND-021 | 2026-10-30 | done | Índice canônico de specs inexistente | `roadmap-plan-commit` recusou a primeira tentativa por qualidade (Spec 007 sem bloco de visão e sem seção no `arch.md`), não pela numeração. Criados V8 e a seção `Spec 007`; a segunda chamada commitou **19 linhas** em `management/roadmap.md`. A faixa 001–019 é aceita pelo pipeline |
| PEND-004 | 2026-10-30 | done | `UsuarioRepository` instanciava o próprio `DbContext` | Passou a receber `ConnectionContext` no construtor (`UsuarioRepository.cs:9-14`), como `AlunoRepository`. Validação: os 12 testes de integração injetam SQLite em memória e persistem via repositório |
| PEND-019 | 2026-10-30 | done | Suíte não cobria a API HTTP | `backend.Tests/Integration` com `ApiFactory`, `AlunoApiTests` (6) e `UsuarioApiTests` (6); `dotnet test` → **152/152** |
| PEND-020 | 2026-10-30 | done | Migration `HashSenhaUsuario` não existia | `dotnet ef migrations add HashSenhaUsuario` com EF Tools 10.0.12; migration documental vazia e comentada; `dotnet build` sem erro |
| PEND-022 | 2026-10-30 | done | Specs 006–008 sem documentos | Criados `requirements.md`, `design.md` e `task.md` para 006 (autorização de aluno), 007 (contrato de erro/DI) e 008 (higiene de frontend e documentação viva); 9 arquivos, todos com `status: created` |
| PEND-023 | 2026-10-30 | done | Gates de frontend não executavam | `npm install` (51 pacotes); `npm run build` OK (1846 módulos); `npm run lint` 0 erros e 5 avisos; `frontend/.env` criado a partir de `.env.example` (ignorado pelo git) |
| PEND-005 | 2026-10-30 | discarded | Frontmatter das specs declararia `status: created`, retirado do fluxo | **Falso positivo.** `templates/prompts/spec_new.md.ejs:125` define `status: created` como o valor que o próprio `/spec-new` escreve no frontmatter. O ciclo de vida (`planned → verified → implemented → audited`) vive no `roadmap.md`, não no frontmatter. Nenhum dos 57 arquivos precisa mudar |
