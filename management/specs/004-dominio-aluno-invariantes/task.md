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

Verificado contra o código em 2026-10-30. O `status` do frontmatter não foi alterado
(ver Spec 001, mesmo registro).

**Concluído**

| Task | Evidência |
| --- | --- |
| T1 | `ValidarDados`, `ValidarNome`, `ValidarTelefone` e `ValidarNascimento` (`Aluno.cs:151-206`) — ver D1 |
| T2 | construtor público valida tudo e só então atribui (`Aluno.cs:63-85`) |
| T3 | `AtualizarDados` reutiliza `ValidarDados` (`Aluno.cs:91-113`); `AlunoTests:282` prova que payload inválido não altera estado |
| T4 | `AlunoService.CriarAluno` passou a compor o aluno (`AlunoService.cs:14-28`) — ver D2 |
| T5 | comentário do construtor privado (`Aluno.cs:21-23` e `:58`) |
| T6 | `dotnet test`: 152 aprovados, 0 falhas (2026-10-30) |

**Desvios registrados**

- D1: não existe `ValidarTexto` em `Aluno`. Cada campo tem validador próprio porque a
  regra é distinta (nome 2–100, telefone 10 ou 11 dígitos, nascimento entre 1900 e hoje).
  O helper genérico `ValidarTexto` ficou em `Turma` e `Responsavel`, que têm regras
  uniformes de texto.
- D2: a task previa apenas trocar a assinatura das restrições. A implementação foi além
  e compôs `Turma` e `Responsavel` como objetos de valor (lição L10), o que mudou a
  assinatura do construtor para receber `Turma` e `Responsavel`.
