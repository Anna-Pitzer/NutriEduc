---
name: restricoes-e-alergias
status: created
references: V1, V6, ADR-004, L2
---

# Tasks — Restrições e alergias

## Pré-condição

- [x] T0: Specs 003, 004 e 009 implementadas.
  - Validação: `dotnet test` verde; `Aluno.Restricoes` é `IReadOnlyList<string>`.

## Domínio

- [x] T1: criar `RestricaoAlimentar` com `Categoria`, `Ativa`, `NomeNormalizado` e
  `Normalizar` estático.
  - Validação: testes de limite 2–60, normalização de acento/caixa e desativação.
- [x] T2: criar `HistoricoRestricaoAluno` com `Acao`, `AutorNome` e `CriadoEmUtc`.
  - Validação: teste de construtor interno e de snapshot de autor.
- [x] T3: em `Aluno`, adicionar `_historico` + `Historico` (padrão da Spec 003) e
  fazer `AtualizarDados` devolver `(adicionadas, removidas)`.
  - Validação: testes de diff com adição, remoção, troca e nenhuma mudança.

## Persistência

- [x] T4: registrar `DbSet`s, índice único `(NomeNormalizado, Categoria)` e índice
  `(AlunoId, CriadoEmUtc)`; gerar migration `RestricoesEAlergias`.
  - Validação: `dotnet ef migrations add RestricoesEAlergias`; teste de modelo
  confirma os índices.
- [x] T5: criar `IRestricaoRepository`/`RestricaoRepository` com `ObterAtivas`,
  `Listar`, `Salvar` e `SalvarHistorico`.
  - Validação: teste com SQLite/SQLite em memória confirma unicidade e paginação.

## Aplicação

- [x] T6: em `AlunoService`, validar o catálogo antes de criar/atualizar e envolver
  aluno + histórico em transação.
  - Validação: teste de restrição desconhecida não persiste aluno;
  - Validação: teste de falha no histórico faz rollback do aluno.
- [x] T7: criar `RestricaoService` para o catálogo (criar, editar, desativar,
  reativar) e para o histórico paginado com escopo por papel.
  - Validação: testes de serviço para duplicidade e para escopo 403.

## API

- [x] T8: criar `RestricaoController` com as rotas da tabela do design.
  - Validação: testes HTTP 400/403 e formato `{ erro }`.
- [x] T9: adicionar `GET /api/alunos/{id}/restricoes/historico` em `AlunoController`.
  - Validação: testes HTTP de paginação e da matriz de acesso.

## Frontend

- [x] T10 (revisado em 2026-11-01): a tela de catálogo foi removida por decisão do
  proprietário; as opções fixas voltaram para a tela do aluno, espelhando o catálogo semeado.
  - Validação: `npm run build`; a tela do aluno exibe as seis opções e nenhuma tela permite
  criar, editar ou desativar restrição.
- [x] T11: na tela do aluno, trocar o campo de texto livre por multi-seleção do
  catálogo ativo e adicionar a linha do tempo do histórico.
  - Validação: salvar aluno com restrição inválida exibe a mensagem do backend;
  - Validação: merendeira e responsável veem somente leitura.

## Fechamento

- [x] T12: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas do README coincidem com os controllers.
- [x] T13: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Revisão 2026-11-01

- [x] T14: mover a regra de existência para o domínio (`CatalogoDeRestricoes`) e remover a
  dependência de `AlunoService` em `RestricaoService`.
  - Validação: `CatalogoDeRestricoesTests` (canônico, acento/caixa, desconhecido, inativo).
- [x] T15: semear o catálogo padrão na migration `SeedCatalogoRestricoes` e remover a tela
  de catálogo, o item de menu, a rota e as funções de CRUD sem consumidor no frontend.
  - Validação: `dotnet test` com SQLite em memória (semente aplicada no startup);
  - Validação: `npm run build` e `npm run lint` sem código morto.

## Definição de pronto (revisada em 2026-11-01)

- Opções de restrição e alergia fixas na tela do aluno, espelhando o catálogo semeado.
- Nenhuma tela cria, edita ou desativa restrição; a API mantém essa capacidade.
- Nenhuma gravação de aluno aceita restrição fora do catálogo.
- Gates verdes.

## Definição de pronto

- Catálogo e histórico funcionando ponta a ponta.
- Nenhuma gravação de aluno aceita restrição fora do catálogo.
- Gates verdes.
