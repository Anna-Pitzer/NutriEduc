# Avaliação AV2 — resultado consolidado (critério estrito)

Consolidação das quatro rodadas de avaliação booleana dos **40 itens** das famílias A–G,
com evidência por item. Sem pontuação parcial: cada item é `Atendido` ou `Não Atendido`.

| Campo | Valor |
| --- | --- |
| Data | 2026-11-18 |
| Base | commit `9544088`, com alterações não commitadas na árvore de trabalho |
| Método | julgamento binário com evidência direta em código, testes, histórico Git e documentação |
| Peso | igual por item (40 itens) |
| Clone limpo | testado em `/tmp/nutriedu-clone` (ver F04) |

---

## 1. Resumo

| Família | Itens | Atendidos | Não atendidos |
| --- | --- | --- | --- |
| A — Domínio e POO no código | 8 | 8 | 0 |
| B — Modelo de classes e decisões | 8 | 7 | 1 (B03) |
| C — Análise de alteração (Git) | 6 | 6 | 0 |
| D — Tratamento de exceções | 4 | 4 | 0 |
| E — Depuração documentada | 6 | 6 | 0 |
| F — Testes, build e rastreabilidade | 6 | 5 | 1 (F04) |
| G — Especificação e prompt para agente | 2 | 2 | 0 |
| **Total** | **40** | **38** | **2** |

**Resultado: 38/40 = 95,0% → 9,5 em escala 0–10.**

Os dois itens reprovados são objetivos e corrigíveis: B03 (nulabilidade de parâmetros no modelo)
e F04 (instruções de build insuficientes para um clone limpo).

> **Revisão pós-auditoria (2026-11-18, commit `1f58edf`).** B03 e F04 foram corrigidos e
> reverificados: o modelo passou a refletir nulabilidade e genéricos do código e o `README.md`
> documenta `APPDATA`/`ProgramFiles`; um clone limpo compila e roda **881/881** testes seguindo as
> instruções. **Resultado corrente: 40/40 = 100% → 10,0.** A tabela acima permanece como registro
> do estado anterior às correções.

---

## 2. Itens não atendidos

### B03 — modelo contradiz o código

`diagrama.md` §3.4 diverge do código na nulabilidade de parâmetros, embora nomes, ordem e
quantidade confiram:

| Modelo | Código |
| --- | --- |
| `Aluno.DefinirUnidade(int unidadeId)` | `DefinirUnidade(int? unidadeId)` (`Aluno.cs:258`) |
| `Aluno.RegistrarCriacao(string autorNome)` | `RegistrarCriacao(string? autorNome)` (`Aluno.cs:177`) |
| `Aluno.RegistrarHistorico(..., string autorNome, string origem)` | `string? autorNome, string? origem = null` (`Aluno.cs:189`) |
| `Aluno.AtualizarDados(..., string telefone, string observacao, IEnumerable restricoes, ..., int unidadeId)` | `string? telefone, string? observacao, IEnumerable<string>? restricoes, int? unidadeId` (`Aluno.cs:120-130`) |
| `Cardapio.AplicarAdaptacao/CriarAdaptado(... IEnumerable refeicoes ...)` | `IEnumerable<Refeicao>?`, `IEnumerable<ItemSubstituido>?`, `IEnumerable<PendenciaDeAdaptacao>?` (`Cardapio.cs:146-153,192-198`) |

**Correção necessária:** repassar os `?` e os tipos genéricos no bloco 3.4 (6 métodos).

### F04 — build a partir de clone limpo

Execução realizada em 2026-11-18:

```text
git clone . /tmp/nutriedu-clone                  # ok
cd /tmp/nutriedu-clone
dotnet build backend/backend.csproj             # exit 1
  NuGet.targets(782,5): error : Value cannot be null. (Parameter 'path1')
grep -n "APPDATA\|ProgramFiles" README.md       # 0 ocorrências
```

Seguindo literalmente o `README.md:129`, o build falha no shell deste ambiente porque o
pré-requisito de ambiente (`APPDATA`/`ProgramFiles`) só está registrado em
`management/pending-items.md` (PEND-016) e `docs/av2/depuracao.md`. Com a injeção não
documentada (`env APPDATA=... ProgramFiles=...`) o clone compila e roda **870/870** testes,
enquanto a árvore de trabalho roda **881/881** — as 11 diferenças são testes do exportador CSV
ainda não commitados, ou seja, o estado verificado não é reproduzível a partir do clone.

