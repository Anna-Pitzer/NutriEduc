---
name: historico-alteracoes-aluno
status: planned
references: V12, ADR-004, ADR-009
---

# Tasks — Histórico de alterações do aluno

## Pré-condição

- [x] T0: Spec 010 implementada.
  - Validação: `dotnet test` verde; `HistoricoRestricaoAluno` existe e é gravado na
    mesma `SaveChanges` do aluno.

## Domínio

- [x] T1: criar `TipoEventoAluno`, `HistoricoAluno` e `AlteracaoDeDado`.
  - Validação: testes de construtor, snapshot de autor e evento `Criado`.
- [x] T2: substituir `HistoricoRestricaoAluno` por `HistoricoAluno` dentro de `Aluno`,
  mantendo `RegistrarHistorico` como único ponto de escrita.
  - Validação: testes de criação e de evento de restrição.
- [x] T3: fazer `AtualizarDados` devolver o diff de campos e de restrições.
  - Validação: testes de campo único, vários campos, nenhuma mudança e conversão de
    anafilaxia/unidade.

## Persistência

- [x] T4: registrar `HistoricoAluno` no `ConnectionContext`, com índice
  `(AlunoId, CriadoEmUtc)` e cascata na exclusão do aluno.
  - Validação: teste de modelo confirma tabela, índice e FK.
- [x] T5: criar migration `HistoricoDeAlteracoesDoAluno` copiando
  `HistoricosRestricoesAlunos` e removendo a tabela antiga.
  - Validação: teste com SQLite em memória aplica a migration e confirma os eventos
    migrados com autor e data originais.

## Aplicação

- [x] T6: `AlunoService` grava o diff (campos + restrições) e o evento `Criado`.
  - Validação: teste de `PUT` que só muda telefone gera exatamente um evento.
- [x] T7: criar `HistoricoAlunoService` com leitura paginada e escopo por papel, e fazer
  `RestricaoService` derivar o histórico de restrições da trilha única.
  - Validação: testes de escopo 403/404 e de formato da rota antiga.

## API

- [x] T8: adicionar `GET /api/aluno/{id}/historico` em `AlunoController`.
  - Validação: testes HTTP de paginação (`tamanho` 1–100) e da matriz de acesso.
- [x] T9: manter `GET /api/aluno/{id}/restricoes/historico` com o mesmo JSON.
  - Validação: teste HTTP do contrato antigo.

## Frontend

- [x] T10: substituir o painel de restrições por linha do tempo única em
  `VisualizarAlunos.tsx` (tipo, campo, anterior → novo, autor, data).
  - Validação: `npm run build`; aluno com alteração de telefone e de restrição exibe os
    três eventos na ordem correta.

## Fechamento

- [x] T11: atualizar a tabela de endpoints do `README.md`.
  - Validação: `DocumentationTests` verde.
- [x] T12: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Uma única trilha por aluno cobre criação, campos e restrições.
- A rota antiga de restrições continua funcionando com o mesmo JSON.
- Nenhuma alteração de dado do aluno fica sem evento.
- Gates verdes.
