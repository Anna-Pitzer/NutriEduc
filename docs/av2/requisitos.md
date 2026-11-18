# Requisitos AV2 — três fluxos e cinco regras

Recorte verificável do sistema: **3 fluxos** de negócio, **5 regras** centrais de domínio,
condições de execução e critérios de aceitação com a prova automatizada de cada um.

As 38 specs de `management/specs/` continuam sendo o contrato por módulo (RF/RNF, design e
tarefas). Este documento **não as substitui**: ele fixa o recorte da avaliação e evita que
"três fluxos" e "cinco regras" existam apenas na fala.

| Campo | Valor |
| --- | --- |
| Base | commit `9544088`, árvore de trabalho com alterações não commitadas |
| Data | 2026-11-18 |
| Escopo | backend (`backend/Domain/`, `backend/API/Services/`) + contrato HTTP |
| Matriz de testes | `docs/av2/rastreabilidade.md` (fluxo → regra → implementação → teste) |
| Prova global | `dotnet test backend.Tests -c Release` → 881/881 |

**Convenções**

- `CA-Fn-x` é critério de aceitação do fluxo `Fn`; `CA-Rx-n` é critério da regra `Rx`.
- **Prova** é `Classe.Metodo` em `backend.Tests/`; o caminho do arquivo está em
  `docs/av2/rastreabilidade.md` §5. Critério sem teste recebe **lacuna declarada** em vez de
  prova inventada.
- Códigos HTTP citados são os do contrato real (`RespostaController` + `ErrorHandlingMiddleware`).

---

## 1. Fluxo F1 — Cadastro e manutenção do aluno com restrições

**Objetivo:** manter o cadastro do aluno que o cardápio adaptado e os relatórios consomem.

| Item | Descrição |
| --- | --- |
| Atores | `AdminSecretaria` escreve `Aluno` (matriz da Spec 028: `RW`); `AdminEscola`, `Nutricionista` e `Merendeira` leem (`R`); `Responsavel` lê apenas os próprios filhos (`S`) |
| Pré-condições | sessão válida; nome, nascimento, turma, responsável e `escolaId` > 0 informados (a **existência** da escola é verificada fora do domínio — `Aluno.ValidarEscola` só rejeita id ≤ 0) |
| Pós-condições | aluno persistido; restrições normalizadas e sem duplicata; evento de criação em `HistoricoAluno` |
| Regras aplicadas | **R2** (invariantes do aluno), **R3** (perfil do usuário) |

**Fluxo principal**

1. `POST /api/aluno` com nome, nascimento, turma, responsável, telefone, observação, restrições, anafilaxia e escola.
2. O domínio valida tudo **antes** de atribuir qualquer campo (buffer de restrições).
3. O aluno é persistido e a trilha registra a criação.
4. A resposta é `201` com `Location`.

**Fluxos alternativos**

| Situação | Resposta | Prova |
| --- | --- | --- |
| Sem sessão | `401` sem dados | `AlunoApiTests.Endpoints_SemSessao_Retornam401SemDados` |
| Nome/nascimento/turma inválidos | `400` com `{ erro }` | `AlunoApiTests.Post_ComDadosInvalidos_Retorna400ComErro` |
| `PUT` em aluno inexistente | `404` | `AlunoApiTests.Put_ComIdInexistente_Retorna404` |
| Papel sem escrita em `Aluno` | `403` | matriz da Spec 028 (`MatrizDePermissoesTests`) |

**Critérios de aceitação**

