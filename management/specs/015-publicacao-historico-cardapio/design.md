---
name: publicacao-historico-cardapio
status: created
references: V6, ADR-004, L7
---

# Design — Publicação, versionamento e histórico

## Estrutura

```text
backend/Domain/Entities/
  Cardapio.cs                    (+ transições)
  HistoricoCardapio.cs
  EventoCardapio.cs              (enum)
  SituacaoCardapio.cs            (Substituido)
backend/Domain/Exceptions/
  TransicaoInvalidaException.cs  (herda de InvalidOperationException → 409 na Spec 007)
backend/Domain/Interface/
  ICardapioRepository.cs         (+ conflito de período, feed)
backend/API/Services/
  PublicacaoCardapioService.cs
backend/API/Controllers/
  CardapioController.cs          (+ publicar, cancelar, revisao, historico, alteracoes)
```

## Domínio — transições

```csharp
public enum SituacaoCardapio { Rascunho, Publicado, Cancelado, Substituido }

public enum EventoCardapio { Criado, Editado, Duplicado, RevisaoCriada, Publicado, Cancelado, Substituido }

public class HistoricoCardapio
{
    private HistoricoCardapio() { }              // EF Core

    public int Id { get; private set; }
    public int CardapioId { get; private set; }
    public EventoCardapio Evento { get; private set; }
    public int Versao { get; private set; }
    public string AutorNome { get; private set; } = string.Empty;
    public string? Motivo { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }

    internal HistoricoCardapio(int cardapioId, EventoCardapio evento, int versao,
                               string autorNome, string? motivo, DateTime utc) { ... }
}
```

```csharp
public void Publicar(string autorNome, DateTime utc)
{
    if (Situacao == SituacaoCardapio.Publicado)
        throw new TransicaoInvalidaException("Cardápio já publicado.");
    if (Situacao != SituacaoCardapio.Rascunho)
        throw new TransicaoInvalidaException("Somente rascunho pode ser publicado.");

    Situacao = SituacaoCardapio.Publicado;
    PublicadoEmUtc = utc;
    PublicadoPor = autorNome;
    AtualizadoEmUtc = utc;
    RegistrarEvento(EventoCardapio.Publicado, autorNome, null, utc);
}

public void Cancelar(string motivo, string autorNome, DateTime utc)
{
    motivo = (motivo ?? string.Empty).Trim();
    if (motivo.Length < 2 || motivo.Length > 500)
        throw new ArgumentException("O motivo deve ter entre 2 e 500 caracteres.");
    if (Situacao is SituacaoCardapio.Cancelado or SituacaoCardapio.Substituido)
        throw new TransicaoInvalidaException("Cardápio já encerrado.");

    Situacao = SituacaoCardapio.Cancelado;
    AtualizadoEmUtc = utc;
    RegistrarEvento(EventoCardapio.Cancelado, autorNome, motivo, utc);
}

public Cardapio CriarRevisao(string autorNome, DateTime utc)
{
    if (Situacao != SituacaoCardapio.Publicado)
        throw new TransicaoInvalidaException("Somente cardápio publicado pode ser revisado.");

    var revisao = new Cardapio(Nome, UnidadeId, Periodo, ClonarRefeicoes(),
                               duplicadoDeId: null, revisaoDeId: Id, versao: Versao + 1);
    revisao.RegistrarEvento(EventoCardapio.RevisaoCriada, autorNome, $"Revisão de #{Id}", utc);
    return revisao;
}

internal void MarcarSubstituido(int porCardapioId, string autorNome, DateTime utc)
{
    if (Situacao != SituacaoCardapio.Publicado)
        throw new TransicaoInvalidaException("Somente publicado pode ser substituído.");
    Situacao = SituacaoCardapio.Substituido;
    AtualizadoEmUtc = utc;
    RegistrarEvento(EventoCardapio.Substituido, autorNome, $"Substituído por #{porCardapioId}", utc);
}
```

`RegistrarEvento` é `private/internal` e adiciona à coleção privada `_historico`.
Nenhum evento é removido. `Substituido` deixa de aparecer na rota pública.

