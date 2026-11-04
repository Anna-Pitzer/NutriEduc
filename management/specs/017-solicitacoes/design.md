---
name: solicitacoes
status: created
references: V6, V17, V19, ADR-020, ADR-021, ADR-004, L7
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
    MudancaCardapio, Ingrediente, FaltaIngrediente, InclusaoAlergia, AlteracaoRestricao,
    DadosCadastrais
}

public enum StatusSolicitacao { Aberta, EmAnalise, Aprovada, Recusada, Concluida, Cancelada }

public class Solicitacao
{
    private readonly List<HistoricoSolicitacao> _historico = new();

    public int Id { get; private set; }
    public TipoSolicitacao Tipo { get; private set; }
    public int UnidadeId { get; private set; }
    public int? EscolaId { get; private set; }
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

    public Solicitacao(TipoSolicitacao tipo, int unidadeId, int? escolaId, int solicitanteId,
                       string solicitanteNome, string descricao,
                       int? alunoId, int? cardapioId, string? restricaoNome,
                       string? ingrediente, DateTime utc)
    {
        // UnidadeId e EscolaId são derivados pelo serviço, nunca informados pelo cliente
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

O efeito é decidido por tipo e executado por um único ponto no serviço. A **decisão**
também é separada por tipo: nutrição decide cardápio/ingrediente; ADM decide restrição e
dados cadastrais (RF5).

```text
InclusaoAlergia / AlteracaoRestricao          (decisão: AdminSecretaria)
    → AlunoService.AdicionarRestricao(alunoId, restricaoNome, autor)
      (valida catálogo na Spec 010 e grava evento no histórico do aluno)
    → se falhar: transação desfeita, solicitação permanece EmAnalise

DadosCadastrais                              (decisão: AdminSecretaria)
    → regra completa na Spec 030 (snapshot, divergência, aplicação atômica)

MudancaCardapio                              (decisão: Nutricionista)
    → nenhum efeito automático; resposta registra "aplique a alteração em 015"

Ingrediente / FaltaIngrediente               (decisão: Nutricionista)
    → nenhum efeito automático; serve de pauta e de fonte para notificação (018)
```

Não existe "aplicar tudo": cada tipo tem efeito declarado, e o que não tem efeito
diz isso na resposta (RF7). Isso evita que aprovar um pedido altere dados em silêncio.

## Escopo

Reutiliza `EscopoService` (Specs 027 e 028):

```text
Nutricionista      → UnidadeId == unidade da sessão
AdminSecretaria    → UnidadeId == unidade da sessão
AdminEscola        → EscolaId == escola da sessão
Merendeira         → EscolaId == escola da sessão
Responsavel        → SolicitanteId == sessão OU AlunoId in (alunos vinculados)
```

`EscolaId` é derivado: escola do solicitante quando ele tem escola; escola do aluno
quando há `AlunoId`; nulo quando o solicitante é `Nutricionista` e não há aluno.

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/solicitacoes?...` | sessão com escopo |
| `GET` | `/api/solicitacoes/{id}` | sessão com escopo |
| `POST` | `/api/solicitacoes` | `Merendeira`, `Responsavel`, `AdminEscola`, `Nutricionista` |
| `POST` | `/api/solicitacoes/{id}/analise` | dono do tipo: `Nutricionista` ou `AdminSecretaria` |
| `POST` | `/api/solicitacoes/{id}/aprovar` | dono do tipo: `Nutricionista` ou `AdminSecretaria` |
| `POST` | `/api/solicitacoes/{id}/recusar` | dono do tipo: `Nutricionista` ou `AdminSecretaria` |
| `POST` | `/api/solicitacoes/{id}/concluir` | dono do tipo: `Nutricionista` ou `AdminSecretaria` |
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
  (`Restrict`); `EscolaId` anulável (`Restrict`); índices `(UnidadeId, CriadoEmUtc)`,
  `(EscolaId, CriadoEmUtc)` e `(Status, Tipo)`.
- `HistoricoSolicitacoes`: FK `SolicitacaoId` (`Cascade`), índice
  `(SolicitacaoId, CriadoEmUtc)`.
- `[ConcurrencyCheck]` em `Status` para RF de concorrência.
- Migration `Solicitacoes`.

## Frontend

- `pages/merendeiras/Solicitacoes.tsx`: criar (mudança de cardápio, ingrediente,
  falta) e acompanhar.
- `pages/pais/Solicitacoes.tsx`: criar (alergia/restrição) e acompanhar.
- `pages/escolas/Solicitacoes.tsx`: acompanhamento da escola.
- `pages/nutricao/solicitacoes/FilaSolicitacoes.tsx`: fila de cardápio/ingrediente
  com decisão e resposta obrigatória em aprovar/recusar.
- `pages/secretaria/Solicitacoes.tsx`: fila de restrição e dados cadastrais, com o
  mesmo fluxo de decisão e resposta obrigatória.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | campos obrigatórios por tipo |
| Domínio | todas as transições válidas e inválidas |
| Domínio | aprovar/recusar sem resposta lança |
| Serviço | pai para aluno não vinculado → 403 |
| Serviço | `Nutricionista` decidindo restrição → 403; `AdminSecretaria` decidindo cardápio → 403 |
| Serviço | `EscolaId` derivado do solicitante e do aluno |
| Serviço | aprovação de restrição aplica no aluno e grava histórico |
| Serviço | restrição fora do catálogo → 400 e permanece `EmAnalise` |
| Serviço | aprovação de cardápio não altera cardápio |
| HTTP | escopo por papel, escola e unidade, e concorrência (200/409) |

## Riscos

- **Efeito silencioso**: a lista de efeitos por tipo é explícita e testada; tipos sem
  efeito retornam resposta que diz isso.
- **Transação entre agregados**: aprovar restrição toca `Solicitacao` e `Aluno`; ambos
  usam o mesmo `ConnectionContext` Scoped e uma transação explícita.
- **Confusão entre aprovar e editar**: `MudancaCardapio` aprovada não edita; o texto
  de resposta e o teste impedem regressão.
- **Dois decisores**: a separação por tipo é uma tabela única no serviço; papel fora do
  dono do tipo recebe 403 e a solicitação não muda de estado.
