---
name: seguranca-senha-hash
status: created
references: V3, ADR-003, L3
---

# Tasks — Senha com hash

## Domínio

- [x] T1: criar `backend/Domain/Entities/HashSenha.cs` (struct readonly) com `Criar`,
  `Confere`, `TentarLer` e `ToString`. Caminho corrigido: a pasta é `Entities`, no
  plural (a versão anterior desta task dizia `Domain/Entity/`).
  - Validação: teste unitário de round-trip e de rejeição de valor malformado.
- [x] T2: em `backend/Domain/Entities/Usuario.cs`, adicionar `DefinirSenha`,
  `SenhaConfere` e `SenhaLegada`; remover `SenhaValida()`; e adicionar o construtor
  `Usuario(string nome, string cpf, DateOnly dataNascimento, string email)` **sem**
  senha, usado por `CriarUsuario` em `design.md` — o construtor atual exige a senha no
  argumento, e nenhuma task previa essa mudança.
  - Validação: `dotnet build`; busca textual confirma ausência de `SenhaValida`.
  - Validação: `CriarUsuario` compila usando o construtor de quatro parâmetros.
- [x] T3: corrigir a ordem de validação de `AtualizarSenha` (guard clause primeiro).
  - Validação: teste com `null` esperando `ArgumentException` determinístico.

## Aplicação

- [x] T4: `UsuarioService.CriarUsuario` usa `DefinirSenha` e trata senha inválida
  como 400 pelo handler global.
  - Validação: teste de serviço com repositório fake.
- [x] T5: `UsuarioService.VerificarLogin` usa `SenhaConfere` e migra registro legado.
  - Validação: teste de login legado válido (migra) e inválido (não migra).

## Persistência

- [x] T6: criar migration `HashSenhaUsuario` documentando a mudança de conteúdo da
  coluna `Senha`.
  - Validação: `dotnet ef migrations list` e `dotnet build`.

## Testes

- [x] T7: cobrir os 8 casos da tabela de `design.md` §"Testes obrigatórios".
  - Validação: `dotnet test` verde com contagem reportada.

## Fechamento

- [x] T8: verificar que nenhum ponto compara senha em claro (busca por `Senha ==`
  fora de `SenhaLegada`).
- [x] T9: registrar em `README.md` a mudança de formato de senha e a migração
  automática no primeiro login.
- [x] T10: executar gates (`dotnet build`, `dotnet test`).

## Definição de pronto

- Nenhuma senha em claro em memória persistida.
- Nenhum teste vermelho; nenhum `catch` vazio.

## Registro de execução

Verificado contra o código em 2026-10-30 e reauditado em 2026-10-31. O `status` do frontmatter não foi alterado
(ver Spec 001, mesmo registro).

**Concluído**

| Task | Evidência |
| --- | --- |
| T1 | `HashSenha.cs` com `Criar`/`Confere`/`TentarLer`/`ToString`; `HashSenhaTests` cobre round-trip, salt distinto e valores malformados |
| T2 | `DefinirSenha`/`SenhaConfere`/`SenhaLegada` em `backend/Domain/Entities/Usuario.cs`; `SenhaValida` ausente (busca textual sem ocorrências); construtor `(nome, cpf, dataNascimento, email, papel?, escolaId?, unidadeId?)` |
| T3 | guard clause → comprimento → derivação → atribuição em `DefinirSenha` — ver D1 |
| T4 | `CriarUsuario` usa `DefinirSenha` (`backend/API/Services/UsuarioService.cs`) — ver D2 |
| T5 | `VerificarLogin` migra legado (`UsuarioService.cs`); testes `VerificarLogin_ComSenhaLegada_AutenticaEMigra` e `..._Incorreta_NaoMigra` |
| T6 | `backend/Migrations/20261030141405_HashSenhaUsuario.cs` documental, sem operação de schema |
| T7 | os 8 casos da tabela cobertos por `UsuarioTests` (hash, guard clauses, salt) e `HashSenhaTests` |
| T8 | única comparação em claro é `Senha == senha` dentro de `SenhaLegada`, permitida pela task |
| T9 | formato e migração automática documentados em `README.md` §Senhas |
| T10 | 2026-10-31 (auditoria): `dotnet test` 246/246, 0 falhas |

**Pendente**

- Nenhuma task pendente.

**Desvios registrados**

- D1: `AtualizarSenha` não existe mais. A ordem de validação foi implementada em
  `DefinirSenha`, que é o único caminho de escrita de senha.
- D2 (atualizado na auditoria de 2026-10-31): o 400 de senha inválida vem do
  `ErrorHandlingMiddleware` (Spec 007), que traduz `ArgumentException`; o `catch` que
  existia em `UsuarioController` foi removido.
- D3 (limitação conhecida): a migração de senha legada reusa `DefinirSenha`, que exige 8
  caracteres. Registro legado com senha curta — possível só por escrita direta no banco,
  já que a validação antiga também exigia 8 — receberia 400 no login em vez de migrar.