| ID | Critério | Prova |
| --- | --- | --- |
| CA-F1-1 | Aluno válido é criado com `201` e `Location` | `AlunoApiTests.Post_ComDadosValidos_Retorna201ComLocation` |
| CA-F1-2 | Aluno sem turma **ou** sem responsável é rejeitado e não é criado | `AlunoTests.Construtor_SemTurma_Lanca`, `AlunoTests.Construtor_SemResponsavel_Lanca` |
| CA-F1-3 | Três restrições informadas são persistidas e relidas na consulta | `AlunoApiTests.Post_ComTresRestricoes_GetDevolveOsValoresPersistidos` |
| CA-F1-4 | Restrição é normalizada e duplicata é rejeitada | `AlunoTests.AdicionarRestricao_NormalizaEEspecificaOValor`, `AlunoTests.AdicionarRestricao_Duplicada_Lanca` |
| CA-F1-5 | A 21ª restrição é rejeitada (limite de 20) | `AlunoTests.Construtor_ComMaisDe20Restricoes_Lanca` |
| CA-F1-6 | Alteração registra evento com valor anterior e novo; sem mudança não registra | `HistoricoAlunoApiTests.Historico_AlteracaoDeTelefone_RegistraCampoComValores`, `HistoricoAlunoApiTests.Historico_AtualizacaoSemMudanca_NaoRegistraEvento` |
| CA-F1-7 | Perfil com e-mail inválido é rejeitado sem alterar o estado | `UsuarioTests.AtualizarPerfil_Invalido_NaoAlteraEstado` |

---

## 2. Fluxo F2 — Geração, adaptação e publicação de cardápio

**Objetivo:** transformar parâmetros e catálogo em cardápio oficial publicado, com versão
adaptada por escola e histórico.

| Item | Descrição |
| --- | --- |
| Atores | nutricionista **ativa, do quadro da unidade e com CRN válido** (cria, gera, adapta, publica); merendeira e família apenas leem o publicado |
| Pré-condições | unidade com alunos; catálogo de alimentos ativo; período de até **366 dias** (`PeriodoVigencia.DiasMaximos`) |
| Pós-condições | `Cardapio.Situacao = Publicado`; evento em `HistoricoCardapio`; versões adaptadas publicadas; notificação às merendeiras no escopo |
| Regras aplicadas | **R1** (transições do cardápio), **R5** (CRN) |

**Fluxo principal**

1. Rascunho criado (`POST /api/cardapios`) ou gerado (`POST /api/geracao`).
2. Adaptação por escola com alunos restritos (`POST /api/cardapios/{id}/adaptar`), que consome `AnaliseCompatibilidade`.
3. Publicação (`POST /api/cardapios/{id}/publicar`): situação `Rascunho` → `Publicado`, com evento e notificação.
4. Revisão de um publicado (`POST .../revisoes`) cria nova versão e marca a anterior como substituída.

**Fluxos alternativos**

| Situação | Resposta | Prova |
| --- | --- | --- |
| Publicar cardápio já publicado/fora de rascunho | `409` | `PublicacaoApiTests.Publicar_JaPublicado_Retorna409` |
| Período sobreposto a outro publicado | `409` com o id do cardápio | `PublicacaoApiTests.Publicar_PeriodoSobreposto_Retorna409ComCardapioId` |
| Nutricionista sem CRN válido | `400` com a mensagem da política | `PoliticaDeCrnTests.NutricionistaSemCrn_RetornaMensagemDeBloqueio` |
| Adaptado pendente/desatualizado bloqueia o geral | `409` | código em `PublicacaoCardapioService`; **sem teste dedicado** (lacuna §5) |
| Item alérgico sem confirmação | `422` | contrato em `diagrama.md` §4; **sem teste de 422** (lacuna §5) |

**Critérios de aceitação**

| ID | Critério | Prova |
| --- | --- | --- |
| CA-F2-1 | Publicar rascunho marca `Publicado` e registra evento | `PublicacaoApiTests.Publicar_Rascunho_RetornaPublicado`, `CardapioTransicaoTests.Publicar_Rascunho_MarcaPublicadoComEvento` |
| CA-F2-2 | Publicar duas vezes é rejeitado com `409` | `CardapioTransicaoTests.Publicar_JaPublicado_LancaTransicaoInvalida`, `PublicacaoApiTests.Publicar_JaPublicado_Retorna409` |
| CA-F2-3 | Cancelar exige motivo de 2 a 500 caracteres | `CardapioTransicaoTests.Cancelar_MotivoCurto_LancaArgumento`, `PublicacaoApiTests.Cancelar_SemMotivo_Retorna400` |
| CA-F2-4 | Revisão de publicado incrementa a versão e mantém o original | `CardapioTransicaoTests.CriarRevisao_IncrementaVersaoEMantemOriginal`, `PublicacaoApiTests.Revisao_CriaNovaVersao` |
| CA-F2-5 | Alergia gera achado **bloqueante**; restrição gera alerta não bloqueante | `AnaliseCompatibilidadeTests.Alergia_DeAluno_GeraBloqueante`, `AnaliseCompatibilidadeTests.Restricao_DeAluno_GeraAlertaNaoBloqueante` |
| CA-F2-6 | Análise de compatibilidade é determinística | `AnaliseCompatibilidadeTests.DuasExecucoes_ProduzemMesmaOrdenacao` |
| CA-F2-7 | Publicar com alérgico e adaptado atualizado publica geral **e** adaptado | `PublicacaoApiTests.Publicar_ComAlergenicoEAdaptadoAtualizado_PublicaGeralEAdaptado` |

