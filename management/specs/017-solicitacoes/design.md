---
name: solicitacoes
status: created
references: V6, ADR-004, L7
---

# Design — Solicitações

## Estrutura

```text
backend/Domain/Entities/
  Solicitacao.cs
  TipoSolicitacao.cs         (enum)
  StatusSolicitacao.cs       (enum)
  HistoricoSolicitacao.cs
backend/Domain/Interface/
  ISolicitacaoRepository.cs
backend/API/Services/
  SolicitacaoService.cs
backend/API/Controllers/
  SolicitacaoController.cs
```

## Domínio

```csharp
public enum TipoSolicitacao
{
    MudancaCardapio, Ingrediente, FaltaIngrediente, InclusaoAlergia, AlteracaoRestricao
}

public enum StatusSolicitacao { Aberta, EmAnalise, Aprovada, Recusada, Concluida, Cancelada }

public class Solicitacao
{
    private readonly List<HistoricoSolicitacao> _historico = new();

    public int Id { get; private set; }
    public TipoSolicitacao Tipo { get; private set; }
    public int UnidadeId { get; private set; }
    public int? AlunoId { get; private set; }
    public int? CardapioId { get; private set; }
    public string? RestricaoNome { get; private set; }
    public string? Ingrediente { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public StatusSolicitacao Status { get; private set; }
    public int SolicitanteId { get; private set; }
    public string SolicitanteNome { get; private set; } = string.Empty;
    public string? Resposta { get; private set; }
    public string? RespondidoPor { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime AtualizadoEmUtc { get; private set; }
    public IReadOnlyList<HistoricoSolicitacao> Historico => _historico.AsReadOnly();

    public Solicitacao(TipoSolicitacao tipo, int unidadeId, int solicitanteId,
                       string solicitanteNome, string descricao,
                       int? alunoId, int? cardapioId, string? restricaoNome,
                       string? ingrediente, DateTime utc)
    {
        // descrição 5..1000; valida campos por tipo (RF2); normaliza strings
        Status = StatusSolicitacao.Aberta;
        CriadoEmUtc = AtualizadoEmUtc = utc;
        _historico.Add(new HistoricoSolicitacao(StatusSolicitacao.Aberta, solicitanteNome, null, utc));
    }

    public void IniciarAnalise(string autorNome, DateTime utc) { /* Aberta → EmAnalise */ }

    public void Aprovar(string resposta, string autorNome, DateTime utc) { /* EmAnalise → Aprovada */ }

    public void Recusar(string resposta, string autorNome, DateTime utc) { /* EmAnalise → Recusada */ }

    public void Concluir(string autorNome, DateTime utc) { /* Aprovada → Concluida */ }

    public void Cancelar(string autorNome, DateTime utc) { /* Aberta → Cancelada */ }
}
```

Todas as transições usam o mesmo `Transicionar(destino, autor, resposta, utc)` com
tabela explícita de pares válidos e validação de `resposta` (2–500) quando o destino
é `Aprovada` ou `Recusada`. `Cancelar` só é permitido ao solicitante (verificado no
serviço, que também chama o domínio).

## Efeito da aprovação

O efeito é decidido por tipo e executado por um único ponto no serviço:

```text
InclusaoAlergia / AlteracaoRestricao
    → AlunoService.AdicionarRestricao(alunoId, restricaoNome, autor)
      (valida catálogo na Spec 010 e grava evento no histórico do aluno)
    → se falhar: transação desfeita, solicitação permanece EmAnalise

MudancaCardapio
    → nenhum efeito automático; resposta registra "aplique a alteração em 015"

Ingrediente / FaltaIngrediente
    → nenhum efeito automático; serve de pauta e de fonte para notificação (018)
```

Não existe "aplicar tudo": cada tipo tem efeito declarado, e o que não tem efeito
diz isso na resposta (RF7). Isso evita que aprovar um pedido altere dados em silêncio.

## Escopo

Reutiliza `EscopoService` (Spec 009):

```text
Nutricionista      → todas
AdminSecretaria    → todas (leitura)
AdminEscola        → UnidadeId in (unidades da escola)
Merendeira         → UnidadeId == sessão
Responsavel        → SolicitanteId == sessão OU AlunoId in (alunos vinculados)
```

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/solicitacoes?...` | sessão com escopo |
| `GET` | `/api/solicitacoes/{id}` | sessão com escopo |
| `POST` | `/api/solicitacoes` | `Merendeira`, `Responsavel`, `AdminEscola`, `Nutricionista` |
| `POST` | `/api/solicitacoes/{id}/analise` | `Nutricionista` |
| `POST` | `/api/solicitacoes/{id}/aprovar` | `Nutricionista` |
| `POST` | `/api/solicitacoes/{id}/recusar` | `Nutricionista` |
| `POST` | `/api/solicitacoes/{id}/concluir` | `Nutricionista` |
| `POST` | `/api/solicitacoes/{id}/cancelar` | solicitante |

```json
{ "id": 14, "tipo": "InclusaoAlergia", "status": "Aprovada",
  "alunoId": 9, "aluno": "Ana", "restricaoNome": "Amendoim",
  "descricao": "Ana apresentou reação leve.", "solicitante": "Paulo",
  "resposta": "Inclusão confirmada com a família.", "respondidoPor": "Maria",
  "criadoEm": "2026-10-29T18:00:00Z",
  "historico": [ { "status": "EmAnalise", "autor": "Maria", "criadoEm": "..." },
                 { "status": "Aprovada", "autor": "Maria", "resposta": "...", "criadoEm": "..." } ] }
```

## Persistência

- `Solicitacoes`: FKs anuláveis `AlunoId`/`CardapioId` (`SetNull`); `UnidadeId`
  (`Restrict`); índices `(UnidadeId, CriadoEmUtc)` e `(Status, Tipo)`.
- `HistoricoSolicitacoes`: FK `SolicitacaoId` (`Cascade`), índice
  `(SolicitacaoId, CriadoEmUtc)`.
- `[ConcurrencyCheck]` em `Status` para RF de concorrência.
- Migration `Solicitacoes`.

## Frontend

- `pages/merendeiras/Solicitacoes.tsx`: criar (mudança de cardápio, ingrediente,
  falta) e acompanhar.
- `pages/pais/Solicitacoes.tsx`: criar (alergia/restrição) e acompanhar.
- `pages/escolas/Solicitacoes.tsx`: acompanhamento da escola.
- `pages/nutricao/solicitacoes/FilaSolicitacoes.tsx`: fila com decisão e resposta
  obrigatória em aprovar/recusar.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | campos obrigatórios por tipo |
| Domínio | todas as transições válidas e inválidas |
| Domínio | aprovar/recusar sem resposta lança |
| Serviço | pai para aluno não vinculado → 403 |
| Serviço | aprovação de restrição aplica no aluno e grava histórico |
| Serviço | restrição fora do catálogo → 400 e permanece `EmAnalise` |
| Serviço | aprovação de cardápio não altera cardápio |
| HTTP | escopo por papel e concorrência (200/409) |

## Riscos

- **Efeito silencioso**: a lista de efeitos por tipo é explícita e testada; tipos sem
  efeito retornam resposta que diz isso.
- **Transação entre agregados**: aprovar restrição toca `Solicitacao` e `Aluno`; ambos
  usam o mesmo `ConnectionContext` Scoped e uma transação explícita.
- **Confusão entre aprovar e editar**: `MudancaCardapio` aprovada não edita; o texto
  de resposta e o teste impedem regressão.
