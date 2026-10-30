---
name: restricoes-aluno-protegidas
status: created
references: V1, ADR-006, L2
---

# Requirements — Restrições alimentares protegidas

## Objetivo

Transformar `List<string> Restricoes { get; private set; }` em coleção realmente
protegida. Hoje `aluno.Restricoes.Add(...)` funciona de fora e quebra qualquer
invariante sem passar por método da entidade.

## Escopo

- Domínio: `Aluno.Restricoes` como `IReadOnlyList<string>` sobre campo privado, com
  `AdicionarRestricao` e `RemoverRestricao`.
- Persistência: conversor para a coluna `Restricoes` (`TEXT`, JSON) já existente.
- Fora de escopo: entidade `Restricao`, normalização de tabela e consultas por
  restrição (Spec 010).

## Requisitos funcionais

- RF1: `Aluno.Restricoes` é exposto como `IReadOnlyList<string>` sobre
  `_restricoes.AsReadOnly()`, sem setter público e sem downcast viável para `List<string>`.
- RF2: `AdicionarRestricao(string)` aplica trim, rejeita vazio e rejeita duplicata
  case-insensitive com `ArgumentException`.
- RF3: `AdicionarRestricao` limita a coleção a 20 itens, lançando `ArgumentException`
  ao exceder.
- RF4: `RemoverRestricao(string)` retorna `bool` e é case-insensitive.
- RF5: o construtor aceita `IEnumerable<string>?` e aplica as mesmas invariantes,
  ignorando itens inválidos em vez de propagar exceção de inicialização.
- RF6: `GET /api/aluno` e `GET /api/aluno/perfil/{id}` continuam devolvendo
  `restricoes` como array JSON.

## Requisitos não funcionais

- RNF1: nenhuma alteração de schema; a coluna `Restricoes TEXT` é preservada.
- RNF2: o mapeamento EF Core não expõe o campo privado publicamente.

## Restrições

- Não alterar o formato JSON consumido pelo frontend.
- Não introduzir coleção paralela (`IEnumerable` convertido em getter, por exemplo).

## Dependências

- Coerente com Spec 004 (invariantes do aluno). Executar 004 primeiro ou no mesmo
  ciclo, pois ambas alteram a mesma entidade.

## Casos de borda

- Duplicata `"lactose"` / `"Lactose"` → rejeitada.
- Item `"  "` → rejeitado.
- Coleção cheia (20) → nova inclusão lança.
- Remoção de item inexistente → `false`, sem exceção.
- Aluno lido do banco com JSON vazio → lista vazia válida.

## Critérios de aceitação

- [x] `aluno.Restricoes.Add("x")` não compila a partir de outro arquivo.
- [x] `((List<string>)aluno.Restricoes)` lança `InvalidCastException` — a coleção real
  não é exposta, apenas o wrapper readonly.
- [x] Duplicata case-insensitive lança `ArgumentException`.
- [x] Item vazio ou composto apenas de espaços lança `ArgumentException`.
- [x] 21ª restrição lança `ArgumentException`.
- [x] `RemoverRestricao` devolve `false` para item ausente.
- [x] `GET /api/aluno` devolve `restricoes` como array com os valores gravados.
- [x] Testes unitários cobrem todos os casos desta seção.

## Validação

Testes de domínio (`dotnet test`) e leitura da listagem via API após cadastro com
três restrições.
