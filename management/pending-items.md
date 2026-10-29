# Pending Items

Canonical list of open follow-ups that are not planned roadmap specs.
Scope: loose bugs, test flakiness, tech debt, and cross-session follow-ups.

## How to use

- Each item has a stable ID (`PEND-XXX`), status, and observable evidence.
- Status: `open` | `in-progress` | `done` | `discarded`.
- When resolved, move to `done` with commit/evidence — never delete the line.
- Source of truth for specs remains `management/roadmap.md`; enquanto ele não existir,
  `management/PLANEJAMENTO.md`.

## Open

Registrados em 2026-10-27 a partir da revisão técnica (`revisao-codigo.md`). São achados
que **nenhuma das 12 specs cobre** ou que atravessam mais de uma spec.

| ID | Date | Status | Title | Evidence | Next action |
|---|---|---|---|---|---|
| PEND-001 | 2026-10-27 | open | IDOR com exposição de CPF: `GET /api/usuario/perfil/{id}` é anônimo e não valida dono | `UsuarioController.cs:33-42` — sem `[Authorize]`; devolve Nome, CPF, DataNascimento, Email, Telefone, Foto via `UsuarioService.ObterUsuarioPorId` | `[Authorize]` + comparar id da rota com id da sessão, como já feito em `AtualizarPerfil` (`:139-156`) |
| PEND-002 | 2026-10-27 | open | `Usuario` possui construtores que produzem objeto inválido | `Usuario.cs:12-19` — `Usuario(string email)` e vazio público deixam nome, CPF e senha nulos | Reduzir ao construtor completo; trocar os demais por método nomeado ou fábrica; tornar o vazio `private` se o EF exigir |
| PEND-003 | 2026-10-27 | open | Criação de usuário não valida domínio; atualização valida — cadastro inválido que nunca pode ser editado | `UsuarioService.cs:14-31` valida só e-mail duplicado e tamanho da senha; `Usuario.cs:35-56` valida nome e formato de e-mail | Mover as invariantes para o construtor e reutilizá-las em criar e atualizar (ADR-004) |
| PEND-004 | 2026-10-27 | open | `UsuarioRepository` instancia o próprio `DbContext` | `UsuarioRepository.cs:9` — `new ConnectionContext()`; contraria `arch.md` §5 e a lição L4 | Injetar `ConnectionContext` pelo construtor, como `AlunoRepository.cs:10-13`. Candidato: Spec 007 |
| PEND-005 | 2026-10-27 | open | Frontmatter das specs 001–005 declara `status: created`, retirado do fluxo SDD vigente | 15 arquivos em `management/specs/` (requirements, design, task de cada spec) | Normalizar via `/spec-new <n>` (→ `verified`) antes de `/spec-implement` |
| PEND-006 | 2026-10-27 | open | `AlunoApi.ts` não envia `credentials: "include"`; quebrará assim que `[Authorize]` entrar | `AlunoApi.ts:21,31,39`; `arch.md` §3 trata a ausência como defeito | Adicionar `credentials` na mesma mudança da Spec 006 |
| PEND-007 | 2026-10-27 | open | `DadosAlunoDTO` serve a entrada e saída e carrega dados de saúde (`Anafilaxia`, `Restricoes`) | `DadosAlunoDTO.cs`; `001/design.md` §Decisões justifica "não contém dado sensível" — justificativa incorreta | Separar `CriarAlunoDTO`/`AlunoResponseDTO` ou abrir exceção explícita em `arch.md` §5 |
| PEND-010 | 2026-10-27 | open | Rota `/cardapio` duplicada e `/colors` (paleta de teste) pública fora do `ProtectedRoutes` | `routes.tsx:28` e `:30` duplicam `/cardapio`; `:19` expõe `/colors` | Remover a duplicata e retirar `/colors` das rotas de produção |
| PEND-011 | 2026-10-27 | open | `buscarEstatisticas` chama endpoint inexistente e com prefixo errado | `Perfil.ts:13-19` — `/usuarios/${id}/estatisticas` (sem `/api/`) | Remover ou implementar o endpoint correspondente |
| PEND-012 | 2026-10-27 | open | Código morto restante no backend | Resolvido em 2026-10-28: `CriarAluno` virou `void` e o branch inalcançável saiu de `AlunoController`; `UpdateAluno` deixou de ser morto com o `PUT /api/aluno/{id}`. Restam `UsuarioService.TrocarSenha` (sem endpoint) e `AlterarSenhaDTO.cs` (sem consumidor) | Remover os dois ou expor o endpoint de troca de senha — candidato à Spec 002 |
| PEND-014 | 2026-10-28 | open | Dois recursos de cardápio ficaram sem UI; estado e handler removidos para destravar o build | `Cardapio.tsx`: `handleExcluirCardapio` nunca era chamado e `setResponsavel` nunca era atribuído — o bloco de responsável técnico era inalcançável e não havia fonte de dados para `Responsavel` | Implementar quando a tela de cardápio for finalizada: botão de excluir refeição e exibição do responsável técnico |
| PEND-018 | 2026-10-28 | open | Persistência em SQLite nunca exercitada; só o modelo é validado | `ConnectionContextTests` constrói o modelo do EF e confere as colunas (`Restricoes`, `Escola`, `Serie`, `NomeResponsavel`, `TelefoneResponsavel`) sem abrir conexão — foi assim que o `OwnsOne` errado, que exigia coluna `AlunoId`, foi detectado. Nenhuma leitura/gravação real foi executada | Verificar com o app no ar: cadastrar aluno com restrições, reiniciar, ler `GET /api/aluno` e conferir as colunas |
| PEND-019 | 2026-10-28 | open | Suíte automatizada cobre domínio e serviços, não a API HTTP | `backend.Tests` tem 140 testes (Domain + Services + Infrastructure), incluindo `HashSenhaTests` e a validação do modelo do EF; falta apenas a T8 da Spec 005 (integração com `WebApplicationFactory`) | Escrever a T8 quando a API for exercitada por HTTP |
| PEND-020 | 2026-10-28 | open | Migration `HashSenhaUsuario` (Spec 002, T6) não foi criada | Não há mudança de schema: a coluna `Senha` já é `TEXT` no SQLite e o valor formatado do hash cabe nela. `dotnet-ef` não está instalado nesta máquina e uma migration vazia exigiria um Designer com o snapshot do modelo copiado à mão | Instalar `dotnet ef` e gerar a migration documental, ou dispensar formalmente na spec |

