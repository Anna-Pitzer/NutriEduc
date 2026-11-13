# Visão — NutriEduc

Documento de visão do produto e da engenharia. Cada bloco `Vxx` declara um valor
entregável e as specs que o entregam. A visão é estável; as specs evoluem.

Fontes: `correcoes.md`, `implementacoes.md`, `README.md` e verificação do código.

## Estado consolidado (2026-11-13)

| Bloco | Entrega | Status |
| --- | --- | --- |
| V1–V5 | domínio do aluno, CRUD, hash de senha, testes e autorização | entregue |
| V6 | cardápios, alimentos, ocorrências, solicitações, feedback e notificações | **parcial**: 019 (relatórios/exportação) não implementada |
| V7–V13 | documentação viva, erro/DI, rotas, telas de secretaria, bootstrap, histórico, DDD | entregue |
| V14 | painel por papel (secretaria, merendeira, família) | planejado (025) |
| V15 | substituição assistida por IA | planejado (026) |
| V16 | hierarquia unidade→escola→aluno | entregue (027, audited) |
| V17 | matriz de permissões e isolamento | entregue (028, audited) |
| V18 | cardápio geral e adaptado | **parcial**: backend entregue e portal dos pais exibe as duas versões; frontend de montagem/detalhe da nutrição pendente (029, PEND-042) |
| V19 | correção de cadastro com pedido e prova | entregue (030, audited) |
| V20 | CRN da nutricionista | entregue (031, audited) |
| V21 | geração automática com prova | entregue (032, implemented) |
| V22 | cadastro com intenção e aprovação com escopo | entregue (033, implemented) |
| V23 | vínculo responsável↔aluno obrigatório e busca | entregue (034, audited) |
| V24 | portal dos pais/responsáveis | entregue (035, audited); validação manual no navegador em PEND-050 |

`implemented` = código e testes verdes, sem certificação `/spec-test`; `audited` = código, testes
e documentação auditados (034 e 035 em 2026-11-13). A promoção automática continua bloqueada
pelo PEND-028.

---

### V1: Domínio de aluno com invariantes reais
**Problema:** `Aluno` é uma entidade anêmica. Dez campos obrigatórios aceitam vazio e
não existe um único método de domínio. O estado inválido é representável, portanto
inevitável.

**Valor entregue:** todo `Aluno` existente em memória ou no banco está em estado
válido, porque a validação acontece no construtor e em `AtualizarDados`, antes de
qualquer atribuição.

**Regra derivada:** validar tudo e só então atribuir — mesmo padrão já correto em
`Usuario.AtualizarPerfil`.

**Canonical creation path:** `backend/Domain/Entities/Aluno.cs`, `backend/Infrastructure/DataBase/ConnectionContext.cs`

**Delivered by:** Spec 003 (restricoes-aluno-protegidas), Spec 004 (dominio-aluno-invariantes)

---

### V2: CRUD completo na mesma entidade
**Problema:** alunos têm Criar, Ler e Excluir; cardápios também. O Update existe
apenas em perfil, que é outro recurso. O requisito de CRUD funcional não fecha.

**Valor entregue:** `C+R+U+D` completo de `Aluno`, exercitado pela interface, com
persistência confirmada por leitura posterior.

**Canonical creation path:** `backend/API/Controllers/AlunoController.cs`, `backend/API/Services/AlunoService.cs`, `frontend/src/services/AlunoApi.ts`, `frontend/src/pages/nutricao/alunos/VisualizarAlunos.tsx`, `frontend/src/routes/routes.tsx`

**Delivered by:** Spec 001 (crud-aluno-completo)

---

### V3: Credenciais não recuperáveis
**Problema:** senha persistida em texto claro e comparada com `==` em
`UsuarioService.VerificarLogin`.

**Valor entregue:** toda senha persistida é resultado de PBKDF2-SHA256 com salt único
por usuário; nenhum caminho compara senhas em claro, exceto a janela única de
migração de dados legados, que re-hasheia no primeiro login bem-sucedido.

**Canonical creation path:** `backend/Domain/Entities/HashSenha.cs`, `backend/Domain/Entities/Usuario.cs`, `backend/API/Services/UsuarioService.cs`

**Delivered by:** Spec 002 (seguranca-senha-hash)

---

