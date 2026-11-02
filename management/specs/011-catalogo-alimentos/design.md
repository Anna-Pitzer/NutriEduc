---
name: catalogo-alimentos
status: created
references: V6, ADR-004, L2, L9
---

# Design — Catálogo de alimentos

## Estrutura

```text
backend/Domain/
  FiltroAlimento.cs          (porta sem palavra de autorização, como FiltroAluno)
backend/Domain/Entities/
  CategoriaAlimento.cs
  ValoresNutricionais.cs     (objeto de valor, record)
  Alimento.cs
  AlimentoRestricao.cs       (vínculo sem motivo: o par basta)
backend/Domain/Interface/
  IAlimentoRepository.cs
backend/Application/
  AlimentoDTOs.cs
  Paginacao.cs               (dono único da regra de página/tamanho)
backend/API/Services/
  AlimentoService.cs
backend/API/Controllers/
  AlimentoController.cs
  CategoriaAlimentoController.cs
```

## Domínio

```csharp
public sealed class ValoresNutricionais
{
    public decimal CaloriasKcal { get; private set; }
    // ... as seis medidas com setter privado

    public static ValoresNutricionais Criar(
        decimal calorias, decimal proteinas, decimal carboidratos,
        decimal gorduras, decimal fibras, decimal sodio)
    {
        return new(
            ValidacaoDeCampo.Medida(calorias,    "Calorias (kcal)"),
            ValidacaoDeCampo.Medida(proteinas,   "Proteínas (g)"),
            ValidacaoDeCampo.Medida(carboidratos,"Carboidratos (g)"),
            ValidacaoDeCampo.Medida(gorduras,    "Gorduras (g)"),
            ValidacaoDeCampo.Medida(fibras,      "Fibras (g)"),
            ValidacaoDeCampo.Medida(sodio,       "Sódio (mg)"));
    }
}

public class AlimentoRestricao
{
    public int RestricaoId { get; private set; }

    internal AlimentoRestricao(int restricaoId)
    {
        if (restricaoId <= 0)
            throw new ArgumentException("Restrição inválida.");

        RestricaoId = restricaoId;
    }
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
                    IEnumerable<int>? restricaoIds)
    {
        var dados = ValidarDados(nome, categoriaId, valores);
        var vinculos = NormalizarVinculos(restricaoIds);   // distinto por RestricaoId

        Nome = dados.Nome;
        NomeNormalizado = dados.NomeNormalizado;
        CategoriaId = categoriaId;
        Valores = valores;
        Ativo = true;
        foreach (var v in vinculos) _vinculos.Add(v);
    }

    public void AtualizarDados(string nome, int categoriaId, ValoresNutricionais valores,
                               IEnumerable<int>? restricaoIds)
    {
        // valida tudo num buffer antes de atribuir (ADR-004); nada é parcial
    }

    public void Desativar() => Ativo = false;
    public void Reativar() => Ativo = true;
}
```

Decisões de modelagem:

- **Sem `MotivoVinculo`.** A severidade de um vínculo é a `Categoria` de
  `RestricaoAlimentar` (Spec 010). Guardar motivo no vínculo seria uma segunda fonte
  para o mesmo fato: reclassificar a restrição passaria a exigir reescrever vínculos.
  Consequência aceita: não existe "este alimento é apenas alerta" para uma restrição do
  tipo `Alergia`; se um dia for necessário, entra como conceito novo (traços), não como
  coluna duplicada.
- `ValoresNutricionais` é objeto de valor **classe** com propriedades de setter privado
  e construtor sem parâmetros para o EF, no mesmo formato de `Turma` e `Responsavel`
  (Spec 004): tipo complexo precisa de construtor que o materializador consiga usar, e
  um `record` posicional obrigaria o ORM a ligar parâmetros por nome. Ele é mapeado como
  tipo complexo (L10): não é entidade possuída e não tem tabela filha.
- `NormalizarNome` reutiliza `RestricaoAlimentar.Normalizar` (Spec 010): uma única
  função de normalização no projeto (L9).
- Regra numérica em um só lugar: `ValidacaoDeCampo.Medida` é adicionada ao helper
  existente (`ValidacaoDeCampo.cs`), que hoje tem apenas `Texto` e `Telefone`. Seis
  `if` repetidos seriam a mesma regra escrita seis vezes.
