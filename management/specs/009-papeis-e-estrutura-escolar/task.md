---
name: papeis-e-estrutura-escolar
status: created
references: V6, ADR-002, L4
---

# Tasks — Papéis e estrutura escolar

## Pré-condição

- [ ] T0: Spec 002 implementada (senha com hash) e Spec 007 implementada (contrato
  `{ erro }` e registro de serviços correto).
  - Validação: `dotnet test` verde; `GET` de rota inexistente devolve `{ erro }`.

## Domínio

- [ ] T1: criar `backend/Domain/Entities/PapelUsuario.cs` com os cinco valores.
  - Validação: compila; nenhum `string` de papel circula em serviços novos.
- [ ] T2: criar `backend/Domain/Entities/Escola.cs` e `UnidadeEscolar.cs` com nome
  normalizado, limites 2–120 e `Desativar`/`Reativar`.
  - Validação: testes de nome vazio, curto, longo e com espaços.
- [ ] T3: alterar `backend/Domain/Entities/Usuario.cs`: adicionar `Papel`,
  `EscolaId`, `UnidadeId`, `Ativo` e `DefinirPapel`; reduzir a um construtor público
  de criação (PEND-002).
  - Validação: teste de papel que exige escola sem `escolaId` lança
    `ArgumentException`; teste de unidade em papel que não aceita lança.
- [ ] T4: criar `VinculoResponsavelAluno` com chave composta `(AlunoId, ResponsavelUsuarioId)`.
  - Validação: teste de igualdade e de chave; duplicata detectada na camada de serviço.

## Persistência

- [ ] T5: registrar `DbSet`s e índices únicos em `ConnectionContext`; adicionar
  `Aluno.UnidadeId` anulável com FK `SetNull`.
  - Validação: `dotnet ef migrations add PapeisEEscolas`; `dotnet ef database update`;
  - Validação: teste de modelo confirma índices `Escolas(Nome)` e
    `Unidades(EscolaId, Nome)`.
- [ ] T6: criar `IEscolaRepository`/`EscolaRepository` e `IVinculoRepository`/`VinculoRepository`
  com `SaveChanges` explícito e consultas que recebem `EscopoUsuario`.
  - Validação: teste com `ConnectionContext` em memória confirma filtro por unidade.

## Aplicação e API

- [ ] T7: criar `EscopoService` com `EscopoUsuario` derivado das claims; usuário sem
  papel → `null` e 401.
  - Validação: teste de claims de cada papel.
- [ ] T8: criar `EscolaService` e `VinculoService` com as regras de RF3, RF4 e RF6;
  nenhuma regra no controller.
  - Validação: testes de serviço para 403 de `AdminEscola` fora do escopo e 409 de
  vínculo duplicado.
- [ ] T9: criar `EscolaController`, `UnidadeController`, `VinculoController` e ampliar
  `UsuarioController` com ativação/desativação e consulta filtrada.
  - Validação: testes HTTP da matriz 401/403/200.
- [ ] T10: no login, acrescentar as claims de papel/escola/unidade; registrar
  `OnValidatePrincipal` que rejeita usuário desativado.
  - Validação: teste HTTP autentica, desativa e repete requisição → 401.
- [ ] T11: seed idempotente do primeiro `AdminSecretaria` a partir de configuração.
  - Validação: startup sem configuração não cria usuário e registra aviso; com
  configuração cria uma única vez.

## Frontend

- [ ] T12: criar `SessaoContext` e `ProtegidaPorPapel`; derivar o menu do papel.
  - Validação: `npm run build` e `npm run lint`; login de cada papel mostra apenas
  suas áreas.
- [ ] T13: substituir os placeholders de `pages/secretaria/` e `pages/escolas/` por
  telas de escola, unidade e usuários.
  - Validação: criar escola, unidade e merendeira pela interface; conflito exibe
  mensagem de erro do backend.

## Fechamento

- [ ] T14: atualizar a tabela de endpoints do `README.md`.
  - Validação: tabela coincide com os controllers (`grep` de rotas).
- [ ] T15: executar os gates completos.
  - Validação: `dotnet build backend/backend.csproj`; `dotnet test`;
  - Validação: `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Todos os critérios de aceitação de `requirements.md` marcados com evidência.
- Nenhuma consulta devolve dado fora do escopo do papel autenticado.
- `dotnet test` verde e frontend compilando sem erro.
