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
  PeriodoVigencia.cs
  SituacaoCardapio.cs        (enum)
  FiltroCardapio.cs
backend/Domain/Services/
  CalculoNutricional.cs      (serviço de domínio puro)
backend/Domain/Interface/
  ICardapioRepository.cs
backend/API/Services/
  CardapioService.cs
backend/API/Controllers/
  CardapioController.cs
backend/Application/
  CardapioDTOs.cs
```

`CalculoNutricional` fica em `Domain/Services`, ao lado de `PoliticaDeExclusao`, para
não repetir o erro que a Spec 024 corrigiu: regra pura não mora na aplicação.

## Domínio

```csharp
public enum SituacaoCardapio { Rascunho, Publicado, Cancelado }
```

A Spec 015 acrescenta `Substituido` ao final — alteração aditiva de enum, sem migração
de dados (o EF persiste enum como inteiro e o valor novo recebe o próximo ordinal).

```csharp
public class PeriodoVigencia
{
    public const int DiasMaximos = 366;

    public DateOnly Inicio { get; private set; }
    public DateOnly Fim { get; private set; }

    /// <summary>Somente para o EF Core materializar o tipo complexo. Não valida (L10).</summary>
    private PeriodoVigencia()
    {
    }

    public PeriodoVigencia(DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio)
            throw new ArgumentException("O fim não pode ser anterior ao início.");

        // Inclusive: 01/01 a 31/12 do mesmo ano bissexto = 366 dias, aceito.
        if (fim.DayNumber - inicio.DayNumber + 1 > DiasMaximos)
            throw new ArgumentException($"O período não pode exceder {DiasMaximos} dias.");

        Inicio = inicio;
        Fim = fim;
    }

    public bool Contem(DateOnly data) => data >= Inicio && data <= Fim;
}
```

Tipo complexo exige construtor sem parâmetros e setters que o materializador alcance —
é a mesma correção que `ValoresNutricionais` recebeu na Spec 011, e o motivo do L10.
Um `record` com propriedades só de leitura **não** funciona aqui.

```csharp
public class ItemRefeicao
{
    public int RefeicaoId { get; private set; }
    public int AlimentoId { get; private set; }
    public decimal QuantidadeGramas { get; private set; }

    /// <summary>Carregado nas consultas de detalhe para nome e valores (Spec 011).</summary>
    public Alimento? Alimento { get; private set; }

    private ItemRefeicao()
    {
    }

    internal ItemRefeicao(int alimentoId, decimal gramas)
    {
        if (alimentoId <= 0)
            throw new ArgumentException("Item exige alimento válido.");

        // Uma regra de medida para o projeto (Spec 011, ValidacaoDeCampo.Medida);
        // o limite de 5000 g é específico da refeição e fica aqui.
        var quantidade = ValidacaoDeCampo.Medida(gramas, "quantidade");

        if (quantidade <= 0 || quantidade > 5000)
            throw new ArgumentException("A quantidade deve estar entre 0 e 5000 gramas.");

        AlimentoId = alimentoId;
        QuantidadeGramas = quantidade;
    }
}
```

`ItemRefeicao` não tem identidade própria: a chave é `(RefeicaoId, AlimentoId)`, que já
é o índice único de RF3.

```csharp
public class Refeicao
{
    private const int ItensMaximos = 50;
    private const int NomeMin = 2;
    private const int NomeMax = 60;

    private readonly List<ItemRefeicao> _itens = new();

    public int Id { get; private set; }
    public int CardapioId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int Ordem { get; private set; }

    public IReadOnlyList<ItemRefeicao> Itens => _itens.AsReadOnly();

    private Refeicao()
    {
    }

    internal Refeicao(string nome, int ordem, IEnumerable<ItemRefeicao>? itens)
    {
        var dados = Validar(nome, ordem, itens);
        Nome = dados.Nome;
        Ordem = dados.Ordem;
        _itens.AddRange(dados.Itens);
    }

    internal static Refeicao Copiar(Refeicao origem)
    {
        var itens = origem._itens
            .Select(i => new ItemRefeicao(i.AlimentoId, i.QuantidadeGramas));

        return new Refeicao(origem.Nome, origem.Ordem, itens);
    }

