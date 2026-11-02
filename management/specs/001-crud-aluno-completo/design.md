---
name: crud-aluno-completo
status: created
references: V2, L8
---

# Design — CRUD completo de aluno

## Contrato de API

| Método | Rota | Corpo | Sucesso | Erro |
| --- | --- | --- | --- | --- |
| `PUT` | `/api/aluno/{id:int}` | `DadosAlunoDTO` | 200 `DadosAlunoDTO` | 404 inexistente (escopo global); 403 fora do escopo; 400 `{ erro }` |

Rota segue o padrão `api/[controller]` já usado por `AlunoController`.

## Backend

### Controller — `backend/API/Controllers/AlunoController.cs`

```csharp
[Authorize(Policy = "GestaoAluno")]
[HttpPut("{id:int}")]
public IActionResult Atualizar(int id, [FromBody] DadosAlunoDTO dto)
{
    var escopo = ObterEscopo(_escopo);
    if (escopo is null) return SessaoInvalida();

    var aluno = _alunoService.AtualizarAluno(id, dto, escopo);
    if (aluno != null) return Ok(aluno);

    return escopo.Global
        ? NotFound(new { erro = "Aluno não encontrado." })
        : Forbid();
}
```

Sem validação no controller; erro de domínio chega pelo handler global (Spec 007).

### Serviço — `backend/API/Services/AlunoService.cs`

```csharp
public DadosAlunoDTO? AtualizarAluno(int id, DadosAlunoDTO dto, EscopoUsuario escopo)
{
    Aluno? aluno = _alunoRepository.Obter(id, escopo);
    if (aluno == null) return null;

    aluno.AtualizarDados(
        dto.Nome,
        dto.Nascimento,
        new Turma(dto.Escola, dto.Serie),
        dto.Telefone,
        dto.Observacao,
        dto.Restricoes,
        dto.Anafilaxia,
        new Responsavel(dto.NomeResponsavel, dto.TelefoneResponsavel));

    aluno.DefinirUnidade(dto.UnidadeId);
    _alunoRepository.UpdateAluno(aluno);
    return ParaDTO(aluno);
}
```

O serviço localiza, delega ao domínio e persiste; `ParaDTO` monta a resposta.

### Repositório

`IAlunoRepository.UpdateAluno` e `AlunoRepository.UpdateAluno` já existem em
`backend/Domain/Interface/IAlunoRepository.cs` e
`backend/Infrastructure/Repositories/AlunoRepository.cs`. Nenhuma alteração.

## Frontend

### Serviço — `frontend/src/services/AlunoApi.ts`

```ts
export async function atualizarAluno(id: number, dados: AlunoCadastro): Promise<AlunoResposta> {
    const resposta = await fetch(`${ApiURL}/api/aluno/${id}`, {
        method: "PUT",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dados),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao atualizar aluno");

    return resposta.json();
}
```

### Tela — `frontend/src/pages/nutricao/alunos/VisualizarAlunos.tsx`

- Ação "Editar" por linha navega para `/alunos/editar/:id` ao lado da exclusão existente.
- A própria tela de cadastro (`frontend/src/pages/nutricao/alunos/CadastrarAlunos.tsx`)
  detecta o parâmetro `id` e entra em modo edição: o formulário não é duplicado (RF5).
- Ao montar em modo edição, `buscarAlunoPorId` preenche o formulário (RF6); o mapeamento
  `AlunoResposta → FormularioAluno` fica em `mapearAlunoParaFormulario`.
- `unidadeId` é carregado no estado e reenviado no `PUT`; sem isso a edição limparia a
  unidade do aluno (`DefinirUnidade(null)`), quebrando o escopo da Spec 009.
- Ao salvar: `atualizarAluno` devolve o aluno atualizado e a tela exibe sucesso/erro; a
  listagem relê do backend no `useEffect` de montagem quando o usuário volta (D2).

## Decisões

- `PUT` (substituição completa), coerente com `PUT /api/usuario/perfil/{id}`.
- `PUT` recebe e devolve `DadosAlunoDTO` nesta spec, para não ampliar escopo.
- **Ressalva:** ao contrário do que afirmava a versão anterior deste design,
  `DadosAlunoDTO` **contém** dados de saúde (`Anafilaxia`, `Restricoes`), e o
  `arch.md` §5 exige DTOs de entrada e de saída distintos. A separação está registrada
  como `PEND-007` em `pending-items.md` e **não** é resolvida por esta spec.
- **Evolução posterior (registrada na auditoria de 2026-10-31):** a Spec 009 acrescentou
  `EscopoUsuario` à assinatura do serviço e `[Authorize]`/`403` ao `PUT`; a Spec 006
  acrescentou `credentials: "include"`; a Spec 020 passou o erro para `{ erro }`. Este
  design foi atualizado para refletir o código vigente.

## Sequência

```text
UI (editar) → GET /api/aluno/perfil/{id} → formulário preenchido
  → PUT /api/aluno/{id} → AlunoService.AtualizarAluno → Aluno.AtualizarDados
  → AlunoRepository.UpdateAluno → 200 DadosAlunoDTO → lista recarregada
```

## Riscos

- Sem a Spec 004, `AtualizarDados` não existe: implementar 004 antes de 001.
- Sem a Spec 006, o endpoint nasce público; fechado em 006.
- Resolvido: as cinco chamadas de `AlunoApi.ts` enviam `credentials: "include"` desde a
  Spec 006 (`PEND-006` concluído).
- Limitação conhecida: as listas de escola e série da tela são estáticas; editar aluno com
  `escola` fora da lista falha na validação local. Resolver exige consumir `GET /api/escolas`
  (Spec 021).
