# Rastreabilidade — fluxo → regra → implementação → teste

Matriz que liga cada **fluxo** de negócio à **regra** que o governa, ao código que a implementa e
ao **teste automatizado** que prova o comportamento. Complementa as matrizes por spec
(`management/specs/*/requirements.md`, seção *Traceability*), que ligam requisito → componente →
task, mas não apontavam o teste.

Os enunciados, condições e critérios de aceitação dos três fluxos e das cinco regras estão em
`docs/av2/requisitos.md`; esta matriz é a prova de execução deles.

**Escopo:** os três fluxos e as cinco regras centrais do domínio. Levantado em 2026-11-18 a partir
de `backend/Domain/`, `backend/API/Services/` e `backend.Tests/`.

**Como ler:** `Implementação` é o arquivo que contém a regra (não o controller); `Teste` é
`Classe.Metodo` em `backend.Tests/`. O caminho completo dos testes está no mapa do fim do
documento.

---

## 1. Fluxos cobertos

| # | Fluxo | Onde começa | Onde termina |
| --- | --- | --- | --- |
| F1 | Cadastro e manutenção do aluno com restrições | `POST /api/aluno` / `PUT /api/aluno/{id}` | `Aluno` persistido, restrições normalizadas e trilha `HistoricoAluno` |
| F2 | Geração, adaptação e publicação de cardápio | `POST /api/geracao` / `POST /api/cardapios` | `Cardapio` em `Publicado`, versão adaptada e evento em `HistoricoCardapio` |
| F3 | Solicitações, ocorrências e feedback | `POST /api/solicitacoes` / `/api/ocorrencias` / `/api/feedbacks` | Decisão única registrada, efeito aplicado no aluno e histórico append-only |

## 2. As cinco regras centrais

| # | Regra | Invariante | Implementação |
| --- | --- | --- | --- |
| R1 | Transição de situação do cardápio | Só `Rascunho` publica; `Publicado`/`Cancelado`/`Substituido` não voltam; cancelamento exige motivo e autor | `backend/Domain/Entities/Cardapio.cs` (`Publicar`, `Cancelar`, `CriarRevisao`) |
| R2 | Invariantes do aluno | Turma e responsável obrigatórios; no máximo 20 restrições; restrição normalizada, sem duplicata | `backend/Domain/Entities/Aluno.cs` (construtor, `AdicionarRestricao`, `RemoverRestricao`) |
| R3 | Perfil do usuário | Nome/e-mail só mudam se válidos; falha não altera estado | `backend/Domain/Entities/Usuario.cs` (`AtualizarPerfil`) |
| R4 | Decisão única da solicitação | Aprovar/recusar só a partir de `EmAnalise`; segunda decisão é conflito | `backend/Domain/Entities/Solicitacao.cs` (`Transicionar`) + `backend/API/Services/SolicitacaoService.cs` |
| R5 | Publicação exige CRN | Nutricionista sem CRN válido não publica | `backend/Domain/Services/PoliticaDeCrn.cs` + `Cardapio.DefinirNutricionista` |

## 3. Matriz

### F1 — Cadastro e manutenção do aluno com restrições

| Regra | Comportamento verificável | Implementação | Teste |
| --- | --- | --- | --- |
| R2 | Aluno sem turma é rejeitado | `Aluno.cs` (construtor) | `AlunoTests.Construtor_SemTurma_Lanca` |
| R2 | Aluno sem responsável é rejeitado | `Aluno.cs` (construtor) | `AlunoTests.Construtor_SemResponsavel_Lanca` |
| R2 | Mais de 20 restrições é rejeitado | `Aluno.cs` (construtor) | `AlunoTests.Construtor_ComMaisDe20Restricoes_Lanca` |
| R2 | Restrição é normalizada e duplicata é rejeitada | `Aluno.AdicionarRestricao` | `AlunoTests.AdicionarRestricao_NormalizaEEspecificaOValor`, `AlunoTests.AdicionarRestricao_Duplicada_Lanca` |
| R2 | Lista de restrições não é mutável de fora | `Aluno.Restricoes` (`IReadOnlyList`) | `AlunoTests.Restricoes_NaoExpoeListaMutavel` |
| R3 | Perfil inválido não altera estado | `Usuario.AtualizarPerfil` | `UsuarioTests.AtualizarPerfil_Invalido_NaoAlteraEstado` |
| R3 | Perfil válido normaliza nome e e-mail | `Usuario.AtualizarPerfil` | `UsuarioTests.AtualizarPerfil_ComDadosValidos_NormalizaNomeEEmail` |
| — | Senha é gravada como hash com prefixo, nunca em claro | `Usuario.DefinirSenha` (`HashSenha`) | `UsuarioTests.DefinirSenha_ComValorValido_PersisteHashComPrefixo`, `UsuarioTests.SenhaConfere_ComSenhaIncorreta_RetornaFalse` |
| — | Restrições criadas na API são persistidas e relidas | `AlunoController` + `AlunoRepository` | `AlunoApiTests.Post_ComTresRestricoes_GetDevolveOsValoresPersistidos` |
| — | Toda alteração de aluno gera evento com valor anterior e novo | `Aluno.RegistrarHistorico` | `HistoricoAlunoApiTests.Historico_AlteracaoDeTelefone_RegistraCampoComValores` |
| — | Atualização sem mudança não gera evento | `Aluno.AtualizarDados` | `HistoricoAlunoApiTests.Historico_AtualizacaoSemMudanca_NaoRegistraEvento` |

