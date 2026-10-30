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

- `management/roadmap.md` é a única fonte de verdade do planejamento, com 19 linhas
  `planned`; `PLANEJAMENTO.md` foi descartado. A promoção de status é feita por
  `/spec-new`, `/spec-implement` e `/spec-test`.
- Frontmatter `status: created` é o valor que o próprio `/spec-new` grava
  (`templates/prompts/spec_new.md.ejs:125`); o ciclo de vida vive no roadmap.
- `frontend/node_modules` instalado (51 pacotes) e `frontend/.env` criado localmente
  (ambos ignorados pelo git).

## Estado do código (verificado em 2026-10-30)

- `dotnet test backend.Tests`: 152/152 verdes (140 unitários + 12 de integração HTTP).
- Gates de frontend passam: `npm run build` OK e `npm run lint` com 0 erros.
- 2 controllers (`AlunoController`, `UsuarioController`), 5 migrations (a última
  documental: `HashSenhaUsuario`).
- Specs 001–005: todas as tasks executadas. Especificadas e não implementadas:
  006–019 (006–008 prontas para o pipeline; 009–019 com documentos).
