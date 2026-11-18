# Avaliação AV2 — Design e Programação Orientados a Objetos

Consolidação das avaliações booleanas (Atendido / Não Atendido) dos **40 itens** das sete
famílias de critérios (A, B, C, D, E, F, G), com a nota calculada e a memória de cálculo.

**Data da avaliação:** 17/11/2026
**Repositório avaliado:** NutriEduc (`HEAD` em `10fb145`, 102 commits)
**Método:** cada item foi julgado com evidência direta em código, documentação ou execução de
gate. Não houve pontuação parcial: apenas `Atendido` ou `Não Atendido`. Itens com "ressalva"
foram aprovados porque a evidência existe, mas com uma lacuna registrada.

> **Aviso sobre a nota.** O repositório **não contém rubrica oficial** da disciplina. A única
> referência de pesos encontrada é `revisao-codigo.md` §2/§5, que cita dimensões A (10%), C (20%)
> e D (20%) de uma régua anterior — esquema diferente destes 40 itens. Por isso a nota abaixo usa
> **peso igual por item** e apresenta também o cálculo alternativo por grupo. A nota é uma
> estimativa derivada desta avaliação, não uma nota oficial.

---

## 1. Nota

### 1.1 Cálculo por item (base principal)

| Família | Itens | Atendidos | Não atendidos | Fração |
| --- | --- | --- | --- | --- |
| A — Domínio e POO no código | 8 | 7 | 1 | 87,5% |
| B — Modelo de classes e decisões | 8 | 6 | 2 | 75,0% |
| C — Análise de alteração (Git) | 6 | 6 | 0 | 100,0% |
| D — Tratamento de exceções | 4 | 4 | 0 | 100,0% |
| E — Depuração documentada | 6 | 5 | 1 | 83,3% |
| F — Testes, build e rastreabilidade | 6 | 4 | 2 | 66,7% |
| G — Especificação e prompt para agente | 2 | 0 | 2 | 0,0% |
| **Total** | **40** | **32** | **8** | **80,0%** |

**Nota final (peso igual por item): 32/40 = 80,0% → 8,0 em escala 0–10.**

Fórmula: `atendidos ÷ total × 10` → `32 ÷ 40 × 10 = 8,0`.

### 1.2 Cálculo alternativo (peso igual por família)

| Família | Fração | Peso |
| --- | --- | --- |
| A | 87,5% | 1/7 |
| B | 75,0% | 1/7 |
| C | 100,0% | 1/7 |
| D | 100,0% | 1/7 |
| E | 83,3% | 1/7 |
| F | 66,7% | 1/7 |
| G | 0,0% | 1/7 |

Média das sete frações: `(87,5 + 75,0 + 100,0 + 100,0 + 83,3 + 66,7 + 0,0) ÷ 7 = 73,2%`
→ **7,3 em escala 0–10**.

**Intervalo da nota: 7,3 a 8,0**, dependendo do peso por família ou por item.

---

## 2. Detalhamento dos 40 itens

### 2.1 Família A — Domínio e POO no código (7/8)

| Item | Veredito | Evidência |
| --- | --- | --- |
| A01 | Atendido | 55 arquivos em `backend/Domain/Entities/`; `Aluno.cs`, `Usuario.cs`, `Cardapio.cs` (616 l.), `Solicitacao.cs` (269 l.), `Ocorrencia.cs` (234 l.), `Escola.cs`, `Alimento.cs`. `grep "using Microsoft.EntityFrameworkCore" backend/Domain` = 0. |
| A02 | Atendido | `Cardapio.Publicar/Cancelar/CriarRevisao/AplicarAdaptacao` (`Cardapio.cs:367,381,398,192`); `Solicitacao.Transicionar` (`Solicitacao.cs:231`); `Ocorrencia.MudarStatus` (`Ocorrencia.cs:123`); `Aluno.AdicionarRestricao`; `Usuario.AtualizarPerfil`. |
| A03 | Atendido | 8 regras em uso: CRN na publicação; item bloqueante impede publicar; 366 dias (`PeriodoVigencia.DiasMaximos`); CPF 11 dígitos (`Usuario.cs:221`); máx. 20 restrições (`Aluno.cs:28`); transições válidas (`Solicitacao.cs:20-37`); exclusão bloqueada por vínculo; só o autor encerra feedback. |
| A04 | Atendido | `EfeitosDaSolicitacao.AplicarRestricao(Aluno, ...)` (`Domain/Services/EfeitosDaSolicitacao.cs:22`): `Solicitacao` (estado) → `EfeitosDaSolicitacao` (efeito) → `Aluno` (invariante). |
| A05 | Atendido | `Cardapio.Situacao`: Rascunho/Publicado/Cancelado/Substituido, com guarda em `Publicar` e `Cancelar`, preservando `Id`/`Versao`. |
| A06 | Atendido | `Solicitacao.Transicionar` valida antes de mutar (`Solicitacao.cs:231-236`); `SolicitacaoService:521` devolve `Conflito` **antes** de `ExecutarEmTransacao`. |
| A07 | Atendido | Zero `{ get; set; }` em `Domain/Entities`; `Aluno.Restricoes` como `IReadOnlyList` (ADR-006); `[ConcurrencyCheck]` em estado (ADR-010). |
| A08 | **Não Atendido** | Nenhum contrato com 2 implementações em produção: `IExportadorDeRelatorio` → só `ExportadorXlsx` (`Program.cs:82`); 22 interfaces de repositório com 1 implementação cada; `RespostaController` é `abstract` sem membro virtual/abstrato e 0 `override` nos 20 controllers. As segundas implementações são `*RepositoryFake` de teste. |

