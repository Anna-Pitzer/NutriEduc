---
name: restricoes-e-alergias
status: created
references: V1, V6, ADR-004, L2
---

# Design — Restrições e alergias

## Estrutura

```text
backend/Domain/Entities/
  RestricaoAlimentar.cs        (catálogo)
  HistoricoRestricaoAluno.cs   (evento append-only)
backend/Domain/Interface/
  IRestricaoRepository.cs
backend/API/Services/
  RestricaoService.cs          (catálogo + histórico)
  AlunoService.cs              (passa a validar o catálogo antes de gravar)
backend/API/Controllers/
  RestricaoController.cs
  AlunoController.cs           (+ historico)
```

## Domínio — catálogo

```csharp
public enum CategoriaRestricao { Alergia, Restricao }

public enum AcaoRestricao { Adicionada, Removida }

public class RestricaoAlimentar
{
    private RestricaoAlimentar() { }          // EF Core

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string NomeNormalizado { get; private set; } = string.Empty;
    public CategoriaRestricao Categoria { get; private set; }
    public bool Ativa { get; private set; } = true;

    public RestricaoAlimentar(string nome, CategoriaRestricao categoria)
    {
        DefinirNome(nome);
        Categoria = categoria;
        Ativa = true;
    }

    public void DefinirNome(string nome)
    {
        nome = (nome ?? string.Empty).Trim();
        if (nome.Length < 2 || nome.Length > 60)
            throw new ArgumentException("O nome da restrição deve ter entre 2 e 60 caracteres.");
        Nome = nome;
        NomeNormalizado = Normalizar(nome);
    }

    public static string Normalizar(string valor) =>
        valor.Trim().ToLowerInvariant()
             .Normalize(System.Text.NormalizationForm.FormD)
             .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                         != System.Globalization.UnicodeCategory.NonSpacingMark)
             .Aggregate(new System.Text.StringBuilder(),
                        (sb, c) => sb.Append(c), sb => sb.ToString());

    public void Desativar() => Ativa = false;
    public void Reativar() => Ativa = true;
}
```

`NomeNormalizado` é coluna própria (persistida) porque SQLite não tem collation
acento-insensível nativa: a unicidade é garantida por índice em
`(NomeNormalizado, Categoria)`.

## Domínio — histórico

`HistoricoRestricaoAluno` é entidade filha de `Aluno`, não objeto de valor: precisa
de identidade própria, consulta paginada e nunca é atualizada.

```csharp
public class HistoricoRestricaoAluno
{
    private HistoricoRestricaoAluno() { }     // EF Core

    public int Id { get; private set; }
    public int AlunoId { get; private set; }
    public string RestricaoNome { get; private set; } = string.Empty;
    public AcaoRestricao Acao { get; private set; }
    public string AutorNome { get; private set; } = string.Empty;
    public DateTime CriadoEmUtc { get; private set; }

    internal HistoricoRestricaoAluno(int alunoId, string restricaoNome,
                                     AcaoRestricao acao, string autorNome, DateTime utc)
    {
        AlunoId = alunoId;
        RestricaoNome = restricaoNome;
        Acao = acao;
        AutorNome = string.IsNullOrWhiteSpace(autorNome) ? "sistema" : autorNome.Trim();
        CriadoEmUtc = utc;
    }
}
```

`Aluno` ganha uma coleção privada com o padrão da Spec 003 (campo de apoio +
`IReadOnlyList` público) e um método interno:

```csharp
private readonly List<HistoricoRestricaoAluno> _historico = new();
public IReadOnlyList<HistoricoRestricaoAluno> Historico => _historico.AsReadOnly();

internal void RegistrarHistorico(IEnumerable<string> adicionadas,
                                 IEnumerable<string> removidas, string autorNome)
{
    var agora = DateTime.UtcNow;
    foreach (var nome in adicionadas)
        _historico.Add(new HistoricoRestricaoAluno(Id, nome, AcaoRestricao.Adicionada, autorNome, agora));
    foreach (var nome in removidas)
        _historico.Add(new HistoricoRestricaoAluno(Id, nome, AcaoRestricao.Removida, autorNome, agora));
}
```

