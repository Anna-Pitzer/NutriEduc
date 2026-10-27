---
name: crud-aluno-completo
status: created
references: V2, L8
---

# Tasks — CRUD completo de aluno

## Pré-condição

- [ ] T0: Spec 004 implementada (`Aluno.AtualizarDados` disponível) **e** Spec 005
  T1–T3 concluídas (projeto `backend.Tests`, `Program` parcial e fakes).

## Backend

- [ ] T1: adicionar `AtualizarAluno(int id, DadosAlunoDTO dto)` em
  `backend/API/Services/AlunoService.cs`, delegando ao domínio e persistindo.
  - Validação: `dotnet build backend/backend.csproj`.
- [ ] T2: adicionar `PUT /api/aluno/{id:int}` em
  `backend/API/Controllers/AlunoController.cs`, com 404 para inexistente.
  - Validação: `GET /api/aluno/perfil/{id}` após o `PUT` devolve os valores gravados,
    **inclusive `restricoes`** — a versão anterior do design da Spec 004 não as
    atualizava. Esse é o caso de teste obrigatório desta task.
- [ ] T3: cobrir o caso de uso com teste de serviço usando repositório fake.
  - Pré-condição: Spec 005 T1–T3 — o projeto de teste **não** nasce nesta spec.
  - Validação: `dotnet test` verde.

## Frontend

- [ ] T4: adicionar `atualizarAluno(id, dados)` em
  `frontend/src/services/AlunoApi.ts`.
  - Validação: `npm run build` em `frontend`.
- [ ] T5: extrair o mapeamento `AlunoResposta → FormularioAluno` para uso compartilhado
  entre cadastro e edição.
  - Validação: `npm run lint` sem novo aviso.
- [ ] T6: adicionar ação "Editar" e modo de edição em
  `frontend/src/pages/VisualizarAlunos.tsx`.
  - Validação: editar um aluno e confirmar a alteração na listagem.
- [ ] T7: recarregar a lista após salvar e exibir retorno de sucesso/erro.
  - Validação: comportamento observado em execução manual.

## Fechamento

- [ ] T8: atualizar a tabela de endpoints do `README.md` com `PUT /api/aluno/{id}`.
  - Validação: tabela do README idêntica ao mapa de controllers.
- [ ] T9: executar os gates completos (`dotnet build`, `dotnet test`, `npm run build`,
  `npm run lint`) e registrar a saída.

## Definição de pronto

- Todos os critérios de aceitação de `requirements.md` verificados com evidência.
- Nenhuma regra de validação no controller nem no serviço.
