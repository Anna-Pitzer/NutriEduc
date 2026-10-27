---
name: crud-aluno-completo
status: created
references: V2, L8
---

# Design — CRUD completo de aluno

## Contrato de API

| Método | Rota | Corpo | Sucesso | Erro |
| --- | --- | --- | --- | --- |
| `PUT` | `/api/aluno/{id:int}` | `DadosAlunoDTO` | 200 `DadosAlunoDTO` | 404 inexistente; 400 `{ erro }` |

Rota segue o padrão `api/[controller]` já usado por `AlunoController`.

## Backend

### Controller — `backend/API/Controllers/AlunoController.cs`

```csharp
[HttpPut("{id:int}")]
public IActionResult Atualizar(int id, [FromBody] DadosAlunoDTO dto)
{
    var aluno = _alunoService.AtualizarAluno(id, dto);
    if (aluno == null) return NotFound("Aluno não encontrado.");
    return Ok(aluno);
}
```

Sem validação no controller; erro de domínio chega pelo handler global (Spec 007).

### Serviço — `backend/API/Services/AlunoService.cs`

```csharp
public DadosAlunoDTO? AtualizarAluno(int id, DadosAlunoDTO dto)
{
    Aluno? aluno = _alunoRepository.GetAluno(id);
    if (aluno == null) return null;

    aluno.AtualizarDados(dto.Nome, dto.Nascimento, dto.Escola, dto.Serie,
        dto.Telefone, dto.Observacao, dto.Restricoes, dto.Anafilaxia,
        dto.NomeResponsavel, dto.TelefoneResponsavel);

    _alunoRepository.UpdateAluno(aluno);
    return ObterAlunoPorId(id);
}
```

O serviço localiza, delega ao domínio e persiste. `ObterAlunoPorId` já existe e monta
a resposta.

### Repositório

`IAlunoRepository.UpdateAluno` e `AlunoRepository.UpdateAluno` já existem em
`backend/Domain/Interface/IAlunoRepository.cs` e
`backend/Infrastructure/Repositories/AlunoRepository.cs`. Nenhuma alteração.

## Frontend

### Serviço — `frontend/src/services/AlunoApi.ts`

```ts
export async function atualizarAluno(id: number, dados: AlunoCadastro): Promise<void> {
    const resposta = await fetch(`${ApiURL}/api/aluno/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dados),
    });

    if (!resposta.ok) throw new Error("Erro ao atualizar aluno");
}
```

### Tela — `frontend/src/pages/VisualizarAlunos.tsx`

- Ação "Editar" por linha, ao lado da exclusão existente.
- Seleciona o aluno em estado local e abre o formulário em modo edição.
- Formulário reaproveita o estado inicial de `frontend/src/pages/CadastrarAlunos.tsx`;
  o mapeamento `AlunoResposta → FormularioAluno` é extraído para evitar duplicação.
- Ao salvar: chamar `atualizarAluno`, recarregar a lista, exibir sucesso; falha exibe
  mensagem sem descartar o formulário.

## Decisões

- `PUT` (substituição completa), coerente com `PUT /api/usuario/perfil/{id}`.
- `PUT` recebe e devolve `DadosAlunoDTO` nesta spec, para não ampliar escopo.
- **Ressalva:** ao contrário do que afirmava a versão anterior deste design,
  `DadosAlunoDTO` **contém** dados de saúde (`Anafilaxia`, `Restricoes`), e o
  `arch.md` §5 exige DTOs de entrada e de saída distintos. A separação está registrada
  como `PEND-007` em `pending-items.md` e **não** é resolvida por esta spec.

## Sequência

```text
UI (editar) → GET /api/aluno/perfil/{id} → formulário preenchido
  → PUT /api/aluno/{id} → AlunoService.AtualizarAluno → Aluno.AtualizarDados
  → AlunoRepository.UpdateAluno → 200 DadosAlunoDTO → lista recarregada
```

## Riscos

- Sem a Spec 004, `AtualizarDados` não existe: implementar 004 antes de 001.
- Sem a Spec 006, o endpoint nasce público; fechado em 006.
- Nenhuma chamada de `AlunoApi.ts` envia `credentials: "include"` hoje, ao contrário
  do que o `arch.md` §3 exige. `atualizarAluno` deve nascer correta e as três funções
  existentes precisam do mesmo ajuste quando o `[Authorize]` da Spec 006 entrar
  (registrado como `PEND-006`).
