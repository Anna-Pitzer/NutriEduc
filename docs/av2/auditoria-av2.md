# Auditoria AV2 — partes 1 a 4 (critérios A01–G02)

Auditoria técnica completa do repositório em quatro partes, com julgamento **binário** (SIM/NÃO),
sem pontuação parcial. Cada critério vale 0,25 ponto.

| Campo | Valor |
| --- | --- |
| Data | 2026-11-18 |
| Base | commit `9544088`, com alterações não commitadas na árvore de trabalho |
| Método | inspeção de código, testes, histórico Git e documentação; nenhuma implementação foi alterada |
| Escopo | backend (`Domain`, `API`, `Infrastructure`, `Tests`), `diagrama.md`, `management/`, `docs/av2/` |
| Execução | suíte da árvore **881/881**; clone limpo **870/870** com workaround não documentado |

## Resumo consolidado

| Parte | Critérios | SIM | NÃO | Pontuação |
| --- | --- | --- | --- | --- |
| 1 — POO e modelo de classes (A/B) | 16 | 15 | 1 (B03) | 3,75 / 4,00 |
| 2 — Análise de código e exceções (C/D) | 10 | 10 | 0 | 2,50 / 2,50 |
| 3 — Depuração e técnicas (E) | 6 | 6 | 0 | 1,50 / 1,50 |
| 4 — Testes, rastreabilidade e specs (F/G) | 8 | 7 | 1 (F04) | 1,75 / 2,00 |
| **Total** | **40** | **38** | **2** | **9,50 / 10,00** |

> **Revisão pós-auditoria (2026-11-18, commit `1f58edf`).** As duas reprovações foram corrigidas e
> reverificadas: **B03** — `diagrama.md` §3.4 passou a declarar a nulabilidade e os genéricos
> exatamente como no código (6 métodos de `Aluno`/`Cardapio`, mais `RestricaoAlimentar.Normalizar`
> e `Alimento.AtualizarDados`), usando a notação `~T~` do Mermaid, sem `<`/`>` no bloco;
> **F04** — o `README.md` agora documenta `APPDATA`/`ProgramFiles` e um clone limpo compila e roda
> **881/881** testes seguindo as instruções. Com isso, o resultado corrente é **40/40 (10,0)**;
> os vereditos por parte abaixo permanecem como registro do estado **antes** das correções.
> Também foram superadas a ressalva de versionamento (`docs/av2/` e `av2-avaliacao.md` commitados
> em `1f58edf`) e a ausência do pré-requisito no README; os itens 1, 2, 3 e 7 da tabela de
> pendências estão resolvidos.

---

## Parte 1 — POO e modelo de classes (A01–A08, B01–B08)

