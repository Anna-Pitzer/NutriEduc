---
name: dominio-cardapio
status: created
references: V6, ADR-004, L10
---

# Design — Domínio de cardápio

## Estrutura

```text
backend/Domain/Entities/
  Cardapio.cs
  Refeicao.cs
  ItemRefeicao.cs
  SituacaoCardapio.cs        (enum)
backend/Domain/Services/
  CalculoNutricional.cs      (serviço de domínio puro)
backend/Domain/Interface/
  ICardapioRepository.cs
backend/API/Services/
  CardapioService.cs
backend/API/Controllers/
  CardapioController.cs
```

## Domínio

```csharp
public enum SituacaoCardapio { Rascunho, Publicado, Cancelado }

public class PeriodoVigencia
{
    public DateOnly Inicio { get; }
    public DateOnly Fim { get; }

    public PeriodoVigencia(DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio) throw new ArgumentException("O fim não pode ser anterior ao início.");
        if (fim.DayNumber - inicio.DayNumber > 366)
            throw new ArgumentException("O período não pode exceder 366 dias.");
        Inicio = inicio; Fim = fim;
    }

    public bool Contem(DateOnly data) => data >= Inicio && data <= Fim;
}
```

`PeriodoVigencia` é objeto de valor mapeado como tipo complexo (L10).

```csharp
public class ItemRefeicao
{
    public int AlimentoId { get; private set; }
    public decimal QuantidadeGramas { get; private set; }

    internal ItemRefeicao(int alimentoId, decimal gramas)
    {
        if (alimentoId <= 0) throw new ArgumentException("Item exige alimento válido.");
        if (gramas <= 0 || gramas > 5000)
            throw new ArgumentException("A quantidade deve estar entre 0 e 5000 gramas.");
        if (decimal.Round(gramas, 2) != gramas)
            throw new ArgumentException("A quantidade aceita no máximo duas casas decimais.");
        AlimentoId = alimentoId; QuantidadeGramas = gramas;
    }
}

public class Refeicao
{
    private readonly List<ItemRefeicao> _itens = new();

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int Ordem { get; private set; }
    public IReadOnlyList<ItemRefeicao> Itens => _itens.AsReadOnly();

    internal Refeicao(string nome, int ordem, IEnumerable<ItemRefeicao>? itens)
    {
        nome = (nome ?? string.Empty).Trim();
        if (nome.Length < 2 || nome.Length > 60)
            throw new ArgumentException("O nome da refeição deve ter entre 2 e 60 caracteres.");
        if (ordem <= 0) throw new ArgumentException("A ordem da refeição deve ser positiva.");

        var validados = (itens ?? Array.Empty<ItemRefeicao>()).ToList();
        if (validados.GroupBy(i => i.AlimentoId).Any(g => g.Count() > 1))
            throw new ArgumentException("O mesmo alimento não pode repetir na refeição.");

        Nome = nome; Ordem = ordem;
        foreach (var item in validados) _itens.Add(item);
    }
}
```

```csharp
public class Cardapio
{
    private readonly List<Refeicao> _refeicoes = new();

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int UnidadeId { get; private set; }
    public PeriodoVigencia Periodo { get; private set; } = null!;
    public SituacaoCardapio Situacao { get; private set; }
    public int? CardapioDuplicadoDeId { get; private set; }
    public int Versao { get; private set; } = 1;
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime AtualizadoEmUtc { get; private set; }
    public IReadOnlyList<Refeicao> Refeicoes => _refeicoes.AsReadOnly();

    public Cardapio(string nome, int unidadeId, PeriodoVigencia periodo,
                    IEnumerable<Refeicao>? refeicoes, int? duplicadoDeId = null)
    {
        var dados = ValidarDados(nome, unidadeId, periodo, refeicoes);
        Aplicar(dados, situacao: SituacaoCardapio.Rascunho, duplicadoDeId);
        CriadoEmUtc = AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Atualizar(string nome, PeriodoVigencia periodo, IEnumerable<Refeicao>? refeicoes)
    {
        if (Situacao != SituacaoCardapio.Rascunho)
            throw new InvalidOperationException("Só rascunho pode ser editado. Use a Spec 015.");
        // valida tudo, depois limpa e repõe _refeicoes
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    internal void MarcarPublicado() { /* Spec 015 */ }
}
```

Nota deliberada: `MarcarPublicado`/`MarcarCancelado` são declarados aqui apenas como
`internal` vazio; a Spec 015 implementa as transições e as pré-condições. Isso evita
que 012 publique por acidente e mantém um único dono da regra.

## Cálculo nutricional

