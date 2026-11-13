# Arquitetura — NutriEduc

Documento de arquitetura vigente. Descreve componentes, responsabilidades,
dependências e as regras que impedem a degradação estrutural.

---

## 1. Visão de componentes

```text
Navegador
  React 19 + TypeScript + Vite 8
  ├── routes/         Rotas, guarda por sessão/papel e homePorPapel
  ├── pages/          Telas por área de usuário
  │   ├── comum/          login, BootstrapAdmin, Perfil, TermosDeUso, PoliticaDePrivacidade, Suporte
  │   ├── nutricao/       HomeNutricao, alunos/, cardapios/, alimentos/
  │   ├── merendeiras/    HomeMerendeiras, AlteracoesCardapios
  │   ├── pais/           LayoutPais, PainelPais, CardapiosPais, OcorrenciasPais,
  │   │                   NovaOcorrenciaPais, SolicitacoesPais, FeedbackPais
  │   ├── secretaria/     HomeSecretaria, Escolas, Unidades, Usuarios, SolicitacoesAcesso
  │   ├── escolas/        HomeEscolas (placeholder)
  │   ├── solicitacoes/   SolicitacoesPage
  │   ├── ocorrencias/    OcorrenciasPage
  │   ├── feedback/       FeedbackPage
  │   └── publico/        CardapiosPublicados
  ├── components/     Header, Navbar, Footer, Button, CardapiosDiv, Notificacoes, SinoNotificacoes,
  │                   SeletorDeAlunos, SeletorDeFilho, CardapioDoAluno, ListaOcorrencias,
  │                   FormularioOcorrencia, ListaSolicitacoes, FormularioSolicitacao,
  │                   ListaFeedback, FormularioFeedback
  ├── context/        authContext (sessão via cookie)
  └── services/       AlunoApi, CardapioApi, SolicitacaoApi, OcorrenciaApi, FeedbackApi,
                      NotificacaoApi, VinculoApi, UsuarioAdminApi, ... → fetch /api via proxy Vite

Regra de organização das telas: cada tela nova entra na pasta da área que a usa.
`comum/` é reservado a telas compartilhadas por mais de uma área; tela de área única
não entra em `comum/`.

API  ASP.NET Core 10 — http://localhost:5001
  ├── API/Controllers (20) Aluno, Usuario, UsuarioAdmin, Escola, Unidade, Vinculo, Bootstrap,
  │                    Cardapio, Geracao, Restricao, Alimento, CategoriaAlimento, Ocorrencia,
  │                    Solicitacao, SolicitacaoAcesso, Feedback, Notificacao, Nutricionista,
  │                    Diagnostico (+ RespostaController, base)
  ├── API/Services (26) AlunoService, UsuarioService, AutenticacaoService, EscolaService,
  │                    VinculoService, BootstrapService, EscopoService, CardapioService,
  │                    PublicacaoCardapioService, GeracaoCardapioService, AdaptarCardapioService,
  │                    CardapioServidoService, OcorrenciaService, SolicitacaoService, ...
  ├── Application      DTOs, filtros, segurança (MatrizDePermissoes/AutorizacaoService)
  ├── Domain           entidades e serviços puros (Aluno, Usuario, Cardapio, Ocorrencia,
  │                    Solicitacao, SolicitacaoDeAcesso, GeradorCardapioAutomatico,
  │                    SeletorDeRefeicao, ValidadorDeCardapioGerado, AdaptacaoDeCardapio,
  │                    PoliticaDeAprovacao, PoliticaDeCrn, ...)
  ├── Infrastructure   ConnectionContext (EF Core), repositórios, UnidadeDeTrabalho, FiltroDeEscopo
  └── Migrations       29 migrations, aplicadas no startup

Persistência
  SQLite — database.db com usuários, alunos, escolas/unidades, vínculos, restrições,
  alimentos/categorias, cardápios/refeições/itens/versões adaptadas, publicações,
  solicitações/solicitações de acesso, ocorrências, feedbacks, notificações, trilha e bootstrap.
```

## 2. Camadas e responsabilidades

| Camada | Pode | Não pode |
| --- | --- | --- |
| Controller | Traduzir HTTP ↔ contrato, decidir status code | Conter regra de negócio ou mensagem de regra, acessar `DbContext` |
| Service | Orquestrar caso de uso, consultar portos, abrir transação por `IUnidadeDeTrabalho` | Validar invariante de entidade, chamar outro serviço, decidir transação em repositório |
| Domain | Validar e proteger invariantes, expor políticas puras (`PoliticaDeExclusao`) e filtros de negócio (`FiltroAluno`) | Conhecer HTTP, EF Core, DTO, sessão, papel ou claims |
| Infrastructure | Persistir, mapear schema, implementar a unidade de trabalho | Conter regra de negócio, confirmar escrita por conta própria |
| Frontend service | Chamar API, tipar contrato | Conter regra de negócio que o backend precisa garantir |

## 3. Fluxo de autenticação

1. `POST /api/usuario/login` valida credencial e emite cookie
   `NutriEduc.Auth` (HTTP-only, `SameSite=Lax`, 8h, sliding).
2. `authContext` recupera a sessão em `GET /api/usuario/me` com
   `credentials: "include"`.