| Critério | Resultado | Evidência encontrada | Justificativa |
|---|---|---|---|
| A01 | SIM | `backend/Domain/Entities/` (55 arquivos): `Aluno.cs`, `Usuario.cs`, `Cardapio.cs`, `Solicitacao.cs`, `Ocorrencia.cs`, `Feedback.cs`, `Escola.cs`, `Alimento.cs`, `RestricaoAlimentar.cs` | Entidades de domínio com estado persistido e regras próprias; infraestrutura não foi contada. |
| A02 | SIM | `Cardapio.Publicar/Cancelar/CriarRevisao` (`Cardapio.cs:367-410`); `Solicitacao.IniciarAnalise/Aprovar/Recusar/Concluir/Cancelar` + `Transicionar` privado (`Solicitacao.cs:225`); `Aluno.AdicionarRestricao` (`:220`); `Usuario.AtualizarPerfil` (`:193`); `Ocorrencia.MudarStatus` (`:123`); `Feedback.Responder/Encerrar` (`:124-135`) | Métodos alteram estado, validam entrada e aplicam transições/invariantes. |
| A03 | SIM | CRN em `CardapioService.cs:581`, `GeracaoCardapioService.cs:132`, `AdaptarCardapioService.cs:106`, `PublicacaoCardapioService.cs:49`; transição `PublicacaoCardapioService.cs:127`; decisão única `SolicitacaoService.cs:514`; compatibilidade `CardapioService.cs:929`, `CompatibilidadeService.cs:52`, `PublicacaoCardapioService.cs:307`; período `PeriodoVigencia.cs:33` (`CardapioService.cs:665`); invariantes `AlunoService.cs:191` | Seis regras distintas com chamador de produção; nenhuma é código morto. |
| A04 | SIM | `EfeitosDaSolicitacao.AplicarRestricao(Aluno, string, IReadOnlyList<RestricaoAlimentar>, string)` (`EfeitosDaSolicitacao.cs:22-45`), chamado em `SolicitacaoService.cs:473` | Colaboração entre `Solicitacao` (estado), `EfeitosDaSolicitacao` (regra) e `Aluno` (invariante). |
| A05 | SIM | `SituacaoCardapio` (`:7-13`: Rascunho, Publicado, Cancelado, Substituido); `Publicar`/`Cancelar` (`Cardapio.cs:367-394`); `CriarRevisao` (`:396-410`) preserva `Id` e incrementa `Versao` com `RevisaoDeId` | Quatro estados com transições explícitas e identidade preservada. |
| A06 | SIM | `Publicar` valida antes de mutar (`Cardapio.cs:371-374`); `SolicitacaoService.cs:513-527` captura `TransicaoInvalidaException` e devolve `Conflito` **antes** de `ExecutarEmTransacao`; `PublicacaoCardapioService.cs:44-48` retorna 409 antes de escrever; `SolicitacaoServiceTests.Aprovar_DuasVezes_DevolveConflito:280` assere `Conflito` e `Assert.Single(Historico)` | Objeto e dados permanecem íntegros após transição proibida. |
| A07 | SIM | 33 entidades com `private set`; `Aluno.Restricoes` `IReadOnlyList` sobre campo privado (`Aluno.cs:54`), invariantes em `AdicionarRestricao` (`:220-236`); `Transicionar` privado (`Solicitacao.cs:225`); ADR-006 | Sem setter público e sem coleção mutável exposta. |
| A08 | SIM | `IExportadorDeRelatorio` (`:9-15`); `ExportadorXlsx` (`:15`) e `ExportadorCsv` (`:14`); DI `Program.cs:83-84`; uso `RelatorioService.cs:87,118-126`; testes `RelatorioApiTests.cs:77,99,114` | Contrato comum, duas implementações de comportamento diferente e seleção em execução. |
| B01 | SIM | `diagrama.md` §3.4 (`classDiagram` Mermaid, linhas 345-453) | Modelo textual com 10 classes de domínio. |
| B02 | SIM | 10 classes do modelo ↔ arquivos homônimos em `backend/Domain/Entities/` | Correspondência 1:1 verificada. |
| B03 | NÃO | Modelo: `Aluno.DefinirUnidade(int)`, `RegistrarCriacao(string)`, `RegistrarHistorico(..., string, string)`, `AtualizarDados(..., string, string, IEnumerable, ..., int)`; `Cardapio.AplicarAdaptacao/CriarAdaptado` sem `?`. Código: `int?` (`Aluno.cs:258`), `string?` (`:177,:189`), `string?/IEnumerable<string>?/int?` (`:120-130`), `IEnumerable<T>?` (`Cardapio.cs:146-153,192-198`) | Contradição de nulabilidade/genéricos em 6 métodos de duas classes. |
| B04 | SIM | `CARDAPIO \|\|--o{ REFEICAO` (`diagrama.md` §3.2); `ConnectionContext.cs:423-426` (`HasMany(...).HasForeignKey(...)`) | Associação 1:N com FK correspondente. |
| B05 | SIM | `ConnectionContext.cs:423-426` (Cardapio→Refeicao, cascade) e `:439-442` (Refeicao→ItemRefeicao, cascade) | Composição com ciclo de vida comprovada. |
| B06 | SIM | `diagrama.md` §3.5: `<<interface>>`, `<\|.. ExportadorXlsx`, `<\|.. ExportadorCsv`, `RelatorioService ..>`; código com 2 implementações e 2 registros no DI | Cardinalidade do modelo igual à do código. |
| B07 | SIM | `diagrama.md:245` (`QuantidadeGramas "máx 5000, 2 casas"`) ↔ `ItemRefeicao.cs:15,40-43` | Invariante anotada no objeto que a protege. |
| B08 | SIM | `management/adr.md` ADR-006 e ADR-010 | ADR-006 compara `List<string>` pública × `IReadOnlyList`; ADR-010 compara `rowversion` × `[ConcurrencyCheck]`. |

**Totais da Parte 1:** 15 SIM, 1 NÃO (B03) — **3,75 / 4,00**.

---

## Parte 2 — Análise de código e exceções (C01–C06, D01–D04)

Alteração selecionada: **commit `644deac`** (Spec 035, incluindo a regra de duplicidade 409 e a
anafilaxia informada).

