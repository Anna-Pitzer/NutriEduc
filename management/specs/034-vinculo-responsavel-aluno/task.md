---
name: vinculo-responsavel-aluno
status: created
references: Spec 034
---

# Tasks — Vínculo responsável↔aluno e busca de alunos

## Backend

- [x] T1: adicionar `Busca`/`Serie` a `FiltroAluno` e aplicá-los no `AlunoRepository`.
  - Validação: `GET /api/aluno?busca=&serie=` filtra no SQL.
- [x] T2: expor `busca`/`serie` em `AlunoController.Listar` e `AlunoService.ListarAlunos`.
  - Validação: `VinculoResponsavelApiTests.BuscarAlunos_FiltraPorNomeEscolaESerie`.
- [x] T3: adicionar `AlunosIds` a `CriarUsuarioDTO` e `AtualizarUsuarioDTO`.
- [x] T4: injetar `IAlunoRepository`/`IVinculoRepository` em `UsuarioService` e criar
  `ResolverAlunosDoResponsavel`.
  - Validação: aluno inexistente/fora da unidade recusado antes de mutar.
- [x] T5: exigir vínculo na criação administrativa e gravar usuário + vínculos + trilha na mesma
  transação.
  - Validação: `CriarResponsavel_SemAlunos_Retorna400`,
  `CriarResponsavel_ComAluno_CriaVinculoNoMesmoAto`,
  `CriarResponsavel_AlunoDeOutraUnidade_Retorna404`.
- [x] T6: exigir vínculo ao definir papel `Responsavel` quando a conta ainda não tem nenhum.
  - Validação: `DefinirPapelResponsavel_SemAlunos_Retorna400_ComAluno_Vincula`.
- [x] T7: recusar vínculo de usuário desativado em `VinculoService`.
  - Validação: conflito `409` sem gravar.

## Frontend

- [x] T8: criar `SeletorDeAlunos` com busca por nome, escola e série.
- [x] T9: estender `buscarAlunos` e criar `VinculoApi`.
- [x] T10: usar o seletor na aprovação de acesso.
  - Validação: `npm run build`.
- [x] T11: usar o seletor no cadastro/definição de papel e enviar `alunosIds`.
- [x] T12: exibir contato + responsáveis vinculados na página de alunos; ADM vincula/desvincula.
- [x] T13: corrigir `HomePais` para listar unidades ativas e remover chamadas `403` por papel.

## Validação final

- [x] `dotnet test backend.Tests/backend.Tests.csproj` → 792/792.
- [x] `npm run build` → sucesso.
- [x] `npm run lint` → 0 erros (6 avisos pré-existentes).
- [ ] Validação manual no navegador por um integrante do grupo.