**Correção necessária:** documentar o pré-requisito no `README.md` (seção de build/testes) e
commitar as alterações pendentes.

---

## 3. Resultado por item

### Família A — domínio e POO (8/8)

| Item | Veredito | Evidência |
| --- | --- | --- |
| A01 | Atendido | 55 arquivos em `backend/Domain/Entities/`, incluindo `Aluno`, `Usuario`, `Cardapio`, `Solicitacao`, `Ocorrencia`, `Feedback`, `Escola`, `Alimento` e `RestricaoAlimentar` |
| A02 | Atendido | Comportamento próprio: `Cardapio.Publicar/Cancelar/CriarRevisao` (`Cardapio.cs:367-410`), `Solicitacao.Transicionar` (`Solicitacao.cs:225`), `Aluno.AdicionarRestricao/AtualizarDados` (`Aluno.cs:120,220`), `Usuario.AtualizarPerfil` (`Usuario.cs:193`), `Ocorrencia.MudarStatus` (`Ocorrencia.cs:123`), `Feedback.Responder/Encerrar` (`Feedback.cs:124-135`) |
| A03 | Atendido | Seis regras chamadas pelos serviços: CRN (`ValidacaoDeNutricionista` em `CardapioService:581`, `GeracaoCardapioService:132`, `AdaptarCardapioService:106`, `PublicacaoCardapioService:49`); transição (`PublicacaoCardapioService:127`); aprovação única (`SolicitacaoService:514`); compatibilidade (`CardapioService:929`, `CompatibilidadeService:52`, `PublicacaoCardapioService:307`); período de 366 dias (`PeriodoVigencia.cs:33`, instanciado em `CardapioService:665`); invariantes do aluno (`AlunoService:191`) |
| A04 | Atendido | `SolicitacaoService:473` chama `EfeitosDaSolicitacao.AplicarRestricao(Aluno, ...)` (`EfeitosDaSolicitacao.cs:22-45`), que usa `CatalogoDeRestricoes`, `Aluno.AdicionarRestricao` e `Aluno.RegistrarHistorico` |
| A05 | Atendido | `SituacaoCardapio` com quatro estados (`SituacaoCardapio.cs:7-13`), transições em `Cardapio.Publicar/Cancelar` (`Cardapio.cs:367-394`) e identidade preservada com `Id`/`Versao` e `CriarRevisao` incrementando versão (`Cardapio.cs:396-410`) |
| A06 | Atendido | `Cardapio.Publicar` lança `TransicaoInvalidaException` antes de mutar (`Cardapio.cs:371-374`); `Solicitacao.Transicionar` valida a tabela antes de atribuir (`Solicitacao.cs:231-247`); prova em `CardapioTransicaoTests.Publicar_JaPublicado_LancaTransicaoInvalida` e `SolicitacaoServiceTests.Aprovar_DuasVezes_DevolveConflito` |
| A07 | Atendido | 33 entidades com `private set`; `Aluno.Restricoes` como `IReadOnlyList` sobre campo privado (`Aluno.cs:54`) e invariantes em `AdicionarRestricao` (`Aluno.cs:220-236`); `Solicitacao.Transicionar` privado (`Solicitacao.cs:225`); decisão na ADR-006 |
| A08 | Atendido | Contrato `IExportadorDeRelatorio` (`IExportadorDeRelatorio.cs:9-15`) com duas implementações (`ExportadorXlsx.cs:15`, `ExportadorCsv.cs:14`), dois registros no DI (`Program.cs:83-84`), seleção em `RelatorioService.Selecionar` (`:118-126`, usada em `:87`) e testes `RelatorioApiTests.cs:77,99,114` |

### Família B — modelo de classes (7/8)