| Critério | Resultado | Evidência | Justificativa |
|---|---|---|---|
| C01 | SIM | `git show --stat 644deac`; `git show 644deac -- backend/Domain/Entities/Solicitacao.cs` → `+ public bool Anafilaxia { get; private set; }`; migration `20261113142544_SolicitacaoAnafilaxia.cs`; `Feedback.cs` (+44), `SolicitacaoService.cs` (+161) | Diff real que altera comportamento de domínio. |
| C02 | SIM | RF20 em `specs/035.../requirements.md:130-132`; diff `IdDaAbertaParaRestricao` + `Conflito`; teste `SolicitacaoApiTests.Criar_InclusaoAlergiaDuplicada_Retorna409ComIdExistente` | A regra pedida está implementada e provada por teste. |
| C03 | SIM | `cross-session/README.md:304-321`: "Cobertura verificada" nomeia artefatos e finalidades; "Desvios aceitos" registra o que saiu do plano (`FiltroFeedback` em outro arquivo; exposição de `cardapioGeral`/`cardapioAdaptado`) | Análise de arquivos com finalidade e mudanças fora do escopo. |
| C04 | SIM | Mesma entrada: contrato 409 com id, índice/migration `FeedbackAluno`, escopo por vínculo; teste dedicado; suíte **829/829** e gates | Impacto e verificação registrados e reproduzíveis. |
| C05 | SIM | `specs/035.../design.md:43,45,50,52` — D1 "baixo acoplamento", D3 "uma responsabilidade, interface estreita" com "SRP, baixo acoplamento"; `revisao-codigo.md:165-243` (§5) | Avaliação em acoplamento e responsabilidade/coesão (≥2 propriedades). |
| C06 | SIM | `cross-session/README.md:315-319` ("Desvios aceitos"); PEND-051 `done` (`pending-items.md:91`); ADR-023 "aceito"; spec `audited` | Decisão de aceitar/corrigir ancorada em teste e requisito. |
| D01 | SIM | `specs/007.../requirements.md:32-36,50` (400 entrada, 409 conflito/estado, 500 excepcional); `ErrorHandlingMiddleware.Mapear` com 400/409/500 e 422 de diagnóstico | Três situações identificadas com comportamento esperado; regra × excepcional distinguidas no status e no log. |
| D02 | SIM | `TransicaoInvalidaException.cs:7` herda `InvalidOperationException`; `AprovarAcessoService.cs:265` chama `Aprovar` sem catch → middleware 409; `AprovarAcessoServiceTests.DecisaoRepetida_Retorna409`; `PublicacaoApiTests.Publicar_JaPublicado_Retorna409`; rotas de injeção em `Program.cs:276-284` (ambiente de teste) | Caminho reproduzível de lançamento até tratamento. |
| D03 | SIM | `ErrorHandlingMiddleware` caso default: 500 + `"Erro interno."`, log com `TraceIdentifier`, sem stack; `ErroApiTests.ExcecaoNaoTratada_Retorna500SemStackTrace` (`:129`) | Falha registrada sem expor informação interna. |
| D04 | SIM | 41 asserções de `BadRequest`, 29 de `Conflict`, 1 de `InternalServerError` (`ErroApiTests.cs:129`) | As três situações (entrada, regra/estado, excepcional) têm teste de status e corpo. |

**Totais da Parte 2:** 10 SIM, 0 NÃO — **2,50 / 2,50**.

---

## Parte 3 — Depuração e avaliação de técnicas (E01–E06)

Fonte: `docs/av2/depuracao.md` (defeito de build sem `APPDATA`/`ProgramFiles`, PEND-016).

| Critério | Resultado | Evidência | Justificativa |
|---|---|---|---|
| E01 | SIM | §1 (18-43) saída literal `NuGet.targets(782,5): error : Value cannot be null. (Parameter 'path1')`, `exit=1`; §2 (45-64) falha **3/3**; §8 (126-144) reprodução | Defeito real e reproduzível com evidência do comportamento incorreto. |
| E02 | SIM | §2 (48: `echo "[$APPDATA] [$ProgramFiles]"` → `[] []`; 50-61); §1 observado; §4 (77-90) `Compilação com êxito`, `exit=0` 3/3; §8 (132-143) | Condições, observado e esperado apresentados (esperado demonstrado na §4 e nos comentários da §8). |
| E03 | SIM | §5 (101: hipótese `NUGET_PACKAGES` descartada; 99-100: só uma variável falha); §5/§6 (105-115: `--no-restore` decisivo, causa confirmada) | Hipótese explícita, descarte medido e confirmação por isolamento. |
| E04 | SIM | §2 (48) inspeção de variáveis; §1 (22-33) análise do log do MSBuild; §2/§4/§5 rastreamento com exit code e tempo | Técnica comprovada (variáveis + log/execução); sem breakpoint, como o próprio registro delimita. |
| E05 | SIM | §6 causa (108-115); §3 correção (67-75); §4 (77-90) sucesso 3/3; §7 (117-124) gates e 881/881 | Causa, correção e eliminação do comportamento incorreto sob a correção (workaround de ambiente). |
| E06 | SIM | §2 × §4 (antes/depois medidos); §5 (92-106) seis alternativas com exit code medido; §9 (146-153) limitações | Antes/depois + comparação fundamentada com alternativas; comparação é de abordagens de correção/experimento. |