## Concorrência

`Situacao` e `Versao` recebem `[ConcurrencyCheck]`. O `UPDATE` inclui os valores
originais; se outro processo mudou a linha, o EF lança
`DbUpdateConcurrencyException`, traduzida para 409. Isso cobre RF13 sem lock
explícito e sem depender de `rowversion` (SQLite).

## Fluxo de publicação

```text
1. Carregar cardápio com refeições/itens        (404 se ausente)
2. Autorizar: Nutricionista                     (403)
3. Pré-condição de situação no domínio          (409)
4. Analisar compatibilidade (Spec 014)          (422 se bloqueante)
5. Verificar sobreposição de publicado          (409 com id conflitante)
6. Se RevisaoDeId != null:
       carregar original e MarcarSubstituido
7. Publicar no domínio
8. Persistir cardápio + histórico na mesma transação
```

A ordem importa: compatibilidade é verificada **antes** de qualquer escrita; se ela
falhar, nada é alterado.

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `POST` | `/api/cardapios/{id}/publicar` | `Nutricionista` |
| `POST` | `/api/cardapios/{id}/cancelar` | `Nutricionista` (publicado) / dono do rascunho |
| `POST` | `/api/cardapios/{id}/revisao` | `Nutricionista` |
| `GET` | `/api/cardapios/{id}/historico?pagina=&tamanho=` | `Nutricionista`, `AdminEscola`, `Merendeira` (escopo) |
| `GET` | `/api/cardapios/alteracoes?desde=&pagina=&tamanho=` | `Nutricionista`, `AdminEscola`, `Merendeira` (escopo) |

422 de publicação:

```json
{ "erro": "Cardápio incompatível com restrições de alunos.",
  "detalhes": [ { "alimento": "Iogurte", "restricao": "Lactose", "refeicao": "Almoço" } ] }
```

409 de sobreposição:

```json
{ "erro": "Já existe cardápio publicado no período.", "detalhes": { "cardapioId": 27 } }
```

Evento de histórico:

```json
{ "evento": "Publicado", "versao": 2, "autor": "Maria",
  "motivo": null, "criadoEm": "2026-10-30T13:00:00Z" }
```

## Persistência

- `HistoricoCardapios`: FK `CardapioId` (`Cascade`), índice
  `(CardapioId, CriadoEmUtc)`; `AutorNome` e `Motivo` são snapshots.
- Índice `(Situacao, UnidadeId, Inicio, Fim)` em `Cardapios` para o conflito de
  período e para o feed.
- Migration `PublicacaoHistoricoCardapio`.
- `RevisaoDeId` anulável, `Restrict` (não cascatear revisão ao apagar original; na
  prática original publicado não é apagado).

## Frontend

- `DetalheCardapio.tsx`: botões Publicar/Cancelar, selo de situação, motivo do
  bloqueio e linha do tempo.
- `pages/merendeiras/AlteracoesCardapios.tsx`: consome `/api/cardapios/alteracoes`
  com filtro de data e unidade da sessão.
- Confirmação de cancelamento exige motivo; o texto é enviado ao backend e exibido
  no histórico.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | publicar fora de rascunho lança `TransicaoInvalidaException` |
| Domínio | cancelar em `Cancelado`/`Substituido` lança |
| Domínio | revisão incrementa versão e mantém original |
| Domínio | eventos gerados na ordem e com autor corretos |
| Serviço | bloqueante → 422 sem alterar situação |
| Serviço | sobreposição → 409 com id conflitante |
| Serviço | publicação de revisão substitui original na mesma transação |
| HTTP | conflito de concorrência → 409 |
| HTTP | feed respeita escopo por unidade |

## Riscos

- **Duas publicações concorrentes**: mitigado por `ConcurrencyCheck`; o teste HTTP
  dispara duas requisições e exige um 200 e um 409.
- **Histórico como fonte de auditoria**: snapshots de autor evitam perda quando o
  usuário é desativado ou renomeado.
- **Revisão em cascata**: se o original for apagado, `Restrict` impede; rascunho
  excluído cascateia apenas o próprio histórico, que é aceitável.
