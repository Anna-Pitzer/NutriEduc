---
name: crud-aluno-completo
status: created
references: V2, L8
---

# Requirements — CRUD completo de aluno

## Objetivo

Fechar o ciclo `C+R+U+D` na entidade `Aluno`, hoje com Criar
(`POST /api/aluno/registrar`), Ler (`GET /api/aluno`, `GET /api/aluno/perfil/{id}`)
e Excluir (`DELETE /api/aluno/{id}`), mas sem atualização. O `Update` existente
pertence a outro recurso (`Usuario`), o que não atende ao requisito de CRUD
funcional na mesma entidade.

## Escopo

- Backend: `PUT /api/aluno/{id}`, caso de uso em `AlunoService`, comportamento em
  `Aluno`, persistência em `IAlunoRepository`.
- Frontend: `atualizarAluno` em `AlunoApi.ts` e modo de edição na tela de alunos.
- Fora de escopo: invariantes do `Aluno` (Spec 004), restrições protegidas
  (Spec 003), autorização do endpoint (Spec 006).

## Requisitos funcionais

- RF1: `PUT /api/aluno/{id}` atualiza um aluno existente e devolve 200 com o aluno
  atualizado no formato `DadosAlunoDTO`.
- RF2: `PUT` em ID inexistente devolve 404.
- RF3: a atualização persiste; `GET /api/aluno/perfil/{id}` após o `PUT` devolve os
  valores gravados.
- RF4: `AlunoService.AtualizarAluno(int id, DadosAlunoDTO dto)` devolve
  `DadosAlunoDTO?`, com `null` quando o aluno não existe.
- RF5: o frontend permite editar um aluno a partir da tela de listagem,
  reaproveitando o formulário de cadastro em modo de edição.
- RF6: a tela de edição carrega os dados atuais via `GET /api/aluno/perfil/{id}`
  antes de exibir o formulário.

## Requisitos não funcionais

- RNF1: nenhuma regra de negócio no controller.
- RNF2: reutilizar `DadosAlunoDTO`; não criar contrato paralelo.
- RNF3: a assinatura dos endpoints existentes não muda.

## Restrições

- Sem alteração de schema nesta spec.
- Sem dependência nova no frontend.
- Nomenclatura em português, conforme padrão do código.

## Dependências

- `Aluno.AtualizarDados` vem da Spec 004. Implementar 004 antes de 001.

## Casos de borda

- ID inexistente → 404, sem efeito colateral.
- Campos ausentes → tratados pela Spec 004.
- Aluno excluído entre leitura e gravação → 404 na gravação.
- Edição sem alteração → 200 com a mesma resposta.

## Critérios de aceitação

- [ ] `PUT /api/aluno/{id}` devolve 200 com o aluno atualizado.
- [ ] `PUT` com ID inexistente devolve 404.
- [ ] `GET /api/aluno/perfil/{id}` após o `PUT` devolve os valores gravados.
- [ ] `AlunoService.AtualizarAluno` não contém regra de validação própria.
- [ ] `atualizarAluno` existe em `frontend/src/services/AlunoApi.ts` e envia `PUT`.
- [ ] A tela de alunos abre um aluno em modo de edição e salva a alteração.
- [ ] `dotnet build backend/backend.csproj` conclui sem erro.
- [ ] `npm run build` no frontend conclui sem erro.
- [ ] `npm run lint` no frontend conclui sem erro.

## Validação

Cadastrar aluno, editar, reabrir a listagem e confirmar os valores. Gates em
`PLANEJAMENTO.md` §Gates por fase.
