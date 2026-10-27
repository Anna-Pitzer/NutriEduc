---
name: dominio-aluno-invariantes
status: created
references: V1, ADR-004, L1
---

# Tasks — Invariantes do domínio Aluno

- [ ] T1: extrair `ValidarTexto`, `ValidarTelefone`, `ValidarNascimento` e
  `ValidarDados` em `backend/Domain/Entities/Aluno.cs`.
  - Validação: `dotnet build backend/backend.csproj`.
- [ ] T2: reescrever o construtor público aplicando validar → atribuir.
  - Validação: testes unitários de cada campo obrigatório.
- [ ] T3: implementar `AtualizarDados` reutilizando `ValidarDados`.
  - Validação: teste confirmando que payload inválido não altera o estado.
- [ ] T4: ajustar `AlunoService.CriarAluno` para o novo construtor
  (`IEnumerable<string>?` nas restrições).
  - Validação: `POST /api/aluno/registrar` com dados válidos persiste.
- [ ] T5: documentar em comentário que o construtor privado do EF Core não valida.
  - Validação: revisão de código.
- [ ] T6: executar gates (`dotnet build`, `dotnet test`).

## Definição de pronto

- Toda propriedade atribuída após validação completa.
- Nenhuma regra duplicada no serviço.
- Critérios de aceitação de `requirements.md` verificados por teste.