### V4: Regressão detectável
**Problema:** nenhum teste automatizado. A avaliação atribuiu 0,0 à dimensão de
testes e toda mudança é uma aposta.

**Valor entregue:** as regras de domínio e os casos de uso críticos têm cobertura
automatizada; toda correção acompanha um teste que falha antes e passa depois.

**Canonical creation path:** `backend.Tests/backend.Tests.csproj`, `backend.Tests/Domain/`, `backend.Tests/Services/`, `backend.Tests/Fakes/`

**Delivered by:** Spec 005 (testes-automatizados-backend)

---

### V5: Dados de menores exigem sessão
**Problema:** todos os endpoints de `AlunoController` são públicos. Listagem,
consulta e exclusão de dados de crianças respondem a requisição anônima.

**Valor entregue:** nenhuma rota de dados de aluno responde sem sessão válida.

**Canonical creation path:** `backend/API/Controllers/AlunoController.cs`, `frontend/src/services/AlunoApi.ts`

**Delivered by:** Spec 006 (autorizacao-endpoints-aluno)

---

### V6: Escopo de produto modelado, não apenas desenhado
**Problema:** `implementacoes.md` descreve cinco perfis de usuário, cardápios,
alimentos, ocorrências e relatórios. O código modela um único tipo de usuário e não
possui domínio de cardápio, embora o frontend já chame seis rotas inexistentes.

**Valor entregue:** cardápios e alimentos existem como domínio com cálculo
nutricional próprio; a compatibilidade entre cardápio e restrições alimentares é uma
regra de domínio testada, não uma checagem visual. O ciclo operacional — ocorrências,
solicitações, feedback, notificações e relatórios exportáveis — existe como fluxo com
estado, histórico append-only e escopo por papel.

**Canonical creation path:** `backend/Domain/Entities/`, `backend/API/Controllers/`, `backend/API/Services/`

**Delivered by:** Spec 009 (papeis-e-estrutura-escolar), Spec 010 (restricoes-e-alergias), Spec 011 (catalogo-alimentos), Spec 012 (dominio-cardapio), Spec 013 (geracao-automatica-cardapios), Spec 014 (compatibilidade-cardapio-restricoes), Spec 015 (publicacao-historico-cardapio), Spec 016 (ocorrencias), Spec 017 (solicitacoes), Spec 018 (feedback-e-notificacoes), Spec 019 (relatorios-e-exportacao)

---

### V7: Documentação que não envelhece
**Problema:** `README.md` descreve endpoints corretos, mas o frontend chama rotas
inexistentes e uma rota com prefixo errado; documentação e contrato se separam
silenciosamente.

**Valor entregue:** a documentação é atualizada na mesma mudança que altera o
contrato, e a tabela de endpoints coincide com o mapa de controllers.

**Canonical creation path:** `README.md`, `frontend/src/routes/routes.tsx`, `frontend/src/services/`

**Delivered by:** Spec 008 (higiene-frontend)

### V8: Contrato de erro único e infraestrutura de DI
**Problema:** cada controller inventa o próprio texto e status de erro, e `UsuarioRepository` instancia `new ConnectionContext()` em vez de receber o `DbContext` pelo construtor, ignorando o ciclo de vida Scoped.

**Valor entregue:** um middleware global traduz `ArgumentException` em 400 e exceções não tratadas em 500, sempre no formato `{ "erro": "mensagem" }`; os repositórios recebem o `ConnectionContext` por injeção de dependência e os serviços são registrados com o mesmo ciclo de vida do contexto.

**Canonical creation path:** `backend/API/Middleware/`, `backend/Program.cs`, `backend/Infrastructure/Repository/UsuarioRepository.cs`

**Delivered by:** Spec 007 (contrato-erro-e-infra-di)

### V9: Contrato de rota uniforme e verificável
**Problema:** as onze rotas existentes seguem três padrões distintos. Duas devolvem corpo `string` no sucesso, o `id` é `{id:int}` em duas rotas e `{id}` em três, e o contrato `{ "erro": "mensagem" }` do Spec 007 vaza em validação de modelo (`ProblemDetails`), em `405` e em `415` (corpo vazio). `GET /api/usuario/perfil/{id}` ainda é anônimo e devolve CPF de qualquer usuário.

