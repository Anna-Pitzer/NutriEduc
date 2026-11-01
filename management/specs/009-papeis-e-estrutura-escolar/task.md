---
name: papeis-e-estrutura-escolar
status: implemented
references: V6, ADR-002, L4
---

# Tasks — Papéis e estrutura escolar

## Pré-condição

- [x] T0: Spec 002 implementada (senha com hash) e Spec 007 implementada (contrato
  `{ erro }` e registro de serviços correto).
  - Validação: `dotnet test` verde; `GET` de rota inexistente devolve `{ erro }`.

## Domínio

- [x] T1: criar `backend/Domain/Entities/PapelUsuario.cs` com os cinco valores.
  - Validação: compila; nenhum `string` de papel circula em serviços novos.
- [x] T2: criar `backend/Domain/Entities/Escola.cs` e `UnidadeEscolar.cs` com nome
  normalizado, limites 2–120 e `Desativar`/`Reativar`.
  - Validação: testes de nome vazio, curto, longo e com espaços.
- [x] T3: alterar `backend/Domain/Entities/Usuario.cs`: adicionar `Papel`,
  `EscolaId`, `UnidadeId`, `Ativo` e `DefinirPapel`; reduzir a um construtor público
  de criação (PEND-002).
  - Validação: teste de papel que exige escola sem `escolaId` lança
    `ArgumentException`; teste de unidade em papel que não aceita lança.
- [x] T4: criar `VinculoResponsavelAluno` com chave composta `(AlunoId, ResponsavelUsuarioId)`.
  - Validação: teste de igualdade e de chave; duplicata detectada na camada de serviço.

## Persistência

- [x] T5: registrar `DbSet`s e índices únicos em `ConnectionContext`; adicionar
  `Aluno.UnidadeId` anulável com FK `SetNull`.
  - Validação: migration `20261030220259_PapeisEEscolas` criada e aplicada no startup;
  - Validação: teste de modelo confirma as colunas de `Aluno` (inclui `UnidadeId`).
  - Desvio D5: o teste de modelo verifica a lista de colunas, não os índices; a
    unicidade de `Escolas(Nome)` e `Unidades(EscolaId, Nome)` é provada por HTTP (409).
- [x] T6: criar `IEscolaRepository`/`EscolaRepository` e `IVinculoRepository`/`VinculoRepository`
  com `SaveChanges` explícito e consultas que recebem `EscopoUsuario`.
  - Validação: filtro por papel exercitado nos testes HTTP (merendeira por unidade,
    responsável por vínculo).

## Aplicação e API

- [x] T7: criar `EscopoService` com `EscopoUsuario` derivado das claims; usuário sem
  papel → `null` e 401.
  - Validação: claims exercitadas de ponta a ponta em `PapeisApiTests` (401 sem sessão,
    403 fora do escopo, 200 dentro dele).
- [x] T8: criar `EscolaService` e `VinculoService` com as regras de RF3, RF4 e RF6;
  nenhuma regra no controller.
  - Validação: 403 de `AdminEscola` fora do escopo e 409 de vínculo duplicado, via HTTP.
- [x] T9: criar `EscolaController`, `UnidadeController`, `VinculoController` e ampliar
  `UsuarioController` com ativação/desativação e consulta filtrada.
  - Validação: testes HTTP da matriz 401/403/200.
- [x] T10: no login, acrescentar as claims de papel/escola/unidade; registrar
  `OnValidatePrincipal` que rejeita usuário desativado.
  - Validação: `UsuarioDesativado_PerderSessaoNaProximaRequisicao`.
- [x] T11: seed idempotente do primeiro `AdminSecretaria` a partir de configuração.
  - Validação: startup sem configuração não cria usuário e registra aviso; com
    configuração cria uma única vez (`CriarAdminInicial` no `Program.cs`).

## Frontend

- [x] T12: criar `SessaoContext` e `ProtegidaPorPapel`; derivar o menu do papel.
  - Validação: `npm run build` e `npm run lint`; login de cada papel mostra apenas
  suas áreas.
  - Desvio D6: `SessaoContext` não foi criado como contexto novo; `papel`, `escolaId` e
    `unidadeId` foram adicionados ao `authContext`, que já era a fonte única da sessão.
- [x] T13: substituir os placeholders de `pages/secretaria/` e `pages/escolas/` por
  telas de escola, unidade e usuários.
  - **Transferida e concluída na Spec 021** (`telas-secretaria-e-escola`, V10): as telas
    de escolas, unidades e usuários existem e estão cobertas por `021/task.md` (T2–T11).
    Fechada aqui para a 009 poder ser promovida a `implemented` sem task pendente.

## Fechamento

- [x] T14: atualizar a tabela de endpoints do `README.md`.
  - Validação: `DocumentationTests.Readme_ListaTodosOsEndpoints` compara a tabela com
    os atributos dos controllers nos dois sentidos.
- [x] T15: executar os gates completos (reauditado em 2026-10-31).
  - `dotnet build backend/backend.csproj`: 0 erros, 0 avisos.
  - `dotnet test`: **250/250**, 0 falhas.
  - `npm run build`: OK. `npm run lint`: 0 erros, 5 avisos (baseline).

## Desvios registrados

| # | Desvio | Motivo |
| --- | --- | --- |
| D1 | Rotas de aluno mantidas no singular (`/api/aluno`), incluindo `/api/aluno/{id}/responsaveis` | Renomear quebraria frontend, README e 21 testes. Decisão do usuário |
| D2 | `Papel` é `PapelUsuario?` (anulável) | O enum 0 é `Nutricionista`; não anulável tornaria todo usuário legado nutricionista global |
| D3 | Autocadastro cria conta **sem papel**; login responde 401 até um administrador atribuir | RF4 revisada por decisão do usuário: cadastro aberto, autorização pelo admin |
| D4 | Nova política `LeituraAluno` nos `GET` de aluno | `EquipeEscolar` não inclui `Responsavel`, então RF9 ficava impossível. Achado pelos testes |
| D5 | `EquipeEscolar` inclui `AdminSecretaria` | Administrador global não pode ser barrado na leitura; o escopo de dados é o mesmo |
| D6 | Papel/escola/unidade no `authContext` em vez de um `SessaoContext` novo | Dois contextos duplicariam o estado de sessão |
| D7 | Usuários existentes ficam sem papel e não autenticam até um admin definir | Migração não inventa papel; o seed `AdminInicial` é o único caminho de bootstrap |
| D8 | `PUT /api/usuarios/{id}` (gestão) convive com `PUT /api/usuario/perfil/{id}` (autoatendimento) | O primeiro muda papel; o segundo só edita os próprios dados |
| D9 | Com a ADR-013 (Spec 022), o escopo de aluno de `AdminEscola` depende de unidade legada | PEND-024: adicionar `Aluno.EscolaId`; enquanto isso, listagem e vínculo por escola são exercitados com `AdminSecretaria` |

## Definição de pronto

- Todos os critérios de aceitação de `requirements.md` marcados com evidência.
- Nenhuma consulta devolve dado fora do escopo do papel autenticado.
- `dotnet test` verde e frontend compilando sem erro.