## Resolved

| ID | Date | Status | Title | Evidence |
|---|---|---|---|---|
| PEND-008 | 2026-10-28 | done | `.dscode/` fora do git | `.dscode/` adicionado ao `.gitignore`; `git rm -r --cached .dscode` retirou os 4 arquivos do índice (`git ls-files .dscode` → 0); arquivos preservados no disco. Exposição no histórico: ver PEND-013 |
| PEND-009 | 2026-10-28 | done | `.env.example` versionado | Criado `frontend/.env.example` com `VITE_API_URL=http://localhost:5001` e instrução de cópia para `frontend/.env` |
| PEND-015 | 2026-10-28 | done | `npm run build` do frontend falhava por 5 erros `TS6133` pré-existentes | `Perfil.tsx` (`carregando`, `salvando`, `atualizarCampo`) e `Cardapio.tsx` (`setResponsavel`, `handleExcluirCardapio`). Os três primeiros foram ligados à UI em vez de removidos: `atualizarCampo` substituiu 3 `onChange` duplicados e agora limpa `erro`/`mensagem`; `carregando` corrige o texto "Carregando perfil..." eterno quando a carga falha; `salvando` desabilita o botão e troca o rótulo. `npx tsc -b` → exit 0 |
| PEND-016 | 2026-10-28 | done | `dotnet build`/`dotnet restore` falhavam no shell do agente | Causa raiz: o shell não exporta `APPDATA` nem `ProgramFiles`; o NuGet monta os caminhos de configuração do usuário e da máquina com `Path.Combine`, que lança `ArgumentNullException("path1")` quando a variável falta. Workaround validado em 2026-10-28: `env APPDATA='C:\Users\Kayke\AppData\Roaming' ProgramFiles='C:\Program Files' dotnet test backend.Tests/backend.Tests.csproj` → restore, build e 80 testes verdes |
| PEND-017 | 2026-10-28 | done | Requisições de aluno respondem 400 para payload inválido e 404 no formato `{ erro }` | `AlunoController` envolve `CriarAluno`/`AtualizarAluno` em `catch (ArgumentException)` → `BadRequest(new { erro = erro.Message })`; `backend.dll` recompilado sem erro CS em 2026-10-28 20:20; README atualizado |
| PEND-013 | 2026-10-28 | done | Chave de API exposta no histórico do GitHub | Histórico reescrito: `627e302` → `d431349`, sem `.dscode`; `branch-kayke` e `main` atualizados por `--force-with-lease` e conferidos (`git log --all -- .dscode` vazio). Chave antiga revogada e substituída no provedor pelo dono em 2026-10-28. O objeto antigo pode sobreviver em clones já sincronizados até o GC do GitHub, mas a credencial está inválida |