3. `ProtectedRoutes` bloqueia rotas quando não há sessão.
4. `PUT /api/usuario/perfil/{id}` exige que o ID da rota coincida com o ID da sessão.

**Regra:** toda chamada autenticada carrega `credentials: "include"`. Uma chamada
sem esse atributo é um defeito, não um detalhe.

## 4. Fluxo de escrita no domínio

```text
Controller recebe DTO
  → Service busca entidade no repositório
  → entidade valida TODOS os campos
  → entidade só então atribui (estado nunca parcialmente atualizado)
  → Service persiste via repositório
  → Controller devolve DTO de saída, nunca a entidade
```

Violação desse fluxo é a causa raiz de estado inválido persistido.

## 5. Contratos e dependências

- `IUsuarioRepository`, `IAlunoRepository` são consumidos por serviços via DI.
  Nenhum repositório instancia `ConnectionContext` por conta própria.
- Desde a Spec 024, os portos recebem filtros de negócio (`FiltroAluno`, `FiltroUsuarios`)
  e nunca escopo de sessão; a tradução papel → filtro vive em `Application`.
- Contratos de entrada e de saída são tipos distintos por operação (`AlunoEntrada`,
  `AlunoResposta`, `CriarUsuarioEntrada`, `UsuarioRespostaDTO`); nenhum DTO de entrada é
  devolvido por rota.
- Escrita passa por `IUnidadeDeTrabalho`: o caso de uso abre a transação e confirma; nenhum
  repositório chama `SaveChanges`.
- Services são `Scoped`, alinhados ao ciclo de vida do `ConnectionContext`.
- DTOs de entrada e de saída são distintos. Campo de senha nunca aparece em DTO de
  saída.
- Coleções expostas pelo domínio são `IReadOnlyList`/`IReadOnlyCollection`
  acompanhadas de operação de domínio (`AdicionarRestricao`, `RemoverRestricao`).
- Conta `Responsavel` só existe com ao menos um vínculo (Spec 034): `POST /api/usuarios` e
  `PUT /api/usuarios/{id}` recebem `alunosIds`, validam a unidade do aluno e gravam usuário,
  vínculos e trilha na mesma transação.
- A busca de aluno (`busca`, `escolaId`, `serie`) é filtro de negócio aplicado no SQL, sempre
  depois do recorte de escopo; nenhum parâmetro de consulta amplia o alcance.

## 6. Tratamento de erro

Formato único de erro:

```json
{ "erro": "mensagem legível" }
```

| Origem | Status |
| --- | --- |
| `ArgumentException` no domínio | 400 |
| modelo inválido ou JSON malformado (`ContratoDeErro`) | 400 |
| não autenticado | 401 |
| autenticado sem o papel exigido | 403 |
| recurso inexistente | 404 |
| rota inexistente (`ContratoDeErro`) | 404 |
| verbo não permitido (`ContratoDeErro`) | 405 |
| conflito de estado | 409 |
| content-type não suportado (`ContratoDeErro`) | 415 |
| exceção não tratada | 500, sem stack trace no corpo |

Duas autoridades, nenhuma fora delas: `API/Middleware/ErrorHandlingMiddleware.cs` traduz
exceções e `API/Middleware/ContratoDeErro.cs` cobre o que não é exceção (modelo inválido, JSON
malformado, `404` de rota, `405` e `415`). `401` e `403` saem dos eventos do cookie em
`Program.cs`. Nenhum controller monta erro fora do formato `{ erro }` (Spec 020).

## 7. Persistência

- EF Core 10 + SQLite; `Database.Migrate()` no startup.
- Migrations são versionadas e nunca editadas após aplicadas.
- `Restricoes` é armazenada como coluna `TEXT` (JSON). A normalização em catálogo
  próprio depende da entidade `RestricaoAlimentar` (Spec 010).
- Toda mudança de schema exige migration nova, nunca alteração do snapshot manual.

## 8. Regras de evolução

1. Nenhuma mudança de contrato de API é considerada concluída sem atualização da
   tabela de endpoints do `README.md` na mesma alteração.
2. Nenhum endpoint novo é aceito sem teste de integração do caminho feliz e do
   caminho de erro correspondente.
3. Nenhuma regra de negócio é adicionada ao frontend quando o backend precisa
   garanti-la; o frontend valida apenas para experiência de uso.
4. Dependência nova exige justificativa contra a alternativa sem dependência.
5. Toda correção acompanha teste que falha antes e passa depois.

## 9. Dívida arquitetural conhecida

| Item | Situação | Encaminhamento |
| --- | --- | --- |
| Spec 019 sem implementação | Relatórios/exportação ausentes; só a matriz tem `Recurso.Relatorio` | Implementar a 019 |
| 029 sem frontend | Backend geral/adaptado pronto; montagem/detalhe pendentes | PEND-042 |
| `UsuarioService` com 757 linhas | Cadastro, perfil e administração no mesmo serviço | PEND-027 |
| Matriz × `CardapioService.PodeLer` | Matriz dá `R` a `Responsavel` em cardápio; o serviço devolve 403 | PEND-048 |
| Sem testes de frontend | Validação só por build e lint | PEND-049 |
| `Aluno.Restricoes` em JSON | Catálogo e histórico normalizados; vínculo do aluno segue JSON | Aceito; reavaliar na migração |
| Sem paginação em `/api/aluno` | Listagem devolve todo o escopo do solicitante | 025 / dívida técnica |
| Sem versionamento de API | Contrato muda sem sinal | Pós-F2 |
| Exclusão definitiva sem auditoria | `DELETE` remove o registro; resta o log da requisição | ADR-014; avaliar se auditoria é exigida |
| SQLite sem decimal/collation/date/concorrência reais | PEND-029 a PEND-037 | Migrar de provedor |

