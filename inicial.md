# inicial.md — Contexto para a IA

Arquivo de bootstrap. Leia isto primeiro; os documentos canônicos continuam em
`management/`. Última verificação: **2026-10-30**.

---

## 1. O que é o projeto

**NutriEduc** — aplicação web acadêmica (5 alunos, Ciência da Computação) para apoio à
educação alimentar escolar: alunos, restrições alimentares, cardápios, ocorrências,
solicitações e relatórios, com cinco perfis (nutrição, merendeira, pais, secretaria,
escola).

- Fonte de funcionalidades: `implementacoes.md` — **128 itens** em 5 blocos.
- Avaliação anterior: `correcoes.md` — nota **6,72**; cinco correções recomendadas já
  foram implementadas nas specs 001–005.

## 2. Stack e como rodar

| Camada | Tecnologia |
|---|---|
| Frontend | React 19, TypeScript, Vite 8, React Router 7, Tailwind 4, Oxlint |
| Backend | ASP.NET Core 10, 4 camadas (API / Application / Domain / Infrastructure) |
| Banco | EF Core 10 + SQLite (`database.db`), `Database.Migrate()` no startup |
| Testes | xUnit — 138 unitários + 32 de integração HTTP |

```bash
# Backend (http://localhost:5001)
dotnet run --project backend/backend.csproj

# Frontend
cd frontend && npm install
cp .env.example .env        # VITE_API_URL=http://localhost:5001 (obrigatório)
npm run dev

# Gates
dotnet build backend/backend.csproj
dotnet test backend.Tests/backend.Tests.csproj
cd frontend && npm run build && npm run lint
```

**Gotcha de ambiente (PEND-016):** neste shell, `dotnet` pode falhar com
`ArgumentNullException("path1")` no NuGet. Use:

```bash
env APPDATA='C:\Users\Notebook-Maria\AppData\Roaming' ProgramFiles='C:\Program Files' \
  dotnet test backend.Tests/backend.Tests.csproj
```

## 3. Estrutura

```text
backend/
  Program.cs                     startup, DI, cookie, migrate
  API/Controllers/               AlunoController, UsuarioController
  API/Services/                  AlunoService, UsuarioService
  Application/                   DTOs
  Domain/Entities/               Aluno, Turma, Responsavel, Usuario, HashSenha
  Domain/Interface/              IAlunoRepository, IUsuarioRepository
  Infrastructure/DataBase/       ConnectionContext
  Infrastructure/Repositories/   AlunoRepository, UsuarioRepository
  Migrations/                    5 migrations (a última documental)
backend.Tests/                   Domain, Services, Infrastructure, Integration, Fakes
frontend/src/                    pages/{nutricao,merendeiras,pais,secretaria,escolas,comum}
                                 services/, context/, routes/, components/
management/                      SDD: roadmap, vision, arch, adr, lessons, pending-items, specs/
```

## 4. SDD — regras que não podem ser quebradas

1. **`management/roadmap.md` é a única fonte de status.** Nunca editar status à mão.
   A promoção é feita por `/spec-new` → `/spec-implement` → `/spec-test`.
2. **`management/PLANEJAMENTO.md` foi descartado.** Não recriar.
3. Frontmatter `status: created` é o valor que o `/spec-new` grava
   (`templates/prompts/spec_new.md.ejs:125`). Não é status aposentado; não "corrigir".
4. Cada spec vive em `management/specs/<N>-<name>/` com `requirements.md`,
   `design.md` e `task.md`.
5. Specs planejadas exigem, para ir ao roadmap, `Canonical creation path` em um bloco
   da `vision.md` e seção correspondente em `arch.md` (L11).
6. Ordem de execução é o **grafo**, não a numeração:
   `006 → 007 → 008 → 009 → 010 → 011 → 012 → 014 → 013 → 015 → 016 → 017 → 018 → 019`.
7. Toda mudança de contrato atualiza a tabela de endpoints do `README.md` na mesma
   alteração (V7/L6). A Spec 008 cria o teste que verifica isso.
8. Nada é commitado sem os gates verdes.

## 5. Estado atual verificado

| Item | Estado |
|---|---|
| `dotnet test` | **170/170** (138 unitários + 32 integração) |
| `npm run build` | OK (Vite, 1846 módulos) |
| `npm run lint` | 0 erros, 5 avisos |
| Avisos de build | 7 `CS8604` em `UsuarioService`/`UsuarioController` |
| Migrations | 5, incluindo `HashSenhaUsuario` (documental) |
| Controllers | 2 (`AlunoController`, `UsuarioController`) |
| Roadmap | 20 linhas, **todas `planned`** (promoção pendente de comandos) |
| 001–005 | implementadas; **todas as tasks e critérios concluídos** |
| 006–008 | documentos completos; **nada implementado** |
| 009–019 | documentos completos; **nada implementado** |

## 6. Mapa das specs

