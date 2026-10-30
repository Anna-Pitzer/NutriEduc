---
name: geracao-automatica-cardapios
status: created
references: V6, ADR-004, L7
---

# Design — Geração automática de cardápios

## Estrutura

```text
backend/Domain/Entities/
  ParametroGeracao.cs
backend/Domain/Services/
  GeradorCardapioAutomatico.cs   (função pura)
  ResultadoGeracao.cs
backend/Domain/Interface/
  IParametroGeracaoRepository.cs
  ICardapioRepository.cs         (consulta de publicados recentes)
backend/API/Services/
  GeracaoCardapioService.cs
backend/API/Controllers/
  GeracaoController.cs
```

## Domínio — parâmetros

```csharp
public class ParametroGeracao
{
    private readonly List<int> _categoriasObrigatorias = new();

    public int Id { get; private set; }
    public int UnidadeId { get; private set; }
    public decimal CaloriasMinimasRefeicao { get; private set; }
    public decimal CaloriasMaximasRefeicao { get; private set; }
    public int OpcoesPorRefeicao { get; private set; }
    public int JanelaRepeticaoDias { get; private set; }
    public bool ExcluirAlimentosIncompativeis { get; private set; }
    public IReadOnlyList<int> CategoriasObrigatorias => _categoriasObrigatorias.AsReadOnly();

    public ParametroGeracao(int unidadeId, decimal min, decimal max, int opcoes,
                            int janela, bool excluirIncompativeis, IEnumerable<int> categorias)
    {
        if (unidadeId <= 0) throw new ArgumentException("Parâmetro exige unidade válida.");
        if (min < 0 || max < 0) throw new ArgumentException("Faixa calórica não pode ser negativa.");
        if (min > max) throw new ArgumentException("O mínimo não pode exceder o máximo.");
        if (opcoes is < 1 or > 10) throw new ArgumentException("Opções por refeição deve estar entre 1 e 10.");
        if (janela is < 0 or > 30) throw new ArgumentException("Janela de repetição deve estar entre 0 e 30.");
        // categorias: ids > 0, distintos
    }

    public void Atualizar(decimal min, decimal max, int opcoes, int janela,
                          bool excluirIncompativeis, IEnumerable<int> categorias) { /* mesmas invariantes */ }
}
```

## Domínio — gerador

```csharp
public sealed record CandidatoAlimento(int Id, int CategoriaId, ValoresNutricionais Valores);

public sealed record EntradaGeracao(
    IReadOnlyList<RefeicaoPadrao> Refeicoes,
    IReadOnlyList<CandidatoAlimento> Candidatos,
    ParametroGeracao Parametros,
    IReadOnlyCollection<int> AlimentosBloqueados,
    int Seed);

public sealed record RefeicaoPadrao(string Nome, int Ordem);

public sealed record ResultadoGeracao(
    IReadOnlyList<(RefeicaoPadrao Refeicao, IReadOnlyList<ItemGerado> Itens)> Refeicoes,
    int Seed,
    IReadOnlyList<string> Avisos);

public static class GeradorCardapioAutomatico
{
    public static ResultadoGeracao Gerar(EntradaGeracao entrada)
    {
        var random = new Random(entrada.Seed);
        var usados = new HashSet<int>();
        var resultado = new List<...>();

        foreach (var refeicao in entrada.Refeicoes.OrderBy(r => r.Ordem))
        {
            // 1. candidatos ativos, não bloqueados, não usados no dia
            // 2. embaralhar com random (Fisher-Yates)
            // 3. atender categorias obrigatórias primeiro
            // 4. completar até OpcoesPorRefeicao respeitando faixa calórica
            // 5. se faixa não fechada → throw GeracaoImpossivelException(refeicao, motivo)
            usados.Add(...);
        }
        return new(resultado, entrada.Seed, avisos);
    }
}
```