## 10. Estado atual verificado (2026-11-13)

| Verificação | Resultado |
| --- | --- |
| `dotnet test backend.Tests` | 830 testes, 830 aprovados, 0 falhas |
| Build backend | 0 erros; avisos de nulabilidade pré-existentes em `GeracaoCardapioService` |
| `npm run build` / `npm run lint` | build OK; lint 0 erros e 21 avisos pré-existentes |
| Migrations | 29, aplicadas no startup por `Database.Migrate()` |
| Superfície HTTP | 20 controllers (19 de recurso + `RespostaController` base) |
| Serviços de aplicação | 27 (inclui `NotificacaoDeAcessoService` e `ConfiguracaoDeBootstrap`) |
| Domínio implementado | aluno, usuário/papéis/CRN, escola/unidade, vínculo, restrições, alimentos, cardápio/refeições/adaptação/publicação, geração, ocorrências, solicitações (com anafilaxia informada), solicitações de acesso, feedback, notificações, trilha |
| Specs | `audited`: 001–009, 013–015, 020–022, 027, 028, 030, 034, 035. `implemented`: 010–012, 016–018, 023, 024, 031–033. `in-progress`: 029. `created`: 019. `planned`: 025, 026 |
| Pendências | `management/pending-items.md`; PEND-028 bloqueia o motor; 019 e 029 incompletas; PEND-047 e PEND-050 aguardam validação manual |

Estado formal: o roadmap tem 35 linhas. As Specs 034 e 035 foram auditadas manualmente em
2026-11-13 (código, testes e documentação); a promoção automática pelo `/spec-test` continua
bloqueada pelo PEND-028.

## 11. Mapa de módulos especificados (009–035)

| Spec | Módulo | Entidades / tipos principais | Superfície HTTP | Status |
| --- | --- | --- | --- | --- |
| 009 | Identidade e estrutura escolar | `Escola`, `UnidadeEscolar`, `VinculoResponsavelAluno`, `PapelUsuario` | `EscolaController`, `UnidadeController`, `VinculoController`, `UsuarioController` | audited |
| 010 | Restrições e alergias | `RestricaoAlimentar`, histórico do aluno | `RestricaoController`, histórico em `AlunoController` | implemented |
| 011 | Catálogo de alimentos | `CategoriaAlimento`, `Alimento`, `AlimentoRestricao`, `ValoresNutricionais` | `AlimentoController`, `CategoriaAlimentoController` | implemented |
| 012 | Cardápio | `Cardapio`, `Refeicao`, `ItemRefeicao`, `PeriodoVigencia` | `CardapioController` (interno + público) | implemented |
| 013 | Geração automática | `ParametroGeracao`, `GeradorCardapioAutomatico` | `GeracaoController` | audited |
| 014 | Compatibilidade | `AnaliseCompatibilidade` (função pura) | rota em `CardapioController` | audited |
| 015 | Publicação e histórico | `HistoricoCardapio`, transições de `Cardapio` | `CardapioController` | audited |
| 016 | Ocorrências | `Ocorrencia`, `HistoricoOcorrencia` | `OcorrenciaController` | implemented |
| 017 | Solicitações | `Solicitacao`, `HistoricoSolicitacao` | `SolicitacaoController` | implemented |
| 018 | Feedback e notificações | `Feedback`, `Notificacao` | `FeedbackController`, `NotificacaoController` | implemented |
| 019 | Relatórios e exportação | `RelatorioGerado`, exportadores | `RelatorioController` (não existe) | created |
| 020 | Contrato de rotas | middleware de erro e padronização | `ContratoDeErro`, controllers | audited |
| 021 | Telas de secretaria | nenhuma entidade nova | consome escolas, unidades e usuários | audited |
| 022 | Bootstrap por token | `ConsumoBootstrap`, `TokenDeCadastro` | `BootstrapController`, `UnidadeController` | audited |
| 023 | Histórico do aluno | `HistoricoAluno` | `AlunoController` | implemented |
| 024 | Fronteiras DDD | DTOs, filtros, unidade de trabalho | todos os controllers | implemented |
| 025 | Painel por papel | agregações de leitura | `PainelController` (planejado) | planned |
| 026 | Substituição assistida por IA | sugestão determinística + provedor plugável | rotas de substituição (planejadas) | planned |
| 027 | Hierarquia | `Escola.UnidadeId`, `Aluno.EscolaId`, `Aluno.UnidadeId` derivado | `EscolaController`, `UnidadeController`, `VinculoController` | audited |
| 028 | Matriz de permissões | `MatrizDePermissoes`, `AutorizacaoService`, `FiltroDeEscopo`, `TrilhaAcao` | transversal | audited |
| 029 | Cardápio geral e adaptado | `ConjuntoDeRestricoes`, `AdaptacaoDeCardapio`, `CardapioServido` | `CardapioController`, `AlunoController` | in-progress |
| 030 | Solicitação de dados cadastrais | `SolicitacaoAlteracao`, `AlteracaoSolicitada` | `SolicitacaoController` | audited |
| 031 | CRN da nutricionista | `CrnDeNutricionista`, `PoliticaDeCrn` | `UsuarioAdminController`, `UsuarioController` | audited |
| 032 | Correção da geração | `SeletorDeRefeicao`, `ValidadorDeCardapioGerado` | `GeracaoController` | implemented |
| 033 | Cadastro e autorização | `SolicitacaoDeAcesso`, `PoliticaDeAprovacao` | `SolicitacaoAcessoController`, `UsuarioController` | implemented |
| 034 | Vínculo responsável↔aluno | `FiltroAluno`, `VinculoResponsavelAluno` | `AlunoController`, `VinculoController`, `UsuarioAdminController` | audited |
| 035 | Portal dos pais/responsáveis | `Feedback.AlunoId`/`AlunoNome`; `Solicitacao.Anafilaxia`; componentes de cardápio, ocorrência, solicitação e feedback | `AlunoController`, `OcorrenciaController`, `SolicitacaoController`, `FeedbackController` (reuso) | audited |

