---
name: historico-alteracoes-aluno
status: planned
references: V12, ADR-004, ADR-009
---

# Design — Histórico de alterações do aluno

## Estrutura

```text
backend/Domain/Entities/
  HistoricoAluno.cs            (trilha única, append-only)
  AlteracaoDeDado.cs           (objeto de valor do diff)
  Aluno.cs                     (+ RegistrarHistorico generalizado)
backend/Domain/Interface/
  IHistoricoAlunoRepository.cs
backend/API/Services/
  AlunoService.cs              (grava o diff na mesma SaveChanges)
  RestricaoService.cs          (histórico de restrições lê a trilha única)
backend/API/Controllers/
  AlunoController.cs           (+ GET /api/aluno/{id}/historico)
frontend/src/pages/nutricao/alunos/
  VisualizarAlunos.tsx         (linha do tempo única)
```

## Domínio — trilha

```csharp
public enum TipoEventoAluno { Criado, Alterado, RestricaoAdicionada, RestricaoRemovida }

public class HistoricoAluno
{
    public int Id { get; private set; }
    public int AlunoId { get; private set; }
    public TipoEventoAluno Tipo { get; private set; }
    public string? Campo { get; private set; }
    public string? ValorAnterior { get; private set; }
    public string? ValorNovo { get; private set; }
    public string AutorNome { get; private set; } = string.Empty;
    public DateTime CriadoEmUtc { get; private set; }

    public HistoricoAluno(int alunoId, TipoEventoAluno tipo, string? campo,
                          string? valorAnterior, string? valorNovo,
                          string? autorNome, DateTime criadoEmUtc);
}
```

`AlteracaoDeDado` é um record de domínio com `Campo`, `ValorAnterior` e `ValorNovo`; é o
retorno do diff, nunca um DTO.

## Domínio — diff de dados

`Aluno.AtualizarDados` compara o estado atual com o validado **antes** de aplicar e devolve
`(IReadOnlyList<AlteracaoDeDado> Alterados, IReadOnlyList<string> Adicionadas,
IReadOnlyList<string> Removidas)`. Comparações usam a representação canônica:

| Campo | Representação comparada |
|---|---|
| Nome, Telefone, Observação | texto normalizado (trim) |
| Nascimento | data ISO (`yyyy-MM-dd`) |
| Turma | `Escola — Série` (as duas colunas são um objeto de valor) |
| Responsável | nome e telefone como dois campos distintos |
| Anafilaxia | `Sim` / `Não` |
| Unidade | `UnidadeId` em texto; vazio quando nulo |
| Restrições | conjunto, comparado pela regra da Spec 010 |

`RegistrarHistorico(alteracoes, adicionadas, removidas, autor)` grava um evento por
alteração e um por restrição. Na criação, grava um `Criado` seguido dos
`RestricaoAdicionada` de cada nome informado.

## Persistência

- Tabela `HistoricosAlunos`: FK `AlunoId` com `OnDelete(Cascade)`; índice
  `(AlunoId, CriadoEmUtc)`.
- `Campo`, `ValorAnterior` e `ValorNovo` são anuláveis: `Criado` e eventos de restrição não
  preenchem todos.
- Migration `HistoricoDeAlteracoesDoAluno`:
  1. cria `HistoricosAlunos`;
  2. copia cada linha de `HistoricosRestricoesAlunos` (`Acao = Adicionada` →
     `RestricaoAdicionada`; `Removida` → `RestricaoRemovida`; `Campo = "Restrições"`);
  3. remove `HistoricosRestricoesAlunos`.
- A cópia usa `INSERT ... SELECT` mantendo `CriadoEmUtc` e `AutorNome` originais.

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/aluno/{id}/historico?pagina=&tamanho=` | `LeituraAluno` + escopo |
| `GET` | `/api/aluno/{id}/restricoes/historico?pagina=&tamanho=` | mantida (RF7), filtrada por tipo |

Resposta:

```json
{ "pagina": 1, "tamanho": 20, "total": 4,
  "itens": [ { "tipo": "Alterado", "campo": "Telefone",
               "valorAnterior": "11999999999", "valorNovo": "11888888888",
               "autor": "Maria", "criadoEm": "2026-11-01T13:00:00Z" } ] }
```

A rota antiga devolve `{ restricao, acao, autor, criadoEm }`, derivando `restricao` de
`ValorNovo` e `acao` do tipo — mesmo formato da Spec 010.

## Escopo por papel

O serviço reutiliza `IAlunoRepository.Obter(id, escopo)` antes de ler a trilha: aluno
inexistente devolve 404 no escopo global e 403 fora dele, exatamente como o histórico de
restrições já faz.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | diff de um campo, de vários campos e sem mudança |
| Domínio | criação gera `Criado` + um evento por restrição |
| Domínio | anafilaxia e unidade convertem valor para `Sim`/`Não` e id |
| Persistência | migração copia eventos antigos e remove a tabela de origem |
| Serviço | aluno fora do escopo → 403; inexistente no global → 404 |
| HTTP | `/historico` paginado; `/restricoes/historico` mantém o formato |

## Riscos

- **Ruído na trilha**: alterar observação a cada edição gera muito evento. Mitigação:
  comparar valores normalizados e ignorar campo inalterado (RF3).
- **Migração de dados**: se a cópia falhar no meio, `Database.Migrate()` aborta e o banco
  fica na versão anterior — comportamento desejado, sem perda silenciosa.
- **Crescimento**: um evento por campo alterado multiplica linhas. Mitigação: índice por
  aluno/data e leitura paginada.