| Item | Veredito | Evidência |
| --- | --- | --- |
| B01 | Atendido | `diagrama.md` §3.4 em `classDiagram` Mermaid com 10 classes de domínio |
| B02 | Atendido | As 10 classes correspondem 1:1 a `backend/Domain/Entities/` (`Cardapio`, `Aluno`, `Usuario`, `Solicitacao`, `Ocorrencia`, `Feedback`, `Escola`, `UnidadeEscolar`, `RestricaoAlimentar`, `Alimento`) |
| B03 | **Não Atendido** | Divergência de nulabilidade/Generics entre `diagrama.md` §3.4 e `Aluno.cs:120-130,177,189,258` e `Cardapio.cs:146-153,192-198` (tabela na §2 deste documento) |
| B04 | Atendido | `CARDAPIO ||--o{ REFEICAO` (`diagrama.md` §3.2) implementado em `ConnectionContext.cs:423-426` (`HasMany(...).WithOne().HasForeignKey(...)`) |
| B05 | Atendido | Composição com cascata `Cardapio → Refeicao` (`ConnectionContext.cs:423-426`) e `Refeicao → ItemRefeicao`, coerente com o ER |
| B06 | Atendido | `diagrama.md` §3.5 representa `IExportadorDeRelatorio` com `<<interface>>`, duas realizações (`<|..`) e a dependência `RelatorioService ..>`; código com duas implementações e dois registros no DI |
| B07 | Atendido | `ITEM_REFEICAO { QuantidadeGramas "máx 5000, 2 casas" }` (`diagrama.md:245`) protegido em `ItemRefeicao` (`GramasMaximas = 5000m` em `:15`, validação em `:40-43`) |
| B08 | Atendido | ADR-006 compara `List<string>` pública com `IReadOnlyList` sobre campo privado; ADR-010 compara `rowversion` (indisponível no SQLite) com `[ConcurrencyCheck]`, ambas com contexto, decisão e consequências |

### Família C — análise de alteração (6/6)

Alteração selecionada: commit `644deac` (implementação da Spec 035, incluindo a regra de
duplicidade com 409 e a anafilaxia informada).

| Item | Veredito | Evidência |
| --- | --- | --- |
| C01 | Atendido | `git show --stat 644deac` e diff verificável: `+ public bool Anafilaxia { get; private set; }` em `Solicitacao.cs`, `Feedback.cs` (+44), `SolicitacaoService.cs` (+161), migration `20261113142544_SolicitacaoAnafilaxia.cs` |
| C02 | Atendido | RF20 em `specs/035.../requirements.md:130-132` (pedido duplicado responde 409 com id existente) implementado no diff (`IdDaAbertaParaRestricao` + `Conflito`) e provado por `SolicitacaoApiTests.Criar_InclusaoAlergiaDuplicada_Retorna409ComIdExistente` |
| C03 | Atendido | `cross-session/README.md:304-321` lista artefatos e finalidade ("Cobertura verificada") e identifica desvios fora do plano ("Desvios aceitos": `FiltroFeedback` em outro arquivo; cardápio do filho expondo geral + adaptado) |
| C04 | Atendido | Impacto descrito (contrato 409 com id, índice/migration `FeedbackAluno`, escopo por vínculo) e verificado com teste dedicado, suíte 829/829 e gates de build/lint |
| C05 | Atendido | `specs/035.../design.md:43,45,50,52` avalia as decisões em acoplamento e responsabilidade única/SRP; `revisao-codigo.md:165-243` (§5) avalia encapsulamento, coesão e acoplamento |
| C06 | Atendido | Decisão registrada em "Desvios aceitos (não bloqueantes)" com justificativa; PEND-051 encerrado; spec `audited`; ADR-023 aceita a mudança de anafilaxia com contexto, decisão e consequências |

### Família D — tratamento de exceções (4/4)

| Item | Veredito | Evidência |
| --- | --- | --- |
| D01 | Atendido | `specs/007.../requirements.md:32-36,50` define entrada inválida (400), conflito/estado (409) e excepcional (500) com corpo `{ erro }`; `ErrorHandlingMiddleware.Mapear` implementa 400/409/500 e ainda 422 para diagnóstico de domínio, com log Warning (previsível) versus Error (excepcional) |
| D02 | Atendido | `Cardapio.Publicar` lança `TransicaoInvalidaException` (`Cardapio.cs:371-374`), que herda de `InvalidOperationException` (`TransicaoInvalidaException.cs:7`), traduzida em 409 pelo middleware; prova em `PublicacaoApiTests.Publicar_JaPublicado_Retorna409`; injeção controlada em `Program.cs:276-284` sob `IsEnvironment("Testing")` |
| D03 | Atendido | Caso default devolve 500 com `"Erro interno."` e registra a exceção com `TraceIdentifier`, sem stack no corpo; `ErroApiTests.ExcecaoNaoTratada_Retorna500SemStackTrace` (`:129`) verifica ausência de "Exception" e "stack"; 422 expõe apenas diagnóstico de domínio |
| D04 | Atendido | 41 asserções de `BadRequest` (ex.: `ErroApiTests.Aluno_Invalido_Retorna400ComErro`), 29 de `Conflict` (ex.: `ErroApiTests.ExcecaoDeConflito_Retorna409ComErro`, `PublicacaoApiTests.Publicar_JaPublicado_Retorna409`) e 1 de `InternalServerError` (`ErroApiTests.cs:129`); **0** asserções de `UnprocessableEntity` (lacuna registrada em `docs/av2/requisitos.md` §5) |