### F2 — Geração, adaptação e publicação de cardápio

| Regra | Comportamento verificável | Implementação | Teste |
| --- | --- | --- | --- |
| R1 | Rascunho publica e registra evento | `Cardapio.Publicar` | `CardapioTransicaoTests.Publicar_Rascunho_MarcaPublicadoComEvento`, `PublicacaoApiTests.Publicar_Rascunho_RetornaPublicado` |
| R1 | Publicar duas vezes é transição inválida | `Cardapio.Publicar` | `CardapioTransicaoTests.Publicar_JaPublicado_LancaTransicaoInvalida`, `PublicacaoApiTests.Publicar_JaPublicado_Retorna409` |
| R1 | Cancelar cardápio já encerrado é rejeitado | `Cardapio.Cancelar` | `CardapioTransicaoTests.Cancelar_JaEncerrado_LancaTransicaoInvalida`, `PublicacaoApiTests.Cancelar_SemMotivo_Retorna400` |
| R1 | Revisão incrementa versão e mantém o original | `Cardapio.CriarRevisao` | `CardapioTransicaoTests.CriarRevisao_IncrementaVersaoEMantemOriginal`, `PublicacaoApiTests.Revisao_CriaNovaVersao` |
| R5 | Nutricionista sem CRN não publica | `PoliticaDeCrn` | `PoliticaDeCrnTests.NutricionistaSemCrn_RetornaMensagemDeBloqueio`, `PoliticaDeCrnTests.NutricionistaComCrnInvalido_RetornaMensagemDeBloqueio` |
| R5 | CRN válido não bloqueia; formato é normalizado | `PoliticaDeCrn` + `CrnDeNutricionista` | `PoliticaDeCrnTests.NutricionistaComCrnValido_NaoBloqueia`, `CrnDeNutricionistaTests.TryParse_AceitaVariacoes_Normaliza` |
| — | Alergia gera achado bloqueante; restrição gera alerta | `AnaliseCompatibilidade` | `AnaliseCompatibilidadeTests.Alergia_DeAluno_GeraBloqueante`, `AnaliseCompatibilidadeTests.Restricao_DeAluno_GeraAlertaNaoBloqueante` |
| — | Análise é determinística (mesma entrada, mesma ordem) | `AnaliseCompatibilidade` | `AnaliseCompatibilidadeTests.DuasExecucoes_ProduzemMesmaOrdenacao` |
| — | Publicar com alérgico e adaptado atualizado publica geral e adaptado | `PublicacaoCardapioService` | `PublicacaoApiTests.Publicar_ComAlergenicoEAdaptadoAtualizado_PublicaGeralEAdaptado` |
| — | Período sobreposto é rejeitado com o id do cardápio | `PeriodoVigencia` | `PublicacaoApiTests.Publicar_PeriodoSobreposto_Retorna409ComCardapioId` |
| — | Geração do geral não filtra por restrição individual | `GeracaoCardapioService` | `GeracaoCardapioServiceTests.Gerar_GeralNaoFiltraPorRestricao` |
| — | Janela de repetição exclui alimento publicado recentemente | `GeracaoCardapioService` | `GeracaoCardapioServiceTests.Gerar_ComJanelaDeRepeticao_ExcluiAlimentoDePublicadoRecente` |

### F3 — Solicitações, ocorrências e feedback