- `ValidacaoDeCampo` é `internal`: continua detalhe do domínio, sem virar contrato.

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/categorias-alimento` | `SessaoValida` |
| `POST` | `/api/categorias-alimento` | `Nutricionista` |
| `PUT` | `/api/categorias-alimento/{id}` | `Nutricionista` |
| `POST` | `/api/categorias-alimento/{id}/desativar` | `Nutricionista` |
| `POST` | `/api/categorias-alimento/{id}/reativar` | `Nutricionista` |
| `GET` | `/api/alimentos?busca=&categoriaId=&pagina=&tamanho=&incluirInativos=` | `SessaoValida` |
| `GET` | `/api/alimentos/{id}` | `SessaoValida` |
| `POST` | `/api/alimentos` | `Nutricionista` |
| `PUT` | `/api/alimentos/{id}` | `Nutricionista` |
| `POST` | `/api/alimentos/{id}/desativar` | `Nutricionista` |
| `POST` | `/api/alimentos/{id}/reativar` | `Nutricionista` |

Leitura usa `SessaoValida` porque a política por papel é decidida no serviço, com
`EscopoService`, exatamente como `RestricaoService.Listar` faz na Spec 010 — e é lá que
`incluirInativos=true` vira 403 para quem não é `Nutricionista`.

Resposta de listagem:

```json
{ "pagina": 1, "tamanho": 20, "total": 1,
  "itens": [ { "id": 7, "nome": "Iogurte natural",
               "categoria": { "id": 2, "nome": "Laticínios" },
               "ativo": true,
               "valores": { "caloriasKcal": 61.0, "proteinasG": 3.5,
                            "carboidratosG": 4.7, "gordurasG": 3.3,
                            "fibrasG": 0.0, "sodioMg": 46.0 },
               "vinculos": [ { "restricaoId": 3, "restricao": "Lactose",
                               "categoria": "Restricao" } ] } ] }