### Família E — depuração documentada (6/6)

Fonte: `docs/av2/depuracao.md` (caso `dotnet build` sem `APPDATA`/`ProgramFiles`, PEND-016).

| Item | Veredito | Evidência |
| --- | --- | --- |
| E01 | Atendido | §1 transcreve a falha real (`NuGet.targets(782,5): error : Value cannot be null. (Parameter 'path1')`, `exit=1`); §2 mede falha **3/3**; §8 entrega o comando de reprodução |
| E02 | Atendido | §2/§8 trazem as condições (`env -u APPDATA -u ProgramFiles`, `echo "[$APPDATA] [$ProgramFiles]"` → `[] []`), o observado incorreto (§1/§2) e o esperado demonstrado (§4/§8: `Compilação com êxito.`, `exit=0`) |
| E03 | Atendido | §5 descarta a hipótese "pasta de pacotes ausente" com `NUGET_PACKAGES` (`exit=1`) e isola a causa (só `APPDATA` falha, só `ProgramFiles` falha, ambas juntas passam; `--no-restore` compila sem as variáveis); §6 consolida a causa confirmada |
| E04 | Atendido | Inspeção de variáveis de ambiente (§2) e análise de log/rastreamento da execução (alvo `NuGet.targets(782,5)`, saída do restore, execuções controladas com exit code e tempo em §2/§4/§5) |
| E05 | Atendido | Causa em §6, correção em §3 (injetar as duas variáveis no processo do comando) e eliminação demonstrada em §4 (`exit=0` em 3/3) e §7 (build e 881/881 testes) |
| E06 | Atendido | Antes/depois medidos (§2 × §4) e comparação com seis alternativas executadas em §5 (`export` persistente, só `APPDATA`, só `ProgramFiles`, `NUGET_PACKAGES`, `--no-restore`, `cmd.exe //c`), com motivo de descarte; limitações em §9 |

### Família F — testes, build e rastreabilidade (5/6)

| Item | Veredito | Evidência |
| --- | --- | --- |
| F01 | Atendido | 10 cenários, dois por regra: R1 `Publicar_Rascunho_MarcaPublicadoComEvento` × `Publicar_JaPublicado_LancaTransicaoInvalida`; R2 `Construtor_ComDadosValidos_CriaAluno` × `Construtor_SemTurma_Lanca`; R3 `AtualizarPerfil_ComDadosValidos_NormalizaNomeEEmail` × `AtualizarPerfil_Invalido_NaoAlteraEstado`; R4 `Aprovar_Restricao_AplicaNoAlunoEGravaTrilha` × `Aprovar_DuasVezes_DevolveConflito`; R5 `NutricionistaComCrnValido_NaoBloqueia` × `NutricionistaSemCrn_RetornaMensagemDeBloqueio` |
| F02 | Atendido | `UsuarioTests.AtualizarPerfil_Invalido_NaoAlteraEstado` (`:77`) espera `ArgumentException` e confirma `Nome`/`Email` inalterados; equivalente em `AlunoTests.Construtor_ComMaisDe20Restricoes_Lanca` |
| F03 | Atendido | `SolicitacaoApiTests.Fluxo_AprovarRestricao_AplicaNoAlunoEGravaTrilha` (`:151`) cria, analisa e aprova via API (regra R4), e verifica persistência em `GET /api/aluno/perfil/{id}` (restrição "Lactose") e `GET /api/aluno/{id}/historico` (evento `RestricaoAdicionada` com autor) |
| F04 | **Não Atendido** | Clone limpo executado; seguindo `README.md:129`, o build falha (`path1`); `grep APPDATA README.md` → 0; com injeção não documentada, 870/870 testes (contra 881/881 na árvore, por alterações não commitadas). Detalhes na §2 |
| F05 | Atendido | `docs/av2/rastreabilidade.md` liga F1–F3, R1–R5, implementação e teste em 33 linhas, com 50 referências verificadas e script de checagem (§6). Ressalva: não versionado (`?? docs/`) |
| F06 | Atendido | `av2-avaliacao.md` traz veredito por item, evidência e 8 reprovações explícitas (§3), sem declarar concluído o não comprovado — a linha F06 denuncia o overclaim de 026/037, já corrigido (`roadmap.md`: 36 `audited` + 2 `discarded`); lacunas em `docs/av2/requisitos.md` §5 e PEND-028/PEND-049 abertos. Ressalva: auditoria datada de 17/11, desatualizada frente ao código de 18/11 |