**Totais da Parte 3:** 6 SIM, 0 NÃO — **1,50 / 1,50**.

---

## Parte 4 — Testes, rastreabilidade e especificações (F01–F06, G01–G02)

| Critério | Resultado | Evidência | Justificativa |
|---|---|---|---|
| F01 | SIM | R1 `Publicar_Rascunho_MarcaPublicadoComEvento` × `Publicar_JaPublicado_LancaTransicaoInvalida`; R2 `Construtor_ComDadosValidos_CriaAluno` × `Construtor_SemTurma_Lanca`; R3 `AtualizarPerfil_ComDadosValidos_NormalizaNomeEEmail` × `AtualizarPerfil_Invalido_NaoAlteraEstado`; R4 `Aprovar_Restricao_AplicaNoAlunoEGravaTrilha` × `Aprovar_DuasVezes_DevolveConflito`; R5 `NutricionistaComCrnValido_NaoBloqueia` × `NutricionistaSemCrn_RetornaMensagemDeBloqueio` | 10 cenários (sucesso e violação por regra), todos aprovados na suíte. |
| F02 | SIM | `UsuarioTests.AtualizarPerfil_Invalido_NaoAlteraEstado` (`:77`): `Assert.Throws<ArgumentException>` + `Nome`/`Email` inalterados | Invariante violada e objeto permanece válido. |
| F03 | SIM | `SolicitacaoApiTests.Fluxo_AprovarRestricao_AplicaNoAlunoEGravaTrilha` (`:151`) com `ApiFactory`/SQLite: POST, analisar, aprovar, `GET /api/aluno/perfil/{id}` e `GET /api/aluno/{id}/historico` | Integração real (HTTP + EF) que aplica R4 e verifica persistência. |
| F04 | NÃO | Clone novo `/tmp/nutriedu-clone-2`: `README.md:129` (`dotnet build`) → **exit 1**, `path1`; `grep APPDATA README.md` = 0; com workaround não documentado: build OK e **870/870** (árvore: **881/881**); `docs/av2` ausente no clone | Instruções do repositório não bastam para clone limpo; depende de configuração local não documentada. |
| F05 | SIM | `docs/av2/rastreabilidade.md`: F1–F3, R1–R5, matriz de 33 linhas (regra → comportamento → implementação → teste), mapa de testes e script de verificação; 50 referências conferidas | Rastreabilidade completa. Ressalva à época: não versionado; hoje versionado em `1f58edf`. |
| F06 | SIM | `av2-avaliacao.md` (32/40, 8 reprovações e ressalvas) e `docs/av2/avaliacao-final.md` (38/40, B03 e F04 como NÃO); `requisitos.md` §5 com 5 lacunas; PEND-028/PEND-049 abertos | Auditoria com evidência e pendências, sem declarar concluído o não comprovado. Ressalvas: duas auditorias divergentes; hoje versionadas. |
| G01 | SIM | `docs/av2/requisitos.md`: F1–F3 (atores, pré-condições, fluxo principal, alternativas, pós-condições) e R1–R5 (condições, onde é imposta, exceções); `CA-F1-1`…`CA-R5-3`; 57 referências verificadas no código | Especificação verificável compatível com a implementação. Ressalva à época: não versionado; hoje versionado. |
| G02 | SIM | `docs/av2/prompt-agente.md`: alteração CSV (ADR-012/Spec 019), 11 achados de inspeção (I1–I11), prompt com escopo/RFs/gates, §3 com execução (11 testes, 881/881) e desvio registrado | Especificação para agente com evidência de inspeção e verificação. Ressalva à época: não versionado; hoje versionado. |