**Valor entregue:** um padrão único e observável — criação responde `201` com `Location` e objeto, toda rota com `id` usa `{id:int}`, e todo erro sob `/api/` responde `{ erro }` em português, inclusive modelo inválido, JSON malformado, verbo errado, content-type errado e rota inexistente. O perfil passa a exigir sessão e dono, sem expor `cpf` nem `senha`.

**Canonical creation path:** `backend/API/Middleware/`, `backend/API/Controllers/`, `backend/API/Services/`

**Delivered by:** Spec 020 (padronizacao-contrato-rotas)

### V10: Telas de secretaria e de escola
**Problema:** as áreas de secretaria e de escola existem apenas como páginas vazias. A Spec 009 entrega o domínio, a autorização e as rotas de gestão, mas criar escola, unidade, merendeira e vínculo de responsável continua exigindo HTTP manual.

**Valor entregue:** as telas de secretaria (escolas, unidades, usuários da rede) e de escola (unidade, merendeiras, responsáveis, vínculo responsável↔aluno) consomem as rotas já autorizadas, exibindo o erro do backend quando o conflito é de escopo ou de unicidade. O menu e a guarda de rota por papel já existem; esta spec entrega o conteúdo.

**Canonical creation path:** `frontend/src/pages/secretaria/`, `frontend/src/pages/escolas/`, `frontend/src/services/`

**Delivered by:** Spec 021 (telas-secretaria-e-escola)

### V11: Bootstrap de administrador por token
**Problema:** criar o primeiro (ou um novo) `AdminSecretaria` exigia editar o banco ou reiniciar o processo com variáveis de ambiente. Sem isso, um deploy limpo não tinha como abrir a primeira sessão administrativa. E um token permanente não distinguia unidade nem limitava o período de criação.

**Valor entregue:** duas vias de cadastro, ambas com token no corpo.

1. **Token de ambiente** (`Bootstrap__Token`, mínimo 32 caracteres, comparação em tempo constante): a página `/bootstrap` cria um `AdminSecretaria` informando o **nome exato de uma unidade** existente. O mesmo token vale **uma vez por unidade** — a unidade fica registrada e não pode consumir de novo.
2. **Token temporário**: um `AdminSecretaria` vinculado a uma unidade gera, no painel da secretaria, um token de **30 minutos** e **uso único**, exibido uma única vez. Enquanto vale, o botão fica desabilitado com contagem regressiva; ao expirar, o cadastro é bloqueado e o botão volta a liberar um novo token.

O banco guarda apenas o hash SHA-256 do token temporário. A rota responde `404` quando não há nenhuma via disponível, e `Bootstrap__Permitir=false` funciona como trava dura de ambiente. A tela de unidades (`/secretaria/unidades`) existe porque o nome exigido precisa estar cadastrado.

**Decisão de risco registrada:** o token de ambiente não expira — vale para sempre, mas uma vez por unidade. Tratar esse token como credencial de administrador é requisito operacional, não detalhe: rotacione-o após configurar a rede e use o token de 30 minutos para os administradores seguintes.

**Canonical creation path:** `backend/API/Controllers/BootstrapController.cs`, `backend/API/Services/BootstrapService.cs`, `backend/Domain/Entities/ConsumoBootstrap.cs`, `backend/Domain/Entities/TokenDeCadastro.cs`, `frontend/src/pages/comum/BootstrapAdmin.tsx`, `frontend/src/pages/secretaria/HomeSecretaria.tsx`, `frontend/src/pages/secretaria/Unidades.tsx`

**Delivered by:** Spec 022 (bootstrap-admin-por-token)

### V12: Trilha de alterações do aluno
**Problema:** a Spec 010 criou histórico append-only apenas para restrições. Alteração de nome, nascimento, responsável, telefone, observação, anafilaxia ou unidade não deixa rastro — quem consulta o cadastro não sabe o que mudou, quando, nem por quem.

**Valor entregue:** uma única trilha por aluno, somente inserção, com autor (nome copiado), data em UTC e o que mudou em cada evento: criação do cadastro, cada campo alterado com valor anterior e novo, e as restrições adicionadas/removidas. A leitura é paginada e respeita o escopo por papel, e a tela do aluno passa a mostrar uma linha do tempo única em vez de olhar apenas restrições. A rota antiga de histórico de restrições continua respondendo (filtrada da nova trilha) para não quebrar consumidores.

