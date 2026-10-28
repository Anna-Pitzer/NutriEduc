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
| PEND-012 | 2026-10-27 | open | Código morto e caminho de erro inalcançável (backend pendente) | Backend: `AlunoService.cs:30` (`CriarAluno` sempre `true`); `AlunoController.cs:21-24` (branch nunca executa); `UsuarioService.cs:109-127` (`TrocarSenha` sem endpoint); `AlterarSenhaDTO.cs` sem consumidor. Frontend resolvido em 2026-10-28 (ver PEND-015) | Remover no backend; `UpdateAluno` deixa de ser morto na Spec 001 |
| PEND-014 | 2026-10-28 | open | Dois recursos de cardápio ficaram sem UI; estado e handler removidos para destravar o build | `Cardapio.tsx`: `handleExcluirCardapio` nunca era chamado e `setResponsavel` nunca era atribuído — o bloco de responsável técnico era inalcançável e não havia fonte de dados para `Responsavel` | Implementar quando a tela de cardápio for finalizada: botão de excluir refeição e exibição do responsável técnico |
| PEND-016 | 2026-10-28 | open | `dotnet build`/`dotnet restore` não funcionam no shell do agente neste ambiente | `NuGet.Configuration.ConfigurationDefaults` lança `Value cannot be null (Parameter 'path1')` porque `%APPDATA%` chega indefinido ao processo; reproduzível em `dotnet nuget list source`. Código foi validado compilando com `csc` do SDK contra os assemblies de referência | Rodar `dotnet build backend/backend.csproj` no terminal do usuário para validar o projeto completo; investigar a variável `APPDATA` do ambiente |
| PEND-017 | 2026-10-28 | open | `POST /api/aluno/registrar` responde 500 para payload inválido em vez de 400 | Spec 004 passou a lançar `ArgumentException` no construtor de `Aluno`, mas `AlunoController` não trata a exceção (o branch de erro em `:21-24` é inalcançável, PEND-012) | Tratar `ArgumentException` no controller e devolver 400 com o formato `{erro}` de `arch.md` — cabe na Spec 001 |
| PEND-018 | 2026-10-28 | open | Mapeamento EF da coleção de restrições validado por compilação, não por runtime | `ConnectionContext` usa `PrimitiveCollection(...).HasField("_restricoes")`; compila contra EF Core 10.0.11, mas nenhuma leitura/gravação em SQLite foi executada | Verificar com o app no ar: cadastrar aluno com restrições, reiniciar, ler `GET /api/aluno` e conferir a coluna `Restricoes` |

## Resolved

| ID | Date | Status | Title | Evidence |
|---|---|---|---|---|
| PEND-008 | 2026-10-28 | done | `.dscode/` fora do git | `.dscode/` adicionado ao `.gitignore`; `git rm -r --cached .dscode` retirou os 4 arquivos do índice (`git ls-files .dscode` → 0); arquivos preservados no disco. Exposição no histórico: ver PEND-013 |
| PEND-009 | 2026-10-28 | done | `.env.example` versionado | Criado `frontend/.env.example` com `VITE_API_URL=http://localhost:5001` e instrução de cópia para `frontend/.env` |
| PEND-015 | 2026-10-28 | done | `npm run build` do frontend falhava por 5 erros `TS6133` pré-existentes | `Perfil.tsx` (`carregando`, `salvando`, `atualizarCampo`) e `Cardapio.tsx` (`setResponsavel`, `handleExcluirCardapio`). Os três primeiros foram ligados à UI em vez de removidos: `atualizarCampo` substituiu 3 `onChange` duplicados e agora limpa `erro`/`mensagem`; `carregando` corrige o texto "Carregando perfil..." eterno quando a carga falha; `salvando` desabilita o botão e troca o rótulo. `npx tsc -b` → exit 0 |
| PEND-013 | 2026-10-28 | done | Chave de API exposta no histórico do GitHub | Histórico reescrito: `627e302` → `d431349`, sem `.dscode`; `branch-kayke` e `main` atualizados por `--force-with-lease` e conferidos (`git log --all -- .dscode` vazio). Chave antiga revogada e substituída no provedor pelo dono em 2026-10-28. O objeto antigo pode sobreviver em clones já sincronizados até o GC do GitHub, mas a credencial está inválida |