```csharp
public static class CalculoNutricional
{
    public static ValoresNutricionais DoItem(ValoresNutricionais por100g, decimal gramas)
        => new(
            Arredondar(por100g.CaloriasKcal * gramas / 100m),
            Arredondar(por100g.ProteinasG * gramas / 100m),
            Arredondar(por100g.CarboidratosG * gramas / 100m),
            Arredondar(por100g.GordurasG * gramas / 100m),
            Arredondar(por100g.FibrasG * gramas / 100m),
            Arredondar(por100g.SodioMg * gramas / 100m));

    public static ValoresNutricionais Somar(params ValoresNutricionais[] valores) => ...;

    private static decimal Arredondar(decimal v) =>
        decimal.Round(v, 2, MidpointRounding.AwayFromZero);
}
```

`CalculoNutricional` recebe `ValoresNutricionais` já carregados; não consulta banco e
não conhece EF. É a única implementação do cálculo (RNF3).

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/cardapios?unidadeId=&data=&situacao=&pagina=&tamanho=` | `EquipeEscolar` escopada |
| `GET` | `/api/cardapios/{id}` | `EquipeEscolar` escopada |
| `POST` | `/api/cardapios` | `Nutricionista` |
| `PUT` | `/api/cardapios/{id}` | `Nutricionista` |
| `DELETE` | `/api/cardapios/{id}` | `Nutricionista` (só rascunho) |
| `POST` | `/api/cardapios/{id}/duplicar` | `Nutricionista` |
| `GET` | `/api/cardapios/publicados?escolaId=&unidadeId=&data=&refeicao=` | anônimo |

Detalhe:

```json
{ "id": 12, "nome": "Semana 12", "unidadeId": 3, "situacao": "Rascunho",
  "periodo": { "inicio": "2026-11-02", "fim": "2026-11-06" },
  "refeicoes": [
    { "nome": "Almoço", "ordem": 1,
      "itens": [ { "alimentoId": 7, "alimento": "Arroz", "gramas": 100,
                   "valores": { "caloriasKcal": 128.0, "proteinasG": 2.5,
                                "carboidratosG": 28.1, "gordurasG": 0.2,
                                "fibrasG": 1.6, "sodioMg": 1.0 } } ],
      "total": { "caloriasKcal": 128.0, "...": "..." } } ],
  "total": { "caloriasKcal": 128.0, "...": "..." } }
```

## Persistência

- `Cardapios`: FK `UnidadeId` (`Restrict`); `Periodo` como tipo complexo; índice
  `(UnidadeId, Inicio, Fim)` e `(Situacao)`.
- `Refeicoes`: FK `CardapioId` (`Cascade`), único `(CardapioId, Ordem)`.
- `ItensRefeicao`: FK `RefeicaoId` (`Cascade`), FK `AlimentoId` (`Restrict`), único
  `(RefeicaoId, AlimentoId)`.
- Migration `DominioCardapio`.

## Aplicação

`CardapioService` resolve escopo, carrega alimentos com valores em uma consulta
(`ObterPorIds`), monta o agregado e delega o cálculo. Leitura pública usa
`ListarPublicados(filtro)` sem escopo de sessão.

## Frontend

- `frontend/src/services/CardapioApi.ts` passa a apontar para rotas reais.
- `pages/nutricao/cardapios/ListarCardapios.tsx`, `FormularioCardapio.tsx`,
  `DetalheCardapio.tsx`.
- Totais exibidos vêm do backend; o formulário não recalcula (RNF3).
- `pages/pais/Cardapios.tsx` e `pages/merendeiras/Cardapios.tsx` consomem apenas
  `/api/cardapios/publicados`.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | período invertido e > 366 dias lançam `ArgumentException` |
| Domínio | item com 0 g, 5001 g e 3 casas lança |
| Domínio | alimento repetido na refeição lança |
| Domínio | cálculo confere para 30 g, 100 g e 250 g |
| Domínio | `Atualizar` em publicado lança `InvalidOperationException` |
| Serviço | duplicar copia refeições/itens e zera situação |
| Serviço | adicionar alimento inativo → 400 |
| HTTP | rota pública sem sessão devolve só publicados |
| HTTP | excluir publicado → 409 |

## Riscos

- **Cardápio grande**: carregar todos os itens em cada GET é aceitável até algumas
  centenas; o índice por unidade/data cobre a listagem. Paginação de refeições não é
  requisito.
- **Arredondamento**: somar valores já arredondados difere de arredondar a soma total.
  A decisão é arredondar por item e somar; está registrada em teste para não mudar
  silenciosamente.
- **Duplicação entre períodos**: permitida em 012; a Spec 015 é quem impede dois
  publicados com o mesmo período/unidade.
