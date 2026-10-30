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

Verificado contra o código em 2026-10-30. O `status` do frontmatter não foi alterado
(ver Spec 001, mesmo registro).

**Concluído**

| Task | Evidência |
| --- | --- |
| T1 | `HashSenha.cs` com `Criar`/`Confere`/`TentarLer`/`ToString`; `HashSenhaTests` cobre round-trip e valor malformado |
| T2 | `DefinirSenha`/`SenhaConfere`/`SenhaLegada` (`Usuario.cs:128-175`); `SenhaValida` ausente (busca textual sem ocorrências); construtor de 4 parâmetros (`Usuario.cs:21`) |
| T3 | guard clause → comprimento → derivação → atribuição em `DefinirSenha` (`Usuario.cs:128-142`) — ver D1 |
| T4 | `CriarUsuario` usa `DefinirSenha` (`UsuarioService.cs:24`) — ver D2 |
| T5 | `VerificarLogin` migra legado (`UsuarioService.cs:55-79`); testes `UsuarioServiceTests:211` e `:232` |
| T7 | os 8 casos da tabela cobertos: `UsuarioTests:165,175,183,191,203` e `HashSenhaTests:9-79` |
| T8 | única comparação em claro é `Senha == senha` dentro de `SenhaLegada` (`Usuario.cs:174`), permitida pela task |
| T9 | formato e migração automática documentados em `README.md:159-161` |
| T10 | `dotnet test`: 152 aprovados, 0 falhas (2026-10-30) |
| T6 | `20261030141405_HashSenhaUsuario.cs` criada com `dotnet ef` 10.0.12; migration vazia com comentário justificando; `dotnet ef migrations list` e `dotnet build` sem erro |

**Pendente**

- Nenhuma task pendente.

**Desvios registrados**

- D1: `AtualizarSenha` não existe mais. A ordem de validação foi implementada em
  `DefinirSenha`, que é o único caminho de escrita de senha.
- D2: não existe handler global de exceção (ADR-005 / Spec 007). O 400 vem do
  `catch (ArgumentException)` em `UsuarioController.cs:35-38`.