**Regra de dependência entre módulos:** o fluxo principal é
`009 → 010 → 011 → 012 → 014 → {013, 015} → 016 → 017 → 018 → 019`, com 027/028/033/034
fornecendo hierarquia, autorização e vínculo. Nenhum módulo reimplementa regra de outro:
compatibilidade (014) é a única fonte de severidade, notificação (018) é a única fonte de
`Notificacao` e o vínculo (034) é a única autorização de leitura do responsável.

**Regra de escrita entre agregados:** operações que tocam dois agregados (ex.: aprovar
solicitação de restrição, em 017) usam o mesmo `ConnectionContext` Scoped e uma
transação explícita. Não existe repositório que abre conexão própria (L4).

## Spec 007 — contrato-erro-e-infra-di

**Canonical creation path:** `backend/API/Middleware/`, `backend/Program.cs`.

O contrato único de erro (§6) e a injeção de dependência por contrato (§5) ganham um
dono explícito: um middleware de exceção registrado em `Program.cs`, que traduz
`ArgumentException` em 400 e elimina os `try/catch` repetidos nos controllers, e a
correção do `UsuarioRepository`, que hoje instancia o próprio `DbContext` (PEND-004).

## Spec 023 — historico-alteracoes-aluno

**Canonical creation path:** `backend/Domain/Entities/HistoricoAluno.cs`,
`backend/Domain/Interface/IHistoricoAlunoRepository.cs`,
`backend/API/Services/HistoricoAlunoService.cs`, `backend/API/Controllers/AlunoController.cs`,
`frontend/src/pages/nutricao/alunos/VisualizarAlunos.tsx`.

**Problema estrutural:** a trilha append-only nasceu presa a um único atributo
(`HistoricoRestricaoAluno`, Spec 010). Registrar qualquer outra mudança exigiria uma
tabela nova por atributo — o mesmo erro que a Spec 010 corrigiu no catálogo.

**Decisão:** uma entidade `HistoricoAluno` genérica, filha do agregado `Aluno`, com
`Tipo` (`Criado`, `Alterado`, `RestricaoAdicionada`, `RestricaoRemovida`), `Campo`
(opcional), `ValorAnterior`/`ValorNovo` (snapshots textuais), `AutorNome` e
`CriadoEmUtc`. O diff é calculado no domínio (`Aluno.AtualizarDados` devolve a lista de
alterações) e persistido na mesma `SaveChanges` do aluno, preservando ADR-009 e RNF2.

**Compatibilidade:** a tabela `HistoricosRestricoesAlunos` é migrada para a nova trilha e
removida; a rota `GET /api/aluno/{id}/restricoes/historico` continua respondendo, agora
filtrando os eventos de restrição da trilha única — nenhum consumidor quebra.

## Spec 025 — painel-e-visao-por-papel

**Status:** planejada — não implementada (roadmap `planned`).

**Canonical creation path:** `backend/API/Controllers/PainelController.cs`,
`backend/API/Services/PainelService.cs`, `frontend/src/pages/secretaria/HomeSecretaria.tsx`,
`frontend/src/pages/merendeiras/HomeMerendeiras.tsx`, `frontend/src/pages/pais/HomePais.tsx`,
`frontend/src/services/PainelApi.ts`.

**Problema estrutural:** a autorização já existe por papel (Spec 009) e as guardas de rota
também, mas três áreas são placeholders. A secretaria não tem indicadores; a merendeira não
vê o cardápio do dia da própria unidade; a família não vê nada do filho. As features que
alimentam essas telas (012–019) definem **acesso por papel de cada recurso**, não a
consolidação em uma área de entrada.

