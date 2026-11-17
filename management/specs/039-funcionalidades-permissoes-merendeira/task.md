---
name: funcionalidades-permissoes-merendeira
status: audited
references: Spec 039
---

# Tasks — Funcionalidades e Permissões do Perfil de Merendeira

> **Estado:** nenhuma tarefa iniciada. Nenhuma alteração de código foi feita nesta etapa
> (SPEC-only). A ordem sugerida: T1 → T2 → T3 → T4 → T5 → T6.

## Frontend — navegação e guarda

- [x] **T1 — Expor o menu à merendeira (`<Navbar />` em `HomeMerendeiras`).**
  - Arquivos: `frontend/src/pages/merendeiras/HomeMerendeiras.tsx` (adicionar import e
    `<Navbar />` logo após `<Header />`). Nenhum arquivo novo.
  - Ação: importar `Navbar` e renderizar `<Navbar />` após `<Header />` na coluna flex de
    `HomeMerendeiras.tsx`, expondo Cardápio, Solicitações, Ocorrências e Feedback (4 abas).
  - Critério: a partir de `/merendeiras` a merendeira navega para `/cardapios`, `/solicitacoes`,
    `/feedbacks` e `/ocorrencias`; `npm run build` e `npm run lint` sem erro.

- [x] **T2 — Guarda de rota de alimentos (exclusiva da nutrição).**
  - Arquivos: `frontend/src/routes/routes.tsx`.
  - Ação: mover `/alimentos`, `/alimentos/novo` e `/alimentos/editar/:id` para a guarda
    `ProtegidaPorPapel(["Nutricionista"])` — o catálogo de alimentos fica exclusivo da nutrição
    (a merendeira deixa de acessá-lo no frontend).
  - Critério: a merendeira não acessa o catálogo de alimentos (guarda 403 no cliente e, por trás,
    o backend nega escrita com 403).

## Backend — invariante de escopo e isolamento

- [x] **T3 — Teste de conformidade da invariante de escopo (sem mudança de produção).**
  - Arquivos: `backend.Tests/…` (novo teste de conformidade).
  - Ação: afirmar que os três caminhos de criação/edição de `Merendeira` (criação administrativa
    via `UsuarioService.ResolucaoEscopo`, edição de papel via `UsuarioService.AtualizarUsuario` e
    aprovação via `AprovarAcessoService.Aprovar`) resultam em `EscolaId` preenchido e
    `UnidadeId == escola.UnidadeId`; escola sem unidade é rejeitada. Nenhum arquivo de produção é
    alterado.
  - Critério: o teste falha se qualquer caminho produzir `UnidadeId` nulo ou divergente da escola.

- [x] **T4 — Testes de isolamento por escola/unidade (integração HTTP).**
  - Arquivos: `backend.Tests/Integration/…` (novo arquivo de testes da merendeira).
  - Ação: cobrir (a) merendeira A lê cardápios/solicitações/feedbacks da própria escola; (b) acesso
    à escola B → 403; (c) outra unidade → 404; (d) `POST /api/solicitacoes` com payload tentando
    forjar escola é ignorado e grava a escola da sessão; (e) notificação para a Nutricionista na
    criação.
  - Critério: `dotnet test` verde com os novos casos.

## Opcional (PEND-025)

- [ ] **T5 — Tela de leitura da trilha de restrições de um aluno (merendeira).**
  - Arquivos: `frontend/src/pages/merendeiras/HistoricoRestricoes.tsx` (novo),
    `frontend/src/services/HistoricoAlunoApi.ts` (reuso).
  - Ação: consulta somente leitura ao `GET /api/aluno/{id}/restricoes/historico` para a merendeira
    da escola do aluno; exibir restrições vigentes e alterações no período.
  - Critério: a merendeira vê o histórico de um aluno da própria escola e nada de outra escola.

## Fechamento

- [x] **T6 — Validação e gates.**
  - Ação: percorrer o roteiro de `Validação` do requirements e executar
    `dotnet build backend/backend.csproj`, `dotnet test backend.Tests/backend.Tests.csproj` e
    `cd frontend && npm run build && npm run lint`.
  - Critério: todos os critérios de aceitação atendidos; gates verdes; nenhuma alteração de matriz
    nem de banco.