### 2.2 Família B — Modelo de classes e decisões (6/8)

| Item | Veredito | Evidência |
| --- | --- | --- |
| B01 | Atendido | `diagrama.md` §3: 15 entidades em Mermaid `erDiagram` (USUARIO, ALUNO, CARDAPIO, REFEICAO, ITEM_REFEICAO, SOLICITACAO, OCORRENCIA, FEEDBACK...). Não existe `docs/av2/`. |
| B02 | Atendido | Correspondência 1:1 com o código: ALUNO↔`Aluno.cs`, CARDAPIO↔`Cardapio.cs`, REFEICAO↔`Refeicao.cs`, ITEM_REFEICAO↔`ItemRefeicao.cs`, SOLICITACAO↔`Solicitacao.cs`. |
| B03 | **Não Atendido** | O modelo lista apenas atributos; nenhum método (`Cardapio.Publicar`, `Solicitacao.Transicionar`, `Aluno.AdicionarRestricao` ausentes). |
| B04 | Atendido | `ALUNO \|\|--o{ HISTORICO_ALUNO` e `CARDAPIO \|\|--o{ REFEICAO`, implementados em `ConnectionContext.cs:190-194` e `:422-426`. |
| B05 | Atendido | Composição por agregado com `OnDelete(DeleteBehavior.Cascade)` em Cardapio→Refeicao (`:426`) e Refeicao→ItemRefeicao (`:442`); chave composta `(RefeicaoId, AlimentoId)` em `:435-437`. |
| B06 | **Não Atendido** | Modelo não representa contrato polimórfico e o código não tem duas implementações (mesma causa de A08). |
| B07 | Atendido (ressalva) | Invariantes anotadas na própria classe: `ITEM_REFEICAO { QuantidadeGramas "máx 5000, 2 casas" }` ↔ `ItemRefeicao.GramasMaximas`; `CARDAPIO.ConjuntoRestricoesChave "índice único filtrado"` ↔ `ConnectionContext.cs:415`. Ressalva: não há seção dedicada a invariantes. |
| B08 | Atendido | `management/adr.md`: ADR-006 (coleção protegida × `List<string>` pública) e ADR-010 (`ConcurrencyCheck` × `rowversion`), ambos com contexto, decisão e consequências; ADR-007 registra alternativa rejeitada (`:143`). |

### 2.3 Família C — Análise de alteração (6/6)

| Item | Veredito | Evidência |
| --- | --- | --- |
| C01 | Atendido | Commit `644deac` com diff verificável: `git show 644deac -- backend/Domain/Entities/Solicitacao.cs` retorna `+ public bool Anafilaxia { get; private set; }`; migration `20261113142544_SolicitacaoAnafilaxia`. |
| C02 | Atendido | Entrada "Anafilaxia informada no pedido de alergia" em `management/cross-session/README.md` liga requisito ao código (`SolicitacaoService.Aprovar` → `MarcarAnafilaxia`). |
| C03 | Atendido | A mesma entrada nomeia cada artefato e sua finalidade; a entrada da Spec 039 identifica mudança fora do escopo (remoção de itens do `Navbar` e da guarda de `/alimentos`) e registra "Backend: sem mudança de produção". |
| C04 | Atendido | Impacto e verificação registrados: efeito na mesma transação + trilha `Solicitacao:<id>`; gates "830/830", build e lint; `arch.md` §10 consolida 870/870. |
| C05 | Atendido | `revisao-codigo.md` §5 avalia encapsulamento (§5.2), coesão/responsabilidades (§5.1, §5.5) e acoplamento (§4.4); complementam `lessons.md` L2/L4/L10. |
| C06 | Atendido | `pending-items.md` PEND-045 ("Decisão do dono (opção A, 2026-11-10)") com implementação e testes; PEND-028 com rejeição justificada; ADRs com "Status: aceito". |