**Totais da Parte 4:** 7 SIM, 1 NÃO (F04) — **1,75 / 2,00**.

### Execução real de build e testes (2026-11-18)

| Execução | Comando | Resultado |
| --- | --- | --- |
| Clone limpo, instrução do README | `dotnet build backend/backend.csproj` | **exit 1** — `NuGet.targets(782,5): Value cannot be null. (Parameter 'path1')` |
| Clone limpo, com workaround não documentado | `dotnet build backend.Tests/... -c Release` + `dotnet test ... --no-build` | build **exit 0**; **870/870** aprovados |
| Árvore de trabalho | `dotnet test backend.Tests/... -c Release --no-build` | **881/881** aprovados, 0 falhas |

---

## Pendências consolidadas

| # | Pendência | Impacto | Correção objetiva | Situação |
| --- | --- | --- | --- | --- |
| 1 | B03: nulabilidade/genéricos divergentes no `diagrama.md` §3.4 (6 métodos de `Aluno` e `Cardapio`) | Reprova B03 | Repassar `?` e genéricos conforme `Aluno.cs:120-130,177,189,258` e `Cardapio.cs:146-153,192-198` | **Resolvido em `1f58edf`** |
| 2 | F04: `README.md` não documenta `APPDATA`/`ProgramFiles`; clone limpo falha seguindo as instruções | Reprova F04 | Documentar o pré-requisito (ou script de build) e commitar as alterações | **Resolvido em `1f58edf`** |
| 3 | `docs/av2/` e `av2-avaliacao.md` não versionados (`??` no Git) | Não existem em clone limpo; enfraquece F05/F06/G01/G02 na entrega | `git add` e commit | **Resolvido em `1f58edf`** |
| 4 | `422` sem asserção (`UnprocessableEntity` = 0) | Lacuna de cobertura do ramo de diagnóstico de domínio | Teste de publicação/geração que asserte 422 | Aberto (lacuna declarada) |
| 5 | Duas auditorias divergentes (`av2-avaliacao.md` 17/11, 32/40; `avaliacao-final.md` 18/11, 38/40) | Risco de interpretação | Eleger a versão válida e arquivar a outra | Parcial: `auditoria-av2.md` e `avaliacao-final.md` consolidados; `av2-avaliacao.md` marcado como histórico |
| 6 | C03/C05 dependem do recorte da Spec 035; para a anafilaxia isolada não há análise de escopo nem de POO | C03/C05 ficariam NÃO naquele recorte | Documentar o recorte da anafilaxia ou manter o recorte da Spec 035 | Aberto (recorte declarado) |
| 7 | E05: correção é workaround de ambiente, não institucionalizado | Eliminação condicionada | Levar o pré-requisito ao README (mesma correção de F04) | **Resolvido em `1f58edf`** |

## Como reproduzir

```bash
# gates (o env está documentado no README — ver F04)
env APPDATA="<seu %APPDATA%>" ProgramFiles='C:\Program Files' \
  dotnet build backend.Tests/backend.Tests.csproj -c Release
env APPDATA="<seu %APPDATA%>" ProgramFiles='C:\Program Files' \
  dotnet test backend.Tests/backend.Tests.csproj -c Release --no-build   # 881/881

# clone limpo (evidência de F04)
git clone . /tmp/nutriedu-clone-2 && cd /tmp/nutriedu-clone-2
dotnet build backend/backend.csproj        # exit 1, path1
ls docs/av2                                # presente e versionado em 1f58edf

# referências de teste citadas em docs/av2
for f in docs/av2/requisitos.md docs/av2/rastreabilidade.md; do
  grep -oE '`[A-Za-z]+Tests\.[A-Za-z0-9_]+`' "$f" | tr -d '`' | sort -u |
    while IFS=. read -r cls m; do
      grep -rqE "public [^(]* $m\(" backend.Tests --include="$cls.cs" || echo "FALTA $cls.$m"
    done
done   # saída vazia = todas existem

# cenários de falha por status (D04)
for s in BadRequest Conflict UnprocessableEntity InternalServerError; do
  printf "%-22s " "$s"; grep -rn "HttpStatusCode.$s" backend.Tests --include=*.cs | wc -l
done   # 41, 29, 0, 1
```

---

## Nota de escopo

Este documento consolida as quatro partes da auditoria AV2 e **substitui** o antigo
`docs/av2/auditoria-parte1.md`, que continha apenas a Parte 1. Nenhum arquivo de implementação
foi alterado em nenhuma das quatro auditorias.
