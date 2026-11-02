---
name: historico-alteracoes-aluno
status: planned
references: V12, ADR-004, ADR-009
---

# Requirements — Histórico de alterações do aluno

## Objetivo

A Spec 010 entregou histórico append-only **somente** para restrições. Nome, nascimento,
turma, telefone, observação, anafilaxia, responsável e unidade não deixam rastro: quem
consulta o cadastro não sabe o que mudou, quando, nem por quem. Esta spec entrega uma
trilha única por aluno, cobrindo cadastro, dados e restrições, com leitura paginada e
escopo por papel.

## Escopo

- Backend: entidade `HistoricoAluno` genérica, diff calculado no domínio, repositório de
  leitura, migração de dados dos eventos existentes e endpoint único de consulta.
- Frontend: linha do tempo única na tela do aluno, substituindo o painel que hoje só
  mostra restrições.
- Fora de escopo: histórico de cardápio (Spec 015), de ocorrências (Spec 016) e de
  solicitações (Spec 017) — cada agregado tem a própria trilha.

## Requisitos funcionais

- RF1: `HistoricoAluno` tem `Id`, `AlunoId`, `Tipo`, `Campo`, `ValorAnterior`,
  `ValorNovo`, `AutorNome` e `CriadoEmUtc`. `Tipo` é `Criado`, `Alterado`,
  `RestricaoAdicionada` ou `RestricaoRemovida`.
- RF2: criar um aluno grava um evento `Criado` e um evento por restrição informada.
- RF3: cada campo alterado em uma atualização grava um evento `Alterado`, com o nome do
  campo, o valor anterior e o novo. Campo não alterado não gera evento.
- RF4: restrições adicionadas e removidas continuam gerando um evento por nome, como na
  Spec 010, agora como `RestricaoAdicionada`/`RestricaoRemovida` com `Campo = "Restrições"`.
- RF5: nenhum evento é atualizado ou removido; a exclusão do aluno remove a trilha em
  cascata (o dado pessoal não sobrevive ao titular).
- RF6: `GET /api/aluno/{id}/historico?pagina=&tamanho=` devolve os eventos em ordem
  decrescente de data, paginado (`tamanho` de 1 a 100), autorizado por papel:
  `Nutricionista` (global), `AdminEscola` (escola do aluno), `Merendeira` (unidade do
  aluno), `Responsavel` (aluno vinculado).
- RF7: `GET /api/aluno/{id}/restricoes/historico` continua respondendo com o mesmo formato
  da Spec 010, agora derivado da trilha única (compatibilidade; marcada como obsoleta).
- RF8: a tela do aluno exibe uma linha do tempo única com tipo, campo (quando houver),
  valor anterior → novo, autor e data.
- RF9: merendeira e responsável têm leitura somente; nenhuma rota de escrita aceita esses
  papéis.

## Requisitos não funcionais

- RNF1: o diff de campos é regra de domínio — `Aluno.AtualizarDados` devolve a lista de
  alterações; nenhum serviço compara campos.
- RNF2: aluno e eventos são gravados na mesma unidade de persistência, com uma única
  `SaveChanges` (mantém o padrão da Spec 010).
- RNF3: datas em UTC no banco e ISO 8601 na API.
- RNF4: leitura sempre paginada e indexada por `(AlunoId, CriadoEmUtc)`.
- RNF5: `Aluno.Restricoes` continua `IReadOnlyList<string>`; nenhuma rota existente de
  aluno muda de contrato.

## Restrições

- Migration nova que cria `HistoricosAlunos`, migra os eventos de
  `HistoricosRestricoesAlunos` e remove a tabela antiga. Migrations anteriores não são
  reescritas.
- Nenhuma regra de negócio no controller.

## Dependências

- Spec 010 implementada (histórico de restrições e `CatalogoDeRestricoes`).
- Spec 009 implementada (escopo por papel).

## Casos de borda

- Atualização sem mudança de campo nem de restrição → zero eventos.
- Campo que passa de vazio para preenchido e vice-versa → valor anterior vazio é
  preservado como vazio, nunca como `null` no DTO.
- Renomear o autor depois não altera o histórico (snapshot).
- Aluno inexistente → 404 no escopo global, 403 fora do escopo.
- Trilha migrada de aluno removido segue a cascata do aluno.
- Alteração de anafilaxia gera evento com `ValorAnterior`/`ValorNovo` `"Sim"`/`"Não"`.

## Critérios de aceitação

- [ ] Criar aluno grava `Criado` + um evento por restrição.
- [ ] `PUT` que só muda o telefone grava exatamente um evento `Alterado` de `Telefone`.
- [ ] `PUT` sem mudança alguma não grava evento.
- [ ] Trocar uma restrição grava `RestricaoRemovida` e `RestricaoAdicionada`.
- [ ] Histórico de aluno de outra unidade devolve 403 para `Merendeira`.
- [ ] Histórico de aluno não vinculado devolve 403 para `Responsavel`.
- [ ] A rota antiga `/restricoes/historico` continua devolvendo os mesmos campos.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Cadastrar um aluno, alterar telefone e uma restrição, consultar os dois endpoints e
confirmar a matriz de acesso. Executar `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e,
em `frontend`, `npm run build && npm run lint`.
