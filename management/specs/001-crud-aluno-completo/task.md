---
name: crud-aluno-completo
status: created
references: V2, L8
---

# Tasks — CRUD completo de aluno

## Pré-condição

- [x] T0: Spec 004 implementada (`Aluno.AtualizarDados` disponível) **e** Spec 005
  T1–T3 concluídas (projeto `backend.Tests`, `Program` parcial e fakes).

## Backend

- [x] T1: adicionar `AtualizarAluno(int id, DadosAlunoDTO dto)` em
  `backend/API/Services/AlunoService.cs`, delegando ao domínio e persistindo.
  - Validação: `dotnet build backend/backend.csproj`.
- [x] T2: adicionar `PUT /api/aluno/{id:int}` em
  `backend/API/Controllers/AlunoController.cs`, com 404 para inexistente.
  - Validação: `GET /api/aluno/perfil/{id}` após o `PUT` devolve os valores gravados,
    **inclusive `restricoes`** — a versão anterior do design da Spec 004 não as
    atualizava. Esse é o caso de teste obrigatório desta task.
- [x] T3: cobrir o caso de uso com teste de serviço usando repositório fake.
  - Pré-condição: Spec 005 T1–T3 — o projeto de teste **não** nasce nesta spec.
  - Validação: `dotnet test` verde.

## Frontend

- [x] T4: adicionar `atualizarAluno(id, dados)` em
  `frontend/src/services/AlunoApi.ts`.
  - Validação: `npm run build` em `frontend`.
- [x] T5: extrair o mapeamento `AlunoResposta → FormularioAluno` para uso compartilhado
  entre cadastro e edição.
  - Validação: `npm run lint` sem novo aviso.
- [x] T6: adicionar ação "Editar" e modo de edição em
  `frontend/src/pages/VisualizarAlunos.tsx`.
  - Validação: editar um aluno e confirmar a alteração na listagem.
- [x] T7: recarregar a lista após salvar e exibir retorno de sucesso/erro.
  - Validação: comportamento observado em execução manual.

## Fechamento

- [x] T8: atualizar a tabela de endpoints do `README.md` com `PUT /api/aluno/{id}`.
  - Validação: tabela do README idêntica ao mapa de controllers.
- [x] T9: executar os gates completos (`dotnet build`, `dotnet test`, `npm run build`,
  `npm run lint`) e registrar a saída.

## Definição de pronto

- Todos os critérios de aceitação de `requirements.md` verificados com evidência.
- Nenhuma regra de validação no controller nem no serviço.

## Registro de execução

Verificado contra o código em 2026-10-30 e reauditado em 2026-10-31. O `status` do frontmatter não foi alterado:
`created` é o valor que o `/spec-new` grava (`templates/prompts/spec_new.md.ejs:125`),
e o ciclo de vida é propriedade do `roadmap.md`.

**Concluído**

| Task | Evidência |
| --- | --- |
| T0 | `Aluno.AtualizarDados` (`backend/Domain/Entities/Aluno.cs`); `backend.Tests/backend.Tests.csproj`; `public partial class Program` (`backend/Program.cs`) e os fakes em `backend.Tests/Fakes/` |
| T1 | `AlunoService.AtualizarAluno` (`backend/API/Services/AlunoService.cs`) |
| T2 | `PUT api/aluno/{id:int}` com 404/403 por escopo (`backend/API/Controllers/AlunoController.cs`) |
| T3 | `AlunoServiceTests.AtualizarAluno_Existente/Inexistente/ComDadosInvalidos` (`backend.Tests/Services/AlunoServiceTests.cs`) |
| T4 | `atualizarAluno` (`frontend/src/services/AlunoApi.ts`) |
| T5 | `mapearAlunoParaFormulario` (`frontend/src/pages/nutricao/alunos/CadastrarAlunos.tsx`) — ver D1 |
| T6 | botão Editar (`VisualizarAlunos.tsx`), `useParams` (`CadastrarAlunos.tsx`) e rota `/alunos/editar/:id` (`frontend/src/routes/routes.tsx`) |
| T7 | mensagens de sucesso/erro (`CadastrarAlunos.tsx`); lista recarrega no `useEffect` (`VisualizarAlunos.tsx`) — ver D2 |
| T8 | `PUT /api/aluno/{id}` na tabela do `README.md` |
| T9 | 2026-10-31 (auditoria): `dotnet test` 246/246; `npm run build` OK (Vite, 1855 módulos); `npm run lint` 0 erros e 5 avisos |

**Pendente**

- Nenhuma task pendente.

**Desvios registrados**

- D1: o mapeamento não foi extraído para módulo próprio. Criação e edição vivem no mesmo
  componente (`CadastrarAlunos.tsx`), então a função local já é a única cópia.
- D2: não há refetch explícito após salvar. A listagem é outra rota e busca os dados no
  `useEffect` de montagem, portanto voltar para ela já relê do backend.
- D3 (corrigido na auditoria de 2026-10-31): a edição apagava `UnidadeId` do aluno —
  `AlunoCadastro` não carregava o campo e o `PUT` sem `unidadeId` executava
  `DefinirUnidade(null)`. O campo passou a ser preservado no formulário e reenviado.
- D4 (limitação conhecida): escolas e séries em `CadastrarAlunos.tsx` são listas estáticas;
  editar aluno com `escola` fora delas falha na validação local. Resolver exige consumir
  `GET /api/escolas` (Spec 021).
- A validação por HTTP deixou de ser lacuna: `Backend.Tests/Integration/AlunoApiTests`
  cobre POST, GET, PUT e 404 via `WebApplicationFactory`. A persistência real no
  arquivo `database.db` continua não exercitada (PEND-018).