**Decisão:** uma leitura agregada só (`PainelService`) alimenta três telas — painel da
secretaria, cardápio do dia da merendeira e visão da família. Nenhuma delas cria recurso
próprio: todas compõem dados de cardápio publicado (012/015), restrições (010/023) e
ocorrências/solicitações (016/017). O relatório da família é **escopado ao filho** e vive
aqui, não na 019, que permanece o catálogo de relatórios de gestão.

**Regra derivada:** tela de área lê por composição; nunca duplica regra de recurso nem
consulta fora do escopo — aluno fora do vínculo responde 403 sem revelar existência.

## Spec 026 — substituicao-assistida-ia

**Status:** planejada — não implementada (roadmap `planned`).

**Canonical creation path:** `backend/Domain/Entities/SugestaoDeSubstituicao.cs`,
`backend/Domain/Interface/ISugestorDeSubstituicao.cs`,
`backend/API/Services/SubstituicaoService.cs`, `backend/Infrastructure/IA/SugestorPorIa.cs`,
`frontend/src/pages/nutricao/cardapios/MontarCardapio.tsx`.

**Problema estrutural:** a compatibilidade é função pura e determinística (ADR-011) e a
geração (013) também. Apoio à decisão não existe: trocar um item bloqueado é trabalho
manual. Introduzir IA sem fronteira colocaria não-determinismo no caminho de uma regra de
segurança alimentar.

**Decisão:** a IA nunca decide o que é permitido. A porta `ISugestorDeSubstituicao` recebe
candidatos **já validados** pelo motor determinístico (mesma categoria, valores próximos,
sem vínculo bloqueante) e devolve ranking com justificativa; a escolha é revalidada por
`AnaliseCompatibilidade` antes de tocar no cardápio e só entra após aprovação humana. Duas
implementações: `SugestorPorIa` (provedor externo, opcional, com timeout e teto de uso) e
`SugestorDeterministico` (fallback sempre disponível). Prompt carrega apenas nomes de
alimentos e restrições agregadas — nunca dado de aluno. Toda sugestão aceita ou recusada é
registrada no padrão append-only da Spec 023.

**Regra derivada:** resultado de IA é proposta, não estado; nenhuma escrita de cardápio
depende de resposta de provedor externo.

## Spec 024 — fronteiras-ddd

**Canonical creation path:** `backend/Application/`, `backend/Domain/Entities/`,
`backend/Domain/Interface/`, `backend/API/Services/`, `backend/Infrastructure/Repositories/`.

**Problema estrutural:** as camadas existem, mas a fronteira entre elas é permeável em sete
pontos: DTO de entrada é reutilizado como saída (`DadosAlunoDTO`), regra de negócio mora em
serviço de aplicação (hash de token em `BootstrapService`, unicidade de e-mail virando
`null`, existência de unidade não validada), `EscopoUsuario` (autorização) é vocabulário do
domínio, não existe unidade de trabalho (19 `SaveChanges` em repositórios), `ExclusaoService`
decide política e escopo ao mesmo tempo, `Usuario.Senha` é pública, e `UsuarioService` /
`BootstrapService` passam de 370 linhas acumulando casos de uso.

**Decisão:** cada correção move a regra para a camada dona: contratos separados por operação
em `Application`; invariantes e objetos de valor em `Domain` (`HashDeToken`, referências
validadas na entidade); filtros de negócio no lugar de escopos de sessão dentro dos portos
de repositório; `IUnidadeDeTrabalho` com transação explícita por caso de uso; política de
exclusão como serviço de domínio, com a autorização decidida na aplicação; e divisão dos
serviços grandes por intenção, preservando os contratos HTTP existentes.

**Regra derivada:** nenhum tipo de `Domain` pode conhecer sessão, HTTP, EF Core ou DTO; nenhum
repositório decide transação sozinho; nenhum controller conhece mensagem de regra de negócio.

## Spec 027 — hierarquia-unidade-escola-aluno

**Canonical creation path:** `backend/Domain/Entities/Escola.cs`, `backend/Domain/Entities/Aluno.cs`,
`backend/Domain/Entities/Usuario.cs`, `backend/Application/HierarquiaService.cs`,
`backend/Infrastructure/Repositories/AlunoRepository.cs`, `backend/API/Services/EscolaService.cs`,
`backend/API/Services/BootstrapService.cs`, `backend/Migrations/*_HierarquiaEscolaAluno.cs`,
`backend.Tests/Integration/HierarquiaApiTests.cs`, `frontend/src/pages/secretaria/Escolas.tsx`,
`frontend/src/pages/nutricao/alunos/CadastrarAlunos.tsx`.

**Problema estrutural:** a ADR-013 tirou a escola da hierarquia e deixou o aluno apenas com
`UnidadeId`; `AlunoRepository.Filtrar` ainda usava `Unidade.EscolaId`, nulo em unidades novas, e
`AdminEscola`/`Merendeira` ficaram sem escopo de escola (PEND-024).

**Decisão:** `Escola.UnidadeId` obrigatório, `Aluno.EscolaId` obrigatório e `Aluno.UnidadeId`
denormalizado com escritor único no domínio; `HierarquiaService` deriva e valida a cadeia; o
bootstrap cria a unidade inicial; migration `HierarquiaEscolaAluno` adiciona FKs/índices e faz
backfill idempotente. O escopo de leitura deriva da sessão e da cadeia, nunca de parâmetro do
cliente. Criação de escola exige `unidadeId` no payload, apenas confirmado contra a sessão.

