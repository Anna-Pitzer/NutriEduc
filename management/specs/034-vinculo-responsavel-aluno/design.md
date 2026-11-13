---
name: vinculo-responsavel-aluno
status: created
references: Spec 034; Spec 027; Spec 028; Spec 033; ADR-020; ADR-021
---

# Design — Vínculo responsável↔aluno e busca de alunos

## Decisões

- **D1 — Vínculo é invariante de cadastro.** `CriarUsuarioAdministrado` e `AtualizarUsuario`
  exigem `alunosIds` quando o papel resultante é `Responsavel` e a conta ainda não tem vínculo.
  A validação roda antes de mutar a entidade; a gravação é atômica.
- **D2 — Vínculo reutiliza o dono existente.** Nenhum serviço novo de vínculo: `IVinculoRepository`
  e `TrilhaAcao` já cobrem o par `(AlunoId, ResponsavelUsuarioId)`. O evento é `VinculoCriado`,
  igual ao de `VinculoService`.
- **D3 — Busca é filtro de negócio.** `FiltroAluno` ganhou `Busca` e `Serie`; `AlunoRepository`
  aplica ambos no SQL depois do recorte de escopo. `FiltrosDeConsulta.ParaAluno` traduz o pedido
  e nunca amplia o alcance.
- **D4 — Contato manual e conta vinculada são dados distintos.** `Aluno.Responsavel` continua
  sendo o contato do cadastro; a página exibe os dois. Sincronização automática exigiria regra de
  desempate com múltiplos responsáveis e foi descartada.
- **D5 — Escrita de vínculo continua no dono.** A página de alunos consome as rotas existentes
  `GET/POST/DELETE /api/aluno/{alunoId}/responsaveis`; a matriz e o `VinculoService` decidem.
- **D6 — Área de pais usa dado público.** `CardapiosPublicados` passou a listar unidades por
  `GET /api/unidades/ativas`; `GET /api/unidades` continua restrita à hierarquia.

## Backend

- `backend/Domain/FiltroAluno.cs`: `Busca`, `Serie`.
- `backend/Infrastructure/Repositories/AlunoRepository.cs`: filtros de nome e série no SQL.
- `backend/Application/FiltrosDeConsulta.cs`: `ParaAluno(..., busca, serie)`.
- `backend/API/Services/AlunoService.cs`, `backend/API/Controllers/AlunoController.cs`:
  `busca`/`serie` na listagem.
- `backend/Application/CriarUsuarioDTO.cs`, `AtualizarUsuarioDTO.cs`: `AlunosIds`.
- `backend/API/Services/UsuarioService.cs`: injeção de `IAlunoRepository`/`IVinculoRepository`;
  `ResolverAlunosDoResponsavel`; transação única com `Salvar()` antes dos vínculos.
- `backend/API/Services/VinculoService.cs`: recusa vínculo com usuário desativado.

## Frontend

- `frontend/src/components/SeletorDeAlunos.tsx`: busca por nome, escola e série + seleção múltipla.
- `frontend/src/services/AlunoApi.ts`: `buscarAlunos({ busca, escolaId, serie })`.
- `frontend/src/services/VinculoApi.ts`: listar/vincular/desvincular responsáveis.
- `frontend/src/services/UsuarioAdminApi.ts`: `alunosIds`; `Responsavel` deixa de exigir escola.
- `frontend/src/pages/secretaria/SolicitacoesAcesso.tsx`: seletor na aprovação.
- `frontend/src/pages/secretaria/Usuarios.tsx`: seletor no cadastro e na definição de papel.
- `frontend/src/pages/nutricao/alunos/VisualizarAlunos.tsx`: coluna e painel de responsáveis.
- `frontend/src/pages/publico/CardapiosPublicados.tsx`: unidades ativas.
- `frontend/src/pages/ocorrencias/OcorrenciasPage.tsx`: não chama catálogo de alimentos para
  `Responsavel`.

## Fluxos

1. **Aprovação:** fila → seletor de alunos (busca) → `POST /api/solicitacoes-acesso/{id}/aprovar`
   com `alunosIds` → papel + vínculos + trilha + notificação na mesma transação.
2. **Cadastro direto:** formulário de usuário → papel `Responsavel` → seletor → `POST /api/usuarios`
   com `alunosIds` → usuário + vínculos + trilha.
3. **Definição de papel:** linha da conta → papel `Responsavel` → seletor (se ainda não houver
   vínculo) → `PUT /api/usuarios/{id}` com `alunosIds`.
4. **Página de alunos:** linha → painel → lista vinculados + busca de responsáveis → POST/DELETE
   no vínculo → recarga da lista.

## Riscos e limites

- SQLite compara texto sem acento: a busca por nome herda a limitação registrada em PEND-031/032.
- `UsuarioService` acumula cadastro, perfil e administração (PEND-027); a mudança adiciona
  dependências, não um novo serviço, para não ampliar o escopo.
- A promoção formal no roadmap depende do motor SDD (PEND-028); os documentos ficam em `created`.
