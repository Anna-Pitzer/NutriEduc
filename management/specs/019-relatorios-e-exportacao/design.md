---
name: relatorios-e-exportacao
status: created
references: V6, ADR-004, L6
---

# Design — Relatórios e exportação

## Estrutura

```text
backend/Application/Relatorios/
  TipoRelatorio.cs              (enum)
  ColunaRelatorio.cs
  RelatorioGerado.cs
  IRelatorio.cs                 (contrato de cada relatório)
  CardapiosRelatorio.cs
  AlunosRestricoesRelatorio.cs
  OcorrenciasRelatorio.cs
  SolicitacoesFeedbacksRelatorio.cs
  IndicadoresAdministrativosRelatorio.cs
  RelatorioService.cs           (catálogo + escopo + orquestração)
backend/Application/Relatorios/Exportacao/
  IRelatorioExportador.cs
  ExportadorPdf.cs
  ExportadorXlsx.cs
  ExportadorDocx.cs
backend/API/Controllers/
  RelatorioController.cs
```

## Modelo comum

```csharp
public enum TipoRelatorio
{
    Cardapios, AlunosRestricoes, Ocorrencias,
    SolicitacoesFeedbacks, IndicadoresAdministrativos
}

public enum TipoColuna { Texto, Inteiro, Decimal, Data }

public sealed record ColunaRelatorio(
    string Chave, string Titulo, TipoColuna Tipo, string? Formato = null);

public sealed record RelatorioGerado(
    TipoRelatorio Tipo,
    string Titulo,
    IReadOnlyList<ColunaRelatorio> Colunas,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Linhas,
    IReadOnlyDictionary<string, object?> Totais,
    string Ordenacao,
    DateTime GeradoEmUtc,
    IReadOnlyDictionary<string, object?> FiltrosAplicados);

public interface IRelatorio
{
    TipoRelatorio Tipo { get; }
    Task<RelatorioGerado> GerarAsync(FiltroRelatorio filtro, EscopoUsuario escopo);
}
```

`FiltroRelatorio` é validado no serviço:
`de <= ate`, intervalo ≤ 366 dias, `escolaId`/`unidadeId` coerentes com o escopo.
Datas são `DateOnly` na entrada e convertidas para UTC apenas na serialização.

## Catálogo

| Tipo | Colunas principais | Ordenação |
|---|---|---|
| `Cardapios` | unidade, nome, início, fim, situação, versão, kcal total | unidade, início, nome |
| `AlunosRestricoes` | aluno, unidade, restrições, anafilaxia | unidade, aluno |
| `Ocorrencias` | data, tipo, status, unidade, aluno, descrição (120 chars) | data desc, id |
| `SolicitacoesFeedbacks` | tipo, status, solicitante, unidade, criado em, atualizado em | criado em desc, id |
| `IndicadoresAdministrativos` | indicador, valor | ordem fixa do catálogo |

Nenhuma coluna carrega CPF, e-mail, telefone, foto ou senha. Descrição é truncada na
consulta, não no exportador.

## Escopo

`RelatorioService` resolve o escopo antes de delegar:

```text
Nutricionista    → todos os tipos, sem filtro obrigatório
AdminSecretaria  → IndicadoresAdministrativos, SolicitacoesFeedbacks (global)
AdminEscola      → AlunosRestricoes, Ocorrencias, SolicitacoesFeedbacks,
                   IndicadoresAdministrativos (forçado à escola da sessão)
Merendeira       → 403
Responsavel      → 403
```

`escolaId` enviado fora do escopo de `AdminEscola` → 403, sem consultar dados.

## Exportadores

```csharp
public enum FormatoExportacao { Pdf, Xlsx, Docx }

public interface IRelatorioExportador
{
    FormatoExportacao Formato { get; }
    string Extensao { get; }          // "pdf" | "xlsx" | "docx"
    string ContentType { get; }
    byte[] Gerar(RelatorioGerado relatorio);
}
```

- **PDF** — QuestPDF, tabela com cabeçalho repetido, título, filtros aplicados e data
  de geração; sem logo externo.
- **XLSX** — ClosedXML, primeira linha congelada, cabeçalho em negrito, formato de
  número por `TipoColuna`.
- **DOCX** — `DocumentFormat.OpenXml`, tabela com cabeçalho e totais ao final.

Todos recebem `RelatorioGerado` já pronto: nenhum exportador consulta banco, e
adicionar um formato novo não altera a montagem.

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/relatorios/{tipo}/preview?de=&ate=&escolaId=&unidadeId=` | por escopo |
| `GET` | `/api/relatorios/{tipo}/exportar?formato=&de=&ate=&escolaId=&unidadeId=` | por escopo |

```json
{ "tipo": "Ocorrencias", "titulo": "Ocorrências por período",
  "colunas": [ { "chave": "data", "titulo": "Data", "tipo": "Data", "formato": "yyyy-MM-dd" } ],
  "linhas": [ { "data": "2026-10-30", "tipo": "Refeicao", "status": "EmAnalise" } ],
  "totais": { "total": 1 }, "ordenacao": "data desc, id",
  "geradoEmUtc": "2026-10-30T13:00:00Z",
  "filtrosAplicados": { "de": "2026-10-01", "ate": "2026-10-31" } }
```

Exportação responde `FileContentResult` com `Content-Disposition: attachment;
filename="relatorio-ocorrencias-202610301300.pdf"`.

## Limites

- Pré-visualização: `Take(200)`; a contagem total e os totais usam agregação no banco.
- Exportação: `Count()` antes de materializar; acima de 50.000 → 413 sem gerar bytes.
- Timeout de request padrão do ASP.NET; nenhuma tarefa em background.

## Frontend

- `pages/nutricao/relatorios/` e `pages/escolas/relatorios/` com seleção de tipo,
  filtros de data/escola/unidade, tabela de pré-visualização, totais e três botões.
- Download via `fetch` + `Blob` para preservar o `Content-Disposition` e o cookie de
  sessão; nenhuma biblioteca de exportação no cliente.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Serviço | totais de cada relatório conferem com base fixa |
| Serviço | escopo de `AdminEscola` restringe escola e 403 fora dela |
| Serviço | `Cardapios` por `AdminEscola` → 403 |
| Serviço | período > 366 dias → 400 |
| Exportação | PDF inicia com `%PDF`; XLSX/DOCX iniciam com `PK` |
| Exportação | nome e extensão por formato |
| HTTP | preview limita a 200 linhas e informa total real |
| HTTP | 50.001 linhas → 413 sem arquivo |
| HTTP | nenhuma coluna sensível no JSON de qualquer relatório |

## Riscos

- **Dependências de exportação**: três pacotes novos; todos mantidos e compatíveis com
  `net10.0`. Se algum não suportar a versão, o exportador correspondente é o único
  ponto a trocar.
- **Memória em exportações grandes**: o limite de 50.000 e a contagem prévia evitam
  materializações descontroladas.
- **Divergência entre preview e exportação**: ambos usam o mesmo `IRelatorio`; o
  exportador só recebe `RelatorioGerado`, nunca monta consulta própria.