    private static (string Nome, int Ordem, List<ItemRefeicao> Itens) Validar(
        string? nome, int ordem, IEnumerable<ItemRefeicao>? itens)
    {
        var nomeValidado = ValidacaoDeCampo.Texto(nome, "nome da refeição", NomeMin, NomeMax);

        if (ordem <= 0)
            throw new ArgumentException("A ordem da refeição deve ser positiva.");

        var validados = (itens ?? Array.Empty<ItemRefeicao>()).ToList();

        if (validados.Count > ItensMaximos)
            throw new ArgumentException($"A refeição aceita no máximo {ItensMaximos} itens.");

        if (validados.GroupBy(i => i.AlimentoId).Any(g => g.Count() > 1))
            throw new ArgumentException("O mesmo alimento não pode repetir na refeição.");

        return (nomeValidado, ordem, validados);
    }
}
```

```csharp
public class Cardapio
{
    private const int RefeicoesMaximas = 10;

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

    private Cardapio()
    {
    }

    public Cardapio(string nome, int unidadeId, PeriodoVigencia periodo,
                    IEnumerable<Refeicao>? refeicoes, int? duplicadoDeId = null)
    {
        var dados = Validar(nome, unidadeId, periodo, refeicoes);
        Aplicar(dados);

        Situacao = SituacaoCardapio.Rascunho;
        CardapioDuplicadoDeId = duplicadoDeId;
        CriadoEmUtc = AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Atualizar(string nome, PeriodoVigencia periodo, IEnumerable<Refeicao>? refeicoes)
    {
        if (Situacao != SituacaoCardapio.Rascunho)
            throw new InvalidOperationException("Só rascunho pode ser editado. Use a Spec 015.");

        // Valida tudo antes de tocar em qualquer estado (RF12). Um cardápio
        // parcialmente substituído seria pior que um erro.
        var dados = Validar(nome, UnidadeId, periodo, refeicoes);
        Aplicar(dados);
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Cópia para duplicação (RF6) e para revisão (Spec 015). Cria instâncias novas:
    /// compartilhar `Refeicao`/`ItemRefeicao` entre agregados violaria ADR-004.
    ///
    /// O marcador de origem é parâmetro, não `origem.Id` fixo: duplicação grava
    /// `CardapioDuplicadoDeId` e a revisão da Spec 015 grava `RevisaoDeId` na mesma
    /// fábrica, sem inventar uma segunda cópia.
    /// </summary>
    internal static Cardapio Copiar(
        Cardapio origem, string nome, PeriodoVigencia periodo, int? duplicadoDeId = null)
    {
        var refeicoes = origem._refeicoes.Select(Refeicao.Copiar);

        return new Cardapio(nome, origem.UnidadeId, periodo, refeicoes, duplicadoDeId);
    }

    private void Aplicar((string Nome, int UnidadeId, PeriodoVigencia Periodo, List<Refeicao> Refeicoes) dados)
    {
        Nome = dados.Nome;
        UnidadeId = dados.UnidadeId;
        Periodo = dados.Periodo;

        _refeicoes.Clear();
        _refeicoes.AddRange(dados.Refeicoes);
    }

    private static (string Nome, int UnidadeId, PeriodoVigencia Periodo, List<Refeicao> Refeicoes) Validar(
        string? nome, int unidadeId, PeriodoVigencia? periodo, IEnumerable<Refeicao>? refeicoes)
    {
        var nomeValidado = ValidacaoDeCampo.Texto(nome, "nome", 2, 120);

        if (unidadeId <= 0)
            throw new ArgumentException("O cardápio exige unidade válida.");

        if (periodo is null)
            throw new ArgumentException("O período do cardápio é obrigatório.");

        var validados = (refeicoes ?? Array.Empty<Refeicao>()).ToList();

        if (validados.Count > RefeicoesMaximas)
            throw new ArgumentException($"O cardápio aceita no máximo {RefeicoesMaximas} refeições.");

        if (validados.GroupBy(r => r.Ordem).Any(g => g.Count() > 1))
            throw new ArgumentException("Duas refeições não podem ter a mesma ordem.");

        // Nomes distintos sem caixa: o filtro público casa por nome (RF10), e duas
        // refeições homônimas tornariam o filtro ambíguo.
        if (validados.GroupBy(r => r.Nome, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            throw new ArgumentException("Duas refeições não podem ter o mesmo nome.");

        return (nomeValidado, unidadeId, periodo, validados);
    }
}
```

Não há `MarcarPublicado`/`MarcarCancelado` nesta spec: método `internal` vazio seria
código morto por duas specs e convite a chamada indevida. A Spec 015 os cria junto das
pré-condições. `Versao` permanece porque a Spec 015 já a consome e é atributo do ciclo
de vida do agregado, não abstração especulativa.

## Cálculo nutricional

```csharp
public static class CalculoNutricional
{
    public static ValoresNutricionais DoItem(ValoresNutricionais por100g, decimal gramas)
        => ValoresNutricionais.Criar(
            Arredondar(por100g.CaloriasKcal * gramas / 100m),
            Arredondar(por100g.ProteinasG * gramas / 100m),
            Arredondar(por100g.CarboidratosG * gramas / 100m),
            Arredondar(por100g.GordurasG * gramas / 100m),
            Arredondar(por100g.FibrasG * gramas / 100m),
            Arredondar(por100g.SodioMg * gramas / 100m));

    public static ValoresNutricionais Somar(IEnumerable<ValoresNutricionais> valores)
    {
        var lista = valores as IReadOnlyCollection<ValoresNutricionais> ?? valores.ToList();

        return ValoresNutricionais.Criar(
            lista.Sum(v => v.CaloriasKcal),
            lista.Sum(v => v.ProteinasG),
            lista.Sum(v => v.CarboidratosG),
            lista.Sum(v => v.GordurasG),
            lista.Sum(v => v.FibrasG),
            lista.Sum(v => v.SodioMg));
    }

    private static decimal Arredondar(decimal valor)
        => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
}
```

`Criar` é a única porta de entrada de `ValoresNutricionais` (Spec 011) e rejeita mais de
duas casas — por isso o arredondamento acontece **antes** de criar. A soma de valores
já arredondados continua com duas casas exatas, então `Somar` não arredonda de novo.

Consequência registrada: somar valores arredondados difere de arredondar a soma total.
A decisão é arredondar por item e somar, com teste que a fixa.

## Contrato de API

| Método | Rota | Autorização |
|---|---|---|
| `GET` | `/api/cardapios?unidadeId=&data=&situacao=&pagina=&tamanho=` | `SessaoValida` + escopo |
| `GET` | `/api/cardapios/{id}` | `SessaoValida` + escopo |
| `POST` | `/api/cardapios` | `Nutricionista` |
| `PUT` | `/api/cardapios/{id}` | `Nutricionista` |
| `DELETE` | `/api/cardapios/{id}` | `Nutricionista` (só rascunho) |
| `POST` | `/api/cardapios/{id}/duplicar` | `Nutricionista` |
| `GET` | `/api/cardapios/publicados?unidadeId=&data=&refeicao=&pagina=&tamanho=` | **anônimo** |

Escrita usa `[Authorize(Policy = "Nutricionista")]`. Leitura interna usa
`[Authorize(Policy = "SessaoValida")]` + `EscopoService` e recusa por papel no serviço,
como a Spec 010 e a Spec 011 — `AdminEscola` não entra (RF8). A rota pública é a única
ação sem `[Authorize]`, e por isso exige `unidadeId` explicitamente.

Listagem (sem refeições — carregar itens de 20 cardápios para mostrar metadados seria
desperdício):

```json
{ "pagina": 1, "tamanho": 20, "total": 3,
  "itens": [ { "id": 12, "nome": "Semana 12", "unidadeId": 3,
               "situacao": "Rascunho", "totalRefeicoes": 2,
               "periodo": { "inicio": "2026-11-02", "fim": "2026-11-06" } } ] }
```

Detalhe (mesmo DTO na rota pública, sem os campos de gestão):

```json
{ "id": 12, "nome": "Semana 12", "unidadeId": 3, "situacao": "Rascunho",
  "periodo": { "inicio": "2026-11-02", "fim": "2026-11-06" },
  "refeicoes": [
    { "nome": "Almoço", "ordem": 1,
      "itens": [ { "alimentoId": 7, "alimento": "Arroz", "gramas": 100,
                   "valores": { "caloriasKcal": 128.0, "proteinasG": 2.5,
                                "carboidratosG": 28.1, "gordurasG": 0.2,
                                "fibrasG": 1.6, "sodioMg": 1.0 } } ],
      "total": { "caloriasKcal": 128.0, "proteinasG": 2.5, "carboidratosG": 28.1,
                 "gordurasG": 0.2, "fibrasG": 1.6, "sodioMg": 1.0 } } ],
  "total": { "caloriasKcal": 128.0, "proteinasG": 2.5, "carboidratosG": 28.1,
             "gordurasG": 0.2, "fibrasG": 1.6, "sodioMg": 1.0 } }
```

`Situacao` sai como texto pelo `JsonStringEnumConverter` já registrado em `Program.cs`.

## Persistência

- `Cardapios`: FK `UnidadeId` (`Restrict`); `Periodo` como **tipo complexo**
  (`ComplexProperty` → colunas `Periodo_Inicio`, `Periodo_Fim`); índices
  `(UnidadeId, Situacao, Periodo_Inicio, Periodo_Fim)` e `(Situacao)`.
- `Refeicoes`: FK `CardapioId` (`Cascade`); único `(CardapioId, Ordem)`; coleção lida
  por campo:
  `Navigation(c => c.Refeicoes).HasField("_refeicoes").UsePropertyAccessMode(PropertyAccessMode.Field)`.
- `ItensRefeicao`: chave composta `(RefeicaoId, AlimentoId)`; FK `AlimentoId`
  (`Restrict`); navegação `Alimento` para nome e valores.
- `DateOnly` no SQLite é `TEXT` no formato ISO: comparação `>=`/`<=` do período
  funciona lexicograficamente. Nenhum cálculo numérico no SQL (RNF6).
- Migration `DominioCardapio`.

## Exclusão de unidade

`UnidadeId` é `Restrict`: sem tratamento, excluir uma unidade com cardápio lança
`DbUpdateException` e o middleware devolve 500. A mesma entrega acrescenta:

- `ICardapioRepository.ExisteCardapioDaUnidade(int unidadeId)`;
- parâmetro `temCardapios` em `PoliticaDeExclusao.ParaUnidade`;
- leitura correspondente em `ExclusaoService` (linha que monta os três booleanos hoje);
- mensagem "há cardápios vinculados a esta unidade" e teste de 409.

Sem isso a Spec 012 quebra o fluxo de exclusão da Spec 024.

## Aplicação

`CardapioService`:

| Método | Regra |
|---|---|
| `Listar(filtro, pagina, tamanho, escopo)` | `Nutricionista` vê tudo; `Merendeira` só a própria unidade; demais → `Proibido` |
| `Obter(id, escopo)` | carrega refeições, itens e alimentos; monta totais |
| `Criar(dto)` | valida unidade existente (`IEscolaRepository.GetUnidade`), monta o agregado e salva na transação |
| `Atualizar(id, dto)` | 404 se não existir; carrega o cardápio atual; aplica RF4 (alimento inativo só se já estava) |
| `Excluir(id)` | 404; 409 se não for `Rascunho` |
| `Duplicar(id, dto)` | `Cardapio.Copiar` com o período novo; 404 se origem não existir |
| `ListarPublicados(filtro, p)` | sem escopo; `unidadeId` obrigatório; só `Publicado`; `refeicao` comparada sem caixa (`ToLower`) e com acento significativo (RF10) |

RF4 é regra **entre agregados** (precisa do catálogo e do estado anterior), então mora
no serviço: monta `alimentosPermitidos = ids ativos ∪ ids já presentes no cardápio` e
recusa o restante com 400. O agregado valida apenas estrutura (RF1–RF3).

Alimentos são carregados em lote com o novo `IAlimentoRepository.ObterPorIds` — uma
consulta por cardápio, não uma por item.

## Frontend

`CardapioApi.ts` é reescrito: tipos `PeriodoCardapio`, `ValoresItem`, `ItemCardapio`,
`RefeicaoCardapio`, `Cardapio`, `CardapioResumo`, `PaginaCardapios`, `CardapioEntrada`;
funções `listarCardapios`, `obterCardapio`, `criarCardapio`, `atualizarCardapio`,
`excluirCardapio`, `duplicarCardapio`, `listarCardapiosPublicados`. Saem
`buscarCardapio`, `buscarHistoricoCardapios`, `salvarCardapio` e os tipos
`Refeicao`, `Serie`, `AlimentoCardapio`, `CardapioCadastro`, `CardapioHistorico`.

`pages/nutricao/cardapios/`: `ListarCardapios.tsx`, `FormularioCardapio.tsx` e
`DetalheCardapio.tsx`. `Cardapio.tsx` e `MontarCardapio.tsx` (fluxo `localStorage`)
são **removidos**, não reaproveitados: manter dois fluxos seria a duplicação que o L9
descreve. `components/CardapiosDiv.tsx` também sai — seus únicos consumidores eram as
duas páginas e ele declara uma segunda `AlimentoCardapio`.

Rotas: `/cardapios`, `/cardapios/novo`, `/cardapios/:id/editar`, `/cardapios/:id`, sob
`Nutricionista` + `AdminSecretaria` como hoje; menu aponta para `/cardapios`.
`HomeNutricao` passa a chamar `listarCardapios` e linkar `/cardapios/{id}` (a rota
`/cardapio/visualizar/{id}`, que não existe, desaparece com a página).
`pages/pais` e `pages/merendeiras` consomem apenas `listarCardapiosPublicados`,
enviando o `unidadeId` do próprio usuário.

Nenhum total é recalculado no cliente (RNF3): o formulário exibe `valores` e `total`
vindos do backend.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | período invertido, 366 dias aceito e 367 dias lançam |
| Domínio | item com 0 g, 5001 g e 3 casas lança |
| Domínio | alimento repetido na refeição lança |
| Domínio | 11 refeições e 51 itens na refeição lançam |
| Domínio | `Ordem` repetida e nome repetido sem caixa lançam |
| Domínio | cálculo confere para 30 g, 100 g e 250 g; soma de duas refeições |
| Domínio | `Atualizar` inválido não altera refeições nem período |
| Domínio | `Copiar` gera agregado independente (mesma composição, instâncias novas) |
| Modelo | `Periodo` mapeado como tipo complexo, chave composta de `ItemRefeicao`, índices |
| Serviço | duplicar copia refeições/itens e zera situação |
| Serviço | adicionar alimento inativo novo → 400; manter inativo já presente → ok |
| Serviço | `Merendeira` de outra unidade recebe `Proibido` |
| HTTP | rota pública sem sessão, com `unidadeId`, devolve só publicados; sem `unidadeId` → 400 |
| HTTP | excluir publicado → 409 |
| HTTP | excluir unidade com cardápio → 409 (não 500) |
| Infra | todas as 7 rotas na tabela do README (`DocumentationTests`) |

## Riscos

- **Contrato quebrado no frontend**: `CardapioApi` muda por completo e duas telas são
  removidas. É a maior parte do esforço desta spec e a única etapa que exige `npm run
  build` entre a troca da API e a troca das telas.
- **`decimal` em SQLite**: a coluna `decimal(10,2)` tem afinidade NUMERIC e grava
  REAL/INTEGER, sem escala — ver PEND-029 e PEND-030. Qualquer `ORDER BY` ou `SUM`
  futuro sobre medidas seria float; o cálculo fica em C# (RNF6).
- **Cardápio grande**: 10 refeições × 50 itens por detalhe é o pior caso aceito; o
  `GET` de detalhe carrega tudo, e a listagem não carrega nada.
- **Acento no filtro `refeicao`**: decisão explícita de RF10. Se a unidade nomear
  refeições com acento e o cliente filtrar sem acento, a resposta é vazia — por isso o
  frontend envia o nome lido da resposta, nunca um literal.
- **Duplicação entre períodos**: permitida em 012; quem impede dois publicados
  sobrepostos é a Spec 015.
