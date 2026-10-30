---
name: restricoes-e-alergias
status: created
references: V1, V6, ADR-004, L2
---

# Tasks — Restrições e alergias

## Pré-condição

- [ ] T0: Specs 003, 004 e 009 implementadas.
  - Validação: `dotnet test` verde; `Aluno.Restricoes` é `IReadOnlyList<string>`.

## Domínio

- [ ] T1: criar `RestricaoAlimentar` com `Categoria`, `Ativa`, `NomeNormalizado` e
  `Normalizar` estático.
  - Validação: testes de limite 2–60, normalização de acento/caixa e desativação.
- [ ] T2: criar `HistoricoRestricaoAluno` com `Acao`, `AutorNome` e `CriadoEmUtc`.
  - Validação: teste de construtor interno e de snapshot de autor.
- [ ] T3: em `Aluno`, adicionar `_historico` + `Historico` (padrão da Spec 003) e
  fazer `AtualizarDados` devolver `(adicionadas, removidas)`.
  - Validação: testes de diff com adição, remoção, troca e nenhuma mudança.

## Persistência

- [ ] T4: registrar `DbSet`s, índice único `(NomeNormalizado, Categoria)` e índice
  `(AlunoId, CriadoEmUtc)`; gerar migration `RestricoesEAlergias`.
  - Validação: `dotnet ef migrations add RestricoesEAlergias`; teste de modelo
  confirma os índices.
- [ ] T5: criar `IRestricaoRepository`/`RestricaoRepository` com `ObterAtivas`,
  `Listar`, `Salvar` e `SalvarHistorico`.
  - Validação: teste com SQLite/SQLite em memória confirma unicidade e paginação.

## Aplicação

- [ ] T6: em `AlunoService`, validar o catálogo antes de criar/atualizar e envolver
  aluno + histórico em transação.
  - Validação: teste de restrição desconhecida não persiste aluno;
  - Validação: teste de falha no histórico faz rollback do aluno.
- [ ] T7: criar `RestricaoService` para o catálogo (criar, editar, desativar,
  reativar) e para o histórico paginado com escopo por papel.
  - Validação: testes de serviço para duplicidade e para escopo 403.

## API

- [ ] T8: criar `RestricaoController` com as rotas da tabela do design.
  - Validação: testes HTTP 400/403 e formato `{ erro }`.
- [ ] T9: adicionar `GET /api/alunos/{id}/restricoes/historico` em `AlunoController`.
  - Validação: testes HTTP de paginação e da matriz de acesso.

## Frontend

- [ ] T10: criar tela de catálogo em `pages/nutricao/restricoes/`.
  - Validação: `npm run build`; criar, editar e desativar pela interface.
- [ ] T11: na tela do aluno, trocar o campo de texto livre por multi-seleção do
  catálogo ativo e adicionar a linha do tempo do histórico.
  - Validação: salvar aluno com restrição inválida exibe a mensagem do backend;
  - Validação: merendeira e responsável veem somente leitura.

## Fechamento

- [ ] T12: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas do README coincidem com os controllers.
- [ ] T13: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Catálogo e histórico funcionando ponta a ponta.
- Nenhuma gravação de aluno aceita restrição fora do catálogo.
- Gates verdes.
