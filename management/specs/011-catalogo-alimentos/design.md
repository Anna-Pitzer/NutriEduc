---
name: catalogo-alimentos
status: created
references: V6, ADR-004, L2, L9
---

# Design — Catálogo de alimentos

## Estrutura

```text
backend/Domain/Entities/
  CategoriaAlimento.cs
  ValoresNutricionais.cs     (objeto de valor, record)
  Alimento.cs
  AlimentoRestricao.cs       (vínculo com motivo)
backend/Domain/Interface/
  IAlimentoRepository.cs
backend/API/Services/
  AlimentoService.cs
backend/API/Controllers/
  AlimentoController.cs
  CategoriaAlimentoController.cs
```

## Domínio

```csharp
public sealed record ValoresNutricionais(
    decimal CaloriasKcal, decimal ProteinasG, decimal CarboidratosG,
    decimal GordurasG, decimal FibrasG, decimal SodioMg)
{
    public static ValoresNutricionais Criar(
        decimal calorias, decimal proteinas, decimal carboidratos,
        decimal gorduras, decimal fibras, decimal sodio)
    {
        var valores = new[] { calorias, proteinas, carboidratos, gorduras, fibras, sodio };
        if (valores.Any(v => v < 0))
            throw new ArgumentException("Valores nutricionais não podem ser negativos.");
        if (valores.Any(v => decimal.Round(v, 2) != v))
            throw new ArgumentException("Valores nutricionais aceitam no máximo duas casas decimais.");
        return new(calorias, proteinas, carboidratos, gorduras, fibras, sodio);
    }
}

public enum MotivoVinculo { Alergenico, Restricao }

public class AlimentoRestricao
{
    public int RestricaoId { get; private set; }
    public MotivoVinculo Motivo { get; private set; }

    internal AlimentoRestricao(int restricaoId, MotivoVinculo motivo) { /* valida id > 0 */ }
}

public class Alimento
{
    private readonly List<AlimentoRestricao> _vinculos = new();

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string NomeNormalizado { get; private set; } = string.Empty;
    public int CategoriaId { get; private set; }
    public bool Ativo { get; private set; }
    public ValoresNutricionais Valores { get; private set; } = null!;
    public IReadOnlyList<AlimentoRestricao> Vinculos => _vinculos.AsReadOnly();

    public Alimento(string nome, int categoriaId, ValoresNutricionais valores,
                    IEnumerable<AlimentoRestricao>? vinculos)
    {
        var dados = ValidarDados(nome, categoriaId, valores);
        var vinculosValidos = NormalizarVinculos(vinculos);   // detecta duplicata

        Nome = dados.Nome;
        NomeNormalizado = NormalizarNome(dados.Nome);
        CategoriaId = categoriaId;
        Valores = valores;
        Ativo = true;
        foreach (var v in vinculosValidos) _vinculos.Add(v);
    }

    public void AtualizarDados(string nome, int categoriaId, ValoresNutricionais valores,
                               IEnumerable<AlimentoRestricao>? vinculos)
    {
        // mesma validação do construtor; só atribui após validar tudo
    }

    public void Desativar() => Ativo = false;
    public void Reativar() => Ativo = true;
}
```

`ValoresNutricionais` é `record` (valor, sem identidade) e é mapeado como tipo
complexo no EF Core, seguindo a lição L10: não é entidade possuída.

`NormalizarNome` reutiliza `RestricaoAlimentar.Normalizar` (Spec 010) — uma única
função de normalização no projeto (L9).

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/categorias-alimento` | `Nutricionista`, `Merendeira` |
| `POST` | `/api/categorias-alimento` | `Nutricionista` |
| `PUT` | `/api/categorias-alimento/{id}` | `Nutricionista` |
| `POST` | `/api/categorias-alimento/{id}/desativar` | `Nutricionista` |
| `GET` | `/api/alimentos?busca=&categoriaId=&pagina=&tamanho=&incluirInativos=` | `Nutricionista`, `Merendeira` |
| `GET` | `/api/alimentos/{id}` | `Nutricionista`, `Merendeira` |
| `POST` | `/api/alimentos` | `Nutricionista` |
| `PUT` | `/api/alimentos/{id}` | `Nutricionista` |
| `POST` | `/api/alimentos/{id}/desativar` | `Nutricionista` |
| `POST` | `/api/alimentos/{id}/reativar` | `Nutricionista` |

Resposta de alimento:

```json
{ "id": 7, "nome": "Iogurte natural", "categoria": { "id": 2, "nome": "Laticínios" },
  "ativo": true,
  "valores": { "caloriasKcal": 61.0, "proteinasG": 3.5, "carboidratosG": 4.7,
               "gordurasG": 3.3, "fibrasG": 0.0, "sodioMg": 46.0 },
  "vinculos": [ { "restricaoId": 3, "restricao": "Lactose", "motivo": "Restricao" } ] }
```

`incluirInativos=true` por quem não é `Nutricionista` devolve 403 (RF7).

## Persistência

- `CategoriasAlimento`: único `NomeNormalizado`.
- `Alimentos`: único `NomeNormalizado`; FK `CategoriaId` com `OnDelete(Restrict)`;
  `ComplexProperty(Valores)` com seis colunas `decimal(10,2)`.
- `AlimentosRestricoes`: chave composta `(AlimentoId, RestricaoId)`; FK para
  `RestricoesAlimentares` com `OnDelete(Restrict)`.
- Migration `CatalogoAlimentos`.
- Nenhum `bool Permitido` é persistido: o "não permitido" é derivado dos vínculos.

## Aplicação

`AlimentoService` valida existência e estado da categoria e das restrições, monta os
objetos de vínculo e chama o domínio. O repository expõe `ExisteNomeNormalizado`,
`ObterComVinculos`, `Listar(filtro)` e `Salvar`.

## Frontend

- `frontend/src/services/AlimentoApi.ts` substitui as chamadas órfãs de
  `CardapioApi.ts` para `/api/alimentos`.
- `pages/nutricao/alimentos/ListarAlimentos.tsx`, `FormularioAlimento.tsx`.
- Seleção de restrições carrega `/api/restricoes` (Spec 010) e envia
  `{ restricaoId, motivo }`.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | valor negativo e três casas decimais lançam `ArgumentException` |
| Domínio | vínculo duplicado (mesmo par) lança `ArgumentException` |
| Domínio | `AtualizarDados` inválido não altera nenhum campo |
| Serviço | categoria inativa → 400 sem persistir |
| Serviço | restrição inexistente → 400 |
| HTTP | nome duplicado → 409; papel não autorizado → 403 |
| HTTP | paginação e busca sem diferenciar caixa |
| Modelo EF | `Valores` mapeado como tipo complexo, sem tabela filha |

## Riscos

- **Catálogo grande**: sem índice em `NomeNormalizado` a busca degrada; o índice
  único já serve à igualdade, e a busca por prefixo usa `LIKE 'x%'` para aproveitá-lo.
- **Alimento desativado referenciado por cardápio**: a regra de bloqueio vive na
  Spec 012 (`AlimentoService` não conhece cardápio). Aqui só se garante que o
  registro continua legível.
- **Duplicidade de normalização**: proibido criar segunda função; L9 registra o
  custo de reimplementar regra ao lado do utilitário.