O construtor de `Aluno` e `AtualizarDados` calculam o diff **antes** de aplicar e só
registram quando o conjunto muda. `Id` é 0 no construtor; a FK é corrigida pelo EF
na mesma transação porque a navegação é a coleção do aggregate. Para o construtor,
o evento é criado depois de `SaveChanges`, no serviço, com o `Id` já atribuído — não
há como gravar `AlunoId = 0`.

Para evitar dois caminhos de gravação, a decisão é: **o serviço chama
`RegistrarHistorico` após a persistência do aluno**, passando os nomes adicionados e
removidos devolvidos pelo domínio (`Aluno.AtualizarDados` retorna `(adicionadas,
removidas)`), e persiste com `IRestricaoRepository.SalvarHistorico`. As duas
gravações compartilham o `ConnectionContext` Scoped e rodam dentro de
`Database.BeginTransaction()`, garantindo atomicidade sem introduzir `IUnitOfWork`.

## Validação de catálogo no caso de uso

`AlunoService` recebe `IRestricaoRepository` e executa, antes de construir/atualizar:

```csharp
var nomes = (dto.Restricoes ?? Array.Empty<string>())
    .Select(RestricaoAlimentar.Normalizar).Distinct().ToList();
var ativas = _restricoes.ObterAtivas(nomes);          // uma consulta
var desconhecidas = nomes.Except(ativas).ToList();
if (desconhecidas.Count > 0)
    throw new ArgumentException($"Restrição desconhecida: {desconhecidas[0]}.");
```

O domínio continua validando formato, duplicidade e limite de 20 (ADR-004); o
catálogo é uma regra de existência e pertence ao caso de uso.

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/restricoes?categoria=&incluirInativas=` | `SessaoValida` (`incluirInativas` → `Nutricionista`) |
| `POST` | `/api/restricoes` | `Nutricionista` |
| `PUT` | `/api/restricoes/{id}` | `Nutricionista` |
| `POST` | `/api/restricoes/{id}/desativar` | `Nutricionista` |
| `POST` | `/api/restricoes/{id}/reativar` | `Nutricionista` |
| `GET` | `/api/alunos/{id}/restricoes/historico?pagina=&tamanho=` | `EquipeEscolar` + responsável vinculado |

Resposta do histórico:

```json
{ "pagina": 1, "tamanho": 20, "total": 3,
  "itens": [ { "restricao": "Lactose", "acao": "Removida",
               "autor": "Maria", "criadoEm": "2026-10-30T13:00:00Z" } ] }
```

## Persistência

- `RestricoesAlimentares`: índice único `(NomeNormalizado, Categoria)`.
- `HistoricoRestricoesAlunos`: FK `AlunoId` com `OnDelete(Cascade)`; índice
  `(AlunoId, CriadoEmUtc DESC)`; `RestricaoNome` e `AutorNome` são snapshots —
  nenhuma FK para catálogo ou usuário, para o histórico não mudar quando o catálogo
  ou o autor mudarem.
- Migration `RestricoesEAlergias`.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | nome com 1 ou 61 caracteres lança `ArgumentException` |
| Domínio | `Normalizar` remove acento e caixa |
| Domínio | diff de restrições gera exatamente os eventos esperados |
| Serviço | restrição inativa → 400 sem persistir |
| Serviço | nenhuma mudança no conjunto → zero eventos |
| HTTP | catálogo duplicado → 400 `{ erro }` |
| HTTP | histórico por papel: 200/403/404 |

## Riscos

- **Normalização divergente** entre catálogo e aluno: os dois usam
  `RestricaoAlimentar.Normalizar`; qualquer outra normalização é proibida no serviço.
- **Crescimento do histórico**: índice por aluno e paginação; retenção não é
  requisito desta spec.
- **Aluno legado com restrição fora do catálogo**: leitura preservada; a próxima
  gravação exige catálogo, o que pode surpreender. O erro nomeia a restrição para
  permitir correção no catálogo.
