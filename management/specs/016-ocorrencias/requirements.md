---
name: ocorrencias
status: created
references: V6, ADR-004, L7
---

# Requirements — Ocorrências

## Objetivo

Merendeiras, pais e escolas precisam registrar e acompanhar problemas relacionados a
alunos, refeições e alimentos. Hoje não existe registro: um episódio de reação
alérgica ou de refeição servida errada não deixa rastro e não pode ser auditado.

## Escopo

- Backend: `Ocorrencia` com vínculos opcionais (aluno, cardápio/refeição, alimento),
  ciclo de status, histórico e escopo por papel.
- Frontend: formulário e listagem para merendeira, pais e escola; detalhe com
  histórico.
- Fora de escopo: relatório consolidado de ocorrências (Spec 019), notificações de
  mudança de status (Spec 018 consome os eventos), alteração de cardápio (Spec 017).

### Cobertura de `implementacoes.md`

- **Merendeiras (6):** criar relatórios de ocorrências (o registro estruturado que
  alimenta o relatório da Spec 019), registrar ocorrência de aluno, de refeição e de
  alimento, visualizar ocorrências registradas e acompanhar status.
- **Pais (3):** verificar ocorrências dos seus alunos, relatar ocorrências e consultar
  o histórico dos seus alunos.
- **Escolas (1):** acompanhar ocorrências da escola.

## Requisitos funcionais

- RF1: `Ocorrencia` tem `Tipo` (`Aluno`, `Refeicao`, `Alimento`, `Outro`),
  `Descricao` (5–1000), `Status` inicial `Aberta`, `UnidadeId` obrigatório e
  `AutorId`/`AutorNome` (snapshot).
- RF2: ao menos um vínculo deve estar presente: `AlunoId` (tipo `Aluno`),
  `CardapioId`+`RefeicaoId` (tipo `Refeicao`) ou `AlimentoId` (tipo `Alimento`).
  Vínculo incoerente com o tipo devolve 400.
- RF3: `Merendeira` registra ocorrência na própria unidade; `Responsavel` registra
  apenas para aluno vinculado a ele; `Nutricionista` registra em qualquer unidade;
  `AdminEscola` registra na própria escola.
- RF4: transições de status permitidas:
  `Aberta → EmAnalise`, `EmAnalise → Resolvida`, `Resolvida → Arquivada`,
  `EmAnalise → Aberta` (reabertura). Qualquer outra devolve 409.
- RF5: `Nutricionista` e `AdminEscola` mudam status; `Merendeira` e `Responsavel` não.
  Toda mudança exige `observacao` (2–500) e gera evento de histórico.
- RF6: `Responsavel` vê apenas ocorrências dos alunos vinculados; `Merendeira` vê as
  da própria unidade; `AdminEscola` as da própria escola; `Nutricionista` todas
  (filtro global opcional por escola/unidade).
- RF7: `GET /api/ocorrencias` aceita `status`, `tipo`, `alunoId`, `unidadeId`,
  `de`, `ate`, `pagina`, `tamanho` (1–100) e aplica o escopo do papel no banco.
- RF8: `GET /api/ocorrencias/{id}` devolve detalhe com vínculos resolvidos (nomes de
  aluno, cardápio, refeição e alimento) e histórico de status.
- RF9: ocorrência não é apagada; encerramento é `Arquivada`. Autor pode cancelar
  ocorrência `Aberta` (status `Cancelada`) apenas no mesmo dia da criação.
- RF10: a resposta de pai e merendeira não inclui dados de aluno fora do escopo nem
  identificadores de outros autores além do nome.
- RF11: frontend oferece formulário contextual (a partir do aluno, do cardápio ou do
  alimento) e listagem com filtros; a escola vê um painel de acompanhamento.

## Requisitos não funcionais

- RNF1: invariantes de ocorrência e transições no agregado; controller sem regra.
- RNF2: histórico append-only na mesma transação da mudança de status.
- RNF3: snapshot do autor e dos nomes de aluno/cardápio/alimento no evento, para o
  histórico não mudar quando o cadastro mudar.
- RNF4: consultas paginadas e indexadas por `(UnidadeId, CriadoEmUtc)` e
  `(AlunoId, CriadoEmUtc)`.
- RNF5: contrato `{ erro }` em 400/403/404/409.

## Restrições

- Migration nova `Ocorrencias`.
- Não altera os modelos de aluno, cardápio ou alimento; apenas referencia por id.
- Nenhuma dependência nova no frontend.

## Dependências

- Spec 009 (papéis e escopo) e Spec 012 (cardápio e refeições).
- Spec 018 consome os eventos de status; não é pré-requisito.

## Casos de borda

- Ocorrência de aluno de outra unidade → 403.
- Vínculo de aluno inexistente → 400.
- Refeição que não pertence ao cardápio informado → 400.
- Descrição com 4 ou 1001 caracteres → 400.
- Reabrir ocorrência arquivada → 409.
- Cancelar ocorrência de outro dia → 409.
- Aluno excluído depois da ocorrência → histórico preserva o nome; detalhe mostra
  vínculo ausente.
- Pai com dois alunos vê somente as ocorrências de ambos, nunca as de terceiros.
- Unidade sem ocorrências → lista vazia, 200.

## Critérios de aceitação

- [ ] Merendeira registra ocorrência de refeição e ela aparece na listagem da unidade.
- [ ] Pai registra ocorrência apenas para aluno vinculado; outro aluno devolve 403.
- [ ] Transição inválida devolve 409 e não altera status.
- [ ] Mudança de status grava evento com autor, observação e data.
- [ ] Listagem aplica escopo e filtros no banco.
- [ ] Pai não vê ocorrência de aluno de outro responsável.
- [ ] Cancelar no mesmo dia funciona; no dia seguinte devolve 409.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Registrar uma ocorrência de aluno e uma de refeição, mudar status com nutrição e
escola, consultar com merendeira e pai, e conferir escopo, histórico e filtros.
Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
