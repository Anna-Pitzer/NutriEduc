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
| PEND-008 | 2026-10-27 | open | `.dscode/` não está no `.gitignore` e contém `settings.json`, `state.db` e `file-index.db` | `.gitignore` cobre apenas `.env` (linha 10) | Adicionar `.dscode/` ao `.gitignore` antes de qualquer `git add .` |
| PEND-009 | 2026-10-27 | open | Projeto não sobe em máquina limpa: `VITE_API_URL` é obrigatória e não há `.env.example` | `vite.config.ts:7-11` lança exceção se a variável faltar | Versionar `frontend/.env.example` com `VITE_API_URL=http://localhost:5001` |
| PEND-010 | 2026-10-27 | open | Rota `/cardapio` duplicada e `/colors` (paleta de teste) pública fora do `ProtectedRoutes` | `routes.tsx:28` e `:30` duplicam `/cardapio`; `:19` expõe `/colors` | Remover a duplicata e retirar `/colors` das rotas de produção |
| PEND-011 | 2026-10-27 | open | `buscarEstatisticas` chama endpoint inexistente e com prefixo errado | `Perfil.ts:13-19` — `/usuarios/${id}/estatisticas` (sem `/api/`) | Remover ou implementar o endpoint correspondente |
| PEND-012 | 2026-10-27 | open | Código morto e caminho de erro inalcançável no backend | `AlunoService.cs:30` (`CriarAluno` sempre `true`); `AlunoController.cs:21-24` (branch nunca executa); `UsuarioService.cs:109-127` (`TrocarSenha` sem endpoint); `AlterarSenhaDTO.cs` sem consumidor | Remover; `UpdateAluno` deixa de ser morto na Spec 001 |

## Resolved

| ID | Date | Status | Title | Evidence |
|---|---|---|---|---|
| — | — | — | (none yet) | — |