| # | Spec | Estado | Tema |
|---|---|---|---|
| 001 | crud-aluno-completo | implementada | `PUT /api/aluno/{id}` + edição no frontend |
| 002 | seguranca-senha-hash | implementada | PBKDF2-SHA256, migração de legado |
| 003 | restricoes-aluno-protegidas | implementada | `IReadOnlyList` + operações de domínio |
| 004 | dominio-aluno-invariantes | implementada | construtor/`AtualizarDados` validam tudo |
| 005 | testes-automatizados-backend | implementada | suíte + integração HTTP |
| 006 | autorizacao-endpoints-aluno | especificada | `[Authorize]` em aluno + cookie no frontend |
| 007 | contrato-erro-e-infra-di | especificada | middleware `{ erro }`, DI, connection string |
| 008 | higiene-frontend | especificada | `/colors`, stats, README verificado por teste |
| 009 | papeis-e-estrutura-escolar | especificada | 5 papéis, escola/unidade, vínculos |
| 010 | restricoes-e-alergias | especificada | catálogo + histórico do aluno |
| 011 | catalogo-alimentos | especificada | alimentos, nutrientes, alergênicos |
| 012 | dominio-cardapio | especificada | refeições, itens, cálculo, duplicação |
| 013 | geracao-automatica-cardapios | especificada | parâmetros + geração determinística |
| 014 | compatibilidade-cardapio-restricoes | especificada | severidade bloqueante/alerta |
| 015 | publicacao-historico-cardapio | especificada | publicar, revisar, histórico |
| 016 | ocorrencias | especificada | aluno/refeição/alimento + status |
| 017 | solicitacoes | especificada | cardápio, ingrediente, restrição |
| 018 | feedback-e-notificacoes | especificada | feedback + central de notificações |
| 019 | relatorios-e-exportacao | especificada | 5 relatórios + PDF/XLSX/DOCX |
| 020 | padronizacao-contrato-rotas | implementada | 201+Location, `{ erro }` em 400/404/405/415, perfil com dono |

## 7. Regras de arquitetura

- **Domínio valida e protege invariantes.** Construtor e `AtualizarDados` validam tudo
  e só então atribuem (ADR-004). Controller e service não validam campo.
- **Controller** traduz HTTP ↔ DTO; **Service** orquestra e persiste; **Repository**
  recebe `ConnectionContext` por DI — nunca instancia `new ConnectionContext()` (L4).
- **DTOs de entrada e saída são distintos**; senha nunca aparece em DTO de saída.
- **Erro único** `{ "erro": "mensagem" }` (ADR-005) — a Spec 007 implementa o middleware.
- **Autenticação** por cookie HTTP-only `NutriEduc.Auth`, 8h, sliding, `SameSite=Lax`.
  Toda chamada autenticada usa `credentials: "include"`.
- **Escopo por escola/unidade** é resolvido no backend (Spec 009), nunca só na UI.
- **Coleções** expostas como `IReadOnlyList` sobre campo privado (ADR-006).
- **Valores/histórico** seguem ADR-007..012 (escopo institucional, soft delete,
  histórico append-only, concorrência otimista, compatibilidade pura, relatório-tabela).

## 8. Pendências abertas (resumo)

| ID | Assunto | Resolve |
|---|---|---|
| PEND-001 | IDOR em `GET /api/usuario/perfil/{id}` (CPF exposto) | 006/009 |
| PEND-002 | `Usuario` com construtores que geram objeto inválido | 009 |
| PEND-003 | Cadastro de usuário não valida domínio | 009 |
| PEND-006 | `AlunoApi.ts` sem `credentials: "include"` | 006 |
| PEND-007 | `DadosAlunoDTO` entrada+saída com dado de saúde | 009/012 |
| PEND-010 | `/colors` público | 008 |
| PEND-011 | `buscarEstatisticas` chama rota inexistente | 008 |
| PEND-012 | Código morto `TrocarSenha` + `AlterarSenhaDTO` | 002 |
| PEND-014 | Recursos de cardápio sem UI | 012 |
| PEND-018 | SQLite real (`database.db`) não testado | verificação manual |

Fechadas relevantes: PEND-004 (DI), PEND-019 (integração), PEND-020 (migration),
PEND-021 (roadmap), PEND-022 (docs 006–008), PEND-023 (gates de frontend).
PEND-005 foi **descartada** (falso positivo).

## 9. Regras de operação para a IA

- **Peça permissão antes de escrever**: criar/editar/apagar arquivos, `npm install`,
  `dotnet build/restore`, migrations, `git add/commit`, instalar ferramentas.
  Leitura, `git status/diff/log`, buscas e testes que só reportam não exigem.
- Não editar `roadmap.md` manualmente; não recriar `PLANEJAMENTO.md`.
- Não imprimir nem versionar `.dscode/settings.json`, `frontend/.env` ou chaves.
- Mudança cirúrgica: tocar só o necessário, preservar estilo e CRLF dos `.md`.
- Toda correção acompanha teste que falha antes e passa depois (L7).
- Ao terminar, rodar os gates e registrar evidência em `task.md`.

## 10. Ordem de leitura para retomar

1. `inicial.md` (este arquivo)
2. `AGENTS.md` (regra de permissão)
3. `management/roadmap.md` (status canônico)
4. `management/arch.md` §10–§11 (estado e mapa de módulos)
5. `management/pending-items.md` (o que falta)
6. `management/specs/<N>-<name>/requirements.md` e `design.md` da spec atual

## 11. Próximo passo esperado

- Promover specs no roadmap com `/spec-new` (comando do pipeline, não edição manual).
- Implementar na ordem: **006 → 007 → 008 → 009 → …**
- Primeira entrega de código pendente: `[Authorize]` em `AlunoController` + testes de
  401 anônimo + `credentials: "include"` no `AlunoApi.ts` (Spec 006).