---

## 3. Fluxo F3 — Solicitações, ocorrências e feedback

**Objetivo:** receber pedidos da ponta (responsável, merendeira, escola), decidir **uma única
vez** e refletir o efeito com trilha append-only.

| Item | Descrição |
| --- | --- |
| Atores | responsável **vinculado** ao aluno cria solicitação/ocorrência/feedback; merendeira e escola registram; nutricionista decide solicitações de restrição |
| Pré-condições | vínculo responsável↔aluno existente (Spec 034); solicitação em `EmAnalise` para ser decidida; ocorrência `Aberta` para ser cancelada no mesmo dia |
| Pós-condições | decisão registrada com autor e data; efeito aplicado no aluno na mesma transação; histórico append-only |
| Regras aplicadas | **R4** (decisão única da solicitação) + regras de apoio da ocorrência e do feedback |

**Fluxo principal**

1. Criação (`POST /api/solicitacoes`) → status `Aberta`, com histórico.
2. Análise (`POST /api/solicitacoes/{id}/analise`) → `EmAnalise`.
3. Decisão (`POST /api/solicitacoes/{id}/aprovar` ou `/recusar`) → `Aprovada`/`Recusada`; aprovar restrição aplica `EfeitosDaSolicitacao.AplicarRestricao` no aluno e grava trilha.
4. Conclusão (`POST /api/solicitacoes/{id}/concluir`) → `Concluida`.

**Regras de apoio (ocorrência e feedback)**

- `Ocorrencia.MudarStatus` aceita apenas `Aberta→EmAnalise`, `EmAnalise→Resolvida`, `Resolvida→Arquivada` e `EmAnalise→Aberta`; qualquer outra é `TransicaoInvalidaException` (`Ocorrencia.cs`).
- `Ocorrencia.Cancelar` exige status `Aberta`, **mesmo dia local** da criação e autor.
- `Feedback.Responder` só a partir de `Aberto`; `Feedback.Encerrar` só a partir de `Respondido`; só o autor encerra (serviço).

**Fluxos alternativos**

| Situação | Resposta | Prova |
| --- | --- | --- |
| Responsável sem vínculo cria solicitação | `403` | `SolicitacaoApiTests.Criar_PorResponsavelSemVinculo_Retorna403`, `SolicitacaoServiceTests.Criar_Restricao_ResponsavelSemVinculo_Proibido` |
| Decidir fora de `EmAnalise` | `409` | `SolicitacaoApiTests.Aprovar_SemAnalise_Retorna409` |
| Recusar sem resposta | `400` | `SolicitacaoApiTests.Recusar_SemResposta_Retorna400` |
| Transição inválida de ocorrência | `409` | `OcorrenciaApiTests.TransicaoInvalida_409` |
| Cancelar ocorrência de outra pessoa | `403` | `OcorrenciaApiTests.Cancelar_PorNaoAutor_403` |

**Critérios de aceitação**