**Canonical creation path:** `backend/Domain/Entities/HistoricoAluno.cs`, `backend/Domain/Interface/IHistoricoAlunoRepository.cs`, `backend/API/Services/HistoricoAlunoService.cs`, `backend/API/Controllers/AlunoController.cs`, `frontend/src/pages/nutricao/alunos/VisualizarAlunos.tsx`

**Delivered by:** Spec 023 (historico-alteracoes-aluno)

### V13: Fronteiras DDD explícitas
**Problema:** a análise feita para a Spec 024 encontrou sete fronteiras borradas entre camadas: DTOs servindo entrada e saída ao mesmo tempo, regra de negócio em serviço de aplicação (hash de token, unicidade de e-mail, existência de unidade), `EscopoUsuario` — vocabulário de autorização — dentro do domínio, 19 `SaveChanges` espalhados pelos repositórios sem unidade de trabalho, `ExclusaoService` misturando política de negócio com escopo do solicitante, `Usuario.Senha` exposta e serviços com 370+ linhas acumulando casos de uso.

**Valor entregue:** fronteiras explícitas. Contratos `*Entrada`/`*Resposta` separados por operação; invariantes e políticas no domínio (hash de token como objeto de valor, unicidade e referências validadas na entidade); filtros de negócio no lugar de escopos de sessão dentro do domínio; uma unidade de trabalho transacional por caso de uso, tornando atômicas operações que hoje tocam dois agregados (bootstrap e vínculo); exclusão como serviço de domínio com a autorização na aplicação; e casos de uso divididos por intenção, cada um com um motivo único de mudança.

**Canonical creation path:** `backend/Application/`, `backend/Domain/Entities/`, `backend/Domain/Interface/`, `backend/API/Services/`, `backend/Infrastructure/Repositories/`

**Delivered by:** Spec 024 (fronteiras-ddd)

### V14: Cada papel vê o seu
**Problema:** as quatro áreas de acesso existem como estrutura (papéis, guarda de rota e menu), mas três delas são placeholders: secretaria não tem painel consolidado, merendeira não vê o cardápio do dia e a família não enxerga nada do filho. O conteúdo está espalhado por rotas internas que exigem conhecer o sistema.

**Valor entregue:** cada papel entra e vê exatamente o que lhe cabe. A secretaria recebe um painel com indicadores da rede e acesso à gestão; a merendeira vê o cardápio do dia da própria unidade com os alertas de restrição dos alunos; a família vê o cardápio do dia, as restrições e o relatório **do próprio filho**, nunca de outro aluno. Nenhuma tela nova expõe dado fora do escopo: a agregação e o filtro acontecem no SQL, e aluno fora do vínculo responde 403 sem revelar existência.

**Canonical creation path:** `backend/API/Controllers/PainelController.cs`, `backend/API/Services/PainelService.cs`, `frontend/src/pages/secretaria/HomeSecretaria.tsx`, `frontend/src/pages/merendeiras/HomeMerendeiras.tsx`, `frontend/src/pages/pais/HomePais.tsx`, `frontend/src/services/PainelApi.ts`

**Delivered by:** Spec 025 (painel-e-visao-por-papel)

**Status:** planejada — não implementada.

### V15: Substituição assistida por IA, com o determinístico decidindo
**Problema:** quando um item do cardápio é bloqueado por restrição, a troca é manual: o nutricionista precisa lembrar quais alimentos são equivalentes e conferir cada um contra as restrições. A decisão fica lenta e sujeita a esquecimento — e nenhuma spec cobre apoio à decisão, porque a geração (013) e a compatibilidade (014) são determinísticas por decisão de projeto.

**Valor entregue:** sugestão assistida com autoridade invertida de propósito. Primeiro o motor determinístico monta a lista de candidatos **já válidos** (mesma categoria, valores próximos, sem vínculo bloqueante); só então um provedor de IA — plugável e opcional — ranqueia e justifica entre esses candidatos. A escolha volta ao motor determinístico para revalidação e só entra no cardápio depois de aprovação humana. Sem provedor configurado, sem rede ou com falha, o fluxo continua pelo fallback determinístico; nenhum dado de aluno é enviado ao provedor, e cada sugestão aceita ou recusada fica registrada com autor e data.