**Regra derivada:** nenhum DTO de entrada informa a unidade de um aluno; filtro de unidade e de
escola é aplicado no banco. `GET /api/aluno` aceita `escolaId`/`unidadeId` e `GET /api/escolas`
aceita `unidadeId` apenas como estreitamento do escopo. Leitura por id inexistente responde 404;
fora do escopo, 404 na fronteira de unidade e 403 na fronteira de escola/vínculo. Mudança de
papel, unidade ou escola invalida a sessão na requisição seguinte. `POST /api/aluno/{id}/transferir-escola`
é exclusivo de `AdminSecretaria` e recusa unidade diferente. `GET /api/diagnostico/hierarquia`
conta legado sem vínculo e não corrige dados.

## Spec 028 — matriz-permissoes-e-isolamento

**Canonical creation path:** `backend/Application/Seguranca/Recurso.cs`,
`backend/Application/Seguranca/NivelDeAcesso.cs`,
`backend/Application/Seguranca/MatrizDePermissoes.cs`,
`backend/Application/Seguranca/AutorizacaoService.cs`,
`backend/Infrastructure/Seguranca/FiltroDeEscopo.cs`,
`backend/Domain/Entities/TrilhaAcao.cs`,
`backend/Infrastructure/Repositories/TrilhaAcaoRepository.cs`,
`backend.Tests/Domain/MatrizDePermissoesTests.cs`,
`backend.Tests/Domain/AutorizacaoServiceTests.cs`,
`backend.Tests/Integration/MatrizPermissoesTests.cs`,
`backend.Tests/Integration/AuditoriaAcaoTests.cs`.

**Problema estrutural:** a permissão está espalhada por treze specs, `AdminSecretaria` tem
escopo global, `Merendeira` é escopada por unidade e não existe um conjunto único de invariantes
de isolamento; cada consulta nova depende de disciplina.

**Decisão:** matriz `(recurso, papel) → nível` como função pura (`Nivel` executável +
`Documentada` legível), escopo único por papel, `AutorizacaoService` validando o alvo antes da
escrita em aluno, usuário, vínculo, escola e unidade; escrita de catálogo/cardápio continua
governada pela política de papel já equivalente à matriz. `FiltroDeEscopo` expressa o recorte
em SQL para aluno, escola, usuário e cardápio. `TrilhaAcao` é append-only e grava papel,
vínculo, transferência de aluno, desativação e exclusão de usuário na mesma transação da ação.
Fronteira de unidade responde 404; fronteira de escola, 403.

**Regra derivada:** recurso novo nasce sem permissão; célula da matriz sem teste é entrega
incompleta. Recursos de specs 016–019/025/030/033 ainda sem endpoint têm célula na matriz e
teste puro, mas não têm teste HTTP até a spec dona existir.

## Spec 029 — cardapio-geral-e-adaptado

**Canonical creation path:** `backend/Domain/Entities/Cardapio.cs`,
`backend/Domain/Entities/ConjuntoDeRestricoes.cs`,
`backend/Domain/Services/AdaptacaoDeCardapio.cs`,
`backend/API/Services/AdaptarCardapioService.cs`,
`backend/API/Services/CardapioServidoService.cs`,
`backend/API/Services/ContextoDeRestricoesService.cs`.

**Problema estrutural:** o cardápio é único, a compatibilidade apenas detecta conflito e não
existe a refeição que o aluno com restrição recebe; a substituição é manual e sem vínculo com a
origem. A criação manual não mostra alunos nem restrições e não associa autoria.

**Decisão:** `Cardapio.Tipo` (`Geral` | `Adaptado`), `OrigemId` e `EscolaId`; uma versão
adaptada por `(escola, conjunto distinto de restrições)`, gerada por função determinística com
tolerância nutricional e `PendenteDeRevisao` quando não há substituto válido. A API expõe o
contexto de restrições por escola, bloqueia alimento sem substituto seguro e exige confirmação
registrada para incompatibilidade com substituto; a tela de montagem correspondente segue
pendente (Spec 029, T12–T13; PEND-042). Criação exige nutricionista com CRN válido e snapshot de
autor.

**Regra derivada:** item bloqueante nunca é servido; sem versão publicada para `(escola,
conjunto)`, o cardápio do aluno é `Indisponivel`; o geral nunca é fallback da versão servida.
Revisão de 2026-11-13: o responsável vê as duas versões publicadas (`cardapioGeral` e
`cardapioAdaptado`) na mesma consulta, com alerta quando a adaptada falta.

## Spec 017 — solicitacoes

**Canonical creation path:** `backend/Domain/Entities/Solicitacao.cs`,
`backend/Domain/Entities/HistoricoSolicitacao.cs`,
`backend/Domain/Interface/ISolicitacaoRepository.cs`,
`backend/API/Services/SolicitacaoService.cs`,
`backend/API/Controllers/SolicitacaoController.cs`,
`frontend/src/pages/solicitacoes/SolicitacoesPage.tsx`.