### 2.4 Família D — Tratamento de exceções (4/4)

| Item | Veredito | Evidência |
| --- | --- | --- |
| D01 | Atendido | `ErrorHandlingMiddleware.Mapear` separa: regra/estado → 409/422 (`PublicacaoSobreposta`, `PublicacaoBloqueada`, `GeracaoImpossivel`, `LimiteDeAdaptacoes`); entrada inválida → 400 (`ArgumentException`); excepcional → `DbUpdateConcurrencyException` 409 e `_ => 500 "Erro interno."`. |
| D02 | Atendido | `Cardapio.Publicar` lança `TransicaoInvalidaException` (`Cardapio.cs:372`) → middleware (`Program.cs:268`) → 409; `SolicitacaoService:521` captura → `Conflito`; teste `Aprovar_DuasVezes_DevolveConflito` (`SolicitacaoServiceTests:280`). |
| D03 | Atendido | Caso default devolve `"Erro interno."`, log com `context.TraceIdentifier`, `Response.Clear()`; teste `ErroApiTests.ExcecaoNaoTratada_Retorna500SemStackTrace` verifica ausência de "Exception" e "stack". |
| D04 | Atendido (ressalva) | 400: `AlimentoApiTests:144,163,203`; 409: `SolicitacaoServiceTests:280,553`; 500: `ErroApiTests:135`. 69 asserções de 400/409/422. Ressalva: nenhum teste assere **422** especificamente. |

### 2.5 Família E — Depuração documentada (5/6)

| Item | Veredito | Evidência |
| --- | --- | --- |
| E01 | Atendido | `specs/032/requirements.md` tabela P1–P8 com arquivo:linha e sintoma; PEND-028 com 3 execuções (`roadmapStatusBefore: null`). |
| E02 | Atendido | PEND-028: condição (`#` de 3 dígitos), esperado (resolver status), observado (`null`); Spec 032: esperado em RF5–RF7 × observado em P1. |
| E03 | Atendido | PEND-028 confirma a hipótese do parser por simulação e descarta a concorrente ("`#` sem zeros resolvem; com zeros não"). |
| E04 | Atendido | `lessons.md` L16: inspeção de timestamp de `backend.dll` e leitura de `MSB3027`/`MSB3021`; PEND-016: causa raiz via `ArgumentNullException("path1")`. Logs preservados em `.dscode/logs/`. Sem evidência de breakpoint. |
| E05 | Atendido | PEND-051: defeito RF20 + correção + teste `Criar_InclusaoAlergiaDuplicada_Retorna409ComIdExistente`; Spec 032 + `SeletorDeRefeicao`/`ValidadorDeCardapioGerado` validados por testes e 870/870. |
| E06 | **Não Atendido** | Nenhum registro combina antes/depois da correção **com** comparação entre a técnica usada e uma alternativa. PEND-016 tem antes/depois sem comparar técnicas; L16 compara técnicas (`lessons.md:237`, "`dotnet test` usa outro binário e não prova o processo") sem verificação "depois". `docs/av2/` inexistente. |

### 2.6 Família F — Testes, build e rastreabilidade (4/6)

| Item | Veredito | Evidência |
| --- | --- | --- |
| F01 | Atendido (ressalva) | Pares sucesso/violação em 5 regras: `CardapioTransicaoTests.Publicar_Rascunho...` × `Publicar_JaPublicado_LancaTransicaoInvalida`; `AlunoTests.Construtor_ComDadosValidos...` × `Construtor_SemTurma_Lanca`; `UsuarioTests` perfil válido × `AtualizarPerfil_Invalido_NaoAlteraEstado`; `Aprovar_DuasVezes_DevolveConflito`; `CrnDeNutricionistaTests`/`PoliticaDeCrnTests`. 757 `[Fact]` + 35 `[Theory]`. Ressalva: o conjunto formal das "cinco regras" não está definido (ver G01). |
| F02 | Atendido | `UsuarioTests.cs:76-87`: `Assert.Throws<ArgumentException>` e depois `Assert.Equal("Maria Silva", usuario.Nome)` / `Assert.Equal("maria@exemplo.com", usuario.Email)`. |
| F03 | Atendido | `SolicitacaoApiTests.cs:151` `Fluxo_AprovarRestricao_AplicaNoAlunoEGravaTrilha`: POST aluno, POST solicitação, aprova, `GET /api/aluno/perfil/{id}` verifica `restricoes` e `GET /historico` verifica trilha — SQLite real via `ApiFactory`. |
| F04 | Atendido (ressalva) | Executado: `dotnet build backend.Tests -c Release` → 0 erros; `dotnet test -c Release --no-build` → **870/870 aprovados em 46 s**. Ressalvas: não houve `git clone` literal; este shell exigiu `env APPDATA=... ProgramFiles=...` (workaround documentado só em PEND-016, ausente do `README.md`). |
| F05 | **Não Atendido** | Única matriz de rastreabilidade em `specs/035/requirements.md:329`, colunas `Requisito \| Value block \| Componente/contrato \| Task` — **sem coluna de testes**. Nenhum artefato liga fluxo → regra → implementação → teste. |
| F06 | **Não Atendido** | Existem auditorias (`arch.md` §10, `pending-items.md`, `cross-session`), mas `roadmap.md` marca `026` e `037` como `audited` enquanto a própria nota de estado admite "não têm implementação no repositório" — declarar concluído o não comprovado. |