| ID | Critério | Prova |
| --- | --- | --- |
| CA-F3-1 | Responsável vinculado cria solicitação `Aberta`; sem vínculo é `403` | `SolicitacaoServiceTests.Criar_Restricao_ComoResponsavelVinculado_CriaAberta`, `SolicitacaoApiTests.Criar_PorResponsavelSemVinculo_Retorna403` |
| CA-F3-2 | Aprovar restrição aplica no aluno e grava trilha | `SolicitacaoServiceTests.Aprovar_Restricao_AplicaNoAlunoEGravaTrilha`, `SolicitacaoApiTests.Fluxo_AprovarRestricao_AplicaNoAlunoEGravaTrilha` |
| CA-F3-3 | Segunda decisão é conflito | `SolicitacaoServiceTests.Aprovar_DuasVezes_DevolveConflito`, `SolicitacaoApiTests.Aprovar_SemAnalise_Retorna409` |
| CA-F3-4 | Ocorrência segue a sequência canônica e rejeita transição direta | `OcorrenciaTests.Fluxo_Valido_AnalisaResolveArquiva`, `OcorrenciaTests.Transicao_DiretaDeAberta_Lanca`, `OcorrenciaApiTests.MudarStatus_AdminEscola_GravaHistorico` |
| CA-F3-5 | Ocorrência só é cancelada pelo autor, no mesmo dia | `OcorrenciaTests.Cancelar_NoMesmoDia_Funciona`, `OcorrenciaTests.Cancelar_EmOutroDia_Lanca`, `OcorrenciaApiTests.Cancelar_PorNaoAutor_403` |
| CA-F3-6 | Feedback não é respondido duas vezes nem encerrado aberto | `FeedbackTests.Responder_DuasVezes_Lanca`, `FeedbackTests.Encerrar_Aberto_Lanca` |
| CA-F3-7 | Só o autor encerra o feedback respondido | `FeedbackTests.Encerrar_Respondido_MarcaEncerrado`, `FeedbackApiTests.Nutricionista_Responde_AutorEncerra` |

---

## 4. As cinco regras

### R1 — Transição de situação do cardápio

**Enunciado:** a situação do cardápio só muda por operação explícita, a partir do estado
correto, e cada mudança deixa evento.

**Condições**

| Operação | Só é permitida quando | Exige | Falha |
| --- | --- | --- | --- |
| `Publicar` | `Situacao = Rascunho` | autor (nome) e unidade válida | `409` se já publicado/fora de rascunho |
| `Cancelar` | `Situacao ∉ {Cancelado, Substituido}` | motivo de **2 a 500** caracteres | `400` motivo inválido; `409` já encerrado |
| `CriarRevisao` | `Tipo = Geral` **e** `Situacao = Publicado` | — | `409` caso contrário |

**Onde é imposta:** `backend/Domain/Entities/Cardapio.cs` (`Publicar`, `Cancelar`, `CriarRevisao`); o estado inicial é `Rascunho` no construtor.
**Exceção:** cancelar um `Publicado` é permitido (gera evento e mantém histórico); só `Cancelado` e `Substituido` são terminais.

| ID | Critério | Prova |
| --- | --- | --- |
| CA-R1-1 | Só rascunho publica | `CardapioTransicaoTests.Publicar_Rascunho_MarcaPublicadoComEvento`, `CardapioTransicaoTests.Publicar_JaPublicado_LancaTransicaoInvalida` |
| CA-R1-2 | Encerrado não volta a ser cancelado | `CardapioTransicaoTests.Cancelar_JaEncerrado_LancaTransicaoInvalida` |
| CA-R1-3 | Revisão só de publicado e incrementa versão | `CardapioTransicaoTests.CriarRevisao_IncrementaVersaoEMantemOriginal` |

### R2 — Invariantes do aluno

**Enunciado:** nenhum aluno existe sem turma e responsável, com dados fora dos limites e
restrições inconsistentes.

**Condições**

| Campo | Regra | Falha |
| --- | --- | --- |
| Nome | 2 a 100 caracteres | `ArgumentException` → `400` |
| Nascimento | não futuro e não anterior a 1900 | idem |
| Turma / Responsável | obrigatórios | idem |
| `escolaId` | maior que zero (`ValidarEscola`) | `ArgumentException` → `400` |
| Observação | até 500 caracteres | idem |
| Restrições | no máximo 20; não vazias; normalizadas; sem duplicata | idem |

**Onde é imposta:** `backend/Domain/Entities/Aluno.cs` (construtor, `AtualizarDados`, `AdicionarRestricao`, `RemoverRestricao`). O construtor valida em buffer e **não atribui nada** se houver violação.
**Exceção:** `RemoverRestricao` devolve `false` em vez de lançar quando o valor não existe; `MarcarAnafilaxia` é idempotente.

