---
name: compatibilidade-cardapio-restricoes
status: created
references: V6, ADR-004, L5
---

# Design — Compatibilidade entre cardápio e restrições

## Estrutura

```text
backend/Domain/Services/
  AnaliseCompatibilidade.cs
  ResultadoCompatibilidade.cs
backend/API/Services/
  CompatibilidadeService.cs      (carrega dados e chama o domínio)
backend/API/Controllers/
  CardapioController.cs          (+ rota de compatibilidade)
```

## Domínio

Entradas explícitas — o domínio não conhece repositório:

```csharp
public sealed record ItemAnalisado(
    int RefeicaoOrdem, string RefeicaoNome, int AlimentoId, string AlimentoNome);

public sealed record VinculoAnalisado(int RestricaoId, string RestricaoNome, MotivoVinculo Motivo);

public sealed record AlunoAnalisado(int Id, string Nome, IReadOnlyList<string> Restricoes);

public enum SeveridadeCompatibilidade { Alerta, Bloqueante }

public sealed record AchadoCompatibilidade(
    int RefeicaoOrdem, string RefeicaoNome, int AlimentoId, string AlimentoNome,
    string RestricaoNome, SeveridadeCompatibilidade Severidade,
    IReadOnlyList<(int Id, string Nome)> AlunosAfetados);

public sealed record ResultadoCompatibilidade(
    bool Compativel,
    IReadOnlyList<AchadoCompatibilidade> Bloqueantes,
    IReadOnlyList<AchadoCompatibilidade> Alertas,
    IReadOnlyList<string> AlimentosPermitidos,
    IReadOnlyList<string> AlimentosNaoPermitidos,
    int TotalAlunosAnalisados,
    IReadOnlyList<string> Avisos);
```

```csharp
public static class AnaliseCompatibilidade
{
    public static ResultadoCompatibilidade Analisar(
        IReadOnlyList<ItemAnalisado> itens,
        IReadOnlyDictionary<int, IReadOnlyList<VinculoAnalisado>> vinculosPorAlimento,
        IReadOnlyList<AlunoAnalisado> alunos)
    {
        var porRestricao = alunos
            .SelectMany(a => a.Restricoes.Select(r => (Restricao: RestricaoAlimentar.Normalizar(r), Aluno: a)))
            .GroupBy(x => x.Restricao)
            .ToDictionary(g => g.Key, g => g.Select(x => (x.Aluno.Id, x.Aluno.Nome)).ToList());

        var achados = new List<AchadoCompatibilidade>();
        foreach (var item in itens.OrderBy(i => i.RefeicaoOrdem).ThenBy(i => i.AlimentoNome))
        {
            if (!vinculosPorAlimento.TryGetValue(item.AlimentoId, out var vinculos)) continue;
            foreach (var vinculo in vinculos.OrderBy(v => v.RestricaoNome))
            {
                if (!porRestricao.TryGetValue(RestricaoAlimentar.Normalizar(vinculo.RestricaoNome), out var afetados))
                    continue;
                achados.Add(new AchadoCompatibilidade(
                    item.RefeicaoOrdem, item.RefeicaoNome, item.AlimentoId, item.AlimentoNome,
                    vinculo.RestricaoNome,
                    vinculo.Motivo == MotivoVinculo.Alergenico
                        ? SeveridadeCompatibilidade.Bloqueante
                        : SeveridadeCompatibilidade.Alerta,
                    afetados.OrderBy(a => a.Nome).ToList()));
            }
        }

        var naoPermitidos = achados.Select(a => a.AlimentoNome).Distinct().OrderBy(n => n).ToList();
        var permitidos = itens.Select(i => i.AlimentoNome).Distinct()
                              .Except(naoPermitidos).OrderBy(n => n).ToList();

        return new ResultadoCompatibilidade(
            Compativel: achados.All(a => a.Severidade != SeveridadeCompatibilidade.Bloqueante),
            Bloqueantes: achados.Where(a => a.Severidade == SeveridadeCompatibilidade.Bloqueante).ToList(),
            Alertas: achados.Where(a => a.Severidade == SeveridadeCompatibilidade.Alerta).ToList(),
            AlimentosPermitidos: permitidos,
            AlimentosNaoPermitidos: naoPermitidos,
            TotalAlunosAnalisados: alunos.Count,
            Avisos: alunos.Count == 0
                ? new[] { "Nenhum aluno na unidade; a compatibilidade não foi analisada." }
                : Array.Empty<string>());
    }
}
```

A regra de severidade está em **um único lugar** (aqui). Spec 013 usa
`AlimentosNaoPermitidos` para excluir candidatos; Spec 015 usa `Compativel` para
bloquear a publicação. Nenhuma das duas reimplementa o cruzamento (L5).

## Aplicação

`CompatibilidadeService.Analisar(cardapioId, escopo)`:

1. Carrega o cardápio com refeições e itens (uma consulta).
2. Carrega os alimentos dos itens com seus vínculos (uma consulta, `Include`).
3. Carrega os alunos da unidade com `Restricoes` (uma consulta).
4. Monta os records e chama `AnaliseCompatibilidade.Analisar`.
5. Aplica o escopo: `Merendeira` e `AdminEscola` só acessam cardápio da própria
   unidade; fora → 403 (Spec 009).

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/cardapios/{id}/compatibilidade` | `Nutricionista`, `Merendeira`, `AdminEscola` (escopo da unidade) |

```json
{ "compativel": false, "totalAlunosAnalisados": 42,
  "bloqueantes": [
    { "refeicao": "Almoço", "alimentoId": 7, "alimento": "Iogurte",
      "restricao": "Lactose", "severidade": "Bloqueante",
      "alunos": [ { "id": 3, "nome": "Ana" }, { "id": 9, "nome": "Beto" } ] } ],
  "alertas": [],
  "alimentosPermitidos": ["Arroz", "Feijão"],
  "alimentosNaoPermitidos": ["Iogurte"],
  "avisos": [] }
```

## Frontend

- `pages/nutricao/cardapios/DetalheCardapio.tsx`: painel com contagem, listas de
  permitidos/não permitidos e alunos afetados por restrição.
- Listagem mostra selo "incompatível" quando `bloqueantes > 0`.
- Nenhum cálculo local: o frontend só renderiza a resposta.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | alergênico → bloqueante; restrição → alerta |
| Domínio | alimento sem vínculo → permitido |
| Domínio | mesmo alimento em duas refeições → dois achados |
| Domínio | dois alunos com a mesma restrição → um achado com dois alunos |
| Domínio | unidade sem alunos → compativel, aviso |
| Domínio | ordenação estável em duas execuções |
| Serviço | 403 para merendeira de outra unidade e para responsável |
| HTTP | contrato da resposta e 404 de cardápio inexistente |

## Riscos

- **Falso negativo por normalização divergente**: proibido normalizar fora de
  `RestricaoAlimentar.Normalizar`; teste cobre acento e caixa.
- **Custo com muitos alunos**: a análise é linear e roda sob demanda (não a cada
  listagem); a listagem usa apenas contagem quando necessário.
- **Privacidade**: a resposta expõe nomes de alunos a papéis que já os veem pela
  Spec 009. `Responsavel` é excluído da visão agregada.