**Canonical creation path:** `backend/Domain/Entities/SugestaoDeSubstituicao.cs`, `backend/Domain/Interface/ISugestorDeSubstituicao.cs`, `backend/API/Services/SubstituicaoService.cs`, `backend/Infrastructure/IA/SugestorPorIa.cs`, `frontend/src/pages/nutricao/cardapios/MontarCardapio.tsx`

**Delivered by:** Spec 026 (substituicao-assistida-ia)

**Status:** planejada — não implementada.

### V16: Hierarquia institucional única
**Problema:** a ADR-013 tornou a unidade entidade da rede e a escola deixou de pertencer a uma unidade. O aluno passou a existir apenas vinculado à unidade, `AdminEscola` e `Merendeira` perderam escopo verificável e a correção ficou registrada como PEND-024. Sem cadeia, cada consulta precisa inventar o próprio filtro.

**Valor entregue:** uma única cadeia `responsável → aluno → escola → unidade`. Escola pertence a exatamente uma unidade, aluno pertence a exatamente uma escola, `Aluno.UnidadeId` é projeção com escritor único e o bootstrap cria a unidade inicial. Todo escopo passa a ser derivável e testável.

**Regra derivada:** nenhum DTO de entrada aceita `unidadeId` de aluno; a unidade é sempre derivada da escola.

**Canonical creation path:** `backend/Domain/Entities/Escola.cs`, `backend/Domain/Entities/Aluno.cs`, `backend/Domain/Entities/Usuario.cs`, `backend/Infrastructure/Repositories/AlunoRepository.cs`, `backend/API/Services/BootstrapService.cs`, `frontend/src/pages/secretaria/Escolas.tsx`

**Delivered by:** Spec 027 (hierarquia-unidade-escola-aluno)

### V17: Permissão declarada e isolamento verificável
**Problema:** a permissão está declarada em treze specs, a `AdminSecretaria` herdou escopo global, a `Merendeira` é escopada por unidade e uma spec pede relatório global para nutricionista. Não existe um lugar único que responda "quem pode fazer X, sobre qual dado", e o isolamento depende de cada consulta lembrar do filtro.

**Valor entregue:** a matriz de permissões vira dado executável — função pura `(recurso, papel) → nível` e suíte de conformidade que cobre cada célula. O escopo de cada papel é único e derivado da cadeia institucional; a fronteira entre unidades responde 404, a fronteira de escola responde 403 e nenhuma escrita valida o alvo depois de mutar.

**Regra derivada:** recurso novo nasce sem permissão; toda célula da matriz precisa de teste; filtro de escopo é aplicado no SQL.

**Canonical creation path:** `backend/Application/Seguranca/MatrizDePermissoes.cs`, `backend/Application/Seguranca/AutorizacaoService.cs`, `backend/Infrastructure/Seguranca/FiltroDeEscopo.cs`, `backend/Domain/Entities/TrilhaAcao.cs`, `backend.Tests/Integration/MatrizPermissoesTests.cs`, `backend.Tests/Integration/AuditoriaAcaoTests.cs`

**Delivered by:** Spec 028 (matriz-permissoes-e-isolamento)

### V18: Duas versões de cardápio, uma origem
**Problema:** o sistema monta um único cardápio, detecta incompatibilidade com restrições e versiona publicação, mas não produz a refeição que o aluno com restrição realmente recebe. A troca fica manual, sem vínculo com o cardápio de origem e sem prova de preservação nutricional.

**Valor entregue:** duas versões relacionadas. O cardápio geral é a verdade editorial; para cada **escola** e conjunto distinto de restrições existe uma versão adaptada, gerada por função determinística que substitui apenas itens bloqueantes, escolhe o candidato de menor desvio nutricional e prova que a refeição ficou dentro da tolerância. Versão que não prova fica `PendenteDeRevisao` e não pode ser publicada nem servida. A criação exige nutricionista identificado por nome e CRN válido, com snapshot de autor.

**Regra derivada:** item bloqueante nunca é servido; aluno cujo conjunto não tem versão publicada recebe `Indisponivel`, não o cardápio geral.

**Canonical creation path:** `backend/Domain/Entities/Cardapio.cs`, `backend/Domain/Services/AdaptacaoDeCardapio.cs`, `backend/API/Services/AdaptarCardapioService.cs`, `backend/API/Services/CardapioServidoService.cs`, `backend/API/Services/ContextoDeRestricoesService.cs` (UI de montagem, detalhe e servir pendente — Spec 029, T12–T13)