| ID | Critério | Prova |
| --- | --- | --- |
| CA-R2-1 | Turma e responsável são obrigatórios | `AlunoTests.Construtor_SemTurma_Lanca`, `AlunoTests.Construtor_SemResponsavel_Lanca` |
| CA-R2-2 | Limite de 20 restrições é aplicado | `AlunoTests.Construtor_ComMaisDe20Restricoes_Lanca` |
| CA-R2-3 | Restrição é normalizada e não duplica | `AlunoTests.AdicionarRestricao_NormalizaEEspecificaOValor`, `AlunoTests.AdicionarRestricao_Duplicada_Lanca` |
| CA-R2-4 | A lista exposta não é mutável de fora | `AlunoTests.Restricoes_NaoExpoeListaMutavel` |

### R3 — Perfil do usuário

**Enunciado:** dados de perfil só mudam se forem válidos; entrada inválida preserva o estado
anterior.

**Condições**

| Campo | Regra |
| --- | --- |
| Nome | 2 a 100 caracteres |
| E-mail | até 150 caracteres, formato válido, normalizado em minúsculas |
| Telefone | normalizado para dígitos; quantidade válida |
| Foto | PNG válido, até 5 MB |

**Onde é imposta:** `backend/Domain/Entities/Usuario.cs` (`AtualizarPerfil`, `AtualizarTelefone`, `AtualizarFoto`, `DefinirSenha`).
**Exceção:** `DefinirPapel` e `DefinirCrn` têm regras próprias (papel × escola/unidade; CRN só de nutricionista).

| ID | Critério | Prova |
| --- | --- | --- |
| CA-R3-1 | Perfil inválido não altera o estado | `UsuarioTests.AtualizarPerfil_Invalido_NaoAlteraEstado` |
| CA-R3-2 | Perfil válido normaliza nome e e-mail | `UsuarioTests.AtualizarPerfil_ComDadosValidos_NormalizaNomeEEmail` |
| CA-R3-3 | Senha é gravada como hash com prefixo e conferida por função | `UsuarioTests.DefinirSenha_ComValorValido_PersisteHashComPrefixo`, `UsuarioTests.SenhaConfere_ComSenhaIncorreta_RetornaFalse` |

### R4 — Decisão única da solicitação

**Enunciado:** uma solicitação percorre a máquina de estados e só pode ser decidida uma vez.

**Condições**

| Estado atual | Pode ir para |
| --- | --- |
| `Aberta` | `EmAnalise`, `Cancelada` |
| `EmAnalise` | `Aprovada`, `Recusada` |
| `Aprovada` | `Concluida` |

Aprovar/recusar exigem resposta de **2 a 500** caracteres. Decidir fora de `EmAnalise` é
`TransicaoInvalidaException` → `409`.

**Onde é imposta:** `backend/Domain/Entities/Solicitacao.cs` (tabela `TransicoesPermitidas`, linha 20, e `Transicionar` privado, linha 225) + `backend/API/Services/SolicitacaoService.cs`.
**Exceção:** cancelar é permitido a partir de `Aberta` e `EmAnalise`; a aprovação de restrição é a única que altera o aluno, e o faz na mesma transação.

| ID | Critério | Prova |
| --- | --- | --- |
| CA-R4-1 | Segunda decisão devolve conflito | `SolicitacaoServiceTests.Aprovar_DuasVezes_DevolveConflito`, `SolicitacaoApiTests.Aprovar_SemAnalise_Retorna409` |
| CA-R4-2 | Decisão exige resposta válida | `SolicitacaoServiceTests.Recusar_SemResposta_Invalido`, `SolicitacaoApiTests.Recusar_SemResposta_Retorna400` |
| CA-R4-3 | Aprovação aplica o efeito no aluno com trilha | `SolicitacaoServiceTests.Aprovar_Restricao_AplicaNoAlunoEGravaTrilha` |

### R5 — Publicação exige CRN válido

**Enunciado:** cardápios só são criados, gerados, adaptados ou publicados por nutricionista
**ativa**, **do quadro da unidade** e com **CRN válido**.

**Condições**

