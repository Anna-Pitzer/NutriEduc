---
name: dominio-aluno-invariantes
status: created
references: V1, ADR-004, L1
---

# Tasks — Invariantes do domínio Aluno

- [x] T1: extrair `ValidarTexto`, `ValidarTelefone`, `ValidarNascimento` e
  `ValidarDados` em `backend/Domain/Entities/Aluno.cs`.
  - Validação: `dotnet build backend/backend.csproj`.
- [x] T2: reescrever o construtor público aplicando validar → atribuir.
  - Validação: testes unitários de cada campo obrigatório.
- [x] T3: implementar `AtualizarDados` reutilizando `ValidarDados`.
  - Validação: teste confirmando que payload inválido não altera o estado.
- [x] T4: ajustar `AlunoService.CriarAluno` para o novo construtor
  (`IEnumerable<string>?` nas restrições).
  - Validação: `POST /api/aluno/registrar` com dados válidos persiste.
- [x] T5: documentar em comentário que o construtor privado do EF Core não valida.
  - Validação: revisão de código.
- [x] T6: executar gates (`dotnet build`, `dotnet test`).

## Definição de pronto

- Toda propriedade atribuída após validação completa.
- Nenhuma regra duplicada no serviço.
- Critérios de aceitação de `requirements.md` verificados por teste.

## Registro de execução

Verificado contra o código em 2026-10-30 e reauditado em 2026-10-31. O `status` do frontmatter não foi alterado
(ver Spec 001, mesmo registro).

**Concluído**

| Task | Evidência |
| --- | --- |
| T1 | `ValidarDados`, `ValidarNascimento` e `ValidacaoDeCampo.Texto/Telefone` (`backend/Domain/Entities/Aluno.cs`, `backend/Domain/Entities/ValidacaoDeCampo.cs`) — ver D1 |
| T2 | construtor público valida tudo e só então atribui (`Aluno.cs`) |
| T3 | `AtualizarDados` reutiliza `ValidarDados` (`Aluno.cs`); `AtualizarDados_ComPayloadInvalido_NaoAlteraEstado` e `AtualizarDados_ComRestricoesInvalidas_NaoAlteraEstado` |
| T4 | `AlunoService.CriarAluno` compõe `Turma` e `Responsavel` (`backend/API/Services/AlunoService.cs`) — ver D2 |
| T5 | comentário do construtor privado (`Aluno.cs`) |
| T6 | 2026-10-31 (auditoria): `dotnet test` 248/248, 0 falhas |

**Desvios registrados**

- D1 (corrigido na auditoria de 2026-10-31): a versão anterior dizia que o helper
  genérico ficou apenas em `Turma` e `Responsavel`, mas `Responsavel` tinha a própria
  cópia e `Aluno` também. As regras de texto e telefone foram unificadas em
  `ValidacaoDeCampo.Texto`/`Telefone`, usadas pelas três classes. Restaram apenas
  `ValidarNascimento` e o limite de `Observacao`, exclusivos de `Aluno`.
- D2: a task previa apenas trocar a assinatura das restrições. A implementação foi além
  e compôs `Turma` e `Responsavel` como objetos de valor (lição L10), o que mudou a
  assinatura do construtor para receber `Turma` e `Responsavel`.
