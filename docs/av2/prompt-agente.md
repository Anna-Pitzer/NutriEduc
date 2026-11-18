# Prompt para agente — alteração funcional: exportação CSV de relatórios

Este documento é uma **especificação de alteração concreta** para execução por agente, com a
**evidência de inspeção** que a sustenta. Diferente dos templates genéricos de pipeline
(`templates/prompts/spec_new.md.ejs`, `templates/prompts/spec_implement.md.ejs`,
`templates/prompts/spec_audit.md.ejs`), aqui o
prompt descreve **uma mudança real**, em arquivos reais, com pré-condições e critérios
verificáveis.

| Campo | Valor |
| --- | --- |
| Alteração | Exportação de relatórios em **CSV**, ao lado do XLSX |
| Tipo | funcional, aditiva, sem quebra de contrato |
| Origem da decisão | ADR-012 (exportador plugável), Spec 019 (relatórios) |
| Base | commit `9544088`; alteração executada nesta árvore em 2026-11-18 |
| Resultado | 11 testes novos, suíte **881/881**, build e lint verdes |

---

## 1. Evidência de inspeção (coletada **antes** de escrever o prompt)

Comandos executados sobre o repositório, com o achado que cada um produziu e o efeito no prompt.

| # | Inspeção | Achado | Efeito no prompt |
| --- | --- | --- | --- |
| I1 | `backend/Domain/Interface/IExportadorDeRelatorio.cs` | contrato com 3 membros: `Extensao`, `TipoDeConteudo` e `Gerar(TabelaDeRelatorio)` (linhas 11, 13, 15) | a nova implementação **não pode** mudar a interface; ela já basta |
| I2 | `grep -rn "IExportadorDeRelatorio" backend` | **uma** implementação (`ExportadorXlsx`) e **um** registro no DI (`Program.cs:82`; hoje, após a alteração, 83–84) | o trabalho é adicionar a segunda implementação, não reestruturar o contrato |
| I3 | `backend/API/Services/RelatorioService.cs` | recebia **um** `IExportadorDeRelatorio`; `Exportar` usava `_exportador.Extensao`/`TipoDeConteudo` | o serviço precisa passar a receber a coleção e escolher por formato |
| I4 | `backend/API/Controllers/RelatorioController.cs` | rota `GET {tipo}/exportar` sem parâmetro de formato | a escolha entra como **query param**, sem rota nova (evita quebrar `DocumentationTests`) |
| I5 | `backend.Tests/Integration/RelatorioApiTests.cs` | `Exportar_RetornaArquivoXlsx` já assevera `Content-Type` e o magic `PK` | o XLSX deve continuar **padrão**; o teste existente não pode ser alterado |
| I6 | `backend.Tests/Integration/DocumentationTests.cs` | valida apenas pares **verbo+rota** documentados no README (regex da linha 15) | adicionar query param não exige rota nova, mas o README precisa descrever o parâmetro |
| I7 | `management/adr.md` ADR-012 | decisão: “exportador é plugável”; texto citava nome antigo `IRelatorioExportador` | ao tocar a ADR, corrigir o nome e registrar as duas implementações |
| I8 | `backend/Domain/Relatorios/TabelaDeRelatorio.cs` | `Titulo`, `Colunas`, `Linhas`, `Rodape` já formatados; `Colunas` pode ser vazio | o CSV deve tratar cabeçalho vazio (`Valor`) e não recalcular nada |
| I9 | `backend/Infrastructure/Exportacao/ExportadorXlsx.cs` | gera OOXML à mão, **sem dependência externa**, determinístico | o CSV também deve ser sem dependência e determinístico |
| I10 | `frontend/src/services/RelatorioApi.ts` + `frontend/src/pages/relatorios/RelatoriosPage.tsx` | `exportarRelatorio` monta a URL e baixa o blob; botão único “Exportar XLSX” | a UI precisa aceitar o formato e oferecer as duas opções |
| I11 | `dotnet build` sem `APPDATA`/`ProgramFiles` | falha no restore (`path1`) — PEND-016 | o gate build/test precisa do `env` documentado; ver `docs/av2/depuracao.md` |

**O que a inspeção descartou** (evita decisões erradas): não existe enum de formato no domínio;
`RespostaController` já traduz `Invalido` em `400` com `{ erro }`; não há teste de 422 nem de
frontend (PEND-049); a `TabelaDeRelatorio` já vem pronta, então **nenhum** exportador pode
consultar banco.

## 2. Prompt