Pontos que garantem determinismo: `Random(seed)` é a única fonte de aleatoriedade;
`OrderBy` sempre com desempate explícito (`ThenBy(a => a.Id)`) para não depender da
ordem de retorno do banco; nenhuma cultura na formatação; nenhuma leitura de
`DateTime.UtcNow` dentro do gerador.

`GeracaoImpossivelException` carrega `Refeicao` e `Motivo` e é traduzida para 422
pelo controller/middleware.

## Fluxo no serviço

```text
1. Carregar parâmetros da unidade            (ausente → 400)
2. Carregar candidatos (alimentos ativos)    (vazio → 422)
3. Carregar estrutura padrão de refeições da unidade
4. Se ExcluirAlimentosIncompativeis:
      restrições dos alunos da unidade (Spec 009)
      análise de compatibilidade (Spec 014) → ids bloqueantes
5. Se JanelaRepeticaoDias > 0:
      ids usados em cardápios Publicado recentes da unidade (Spec 015)
6. Chamar GeradorCardapioAutomatico.Gerar(entrada)
7. Montar Cardapio (Spec 012) em Rascunho e persistir
```

Estrutura padrão de refeições: `CategoriaAlimento`? Não. A estrutura é uma lista
fixa por unidade, configurável em `ParametroGeracao`? Para não criar entidade extra,
a estrutura é derivada da última configuração: `RefeicoesPadrao` é uma lista de
nomes/ordens persistida em coluna JSON de `ParametroGeracao`
(`["Café da manhã","Almoço","Jantar"]`). Decisão explícita para manter KISS; se o
produto exigir ordem por dia, vira entidade em spec futura.

## Endpoints

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/unidades/{unidadeId}/parametros-geracao` | `Nutricionista`, `AdminEscola` (leitura) |
| `PUT` | `/api/unidades/{unidadeId}/parametros-geracao` | `Nutricionista` |
| `POST` | `/api/cardapios/gerar` | `Nutricionista` |

Resposta de geração:

```json
{ "cardapioId": 31, "seed": 8412,
  "totais": [ { "refeicao": "Almoço", "caloriasKcal": 612.4 } ],
  "avisos": ["Categoria 'Fruta' atendida por um único alimento."] }
```

422:

```json
{ "erro": "Não foi possível gerar a refeição 'Almoço'.",
  "detalhes": "Faixa calórica 550–700 kcal inatingível com 4 candidatos." }
```

## Persistência

- `ParametrosGeracao`: único `UnidadeId`; colunas decimais; `RefeicoesPadrao` como
  coluna `TEXT` com JSON; `CategoriasObrigatorias` como tabela
  `ParametrosCategoriasObrigatorias (ParametroId, CategoriaId)` ou coleção primitiva
  de inteiros (mesmo padrão da Spec 003). Decisão: coleção primitiva, uma coluna
  JSON gerenciada pelo EF.
- Migration `ParametrosGeracao`.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | parâmetros: `min > max`, opções 0/11, janela 31 lançam |
| Domínio | mesma semente → resultado idêntico (comparação item a item) |
| Domínio | sementes diferentes → resultados diferentes (base com candidatos suficientes) |
| Domínio | faixa calórica respeitada em todas as refeições |
| Domínio | alimento bloqueado nunca é escolhido |
| Domínio | categoria obrigatória sem candidato lança `GeracaoImpossivelException` |
| Serviço | parâmetros ausentes → 400; candidatos vazios → 422 |
| HTTP | papel não autorizado → 403; cardápio gerado é rascunho |

## Riscos

- **Determinismo frágil**: qualquer `OrderBy` sem desempate ou `HashSet` iterado
  pode variar entre execuções. Os testes de determinismo rodam duas vezes na mesma
  base e uma vez com a base inserida em ordem inversa.
- **Resultado nutricionalmente pobre**: o algoritmo guloso não otimiza variedade;
  é um gerador de rascunho, não um otimizador. O requisito é faixa calórica +
  categorias obrigatórias, e isso está testado.
- **Janela de repetição cresce com o histórico**: consulta indexada por
  `(UnidadeId, Fim)`; sem índice, degrada.