| Verificação | Regra | Falha |
| --- | --- | --- |
| Papel | `Nutricionista` | `400` "não exerce o papel" |
| Ativo | `Ativo = true` | `400` "inativa" |
| Unidade | mesma unidade do cardápio | `400` "outra unidade" |
| CRN | formato `CRN-<região>-<número>`: região na faixa configurada (padrão 1–11), número com 3 a 8 dígitos | `400` com a mensagem da política |

A entrada aceita `CRN 3 12345`, `crn-3-12345` e `CRN.3.12345` e devolve a forma canônica
`CRN-3-12345`. A validação é **de formato**, nunca de autenticidade no conselho.

**Onde é imposta:** `backend/Domain/Services/PoliticaDeCrn.cs` (predicado único) e
`backend/Domain/Services/ValidacaoDeNutricionista.cs` (`Motivo`), consumidos por
`CardapioService`, `GeracaoCardapioService`, `AdaptarCardapioService` e `PublicacaoCardapioService`.
**Exceção:** login não é afetado — nutricionista legada sem CRN autentica, mas não opera cardápio; papéis que não são nutricionista não são bloqueados por esta regra.

| ID | Critério | Prova |
| --- | --- | --- |
| CA-R5-1 | Sem CRN válido, há bloqueio com mensagem única | `PoliticaDeCrnTests.NutricionistaSemCrn_RetornaMensagemDeBloqueio`, `PoliticaDeCrnTests.NutricionistaComCrnInvalido_RetornaMensagemDeBloqueio` |
| CA-R5-2 | CRN válido não bloqueia; papel diferente não é afetado | `PoliticaDeCrnTests.NutricionistaComCrnValido_NaoBloqueia`, `PoliticaDeCrnTests.OutroPapel_NaoBloqueia` |
| CA-R5-3 | Formato é normalizado e faixa de regiões é respeitada | `CrnDeNutricionistaTests.TryParse_AceitaVariacoes_Normaliza`, `CrnDeNutricionistaTests.TryParse_RejeitaFormatoInvalido` |

---

## 5. Lacunas declaradas

| Lacuna | Impacto no recorte | Ação |
| --- | --- | --- |
| `422` de publicação com item alérgico não tem teste dedicado | CA de F2 para `422` não existe; o contrato está em `diagrama.md` §4 | Criar teste que publique item bloqueante sem confirmação e asserte `422` |
| Bloqueio por adaptado pendente/desatualizado sem teste dedicado | alternativa de F2 sem prova | Testar publicação de geral com adaptado pendente |
| Revisão de cardápio adaptado e revisão de rascunho sem teste dedicado | condição de R1 sem prova negativa | Testar `CriarRevisao` em adaptado e em rascunho |
| Leitura de ocorrências do filho pelo responsável (RF7–RF10 da Spec 035) | F3 tem criação e negação, não leitura positiva | Criar teste de listagem por responsável vinculado |
| Telas dos três fluxos | não há teste de UI (PEND-049) | Introduzir runner de frontend |

## 6. Rastreabilidade deste recorte

- Fluxo/regra → implementação → teste: `docs/av2/rastreabilidade.md` §3 (33 linhas, 50 testes referenciados e verificados).
- Requisito por módulo: `management/specs/<N>-<nome>/requirements.md` (fonte canônica).
- Decisões de arquitetura citadas: ADR-004 (domínio valida), ADR-006 (coleção protegida), ADR-010 (concorrência), ADR-011 (compatibilidade pura), ADR-012 (exportador plugável).

## 7. Como verificar este documento

```bash
cd <raiz do repositório>

# 1. toda prova citada existe?
grep -oE '`[A-Za-z]+Tests\.[A-Za-z0-9_]+`' docs/av2/requisitos.md | tr -d '`' | sort -u |
  while IFS=. read -r cls m; do
    grep -rqE "public [^(]* $m\(" backend.Tests --include="$cls.cs" && echo "OK $cls.$m" || echo "FALTA $cls.$m"
  done

# 2. a suíte que sustenta os critérios passa
env APPDATA="<seu %APPDATA%>" ProgramFiles='C:\Program Files' \
  dotnet test backend.Tests/backend.Tests.csproj -c Release
```