**Delivered by:** Spec 029 (cardapio-geral-e-adaptado)

### V19: Correção de cadastro com pedido e prova
**Problema:** o responsável não tem canal para pedir correção dos dados cadastrais do filho; o ADM altera diretamente e nada registra que existiu um pedido. A Spec 017 cobre apenas cardápio, ingrediente, restrição e alergia.

**Valor entregue:** um tipo novo de solicitação, imutável, com snapshot do valor no momento do pedido. A aprovação é atômica: compõe o estado final, valida pelas regras do próprio aluno e recusa quando o valor atual divergiu do snapshot. Toda aplicação gera evento na trilha do aluno com autor e origem, e toda decisão notifica o responsável.

**Regra derivada:** aprovar não aplica o que foi pedido se o dado mudou; nada é aplicado parcialmente.

**Canonical creation path:** `backend/Domain/Entities/Solicitacao.cs`, `backend/Domain/Entities/AlteracaoSolicitada.cs`, `backend/Domain/Entities/SolicitacaoAlteracao.cs`, `backend/API/Services/SolicitacaoService.cs` (ramo `DadosCadastrais`), `frontend/src/pages/solicitacoes/SolicitacoesPage.tsx`

**Delivered by:** Spec 030 (solicitacao-alteracao-dados)

### V20: CRN como identidade profissional da nutricionista
**Problema:** o CRN existe no cadastro desde PEND-038, mas é opcional, aceita qualquer texto de 3 a 20 caracteres e não bloqueia nada. A geração aceita nutricionista sem registro e o snapshot de autoria nasce incompleto.

**Valor entregue:** CRN como identidade profissional. Objeto de valor com formato canônico `CRN-<região>-<número>`, normalização única, unicidade por usuário e obrigatoriedade exclusiva do papel `Nutricionista` — no cadastro e em toda operação de cardápio. Nutricionista legada sem CRN continua autenticando, entra no diagnóstico e fica bloqueada de criar, gerar, adaptar ou publicar cardápio até regularizar. O sistema valida formato, não autenticidade, e isso está declarado.

**Regra derivada:** nenhum fluxo de cardápio reimplementa a regra de CRN; todos usam a mesma política e a mesma mensagem.

**Canonical creation path:** `backend/Domain/Entities/CrnDeNutricionista.cs`, `backend/Domain/Entities/Usuario.cs`, `backend/Domain/Services/PoliticaDeCrn.cs`, `backend/API/Services/UsuarioService.cs`, `frontend/src/pages/secretaria/Usuarios.tsx`

**Delivered by:** Spec 031 (perfil-crn-nutricionista)

### V21: Geração automática que falha com motivo e não grava lixo
**Problema:** a geração automática falha de forma intermitente. O gerador faz uma única passagem gulosa e, se a faixa calórica não fecha, lança 422 sem nova tentativa; o conjunto `usados` é global entre refeições e esgota o catálogo; `ExcluirAlimentosIncompativeis` filtra o cardápio geral por restrições individuais, contrariando a definição de geral; o nutricionista é validado por papel, mas não por unidade nem CRN; e só um cardápio é criado, sem versão adaptada. Evidências em `GeradorCardapioAutomatico.cs:51-55,96-104`, `GeracaoCardapioService.cs:128-129,137,139-148,180-207` e `ParametroGeracao.cs:138-146`.

**Valor entregue:** geração que propõe e prova. Tentativas limitadas e determinísticas por refeição, validação do cardápio inteiro antes de abrir transação, geral sem filtro de restrições individuais, nutricionista da mesma unidade com CRN válido, e criação do geral e das versões adaptadas por escola e conjunto na mesma transação. Falha devolve 422 com faixa alcançável, categoria ausente e tentativas — e não grava nada. O seed devolvido permite reproduzir o resultado.

**Regra derivada:** nenhum cardápio é salvo sem passar pela validação; nenhuma versão adaptada é publicada sem prova de compatibilidade; o geral nunca é fallback de aluno com restrição.

**Canonical creation path:** `backend/Domain/Services/SeletorDeRefeicao.cs`, `backend/Domain/Services/ValidadorDeCardapioGerado.cs`, `backend/Domain/Services/GeradorCardapioAutomatico.cs`, `backend/API/Services/GeracaoCardapioService.cs`, `frontend/src/pages/nutricao/cardapios/ParametrosGeracao.tsx`

