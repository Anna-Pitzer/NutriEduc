# DsCode Roadmap

## Spec Statuses

| Status | Meaning |
|---|---|---|---|
| `proposed` | Idea stage, no spec written yet |
| `planned` | Roadmap entry created via /spec-plan |
| `created` | Spec documents authored and sealed via /spec-new |
| `verified` | Spec documents verified and corrected via /spec-verify |
| `implemented` | Code implemented via /spec-implement |
| `in-progress` | Under active development on a feature branch |
| `audited` | Final stage — certified by /spec-test, the sole owner of the implemented → audited transition. Feature is live. |
| `discarded` | Intentionally abandoned |

---

## Specs

| # | Name | Status | References |
|---|---|---|---|
| 001 | crud-aluno-completo | audited | V2; L8 |
| 002 | seguranca-senha-hash | audited | V3; ADR-003; L3 |
| 003 | restricoes-aluno-protegidas | audited | V1; ADR-006; L2 |
| 004 | dominio-aluno-invariantes | audited | V1; ADR-004; L1 |
| 005 | testes-automatizados-backend | audited | V4; L7 |
| 006 | autorizacao-endpoints-aluno | audited | V5 |
| 007 | contrato-erro-e-infra-di | audited | V8; ADR-005; L4 |
| 008 | higiene-frontend | audited | V7; L6 |
| 009 | papeis-e-estrutura-escolar | audited | V6; ADR-002; L4 |
| 010 | restricoes-e-alergias | implemented | V1; V6; ADR-004; L2 |
| 011 | catalogo-alimentos | implemented | V6; ADR-004; L9 |
| 012 | dominio-cardapio | implemented | V6; ADR-004; L10 |
| 013 | geracao-automatica-cardapios | created | V6; ADR-004; L7 |
| 014 | compatibilidade-cardapio-restricoes | created | V6; ADR-004; L5 |
| 015 | publicacao-historico-cardapio | created | V6; ADR-004; L7 |
| 016 | ocorrencias | created | V6; ADR-004; L7 |
| 017 | solicitacoes | created | V6; ADR-004; L7 |
| 018 | feedback-e-notificacoes | created | V6; ADR-004; L7 |
| 019 | relatorios-e-exportacao | created | V6; ADR-004; L6 |
| 020 | padronizacao-contrato-rotas | implemented | V9; V8; ADR-005; L6; L7 |
| 021 | telas-secretaria-e-escola | audited | V10; V6; ADR-002 |
| 022 | bootstrap-admin-por-token | audited | V11; V6; ADR-002 |
| 023 | historico-alteracoes-aluno | implemented | V12; ADR-004; ADR-009 |
| 024 | fronteiras-ddd | implemented | V13; ADR-004; ADR-009 |
| 025 | painel-e-visao-por-papel | planned | V14; V6; ADR-002 |
| 026 | substituicao-assistida-ia | planned | V15; ADR-011; ADR-004 |

---

## Dependency Graph

```
```