| Regra | Comportamento verificável | Implementação | Teste |
| --- | --- | --- | --- |
| R4 | Segunda aprovação é conflito | `Solicitacao.Transicionar` + `SolicitacaoService.Aprovar` | `SolicitacaoServiceTests.Aprovar_DuasVezes_DevolveConflito`, `SolicitacaoApiTests.Aprovar_SemAnalise_Retorna409` |
| R4 | Aprovar restrição aplica no aluno e grava trilha | `EfeitosDaSolicitacao.AplicarRestricao` | `SolicitacaoServiceTests.Aprovar_Restricao_AplicaNoAlunoEGravaTrilha`, `SolicitacaoApiTests.Fluxo_AprovarRestricao_AplicaNoAlunoEGravaTrilha` |
| R4 | Aprovar exige análise prévia | `SolicitacaoService` | `SolicitacaoApiTests.Aprovar_SemAnalise_Retorna409` |
| — | Responsável sem vínculo não cria solicitação | `SolicitacaoService.Criar` | `SolicitacaoServiceTests.Criar_Restricao_ResponsavelSemVinculo_Proibido`, `SolicitacaoApiTests.Criar_PorResponsavelSemVinculo_Retorna403` |
| — | Responsável vinculado cria solicitação aberta | `SolicitacaoService.Criar` | `SolicitacaoServiceTests.Criar_Restricao_ComoResponsavelVinculado_CriaAberta` |
| — | Ocorrência segue a sequência Aberta → EmAnalise → Resolvida → Arquivada | `Ocorrencia.MudarStatus` | `OcorrenciaTests.Fluxo_Valido_AnalisaResolveArquiva`, `OcorrenciaApiTests.MudarStatus_AdminEscola_GravaHistorico` |
| — | Transição direta de Aberta para Resolvida é rejeitada | `Ocorrencia.MudarStatus` | `OcorrenciaTests.Transicao_DiretaDeAberta_Lanca`, `OcorrenciaApiTests.TransicaoInvalida_409` |
| — | Só o autor cancela, e apenas no mesmo dia | `Ocorrencia.Cancelar` | `OcorrenciaTests.Cancelar_NoMesmoDia_Funciona`, `OcorrenciaApiTests.Cancelar_PorNaoAutor_403` |
| — | Feedback respondido não é respondido de novo | `Feedback.Responder` | `FeedbackTests.Responder_DuasVezes_Lanca` |
| — | Só o autor encerra o feedback respondido | `Feedback.Encerrar` | `FeedbackTests.Encerrar_Aberto_Lanca`, `FeedbackApiTests.Nutricionista_Responde_AutorEncerra` |

## 4. Lacunas conhecidas (o que a matriz **não** consegue provar)

| Lacuna | Onde | Situação | Ação sugerida |
| --- | --- | --- | --- |
| Leitura de ocorrências do filho pelo responsável (RF7–RF10 da Spec 035) | `PainelService.OcorrenciasDoAluno` | **Sem teste positivo**: só há negação (`PainelApiTests.Relatorio_DeAlunoNaoVinculado_Retorna403`, `OcorrenciaApiTests.Responsavel_NaoMudaStatus_403`) | Criar teste de API que liste ocorrências do filho e devolva 403 para aluno não vinculado |
| Interface (telas) dos fluxos F1–F3 | `frontend/src/pages` | **Sem teste automatizado** — registrado em PEND-049; a UI é exercitada por build/lint e pelo contrato HTTP | Introduzir runner de testes do frontend (PEND-049) |

A coluna de testes desta matriz cobre backend (domínio, serviço e HTTP). Nenhuma linha aponta
teste de frontend porque não existe; em vez de omitir, a lacuna está declarada acima.

## 5. Mapa dos arquivos de teste

| Classe citada | Arquivo |
| --- | --- |
| `AlunoTests` | `backend.Tests/Domain/AlunoTests.cs` |
| `UsuarioTests` | `backend.Tests/Domain/UsuarioTests.cs` |
| `CardapioTransicaoTests` | `backend.Tests/Domain/CardapioTransicaoTests.cs` |
| `PoliticaDeCrnTests` | `backend.Tests/Domain/PoliticaDeCrnTests.cs` |
| `CrnDeNutricionistaTests` | `backend.Tests/Domain/CrnDeNutricionistaTests.cs` |
| `AnaliseCompatibilidadeTests` | `backend.Tests/Domain/AnaliseCompatibilidadeTests.cs` |
| `OcorrenciaTests` | `backend.Tests/Domain/OcorrenciaTests.cs` |
| `FeedbackTests` | `backend.Tests/Domain/FeedbackTests.cs` |
| `SolicitacaoServiceTests` | `backend.Tests/Services/SolicitacaoServiceTests.cs` |
| `GeracaoCardapioServiceTests` | `backend.Tests/Services/GeracaoCardapioServiceTests.cs` |
| `AlunoApiTests` | `backend.Tests/Integration/AlunoApiTests.cs` |
| `HistoricoAlunoApiTests` | `backend.Tests/Integration/HistoricoAlunoApiTests.cs` |
| `PublicacaoApiTests` | `backend.Tests/Integration/PublicacaoApiTests.cs` |
| `SolicitacaoApiTests` | `backend.Tests/Integration/SolicitacaoApiTests.cs` |
| `OcorrenciaApiTests` | `backend.Tests/Integration/OcorrenciaApiTests.cs` |
| `FeedbackApiTests` | `backend.Tests/Integration/FeedbackApiTests.cs` |
| `PainelApiTests` | `backend.Tests/Integration/PainelApiTests.cs` |

## 6. Como verificar esta matriz

```bash
cd <raiz do repositório>

# cada teste citado existe?
grep -rho "`[A-Za-z]*Tests\.[A-Za-z0-9_]*`" docs/av2/rastreabilidade.md \
  | tr -d '`' | sort -u | while read -r t; do
      cls=${t%%.*}; m=${t##*.}
      grep -rq "public .* $m(" backend.Tests --include="$cls.cs" && echo "OK $t" || echo "FALTA $t"
  done

# e a suíte inteira passa
env APPDATA="<seu %APPDATA%>" ProgramFiles='C:\Program Files' \
  dotnet test backend.Tests/backend.Tests.csproj -c Release
```
