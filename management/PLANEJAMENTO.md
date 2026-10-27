# Planejamento — índice de specs (staging do índice do pipeline)

## Por que este arquivo existe

O índice canônico de specs (`management/roadmap`, arquivo do pipeline SDD do DsCode)
é **propriedade exclusiva do pipeline**: o agente não pode criá-lo nem alterá-lo — a
escrita é bloqueada e a ferramenta de commit exige que o arquivo já exista. Enquanto
ele não for gerado pelo pipeline, a tabela abaixo é a fonte de verdade do
planejamento e deve ser promovida pela ferramenta de commit do pipeline, nunca por
edição manual.

## Como promover ao índice canônico

1. Garantir que o DsCode esteja ancorado na raiz do projeto (`NutriEduc/`), de modo
   que o pipeline enxergue `NutriEduc/management/`. **Não existe pasta `Dscode/` no
   repositório** — a versão anterior deste passo afirmava que sim. Enquanto o pipeline
   estiver ancorado em outro diretório, o índice canônico não é gerado e esta tabela
   continua sendo a fonte de verdade.
2. Commitar as 12 linhas abaixo com a ferramenta de planejamento (todas com status
   `planned`, conforme exigido por ela).
3. Executar `/spec-new <n>` para cada spec já documentada (001 a 005). O pipeline
   valida os três documentos e sela a spec como `verified`.
4. Seguir `/spec-implement <n>` e `/spec-test <n>`, ou `/spec-pipe <n>` para encadear
   os dois estágios.

## Tabela de specs

| # | Name | Status | References |
|---|---|---|---|
| 001 | crud-aluno-completo | created | V2; L8 |
| 002 | seguranca-senha-hash | created | V3; ADR-003; L3 |
| 003 | restricoes-aluno-protegidas | created | V1; ADR-006; L2 |
| 004 | dominio-aluno-invariantes | created | V1; ADR-004; L1 |
| 005 | testes-automatizados-backend | created | V4; L7 |
| 006 | autorizacao-endpoints-aluno | planned | V5 |
| 007 | contrato-erro-e-infra-di | planned | ADR-005; L4 |
| 008 | higiene-frontend | planned | V7; L6 |
| 009 | papeis-e-escopo-dados | planned | V6 |
| 010 | dominio-alimentos-cardapios | planned | V6 |
| 011 | compatibilidade-cardapio-restricoes | planned | V6; L5 |
| 012 | ocorrencias-solicitacoes-relatorios | planned | V6 |

Ao commitar, todas entram como `planned`. As specs 001–005 já possuem os três
documentos como rascunho, mas **permanecem não seladas**: `created` é status
aposentado e não é reconhecido pelo fluxo atual. Executar `/spec-new <n>` para cada uma
(→ `verified`) antes de `/spec-implement`. O frontmatter desses 15 arquivos ainda
declara `status: created` e precisa ser normalizado pelo `/spec-new` (registrado em
`pending-items.md`).

## Fases

| Fase | Objetivo | Specs | Estado-alvo verificável |
|---|---|---|---|
| F1 | Fechar domínio e CRUD; eliminar senha em claro; criar rede de testes | 001–005 | `dotnet test` verde; `U` de aluno funcional; senha nunca persistida em claro |
| F2 | Endurecer segurança e infraestrutura; limpar frontend | 006–008 | 401 em rota anônima de aluno; erro padronizado; sem 404 no console das telas existentes |
| F3 | Identidade e escopo de dados | 009 | Autorização por papel e por escola |
| F4 | Domínio de alimentos e cardápios | 010 | Rotas de cardápio e alimento funcionais |
| F5 | Segurança alimentar | 011 | Alerta determinístico de incompatibilidade, testado |
| F6 | Ocorrências, solicitações, notificações e relatórios | 012 | Fluxos de merendeira e responsável ponta a ponta |

## Grafo de dependências

```text
Ordem de execução da F1 (o grafo anterior tinha ciclo 001 ↔ 005):

  004 (dominio-aluno) ──┬──► 001 (crud-aluno) ──┐
                        └──► 003 (restricoes) ───┤
  002 (senha-hash) ──────────────────────────────┼──► 005 T4–T9 (suíte)
  005 T1–T3 (infra de teste + fakes) ──► 001 T3  ┘

Fases seguintes:

  001, 005 ──► 006 (authz) ──► 008 (higiene frontend)
  002 ──► 007 (erro + DI) ──► 009 (papéis) ──► 010 (cardápios) ──► 011 (compatibilidade) ──► 012 (operações)
```

**Motivo da correção:** a Spec 001 task T3 exigia `dotnet test` verde *"projeto criado
na Spec 005"*, e a Spec 005 requisito RF6 exigia testar `PUT /api/aluno/{id}` *"criado
pela Spec 001"*. Como estavam escritas, nenhuma das duas podia vir primeiro. A saída é
executar de 005 apenas a criação do projeto de teste, de `Program` parcial e dos fakes
(T1–T3) antes da 001; o restante da suíte (T4–T9) vem depois.

## Gates por fase

```bash
dotnet build backend/backend.csproj
dotnet test backend.Tests/backend.Tests.csproj   # a partir da Spec 005
cd frontend && npm run build && npm run lint
```

Gate de segurança e integridade nunca é dispensado; `--no-verify` nunca é usado.

## Critérios de transição

Fluxo vigente: `planned` → `verified` → `implemented` → `audited`, executado por
`/spec-new` → `/spec-implement` → `/spec-test` (ou encadeado por `/spec-pipe`).
`/spec-new` sela a spec diretamente como `verified`; não existe estágio de review, e
`/spec-audit` é identidade aposentada — não é comando roteável.

| De | Para | Condição | Dono |
|---|---|---|---|
| planned | verified | `requirements.md`, `design.md` e `task.md` existem, passam validação estrutural e há consistência entre requisito, design e tarefa | `/spec-new` |
| verified | implemented | tarefas concluídas e gates executados com saída registrada | `/spec-implement` |
| implemented | audited | validação determinística final aprovada com evidência | `/spec-test` |