```text
PAPEL
Você é um agente de implementação no repositório NutriEduc (ASP.NET Core 10 + React 19).
Execute a alteração abaixo de ponta a ponta, respeitando as fronteiras de camada do projeto.

OBJETIVO
Permitir que a exportação de relatórios gere CSV além de XLSX, escolhido por requisição,
mantendo XLSX como padrão e sem quebrar nenhum contrato existente.

ESTADO ATUAL (já verificado — não re-descubra, mas confirme antes de editar)
- `backend/Domain/Interface/IExportadorDeRelatorio.cs` define `Extensao`, `TipoDeConteudo`
  e `Gerar(TabelaDeRelatorio)`.
- `backend/Infrastructure/Exportacao/ExportadorXlsx.cs` é a única implementação.
- `backend/API/Services/RelatorioService.cs` recebe UM exportador e o usa em `Exportar`.
- `backend/Program.cs` registra `IExportadorDeRelatorio -> ExportadorXlsx`.
- `GET /api/relatorios/{tipo}/exportar` não aceita formato.
- `backend.Tests/Integration/RelatorioApiTests.cs` tem `Exportar_RetornaArquivoXlsx`
  (assevera Content-Type e magic PK) e não deve ser alterado.

ESCOPO — arquivos que você PODE criar ou alterar
1. backend/Infrastructure/Exportacao/ExportadorCsv.cs (novo)
2. backend/API/Services/RelatorioService.cs
3. backend/API/Controllers/RelatorioController.cs
4. backend/Program.cs
5. backend.Tests/Infrastructure/ExportadorCsvTests.cs (novo)
6. backend.Tests/Integration/RelatorioApiTests.cs (apenas adicionar testes)
7. frontend/src/services/RelatorioApi.ts
8. frontend/src/pages/relatorios/RelatoriosPage.tsx
9. README.md (linha do endpoint de exportação)
10. management/adr.md (ADR-012), management/arch.md (linha do módulo 019),
    funcionalidades.md (menções a "exportar XLSX")
Nada fora dessa lista. Não edite specs 026/037, não crie dependência nova.

REQUISITOS FUNCIONAIS
RF1. `GET .../exportar?formato=csv` devolve `200` com corpo CSV e `Content-Type:
     text/csv; charset=utf-8`.
RF2. Formato ausente (ou em branco) continua devolvendo XLSX, como hoje.
RF3. Formato desconhecido devolve `400` com `{ "erro": ... }` listando os formatos aceitos.
RF4. A autorização é avaliada ANTES do formato: papel sem permissão recebe `403` mesmo com
     formato inválido.
RF5. O nome do arquivo termina com a extensão do formato escolhido.

REQUISITOS DO CSV (RFC 4180)
- separador `;`, fim de linha `\r\n`, UTF-8 com BOM.
- campos com separador, aspas, `\r` ou `\n` são envolvidos em aspas e têm aspas duplicadas.
- cabeçalho = `tabela.Colunas`; se vazio, usar a coluna `Valor`.
- NÃO escrever título nem rodapé (metadados quebrariam o cabeçalho); documente essa escolha.
- determinístico: a mesma tabela produz os mesmos bytes.

REQUISITOS DE DESIGN
- A interface de domínio NÃO muda. A implementação nova vive em Infrastructure.
- `RelatorioService` recebe `IEnumerable<IExportadorDeRelatorio>`, monta um dicionário por
  `Extensao` (comparação case-insensitive) e seleciona por formato; persistir `FormatoPadrao`.
- `Gerar` é chamado apenas depois da autorização, da validação de formato e do limite de
  50.000 linhas.
- Sem `if` de formato espalhado por controller ou serviço: a seleção fica em um único método.

TESTES OBRIGATÓRIOS
- Unidade do CSV: contrato (`Extensao`/`TipoDeConteudo`), cabeçalho + linhas, escape de `;`,
  escape de aspas, campo com quebra de linha, cabeçalho padrão `Valor`, ausência de
  título/rodapé, determinismo.
- API: `formato=csv` (200, text/csv, BOM, contém `;`), formato inválido (400 citando `xlsx`),
  merendeira com formato inválido (403).
- Não alterar `Exportar_RetornaArquivoXlsx`: ele prova que o padrão continua XLSX.

GATES (todos devem passar; rode na raiz)
  env APPDATA="<seu %APPDATA%>" ProgramFiles='C:\Program Files' \
    dotnet build backend.Tests/backend.Tests.csproj -c Release
  env APPDATA="<seu %APPDATA%>" ProgramFiles='C:\Program Files' \
    dotnet test backend.Tests/backend.Tests.csproj -c Release --no-build
  cd frontend && npm run build && npm run lint && cd ..
Observação: sem `APPDATA`/`ProgramFiles` o restore falha com `path1` (PEND-016);
é defeito de ambiente, não do código.

CONDIÇÕES DE PARADA (pare e reporte, sem improvisar)
P1. A suíte falhar por motivo não relacionado à alteração: reporte o nome do teste e não
    “conserte” código alheio.
P2. O gate de frontend falhar por erro pré-existente: reporte; só corrija se for remoção
    inequívoca de código morto, e registre como desvio.
P3. Se já existir outra implementação de `IExportadorDeRelatorio`, revise o plano antes de
    seguir (o contrato pode ter mudado).
P4. Se `DocumentationTests` quebrar, a causa é divergência README↔controllers: corrija a
    documentação, nunca o teste.

FORMATO DA RESPOSTA FINAL
1. Lista de arquivos alterados com uma linha de propósito cada.
2. Saída resumida dos gates (contagem de testes, erros/avisos).
3. Critério de aceitação → teste que o prova.
4. Desvios e decisões não triviais (ex.: por que CSV não tem título).
```

