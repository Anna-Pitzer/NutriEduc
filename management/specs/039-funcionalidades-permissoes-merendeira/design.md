---
name: funcionalidades-permissoes-merendeira
status: audited
references: V26; V14; V17; Spec 009, Spec 012, Spec 015, Spec 016, Spec 017, Spec 018, Spec 025, Spec 027, Spec 028, Spec 029, Spec 031, Spec 033; ADR-002, ADR-004, ADR-020, ADR-021
---

# Design — Funcionalidades e Permissões do Perfil de Merendeira

## Princípio

A autorização e o escopo da merendeira **já estão corretos no backend** (Spec 028 + Spec 027).
A spec 039 é, na maior parte, uma correção de **navegação e de garantia de invariante**, não uma
reescrita de regras. O backend não ganha nenhuma regra nova de permissão; o que muda é (1) expor a
navegação à merendeira no frontend, (2) tornar explícita e testada a derivação de `UnidadeId` a
partir da escola, e (3) fechar uma falha de guarda de rota.

## Escopo da merendeira (fonte única)

```
EscopoUsuario (claims) → Papel = Merendeira, EscolaId = X, UnidadeId = escola.UnidadeId
```

- A matriz permanece a fonte executável (`MatrizDePermissoes.Nivel`). Merendeira:
  `CardapioGeral` R, `CardapioAdaptado` R, `PublicacaoDeCardapio` R, `Solicitacao` RC,
  `Feedback` RC, `Ocorrencia` RC, `Notificacao` R, `Painel` R, `CatalogoDeAlimentos` R,
  `CatalogoDeRestricoes` R, `Aluno`/`RestricaoDoAluno`/`HistoricoDoAluno` R.
- O recorte escola↔unidade continua em `FiltrosDeConsulta` (AdminEscola/Merendeira → `EscolaId`)
  e nos serviços (`CardapioService.TentarRecorte`, `SolicitacaoService.Listar`,
  `FeedbackService.Listar`), que exigem `UnidadeId` + `EscolaId`.

## Alterações — Frontend

1. **Área da merendeira com navegação (decisão única).** Adicionar `<Navbar />` a
   `HomeMerendeiras.tsx`, logo após `<Header />`, dentro da coluna flex que já monta `Header` +
   `AlertaAlunosIndisponiveis` + `main` + `Footer`. Nenhum componente de layout novo é criado; o
   menu da merendeira expõe exatamente 4 abas (Cardápio, Solicitações, Ocorrências e Feedback);
   o catálogo de alimentos e a home `/merendeiras` ficam fora do menu da merendeira.
2. **Guarda de rota de alimentos.** Em `routes.tsx`, mover `/alimentos`, `/alimentos/novo` e
   `/alimentos/editar/:id` para a guarda `ProtegidaPorPapel(["Nutricionista"])` — o catálogo de
   alimentos fica exclusivo da nutrição (a merendeira deixa de acessá-lo no frontend).
3. **(Opcional, PEND-025)** Tela de leitura da trilha de restrições de um aluno específico,
   reutilizando `historicoAlunoApi`/`GET /api/aluno/{id}/restricoes/historico`, para a merendeira
   consultar o histórico de um aluno da sua escola (hoje só existe a contagem agregada).

## Alterações — Backend

1. **Garantia da invariante de escopo (sem mudança de produção).** A derivação já existe e é a
   fonte única: `UsuarioService.ResolucaoEscopo` resolve a escola e retorna
   `(escola.Id, escola.UnidadeId)` — recusando escola sem unidade — e `AprovarAcessoService.Aprovar`
   passa `escola.UnidadeId` a `DefinirPapel`. Nenhum código de produção muda nesta spec; a
   invariante “Merendeira sempre tem `EscolaId` + `UnidadeId == escola.UnidadeId`” é garantida por
   **teste de conformidade** que percorre os três caminhos (criação administrativa, edição de
   papel e aprovação) e falha se qualquer um deixar `UnidadeId` nulo ou divergente da escola.
2. **Teste de conformidade de isolamento.** Cobrir os cenários de borda da spec: merendeira
   lendo/criando em outra escola (403) e outra unidade (404); DTO de criação sem `escolaId`/
   `unidadeId` não permite forjar escola.

## Alterações — Banco

- Nenhuma. `Usuario` já possui `EscolaId` e `UnidadeId`; `Solicitacao` e `Feedback` já possuem
  `EscolaId`/`UnidadeId`. Não há migration.

## Fluxos

- **Visualizar cardápio:** `HomeMerendeiras` → menu “Cardápio” → `ListarCardapios` →
  `GET /api/cardapios?unidadeId=…` → `CardapioService.Listar` (recorte unidade+escola) →
  `CardapioRepository` (`EscolaVisivelId` devolve geral da unidade + adaptado da escola) →
  `DetalheCardapio` (refeições/itens). Histórico: `GET /api/cardapios/alteracoes`.
- **Visualizar/criar solicitação:** menu “Solicitações” → `SolicitacoesPage` →
  `GET /api/solicitacoes` (recorte unidade+escola, sem `DadosCadastrais`) e
  `POST /api/solicitacoes` (`tipo` ∈ MudancaCardapio/Ingrediente/FaltaIngrediente). O serviço
  grava `escolaId = escopo.EscolaId`, `solicitante = escopo.UsuarioId` e notifica a Nutricionista.
- **Feedback:** menu “Feedback” → `FeedbackPage` → `GET /api/feedbacks` (recorte unidade+escola) e
  `POST /api/feedbacks` (unidade/escola derivadas da sessão). Resposta é da nutrição/escola.

## Contrato (sem mudança)

- `POST /api/solicitacoes` mantém `CriarSolicitacaoDTO` **sem** `escolaId`/`unidadeId` (Spec 017/
  030); a escola é derivada no serviço. `POST /api/feedbacks` mantém `CriarFeedbackDTO` sem
  escola/unidade explícita.
- As rotas de leitura (`GET /api/cardapios`, `GET /api/solicitacoes`, `GET /api/feedbacks`)
  continuam escopando por `EscopoUsuario`.

## Estrutura

```
frontend/
  pages/merendeiras/HomeMerendeiras.tsx     (adiciona <Navbar /> após <Header />)
  routes/routes.tsx                         (guarda de /alimentos/novo|editar → Nutricionista)
  pages/merendeiras/HistoricoRestricoes.tsx (opcional, PEND-025)
backend/
  backend.Tests/...                          (teste de conformidade de escopo/isolamento)
```

## Testes

- **Conformidade de escopo:** merendeira criada por aprovação e por administração tem
  `EscolaId` + `UnidadeId == escola.UnidadeId`.
- **Isolamento (integração HTTP):** merendeira A lê/cria cardápio, solicitação e feedback da
  própria escola; acesso à escola B → 403; outra unidade → 404; `escolaId` forjado no payload é
  ignorado.
- **Frontend:** `npm run build` e `npm run lint`; conferir manualmente que a merendeira enxerga o
  menu e que a guarda de alimentos não expõe escrita.

## Riscos

- **Regressão de navegação:** ao introduzir `Navbar`/layout na área da merendeira, não alterar as
  páginas compartilhadas usadas por outros papéis sem necessidade.
- **Invariante legada:** os três caminhos atuais derivam a unidade da escola, então uma
  merendeira sem `UnidadeId` só é possível em dados legados no `database.db`. O teste de
  conformidade documenta a invariante; dados legados inconsistentes ficam fora desta spec (sem
  migration).
- **Fora de escopo:** a divergência matriz × `CardapioService.PodeLer` para `Responsavel`
  (PEND-048) não é tratada aqui.