```

`vinculos[].categoria` é a categoria da restrição — é o campo que a Spec 014 consome para
decidir entre bloquear e alertar. Não existe `motivo`.

## Busca e paginação

A busca roda em duas etapas, dentro do repositório, porque é política de consulta:

1. `LIKE 'busca%'` sobre `NomeNormalizado` — usa o índice único.
2. Se `total == 0` **e** `busca` não for vazia, repete com `LIKE '%busca%'`.

O fallback é decidido pelo `total` da consulta inteira, não pelo tamanho da página: com
página 2 e prefixo sem resultado, a segunda etapa ainda acontece. `busca` vazia ou nula
não dispara a etapa 2.

Paginação: `PaginaDTO<T>` já existe em `Application/RestricaoDTOs.cs` e a validação de
faixa está privada em `HistoricoAlunoService.ValidarPaginacao`. A 011 não copia essa
regra: ela passa a viver em `Application/Paginacao.cs` e os dois serviços a reutilizam.
Página mínima 1, tamanho 1–100 com padrão 20; fora da faixa devolve 400.

## Persistência

- `CategoriasAlimento`: único `NomeNormalizado`.
- `Alimentos`: único `NomeNormalizado` **global** (não por categoria); FK `CategoriaId`
  com `OnDelete(Restrict)`; `ComplexProperty(Valores)` com seis colunas `decimal(10,2)`.
- `AlimentosRestricoes`: chave composta `(AlimentoId, RestricaoId)`; FK para
  `RestricoesAlimentares` com `OnDelete(Restrict)`; nenhuma coluna `Motivo`.
- Seed de categorias na mesma migration, idempotente: insere apenas o
  `NomeNormalizado` que ainda não existe (mesmo padrão de `SeedCatalogoRestricoes`).
  Seis categorias: Cereais, Leguminosas, Hortaliças, Frutas, Carnes e ovos, Laticínios.
  Nenhum alimento é semeado.
- Nenhum `bool Permitido` é persistido: "não permitido" é derivado dos vínculos.

## Aplicação

`AlimentoService` recebe `IAlimentoRepository`, `IUnidadeDeTrabalho` e
`ICategoriaAlimentoRepository`. Responsabilidades:

- `Listar(...)`: recusa `incluirInativos` sem `Nutricionista` (403), valida a paginação
  pela regra compartilhada e monta `PaginaDTO<AlimentoDTO>`.
- `Criar`/`Atualizar`: exigem categoria existente e ativa, restrições existentes e
  ativas e `NomeNormalizado` livre (409 antes de gravar); gravam alimento e vínculos na
  mesma transação via `IUnidadeDeTrabalho` — sem vínculo órfão se a gravação falhar.
- `Desativar`/`Reativar`: transição de estado, 404 quando o id não existe.
- DTOs em `Application/AlimentoDTOs.cs`, no padrão da Spec 010/024.

Regra de acesso: `FiltrosDeConsulta` não é necessário aqui, porque alimento não tem
recorte por escola/unidade — o único recorte é papel (`incluirInativos`).

## Frontend

- `frontend/src/services/AlimentoApi.ts` passa a ser o dono das rotas de alimento:
  `listarAlimentos(filtros)` devolve `PaginaDTO<Alimento>`, `obterAlimento(id)`,
  `salvarAlimento`, `desativarAlimento`, `reativarAlimento` e as rotas de categoria.
- Tipo `Alimento` muda para o contrato aninhado (`valores`, `categoria: {id,nome}`).
- `CardapioApi.ts` perde `buscarAlimentos` e a chamada órfã a `/api/alimentos`.
- `MontarCardapio.tsx`: remove `alimentosFake` e passa a consumir `listarAlimentos`.
- `Cardapio.tsx`: o cálculo de composição nutricional hoje roda sobre campos planos;
  passa a ler `alimento.valores.*`.
- `pages/nutricao/alimentos/ListarAlimentos.tsx` e `FormularioAlimento.tsx`.
- Seleção de restrições carrega `/api/restricoes` (Spec 010) e envia apenas
  `restricaoId`. O cliente dessa rota é `services/RestricaoApi.ts` — dono único do
  catálogo de restrições no frontend, para a tela de alimento não virar mais um ponto
  que fala direto com a API.
- Rotas: `/alimentos`, `/alimentos/novo` e `/alimentos/editar/:id`, sob
  `ProtegidaPorPapel` de `Nutricionista` e `Merendeira` — a mesma matriz do backend, de
  modo que a secretaria não chega a abrir uma tela que responderia 403.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | valor negativo e três casas decimais lançam `ArgumentException` |
| Domínio | vínculo repetido (mesmo `RestricaoId`) lança `ArgumentException` |
| Domínio | `AtualizarDados` inválido não altera nenhum campo |
| Serviço | categoria inativa → 400 sem persistir |
| Serviço | restrição inexistente ou inativa → 400 |
| Serviço | `incluirInativos` por `Merendeira` → 403 |
| Serviço | paginação fora da faixa → 400 |
| Repositório | busca "arr" encontra "Arroz branco"; "leite" encontra "Leite de coco" |
| Repositório | alimento de categoria desativada aparece na listagem |
| HTTP | nome duplicado → 409; papel não autorizado → 403; id inexistente → 404 |
| HTTP | listagem devolve `PaginaDTO` com `pagina`, `tamanho`, `total`, `itens` |
| Modelo EF | `Valores` mapeado como tipo complexo, sem tabela filha |
| Migration | aplicar duas vezes não duplica as seis categorias |

## Riscos

- **`decimal` em SQLite**: o provider não aplica precisão e não ordena numericamente.
  Nenhuma soma ou comparação de medida em SQL; a Spec 012 soma em C#. A precisão de duas
  casas é garantida pela validação, não pelo banco.
- **Contrato quebrado em duas telas**: `MontarCardapio` e `Cardapio` deixam de compilar
  com o tipo antigo. É a tarefa mais arriscada e precisa de `npm run build` no mesmo
  passo, não no fim.
- **Fallback de busca**: duas consultas quando o prefixo falha. Com catálogo de centenas
  de itens é irrelevante; acima de alguns milhares exige índice de texto completo, que
  fica fora desta spec.
- **Restrições fixas (ADR-015)**: se a semente não tiver a restrição desejada, o vínculo
  não pode ser criado pela interface. Isso é pendência de dados da Spec 010, não regra
  desta spec — e não autoriza criar restrição por fora.
- **Unicidade global do nome**: renomear para um nome já usado devolve 409, inclusive se
  estiver em outra categoria. É o comportamento desejado, e o teste fixa isso.