**Problema estrutural:** pedidos de cardápio, ingrediente e restrição existiam apenas fora do
sistema, sem status, responsável ou retorno ao solicitante.

**Decisão:** agregado com seis tipos, ciclo de status explícito e histórico append-only; a
decisão é separada por tipo (nutrição para cardápio/ingrediente, ADM para restrição e dados
cadastrais) e o efeito usa os donos existentes, sem escrita paralela.

**Regra derivada:** aprovar não edita cardápio automaticamente; a resposta diz qual spec é
dona da alteração. Pedido aberto duplicado do mesmo aluno + restrição responde `409` com o id
existente (RF20 da Spec 035). O endpoint `POST /api/solicitacoes/{id}/anafilaxia` permite ao
ADM, ao nutricionista da unidade ou ao responsável do pedido marcar a anafilaxia do aluno.

## Spec 030 — solicitacao-alteracao-dados

**Canonical creation path:** `backend/Domain/Entities/CampoDeDadoDoAluno.cs`,
`backend/Domain/Entities/AlteracaoSolicitada.cs`,
`backend/Domain/Entities/SolicitacaoAlteracao.cs`, `backend/API/Services/SolicitacaoService.cs`
(ramo `DadosCadastrais`), `frontend/src/pages/solicitacoes/SolicitacoesPage.tsx`.

**Problema estrutural:** o fluxo de solicitações cobre cardápio, ingrediente, restrição e
alergia, mas não dados cadastrais; o responsável não tem canal e o ADM edita sem registrar o
pedido.

**Decisão:** novo tipo `DadosCadastrais` no agregado da Spec 017, com alterações imutáveis e
snapshot do valor atual; aprovação atômica via `Aluno.AtualizarDados`, com recusa em 409 quando
o valor divergiu e evento na trilha com a origem da solicitação. A emissão de status usa a
fatia central de `Notificacao` da Spec 018, sem dado clínico.

**Regra derivada:** aprovar não aplica pedido desatualizado; nada é aplicado parcialmente.

## Spec 031 — perfil-crn-nutricionista

**Canonical creation path:** `backend/Domain/Entities/CrnDeNutricionista.cs`,
`backend/Domain/Entities/Usuario.cs`, `backend/Domain/Services/PoliticaDeCrn.cs`,
`backend/API/Services/UsuarioService.cs`, `frontend/src/pages/secretaria/Usuarios.tsx`.

**Problema estrutural:** o CRN é texto livre de 3 a 20 caracteres, sem formato, sem unicidade e
sem obrigatoriedade. A geração aceita nutricionista sem registro e o snapshot nasce incompleto.

**Decisão:** objeto de valor com formato `CRN-<região>-<número>` (regiões configuráveis em
`Crn:RegioesValidas`, variável `Crn__RegioesValidas`), normalização única e unicidade no banco;
obrigatório exclusivo de `Nutricionista`, no cadastro e em toda operação de cardápio. Legado sem
CRN é diagnosticado e bloqueado de operar cardápio, mantendo o login. A validação é de formato,
não de autenticidade. A seleção de autora (`GET /api/nutricionistas`) é lida pela nutrição e pelo
`AdminSecretaria` da unidade.

**Implementação:** `CrnDeNutricionista` (normalização/validação), `PoliticaDeCrn` (gate de
cardápio), `Usuario` (regra por papel), `UsuarioService` (criação/edição, unicidade, trilha e
diagnóstico), migration `CrnUnico` e campo condicional em `Usuarios.tsx` (criação e mudança de
papel). A suíte cobre normalização, papel, duplicidade, seleção, diagnóstico, trilha e bloqueio.

**Regra derivada:** nenhum serviço de cardápio reimplementa a regra; todos usam
`PoliticaDeCrn` e a mesma mensagem.

## Spec 032 — correcao-geracao-automatica-cardapios

**Canonical creation path:** `backend/Domain/Services/SeletorDeRefeicao.cs`,
`backend/Domain/Services/ValidadorDeCardapioGerado.cs`,
`backend/Domain/Services/GeradorCardapioAutomatico.cs`,
`backend/API/Services/GeracaoCardapioService.cs`,
`frontend/src/pages/nutricao/cardapios/ParametrosGeracao.tsx`.

**Problema estrutural:** o gerador faz uma passagem gulosa e lança 422 quando a faixa calórica
não fecha; `usados` é global entre refeições e esgota o catálogo; o parâmetro
`ExcluirAlimentosIncompativeis` filtra o geral por restrições individuais; a nutricionista não é
validada por unidade nem CRN; e só o geral é persistido, sem adaptados.

**Decisão:** seleção com até 5 tentativas determinísticas por refeição, validação do cardápio
inteiro antes de abrir transação, geral sem filtro individual, nutricionista da mesma unidade
com CRN válido e criação de geral + adaptados na mesma transação. Falha devolve 422 com faixa
alcançável, categoria ausente e tentativas; nada é gravado.

**Regra derivada:** cardápio só é salvo depois de validado; adaptado pendente bloqueia a
publicação do geral; o seed devolvido reproduz o resultado.

## Spec 033 — cadastro-e-autorizacao-de-acesso

