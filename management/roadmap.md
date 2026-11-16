# Roadmap — NutriEduc

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

> **Estado em 2026-11-13.** `implemented` = código completo e gates verdes, ainda sem
> certificação `/spec-test`; `audited` = código, testes e documentação auditados; `created` =
> documentos sem implementação; `in-progress` = implementação parcial. Lacunas atuais: 019
> (relatórios) sem implementação; 029 com backend pronto e frontend pendente (PEND-042); 025/026
> planejadas.
> As Specs 034 e 035 foram auditadas manualmente em 2026-11-13; a validação manual no navegador
> segue em PEND-047 (034) e PEND-050 (035).
> A promoção automática pelo `/spec-test` continua bloqueada pelo PEND-028; as auditorias
> registradas foram manuais.

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
| 013 | geracao-automatica-cardapios | audited | V6; ADR-004; L7 |
| 014 | compatibilidade-cardapio-restricoes | audited | V6; ADR-004; L5 |
| 015 | publicacao-historico-cardapio | audited | V6; ADR-004; L7 |
| 016 | ocorrencias | implemented | V6; V17; ADR-020; ADR-021; ADR-004; L7 |
| 017 | solicitacoes | implemented | V6; V17; V19; ADR-020; ADR-021; ADR-004; L7 |
| 018 | feedback-e-notificacoes | implemented | V6; V17; ADR-020; ADR-021; ADR-004; L7 |
| 019 | relatorios-e-exportacao | created | V6; V16; V17; V18; ADR-020; ADR-021; ADR-022; ADR-004; L6 |
| 020 | padronizacao-contrato-rotas | audited | V9; V8; ADR-005; L6; L7 |
| 021 | telas-secretaria-e-escola | audited | V10; V6; ADR-002 |
| 022 | bootstrap-admin-por-token | audited | V11; V6; ADR-002 |
| 023 | historico-alteracoes-aluno | implemented | V12; ADR-004; ADR-009 |
| 024 | fronteiras-ddd | implemented | V13; ADR-004; ADR-009 |
| 025 | painel-e-visao-por-papel | planned | V14; V6; ADR-002 |
| 026 | substituicao-assistida-ia | planned | V15; ADR-011; ADR-004 |
| 027 | hierarquia-unidade-escola-aluno | audited | V16; ADR-020; ADR-021; L11 |
| 028 | matriz-permissoes-e-isolamento | audited | V17; ADR-021; ADR-020; L5; L12 |
| 029 | cardapio-geral-e-adaptado | in-progress | V18; V20; ADR-022; ADR-011; ADR-009; L7 |
| 030 | solicitacao-alteracao-dados | audited | V19; ADR-016; ADR-004; L9 |
| 031 | perfil-crn-nutricionista | audited | V20; ADR-021; ADR-004; ADR-009; PEND-038 |
| 032 | correcao-geracao-automatica-cardapios | implemented | V21; V18; ADR-022; ADR-011; ADR-004 |
| 033 | cadastro-e-autorizacao-de-acesso | implemented | V22; V17; ADR-021; ADR-020; ADR-004; ADR-019 |
| 034 | vinculo-responsavel-aluno | audited | V23; V17; Spec 027; Spec 028; Spec 033; ADR-020; ADR-021 |
| 035 | portal-pais-responsaveis | audited | V24; V6; V17; V23; Spec 010; Spec 016; Spec 017; Spec 018; Spec 025; Spec 029; ADR-009; ADR-021 |

---

## Dependency Graph

```
022 (bootstrap) ──► 027 (hierarquia) ──► 028 (matriz)
                          │                  │
                          ├──► 031 (CRN) ────┼──► 033 (cadastro/autorização)
                          │                  │
                          ├──► 029 (cardápio geral/adaptado) ──► 032 (geração automática)
                          └──► 030 (solicitação de dados)
031 (CRN) ───────────────────────► 029
033 (usuários aprovados) ────────► 029 e 032
017 (solicitações) ──────────────► 030
010, 011, 012, 013, 014, 015 ────► 029
013 (implemented) ───────────────► 032 (correção; 013 é imutável)
016, 019, 025 ───────────────────► 028 (matriz ajusta escopo)
027, 028, 033 ───────────────────► 034 (vínculo obrigatório e busca de alunos)
016, 017, 018, 025, 029, 034 ────► 035 (portal dos pais/responsáveis)
```

