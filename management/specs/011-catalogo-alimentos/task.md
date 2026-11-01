---
name: catalogo-alimentos
status: created
references: V6, ADR-004, L2, L9
---

# Tasks — Catálogo de alimentos

## Pré-condição

- [ ] T0: Specs 009, 010 e 007 implementadas.
  - Validação: `dotnet test` verde; `/api/restricoes` responde com o catálogo semeado.

## Domínio

- [ ] T1: adicionar `Medida(decimal, string)` a `ValidacaoDeCampo` (mínimo zero, no
  máximo duas casas, mensagem nomeando o campo) e criar `ValoresNutricionais` como
  `record` com `Criar` usando essa regra nas seis medidas.
  - Validação: testes de valor negativo, três casas e zero; testes existentes de
  `ValidacaoDeCampo.Texto` e `Telefone` continuam verdes.
- [ ] T2: criar `CategoriaAlimento` com nome 2–60, `NomeNormalizado`, ativo e
  `Desativar`/`Reativar`.
  - Validação: testes de limite 2–60, de normalização e de transição de estado.
- [ ] T3: criar `AlimentoRestricao` (somente `RestricaoId`, sem motivo) e `Alimento` com
  `_vinculos` protegidos e `ValidarDados` compartilhado entre construtor e
  `AtualizarDados`.
  - Validação: construtor inválido não atribui nada parcialmente; vínculo repetido lança;
  `AtualizarDados` válido altera todos os campos e o inválido não altera nenhum.

## Persistência

- [ ] T4: mapear `ValoresNutricionais` como tipo complexo; `Alimento` com índice único
  global em `NomeNormalizado`; `AlimentoRestricao` com chave composta e sem coluna de
  motivo; criar a migration `CatalogoAlimentos` com o seed idempotente das seis
  categorias.
  - Validação: `dotnet ef migrations add CatalogoAlimentos`; teste de modelo confirma
  `decimal(10,2)` e chave composta; aplicar a migration duas vezes não duplica
  categorias.
- [ ] T5: criar `FiltroAlimento`, `ICategoriaAlimentoRepository`,
  `IAlimentoRepository`/`AlimentoRepository` com `Listar(filtro, pagina, tamanho)`
  devolvendo `(Itens, Total)`, `ObterComVinculos`, `ExisteNomeNormalizado(id?)`,
  `Add` e `Update`.
  - Validação: teste em SQLite confirma busca por prefixo, fallback para "contém",
  paginação, e que alimento de categoria desativada continua listado.

## Aplicação e API

- [ ] T6: extrair a validação de página/tamanho de `HistoricoAlunoService` para
  `Application/Paginacao.cs` e reutilizar nos dois serviços.
  - Validação: `dotnet test` verde, inclusive os testes da Spec 023; nenhuma segunda
  cópia da regra (busca por `ValidarPaginacao` encontra um único dono).
- [ ] T7: criar `AlimentoService` validando categoria ativa, restrições existentes e
  ativas, nome único (409) e recusando `incluirInativos` sem `Nutricionista` (403);
  gravar alimento e vínculos na mesma transação (`IUnidadeDeTrabalho`).
  - Validação: testes de serviço para 400/403/409 sem persistência; teste de falha no
  meio da gravação não deixa vínculo órfão.
- [ ] T8: criar `AlimentoController` e `CategoriaAlimentoController` com as rotas e
  políticas do design, traduzindo `Resultado<T>` como na Spec 010.
  - Validação: testes HTTP de 201/400/403/404/409, do formato `{ erro }` e do envelope
  `PaginaDTO`.

## Frontend

- [ ] T9: criar `AlimentoApi.ts` com o contrato aninhado e o envelope paginado; remover
  `buscarAlimentos` e a chamada a `/api/alimentos` de `CardapioApi.ts`.
  - Validação: `grep` não encontra `/api/alimentos` em `CardapioApi.ts`.
- [ ] T10: ajustar `MontarCardapio.tsx` (remover `alimentosFake`, consumir
  `listarAlimentos`) e `Cardapio.tsx` (composição lê `valores`); criar
  `pages/nutricao/alimentos/ListarAlimentos.tsx` e `FormularioAlimento.tsx` com busca,
  filtro de categoria, paginação, os seis valores e seleção múltipla de restrições.
  - Validação: `npm run build` e `npm run lint` depois de T9 e de novo depois de T10;
  criar, editar e desativar pela interface.

## Fechamento

- [ ] T11: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas documentadas coincidem com os controllers.
- [ ] T12: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Catálogo de alimentos funcional ponta a ponta, com composição e vínculos sem motivo
  duplicado.
- Nenhuma rota órfã de alimento no frontend e nenhum dado falso de alimento nas telas.
- Gates verdes.