### Família G — especificação e prompt para agente (2/2)

| Item | Veredito | Evidência |
| --- | --- | --- |
| G01 | Atendido | `docs/av2/requisitos.md` especifica F1–F3 (atores, pré-condições, fluxo principal, alternativas, pós-condições) e R1–R5 (condições, onde é imposta, exceções), com `CA-F1-1`…`CA-R5-3` e 57 referências de prova verificadas contra o código. Ressalva: não versionado |
| G02 | Atendido | `docs/av2/prompt-agente.md` descreve alteração concreta (CSV, ADR-012/Spec 019) com 11 achados de inspeção (I1–I11, com arquivo e linha), prompt executável com escopo fechado, RFs, gates e condições de parada, além do resultado da execução e do desvio registrado. Ressalva: não versionado |

---

## 4. Ressalvas que não reprovam

| Ressalva | Situação |
| --- | --- |
| `422` sem asserção em teste | `ErrorHandlingMiddleware` mapeia 422 para diagnóstico de domínio, mas há 0 asserções de `UnprocessableEntity`; declarado em `docs/av2/requisitos.md` §5 |
| `docs/av2/` e `av2-avaliacao.md` não versionados | `git status` → `?? docs/`, `?? av2-avaliacao.md`; ausentes em clone limpo |
| Frontend sem testes automatizados | PEND-049 aberto; critérios de UI não têm prova automatizada |
| Workaround de `APPDATA`/`ProgramFiles` fora do README | Fonte da reprovação F04; registrado em PEND-016 e em `docs/av2/depuracao.md` |
| Auditoria `av2-avaliacao.md` de 17/11 | Vereditos de A08/B03/E06/F05/G01/G02 anteriores às correções de 18/11 |

---

## 5. Como reproduzir

```bash
# 1. gates (o env é pré-requisito não documentado no README — ver F04)
env APPDATA="<seu %APPDATA%>" ProgramFiles='C:\Program Files' \
  dotnet build backend.Tests/backend.Tests.csproj -c Release
env APPDATA="<seu %APPDATA%>" ProgramFiles='C:\Program Files' \
  dotnet test backend.Tests/backend.Tests.csproj -c Release --no-build   # 881/881

# 2. clone limpo (evidência de F04)
git clone . /tmp/nutriedu-clone && cd /tmp/nutriedu-clone
dotnet build backend/backend.csproj        # exit 1, path1
ls docs/av2                                # ausente (não versionado)

# 3. referências de teste citadas nos documentos
for f in docs/av2/requisitos.md docs/av2/rastreabilidade.md; do
  grep -oE '`[A-Za-z]+Tests\.[A-Za-z0-9_]+`' "$f" | tr -d '`' | sort -u |
    while IFS=. read -r cls m; do
      grep -rqE "public [^(]* $m\(" backend.Tests --include="$cls.cs" || echo "FALTA $cls.$m"
    done
done   # saída vazia = todas as referências existem

# 4. cenários de falha por status (D04)
for s in BadRequest Conflict UnprocessableEntity InternalServerError; do
  printf "%-22s " "$s"; grep -rn "HttpStatusCode.$s" backend.Tests --include=*.cs | wc -l
done   # 41, 29, 0, 1
```

---

## 6. O que elevaria 38/40 para 40/40

| Item | Correção objetiva |
| --- | --- |
| B03 | Repassar nulabilidade (`?`) e genéricos nos 6 métodos de `Aluno` e `Cardapio` no `diagrama.md` §3.4 |
| F04 | Acrescentar ao `README.md` o pré-requisito de `APPDATA`/`ProgramFiles` (ou um script de build que injete as variáveis) e commitar as alterações pendentes para que o estado verificado coincida com o clone |