### 2.7 Família G — Especificação e prompt para agente (0/2)

| Item | Veredito | Evidência |
| --- | --- | --- |
| G01 | **Não Atendido** | Não existe especificação dos "três fluxos" e das "cinco regras" como conjunto verificável. Há 38 `requirements.md` com RF/RNF e critérios de aceitação, mas nenhum recorte AV2; `docs/av2/` não existe (`ls docs` → "No such file or directory"). |
| G02 | **Não Atendido** | `ls management/specs/*/prompt*` vazio. `templates/prompts/*.ejs` são templates genéricos do pipeline (`spec_new`, `spec_implement`, `spec_audit`), não spec de alteração concreta. `.dscode/` tem só estado (`state.db`, `file-index.db`, `logs/`) e `.dscode/sdd-executions/` não existe. |

---

## 3. Lacunas que reprovam (8 itens)

| Item | O que falta | Correção objetiva |
| --- | --- | --- |
| A08 / B06 | Contrato com duas implementações reais | Introduzir estratégia de domínio com 2 implementações em produção (ex.: `ISugestorDeSubstituicao` com `SugestorDeterministico` e um segundo), trocada por DI e usada em serviço — hoje a Spec 026 está declarada e sem código. |
| B03 | Métodos no modelo | Adicionar às classes de `diagrama.md` §3 os métodos públicos relevantes (ex.: `Publicar()`, `Cancelar()`, `Transicionar()`, `AdicionarRestricao()`). |
| E06 | Antes/depois + comparação de técnicas | Registrar um caso único de depuração com: sintoma, técnica aplicada, alternativa descartada e resultado medido antes e depois. |
| F05 | Matriz com testes | Acrescentar coluna "Teste" à tabela de rastreabilidade e cobrir fluxo → regra → implementação → teste. |
| F06 | Auditoria sem overclaim | Reverter 026 e 037 para `planned` no `roadmap.md` (a ressalva já existe; o status contradiz). |
| G01 | Spec dos 3 fluxos + 5 regras | Criar `docs/av2/requisitos.md` com os três fluxos, as cinco regras, condições e critérios de aceitação. |
| G02 | Spec/prompt para agente | Criar `docs/av2/prompt-agente.md` associado a uma alteração funcional, com evidência de inspeção. |
| (F04) | Instrução de build | Levar o workaround de `APPDATA`/`ProgramFiles` (PEND-016) para o `README.md`. |

---

## 4. Como reproduzir esta avaliação

```bash
# compilação e testes (gates usados em F04)
env APPDATA="C:\\Users\\<usuario>\\AppData\\Roaming" ProgramFiles="C:\\Program Files" \
  dotnet build backend.Tests/backend.Tests.csproj -c Release
env APPDATA="..." ProgramFiles="C:\\Program Files" \
  dotnet test backend.Tests/backend.Tests.csproj -c Release --no-build

# contagem de cenários de teste (F01)
grep -rho "\[Fact\]" backend.Tests --include=*.cs | wc -l      # 757
grep -rho "\[Theory\]" backend.Tests --include=*.cs | wc -l    # 35

# evidências de histórico e documentação (C, E)
git show --stat 644deac
ls management/specs/*/prompt*        # vazio → G02
ls docs 2>/dev/null                  # inexistente → B01/G01
```

---

## 5. Resumo em uma linha

**32 de 40 critérios atendidos (80,0% → nota 8,0 por item; 73,2% → nota 7,3 por família).**
As famílias com reprovação são **A08/B06 (polimorfismo)**, **B03 (métodos no modelo)**,
**E06 (comparação de técnicas de depuração)**, **F05/F06 (rastreabilidade e auditoria sem
overclaim)** e **G (especificação e prompt para agente)** — todas corrigíveis com documentação
e uma estratégia de domínio real, sem reescrita da arquitetura.