**Delivered by:** Spec 032 (correcao-geracao-automatica-cardapios)

### V22: Cadastro com intenção e aprovação com escopo
**Problema:** o autocadastro cria a conta sem papel, mas não registra o que a pessoa pediu (`CriarUsuarioEntrada` não tem tipo). A tela administrativa adivinha `Merendeira` para qualquer conta sem papel, não existe fila de solicitações, o vínculo do responsável é uma segunda operação separada e contas pendentes se confundem com recusadas. A hierarquia ainda convive com regras antigas: `Responsavel` exige escola, `Nutricionista` e `AdminSecretaria` são tratados como globais e `EscopoUsuario.Global` ignora o isolamento por unidade.

**Valor entregue:** três estados explícitos — pessoa (conta), intenção (solicitação com tipo e unidade) e ato administrativo (aprovação). O cadastro pergunta o tipo, não concede nada e cria uma solicitação na unidade escolhida; o login fica bloqueado até a decisão. O ADM vê apenas a fila da própria unidade, aprova em uma transação e, no caso do responsável, vincula um ou mais alunos no mesmo ato. Recusa exige motivo; decisão concorrente devolve 409; papel administrativo nunca é concedido pela fila.

**Regra derivada:** pessoa, escola e unidade são conceitos distintos; o backend valida a cadeia unidade→escola→aluno em toda escrita e o frontend não é fonte de integridade.

**Canonical creation path:** `backend/Domain/Entities/SolicitacaoDeAcesso.cs`, `backend/Domain/Services/PoliticaDeAprovacao.cs`, `backend/Application/Cadastro/AprovarAcessoService.cs`, `backend/API/Controllers/SolicitacaoAcessoController.cs`, `frontend/src/pages/secretaria/SolicitacoesAcesso.tsx`

**Delivered by:** Spec 033 (cadastro-e-autorizacao-de-acesso)

### V23: Vínculo responsável↔aluno obrigatório e verificável
**Problema:** a conta de responsável podia existir sem aluno — pela aprovação já era exigido vínculo, mas o cadastro direto e a definição de papel criavam contas soltas; a tela de aprovação listava todos os alunos sem busca e a página de alunos não mostrava as contas vinculadas.

**Valor entregue:** toda conta `Responsavel` nasce e permanece com ao menos um aluno, criado na mesma transação do cadastro. O ADM localiza o aluno por nome, escola e série e confirma o vínculo no próprio ato; a página de alunos exibe o contato do cadastro e as contas vinculadas, permitindo vincular e desvincular. O responsável continua lendo apenas os próprios alunos.

**Regra derivada:** nenhum fluxo cria `Responsavel` sem vínculo; o contato manual do aluno e a conta vinculada são dados distintos; a busca apenas estreita o escopo já autorizado.

**Delivered by:** Spec 034 (vinculo-responsavel-aluno)

### V24: Portal dos pais/responsáveis
**Problema:** a área de pais existe apenas como menu e vitrine de cardápios públicos. O responsável não tem uma visão dos próprios filhos, não consulta o cardápio que cada aluno realmente recebe, não tem fluxo dedicado para ocorrências, solicitações de alergia e feedback, e o feedback enviado nem sequer registra a qual aluno se refere.

**Valor entregue:** um portal próprio dos pais, com seleção de filho e isolamento pelo vínculo já implementado. O responsável consulta o cardápio servido por dia e refeição (geral e adaptado do filho), lê e relata ocorrências dos filhos, solicita inclusão de alergia/restrição — informando quando a reação é anafilática — e acompanha a decisão do profissional, e envia feedback com aluno opcional. Todas as ações reutilizam os contratos existentes; as mudanças de modelo são a associação do feedback ao aluno e a marcação de anafilaxia no pedido de alergia.

**Regra derivada:** nenhuma tela mostra aluno fora do vínculo; solicitação de restrição só altera o cadastro após aprovação; ocorrência de responsável é sempre do tipo aluno; o portal não duplica o painel agregado da Spec 025.

**Status:** entregue e auditada em 2026-11-13; validação manual no navegador pendente (PEND-050).

**Delivered by:** Spec 035 (portal-pais-responsaveis)