## 3. Resultado da execução (prova)

| Arquivo | Propósito | Evidência |
| --- | --- | --- |
| `backend/Infrastructure/Exportacao/ExportadorCsv.cs` (novo) | segunda implementação, RFC 4180 | 65 linhas; `Extensao`/`TipoDeConteudo` nas linhas 20–22 |
| `backend/API/Services/RelatorioService.cs` | dicionário de exportadores + `Selecionar(formato)` | linhas 22, 33, 87–90, 118–126 |
| `backend/API/Controllers/RelatorioController.cs` | query param `formato` | linha 58, repasse na 68 |
| `backend/Program.cs` | dois registros do mesmo contrato | linhas 83–84 |
| `backend.Tests/Infrastructure/ExportadorCsvTests.cs` (novo) | 8 testes de unidade | 8 `[Fact]` |
| `backend.Tests/Integration/RelatorioApiTests.cs` | 3 testes de contrato HTTP | linhas 77, 99, 114 |
| `frontend/src/services/RelatorioApi.ts` | `FormatoDeExportacao` + `formato` na query | `consulta(filtro, formato)` |
| `frontend/src/pages/relatorios/RelatoriosPage.tsx` | botões “Exportar XLSX” e “Exportar CSV” | `exportar("xlsx")` / `exportar("csv")` |
| `README.md:308` | endpoint documenta `formato=xlsx\|csv` | `DocumentationTests` verde |
| `management/adr.md` (ADR-012) | revisão de 2026-11-18 com as duas implementações | registro da decisão |
| `management/arch.md`, `funcionalidades.md` | módulo 019 e menções a exportação | `ExportadorXlsx` + `ExportadorCsv` |

**Critério → prova**

| Critério | Prova |
| --- | --- |
| RF1 CSV 200 com `text/csv` e BOM | `RelatorioApiTests.Exportar_ComFormatoCsv_RetornaTextoCsv` |
| RF2 XLSX continua padrão | `RelatorioApiTests.Exportar_RetornaArquivoXlsx` (inalterado) |
| RF3 formato inválido → 400 com lista | `RelatorioApiTests.Exportar_ComFormatoDesconhecido_Retorna400` |
| RF4 autorização antes do formato | `RelatorioApiTests.Exportar_PorMerendeiraComFormatoInvalido_Retorna403` |
| RF5 extensão no nome | asserção de `.csv` no `Content-Disposition` do teste de RF1 |
| Regras do CSV (escape, BOM, determinismo) | `ExportadorCsvTests` (8 testes) |

**Gates executados:** `dotnet build` 0 erros · `dotnet test` **881/881** (870 + 11) ·
`npm run build` ok · `npm run lint` 0 erros.

**Desvio registrado (P2):** `npm run build` falhou por **3 imports mortos pré-existentes**
(`VoltarPaginas` em `frontend/src/pages/feedback/FeedbackPage.tsx`,
`frontend/src/pages/ocorrencias/OcorrenciasPage.tsx` e
`frontend/src/pages/solicitacoes/SolicitacoesPage.tsx`), vindos
do commit `9544088` e sem relação com a alteração. Como era remoção inequívoca de código morto,
os 3 imports foram removidos e o desvio ficou registrado; nenhum outro arquivo alheio foi tocado.

## 4. Limites deste prompt

- Escrito para **este** contrato (uma tabela pronta, sem consulta a banco). Um exportador que
  precise de dados próprios violaria a ADR-012 e não deve reaproveitar este texto.
- O prompt não define política de formato no frontend (ex.: lembrar a última escolha); a UI
  apenas oferece os dois botões.
- O CSV não carrega título nem rodapé por decisão explícita; se a instituição exigir cabeçalho
  de documento, é outra alteração (e provavelmente outro formato).
- Reexecutável em clone limpo: os gates e as condições de parada independem do estado desta
  árvore, exceto o workaround de `APPDATA`/`ProgramFiles` (PEND-016, medido em
  `docs/av2/depuracao.md`).
- **Relação com a execução:** a inspeção da §1 é anterior à implementação; o texto do prompt é
  a consolidação do que foi executado e aprovado nos gates, não um roteiro escrito depois de
  ler o resultado e ajustado para “bater”. As referências de linha da §3 foram reconferidas
  contra a árvore atual.