**Canonical creation path:** `backend/Domain/Entities/SolicitacaoDeAcesso.cs`,
`backend/Domain/Services/PoliticaDeAprovacao.cs`,
`backend/Application/Cadastro/AprovarAcessoService.cs`,
`backend/API/Controllers/SolicitacaoAcessoController.cs`,
`frontend/src/pages/secretaria/SolicitacoesAcesso.tsx`.

**Problema estrutural:** o autocadastro não registra o tipo pedido; "sem papel" representa ao
mesmo tempo não pediu, pediu e não foi visto, e foi recusado. A tela administrativa adivinha um
papel, o vínculo do responsável é uma segunda operação e a hierarquia convive com regras antigas
(`Responsavel` exige escola; `Nutricionista`/`AdminSecretaria` globais).

**Decisão:** separar pessoa (conta), intenção (solicitação com tipo e unidade) e ato
administrativo (aprovação). O cadastro cria usuário sem papel e solicitação `Pendente` na
unidade escolhida; o login fica bloqueado; o ADM da unidade aprova em uma transação, atribuindo
papel, escopo, CRN e vínculos do responsável. Recusa exige motivo; decisão concorrente devolve
409; papel administrativo nunca é concedido pela fila.

**Regra derivada:** pessoa, escola e unidade são conceitos distintos; a cadeia
unidade→escola→aluno é validada no backend e no banco, e o frontend não é fonte de integridade.

## Spec 034 — vinculo-responsavel-aluno

**Canonical creation path:** `backend/Domain/FiltroAluno.cs`,
`backend/Infrastructure/Repositories/AlunoRepository.cs`,
`backend/API/Services/UsuarioService.cs`, `backend/API/Services/VinculoService.cs`,
`frontend/src/components/SeletorDeAlunos.tsx`, `frontend/src/services/VinculoApi.ts`,
`frontend/src/pages/nutricao/alunos/VisualizarAlunos.tsx`.

**Problema estrutural:** o cadastro direto e a definição de papel criavam `Responsavel` sem
aluno; a aprovação já exigia vínculo, mas a conta podia ficar órfã por outro caminho. A busca de
aluno não existia (listava tudo) e a página de alunos mostrava apenas o contato manual, não as
contas vinculadas.

**Decisão:** vínculo é invariante de cadastro. `CriarUsuarioAdministrado` e `AtualizarUsuario`
recebem `alunosIds`, validam existência e unidade antes de mutar e gravam usuário, vínculos e
trilha na mesma transação (com `Salvar()` intermediário para obter o id). A busca (`busca`,
`escolaId`, `serie`) é filtro de negócio aplicado no SQL depois do recorte de escopo. O contato
manual do aluno e a conta vinculada permanecem dados distintos.

**Regra derivada:** nenhum fluxo cria `Responsavel` sem vínculo; a escrita de vínculo continua
nas rotas dedicadas e na matriz; a página de alunos exibe contato + contas e permite ao ADM
vincular/desvincular.

**Validação:** `VinculoResponsavelApiTests` (7 casos), suíte completa 830/830, `npm run build` e
`npm run lint` sem erro. Auditada em 2026-11-13; validação manual no navegador segue em PEND-047.

## Spec 035 — portal-pais-responsaveis

**Canonical creation path:** `frontend/src/pages/pais/` (layout, painel, cardápios, ocorrências,
solicitações e feedback), `frontend/src/components/` (seletor de filho e componentes extraídos de
ocorrências/solicitações/feedback), `frontend/src/services/CardapioApi.ts`,
`frontend/src/services/FeedbackApi.ts`, `backend/Domain/Entities/Feedback.cs`,
`backend/API/Services/FeedbackService.cs`.

**Problema estrutural:** a área de pais tem menu e vitrine pública de cardápios, mas não tem
visão dos filhos, cardápio servido por aluno, fluxo dedicado de ocorrências, solicitação de
alergia acompanhável nem feedback associado ao aluno. O `AlunoId` do feedback é usado apenas para
derivar a escola e não é persistido.

**Decisão:** criar um portal próprio em `/pais` consumindo os contratos existentes por aluno
(`GET /api/aluno`, `GET /api/aluno/{id}/cardapio`, ocorrências, solicitações e feedback). As
mudanças de modelo são persistir `AlunoId` + snapshot `AlunoNome` no feedback (FK `SetNull`) e
`Solicitacao.Anafilaxia`, informada pelo responsável no pedido de alergia. O painel agregado da
família continua sendo da Spec 025; a 035 não redefine esse contrato.

**Regra derivada:** o vínculo é a única autorização; ocorrência de responsável é sempre tipo
`Aluno`; restrição (e a marcação de anafilaxia) só entra no cadastro após aprovação do ADM;
pedido duplicado do mesmo aluno/restrição responde `409`; nenhuma tela exibe aluno fora do
vínculo. Status: auditada em 2026-11-13; T14 (validação manual no navegador) segue em PEND-050.

**Implementação:** `frontend/src/pages/pais/` (layout com cinco abas e rotas aninhadas),
componentes compartilhados em `components/` reutilizados por ocorrências/solicitações/feedback,
`FeedbackApi` com filtro por aluno e o checkbox de anafilaxia no formulário de solicitação. Os
gates passaram: backend 830/830, `npm run build` e `npm run lint` sem erro.
