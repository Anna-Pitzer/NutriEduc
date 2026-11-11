<img width="100%" src="https://capsule-render.vercel.app/api?type=slice&height=150&color=D92567&reversal=false"/>

# NutriEduc

## Alunos:

- Arthur Torres Candido | Matrícula: 06014213
- Júlio César da Silva Paula | Matrícula: 06016514
- Kayke Silva de Mattos Soares | Matrícula: 06013747
- Maria Anna Silva Pitzer | Matrícula:06014353
- Pedro Vinícius de Almeida Thomaz | Matrícula: 06014305

Curso: Ciência da Computação | Turma: A 

Aplicação web para apoiar atividades de educação alimentar em contexto escolar. O projeto reúne cadastro e consulta de alunos, dados de saúde e restrições alimentares, telas de cardápio e gerenciamento de usuários.

> **Estado da integração:** cadastro, listagem e exclusão de alunos estão conectados ao backend. As operações de cardápio ainda não possuem controller correspondente. Consulte [Pendências conhecidas](#pendências-conhecidas).

## Funcionalidades

- Cadastro e autenticação de usuários com sessão baseada em cookie.
- Gestão de perfil do usuário.
- Telas para cadastro e consulta de alunos, incluindo dados de nascimento, contato de emergência e anafilaxia.
- Telas para consulta e montagem de cardápios.
- Páginas de suporte, termos de uso e política de privacidade.

A existência de uma tela no frontend não significa que todas as operações tenham uma API implementada; as rotas disponíveis estão listadas abaixo.

## Tecnologias

- **Frontend:** React, TypeScript, Vite, React Router, Tailwind CSS e Lucide.
- **Backend:** ASP.NET Core Web API com .NET 10.
- **Persistência:** SQLite com Entity Framework Core e migrations.
- **Lint do frontend:** Oxlint.

## Estrutura

```text
NutriEduc/
├── backend/
│   ├── API/Controllers/       # Endpoints HTTP
│   ├── API/Services/          # Regras de aplicação
│   ├── Application/           # DTOs
│   ├── Domain/                # Entidades e contratos
│   ├── Infrastructure/        # EF Core, SQLite e repositórios
│   ├── Migrations/            # Migrations do banco
│   ├── Program.cs             # Inicialização e porta da API
│   └── backend.csproj
├── frontend/
│   ├── src/components/        # Componentes compartilhados
│   ├── src/context/           # Estado de autenticação
│   ├── src/pages/             # Páginas da aplicação
│   ├── src/routes/            # Rotas e proteção de páginas
│   ├── src/services/          # Chamadas HTTP
│   └── package.json
└── README.md
```

## Requisitos

- .NET SDK 10.
- Node.js compatível com Vite 8 (Node.js 20.19+ ou 22.12+).
- npm, incluído com o Node.js.

## Configuração e execução

Execute o backend e o frontend em terminais separados.

### 1. Configurar a URL da API

O Vite lê `VITE_API_URL` a partir do ambiente do frontend. Crie ou atualize `frontend/.env` com:

```dotenv
VITE_API_URL=http://localhost:5001
```

A porta `5001` é definida pelo backend em `backend/Program.cs`. O frontend encaminha as requisições `/api` para essa URL por meio do proxy do Vite. Não coloque segredos nesse arquivo; arquivos `.env` locais não devem ser versionados.

### 2. Iniciar o backend

No primeiro terminal, a partir da raiz do repositório:

```powershell
cd backend
dotnet restore
dotnet run
```

A API inicia em `http://localhost:5001`. Na inicialização, o backend aplica automaticamente as migrations pendentes. A conexão atual usa SQLite (`database.db`), configurado em `backend/Infrastructure/DataBase/ConnectionContext.cs`.

> **Se `dotnet run` falhar com `MSB3027`/`MSB3021` ("backend.exe está sendo usado por outro processo")** ou `address already in use`, há uma instância anterior rodando. Encerre-a antes de subir de novo:
>
> ```powershell
> Get-Process backend -ErrorAction SilentlyContinue | Stop-Process -Force
> ```
>
> O mesmo vale para `dotnet test` e `dotnet ef`: o build copia `backend.exe` para `bin/` e não consegue substituir o arquivo em uso.

### 3. Iniciar o frontend

No segundo terminal, a partir da raiz do repositório:

```powershell
cd frontend
npm install
npm run dev
```

O Vite informa no terminal o endereço local da interface, normalmente `http://localhost:5173`.

## Comandos úteis

Execute os comandos do frontend dentro de `frontend/`:

```bash
npm run dev       # servidor de desenvolvimento
npm run build     # verificação TypeScript e build de produção
npm run lint      # lint com Oxlint
npm run preview   # pré-visualização do build
```

Execute os comandos do backend a partir da raiz do repositório:

```bash
dotnet restore backend/backend.csproj
dotnet build backend/backend.csproj
dotnet run --project backend/backend.csproj
```

## Páginas do frontend

| Caminho | Página | Acesso |
| --- | --- | --- |
| `/login` | Login | Público |
| `/` | Início | Protegido |
| `/cadastro` | Cadastro de alunos | Protegido |
| `/vizualunos` | Visualização de alunos | Protegido |
| `/perfil` | Perfil do usuário | Protegido |
| `/cardapio` | Cardápios | Protegido |
| `/cardapio/montar/:refeicao` | Montagem de cardápio | Protegido |
| `/suporte` | Suporte | Protegido |
| `/termosdeuso` | Termos de uso | Protegido |
| `/politicadeprivacidade` | Política de privacidade | Protegido |

As rotas protegidas dependem da sessão autenticada.

## Endpoints disponíveis

Os caminhos abaixo são definidos pelos controllers atuais. O ASP.NET Core aceita diferenças entre maiúsculas e minúsculas nos caminhos, mas mantenha a grafia consistente no frontend.

### Usuários

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `POST` | `/api/usuario/registrar` | Registra um usuário | Não |
| `POST` | `/api/usuario/login` | Autentica e cria a sessão | Não |
| `GET` | `/api/usuario/me` | Retorna o usuário da sessão | Sim |
| `POST` | `/api/usuario/logout` | Encerra a sessão | Sim |
| `GET` | `/api/usuario/perfil/{id}` | Consulta perfil pelo ID | Sim; o ID deve corresponder à sessão |
| `PUT` | `/api/usuario/perfil/{id}` | Atualiza o perfil do usuário autenticado | Sim; o ID deve corresponder à sessão |

A autenticação usa cookie HTTP-only chamado `NutriEduc.Auth`, com validade de oito horas. As chamadas feitas diretamente pelo navegador devem incluir credenciais (`credentials: "include"`).

### Senhas

As senhas nunca são persistidas em claro. `Usuario.DefinirSenha` deriva um hash PBKDF2-SHA256 com salt aleatório de 16 bytes e 100.000 iterações, gravado no formato `pbkdf2-sha256$<iteracoes>$<salt>$<hash>` (salt e hash em base64). `Usuario.SenhaConfere` compara em tempo constante.

Registros gravados antes dessa mudança ainda têm a senha em claro: no primeiro login válido o serviço compara uma única vez, grava o hash e passa a autenticar por ele. Não houve alteração de schema — a coluna `Senha` continua `TEXT` no SQLite; a migration `HashSenhaUsuario` existe, mas é documental e não executa operação de schema.

### Alunos

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `POST` | `/api/aluno/registrar` | Cadastra um aluno; `escolaId` é obrigatório | `AdminSecretaria` |
| `GET` | `/api/aluno` | Lista alunos no escopo do papel; aceita `escolaId`/`unidadeId` | Sessão válida |
| `GET` | `/api/aluno/perfil/{id}` | Consulta um aluno pelo ID | Sessão válida; escopo por papel |
| `PUT` | `/api/aluno/{id}` | Atualiza um aluno existente; não muda de unidade | `AdminSecretaria` |
| `POST` | `/api/aluno/{id}/transferir-escola` | Transfere o aluno para outra escola da mesma unidade | `AdminSecretaria` |
| `DELETE` | `/api/aluno/{id}` | Exclui um aluno | `AdminSecretaria` |
| `GET` | `/api/aluno/{id}/historico` | Trilha de alterações do aluno (criação, campos e restrições), paginada e em ordem decrescente | Sim; `Nutricionista`, `AdminEscola`, `Merendeira` da unidade e `Responsavel` vinculado |
| `GET` | `/api/aluno/{id}/restricoes/historico` | Histórico de restrições do aluno, paginado e em ordem decrescente (derivado da trilha; rota mantida por compatibilidade) | Sim; `Nutricionista`, `AdminEscola`, `Merendeira` da unidade e `Responsavel` vinculado |
| `GET` | `/api/aluno/{id}/cardapio` | Versão servida ao aluno na data: geral para aluno sem restrição, adaptado publicado para aluno com restrição ou `Indisponivel` com alerta | Sim; mesmo escopo de leitura do aluno |

O campo de nascimento do DTO é `DateOnly`; envie uma data no formato `yyyy-MM-dd`. `anafilaxia` deve ser um booleano JSON (`true` ou `false`). As restrições alimentares são armazenadas como uma coleção JSON na tabela `Alunos`.

Cada aluno pertence a uma escola (`EscolaId`), obrigatória no cadastro e na edição; a unidade (`UnidadeId`) é derivada da escola pelo servidor. `Aluno.Turma.Escola` continua apenas como rótulo de exibição.

### Restrições e alergias

O catálogo vive na tabela `RestricoesAlimentares`, com `Categoria` (`Alergia` ou `Restricao`) e `Ativa`. O vínculo do aluno continua na coluna JSON `Restricoes` da tabela `Alunos` (contrato inalterado), agora preenchida apenas com nomes do catálogo ativo — a unicidade do catálogo compara o nome sem caixa e sem acento.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/restricoes` | Lista o catálogo; aceita `categoria` (`Alergia`/`Restricao`) e `incluirInativas` | Sim |
| `POST` | `/api/restricoes` | Cria um item do catálogo | Sim; só `Nutricionista` |
| `PUT` | `/api/restricoes/{id}` | Edita nome e categoria | Sim; só `Nutricionista` |
| `POST` | `/api/restricoes/{id}/desativar` | Desativa sem apagar vínculos existentes | Sim; só `Nutricionista` |
| `POST` | `/api/restricoes/{id}/reativar` | Reativa um item desativado | Sim; só `Nutricionista` |

`incluirInativas=true` responde `403` para quem não é `Nutricionista`. Toda gravação de aluno valida os nomes contra o catálogo ativo e responde `400` com `{ "erro": "Restrição desconhecida: <nome>." }` quando o nome não existe ou está inativo. Cada alteração efetiva do conjunto grava um evento append-only (`Adicionada` ou `Removida`) com o nome do autor e a data em UTC; na criação, um evento por restrição informada.

### Catálogo de alimentos

O catálogo vive em `CategoriasAlimento`, `Alimentos` e `AlimentosRestricoes`. O alimento guarda a composição por 100 g em colunas `decimal(10,2)` e uma categoria; o vínculo com uma restrição é apenas o par `(AlimentoId, RestricaoId)` — não existe motivo no vínculo, porque a severidade é sempre a `Categoria` da restrição (`Alergia` bloqueia, `Restricao` alerta). O nome é único no catálogo sem diferenciar caixa nem acento.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/alimentos` | Lista paginada; aceita `busca`, `categoriaId`, `pagina`, `tamanho`, `incluirInativos` e `ordenar` (`nome`, `nome-desc`, `categoria` ou `recentes`) | `AdminSecretaria`, `AdminEscola`, `Nutricionista` ou `Merendeira` |
| `GET` | `/api/alimentos/{id}` | Detalhe com composição, categoria e vínculos resolvidos | `AdminSecretaria`, `AdminEscola`, `Nutricionista` ou `Merendeira` |
| `POST` | `/api/alimentos` | Cria alimento com os seis valores nutricionais e os vínculos | Sim; só `Nutricionista` |
| `PUT` | `/api/alimentos/{id}` | Edita nome, categoria, composição e vínculos | Sim; só `Nutricionista` |
| `POST` | `/api/alimentos/{id}/desativar` | Desativa (soft delete); continua legível por id | Sim; só `Nutricionista` |
| `POST` | `/api/alimentos/{id}/reativar` | Reativa um alimento desativado | Sim; só `Nutricionista` |
| `GET` | `/api/categorias-alimento` | Lista categorias; `incluirInativas` é exclusivo da nutrição | `AdminSecretaria`, `AdminEscola`, `Nutricionista` ou `Merendeira` |
| `POST` | `/api/categorias-alimento` | Cria categoria | Sim; só `Nutricionista` |
| `PUT` | `/api/categorias-alimento/{id}` | Renomeia categoria | Sim; só `Nutricionista` |
| `POST` | `/api/categorias-alimento/{id}/desativar` | Desativa a categoria; ela não recebe alimento novo | Sim; só `Nutricionista` |
| `POST` | `/api/categorias-alimento/{id}/reativar` | Reativa a categoria | Sim; só `Nutricionista` |

A busca usa o nome normalizado: tenta prefixo primeiro e, somente quando não há resultado, repete por "contém" — por isso `leite` encontra `Leite de coco`. `Responsavel` recebe `403` na leitura do catálogo; `incluirInativos=true` fora da nutrição também responde `403`. Ordenação fora de `nome`, `nome-desc`, `categoria` e `recentes` responde `400` (o padrão é `nome`). Nome duplicado responde `409`; valor nutricional negativo ou com três casas decimais responde `400` sem persistir; categoria desativada não aceita alimento novo (`400`); o mesmo vínculo repetido responde `400`. Desativar o alimento não o esconde da leitura por id, e desativar a categoria não esconde os alimentos dela da listagem.

### Cardápio

O cardápio vive em `Cardapios`, `Refeicoes` e `ItensRefeicao`. O período é um tipo complexo (`Periodo_Inicio` e `Periodo_Fim`), a ordem da refeição é única no cardápio e o item usa a chave `(RefeicaoId, AlimentoId)` — o mesmo alimento não repete na refeição, mas pode em outra. Os valores nutricionais são **derivados**: `valores do alimento × gramas / 100`, arredondados a duas casas por item e somados; nada disso é persistido.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/cardapios` | Lista paginada do par geral/adaptado; aceita `unidadeId`, `data`, `situacao`, `pagina` e `tamanho` | Sim; `AdminSecretaria`/`Nutricionista` da unidade e `AdminEscola`/`Merendeira` da própria escola |
| `GET` | `/api/cardapios/publicados` | Publicados cujo período inclui a data (padrão: hoje); exige `unidadeId`; aceita `data`, `refeicao`, `pagina` e `tamanho` | Não |
| `GET` | `/api/cardapios/{id}` | Detalhe com tipo, origem, escola, conjunto, status de adaptação, refeições, totais, substituições, pendências e confirmações | Sim; escopo da unidade ou da própria escola |
| `GET` | `/api/cardapios/contexto-restricoes` | Alunos ativos e restrições por escola da unidade; aceita `escolaId` | Sim; `Nutricionista` da unidade e `AdminSecretaria`/`AdminEscola` no escopo |
| `GET` | `/api/cardapios/{id}/adaptados` | Versões adaptadas da origem, com escola, conjunto, status, substituições e pendências | Sim; escopo da unidade ou da própria escola |
| `GET` | `/api/cardapios/{id}/compatibilidade` | Análise de compatibilidade com as restrições dos alunos (bloqueantes e alertas) | Sim; `Nutricionista` ou `Merendeira` da unidade |
| `POST` | `/api/cardapios` | Cria o rascunho geral e, na mesma transação, uma versão adaptada por `(escola, conjunto)`; exige `nutricionistaId` com CRN válido e aceita `confirmarIncompatibilidades` | Sim; só `Nutricionista` |
| `POST` | `/api/cardapios/{id}/adaptar` | Adapta o geral para uma escola; idempotente por `(origem, escola, conjunto)` e limitada a 50 versões (`422`) | Sim; só `Nutricionista` com CRN |
| `PUT` | `/api/cardapios/{id}` | Substitui nome, período e refeições de um rascunho de forma atômica | Sim; só `Nutricionista` |
| `POST` | `/api/cardapios/{id}/duplicar` | Copia a composição para um novo período como rascunho | Sim; só `Nutricionista` |
| `DELETE` | `/api/cardapios/{id}` | Exclui um rascunho com refeições e itens | Sim; só `Nutricionista` |
| `POST` | `/api/cardapios/{id}/publicar` | Publica um rascunho; exige CRN válido; bloqueia por incompatibilidade (`422`), sobreposição (`409`) e adaptado pendente/desatualizado (`409`) | Sim; só `Nutricionista` |
| `POST` | `/api/cardapios/{id}/cancelar` | Cancela com `motivo` obrigatório (2–500) | Sim; só `Nutricionista` |
| `POST` | `/api/cardapios/{id}/revisao` | Cria uma revisão (`Versao + 1`) de um cardápio publicado | Sim; só `Nutricionista` |
| `GET` | `/api/cardapios/{id}/historico` | Linha do tempo de eventos do cardápio, decrescente e paginada | Sim; `Nutricionista` ou `Merendeira` da unidade |
| `GET` | `/api/cardapios/alteracoes` | Feed de alterações no escopo a partir de `desde`, paginado | Sim; `Nutricionista` ou `Merendeira` da própria unidade |

### Solicitações

O fluxo vive em `Solicitacoes` e `HistoricoSolicitacoes`. A solicitação é imutável no conteúdo; o status segue `Aberta → EmAnalise → Aprovada | Recusada`, com `Aprovada → Concluida` após a aplicação do efeito. A decisão é separada por tipo: nutrição decide cardápio/ingrediente; ADM decide restrição e dados cadastrais. O histórico é append-only, com autor e data UTC.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/solicitacoes` | Lista paginada com filtros `tipo`, `status`, `unidadeId`, `escolaId`, `alunoId`, `cardapioId`, `de`, `ate`, `pagina` e `tamanho` | Sessão; escopo por papel e vínculo |
| `GET` | `/api/solicitacoes/{id}` | Detalhe com alvo, resposta e trilha de status | Sessão; escopo por papel e vínculo |
| `POST` | `/api/solicitacoes` | Cria `MudancaCardapio`, `Ingrediente`, `FaltaIngrediente`, `InclusaoAlergia` ou `AlteracaoRestricao` conforme o papel | `Merendeira`, `AdminEscola`, `Nutricionista` ou `Responsavel` vinculado |
| `POST` | `/api/solicitacoes/{id}/analise` | `Aberta → EmAnalise` | Dono do tipo: `Nutricionista` ou `AdminSecretaria` |
| `POST` | `/api/solicitacoes/{id}/aprovar` | Aprova com `resposta`; restrição é aplicada ao aluno na mesma transação | Dono do tipo |
| `POST` | `/api/solicitacoes/{id}/recusar` | Recusa com `resposta` (2–500) | Dono do tipo |
| `POST` | `/api/solicitacoes/{id}/concluir` | `Aprovada → Concluida` | Dono do tipo |
| `POST` | `/api/solicitacoes/{id}/cancelar` | `Aberta → Cancelada` | Somente o solicitante |

A aprovação de `MudancaCardapio`, `Ingrediente` e `FaltaIngrediente` não altera dados automaticamente; a resposta informa que a edição do cardápio pertence à Spec 015. Aprovar uma restrição inexistente no catálogo devolve `400` e mantém `EmAnalise`.

O tipo `DadosCadastrais` é criado apenas por `Responsavel` vinculado ao aluno, com `justificativa` (10–500) e `alteracoes[]` nos campos `nome`, `dataNascimento`, `nomeResponsavel`, `telefoneResponsavel` e `observacao`. O servidor lê o valor atual e grava o snapshot; a aprovação do ADM aplica todos os campos de uma vez por `Aluno.AtualizarDados`, devolve `409` quando o valor mudou desde o pedido e registra na trilha do aluno a origem `Solicitacao:<id>`. Um segundo pedido aberto para o mesmo campo devolve `409` com o id existente. Cada mudança de status emite uma notificação `SolicitacaoStatus` ao solicitante e ao decisor, sem dado clínico.

### Solicitações de acesso

O cadastro público (`POST /api/usuario/registrar`) pede o tipo (`Nutricionista`, `Merendeira` ou `Responsavel`), a unidade de destino e o CRN (somente nutricionista). Ele cria a conta sem papel e uma `SolicitacaoDeAcesso` pendente na mesma transação; a conta só autentica após a aprovação do ADM da unidade. A fila é exclusiva do `AdminSecretaria` da unidade (outra unidade devolve `404`) e a aprovação grava, numa única transação, papel, escopo, CRN, vínculos, trilha e notificação. Nenhuma aprovação concede papel administrativo.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/solicitacoes-acesso` | Lista a fila da unidade com filtros `status`, `tipo`, `busca`, `pagina` e `tamanho` | `AdminSecretaria` da unidade |
| `GET` | `/api/solicitacoes-acesso/{id}` | Detalhe com dados do solicitante e da decisão | `AdminSecretaria` da unidade |
| `POST` | `/api/solicitacoes-acesso` | Reconsideração: nova solicitação para usuário existente | `AdminSecretaria` da unidade |
| `POST` | `/api/solicitacoes-acesso/{id}/aprovar` | Aprova papel, escopo, CRN e vínculos numa transação | `AdminSecretaria` da unidade |
| `POST` | `/api/solicitacoes-acesso/{id}/recusar` | Recusa com `motivo` (5–500) | `AdminSecretaria` da unidade |

A conta pendente/recusada devolve `401` genérico no login, sem confirmar a existência do e-mail. Aprovar `Responsavel` exige ao menos um aluno da unidade e cria os vínculos no mesmo ato; aprovar `Merendeira` exige escola da unidade; aprovar `Nutricionista` grava o CRN validado. Duas decisões simultâneas resultam em uma `200` e uma `409`.

### Ocorrências

O fluxo vive em `Ocorrencias` e `HistoricoOcorrencias`. A ocorrência tem um `Tipo` (`Aluno`, `Refeicao`, `Alimento` ou `Outro`) com vínculo opcional coerente com o tipo, `Descricao` (5–1000) e ciclo `Aberta → EmAnalise → Resolvida → Arquivada`, com reabertura `EmAnalise → Aberta`. O autor (nome) e os nomes de aluno/cardápio/refeição/alimento são snapshots (RNF3/ADR-009): renomear ou excluir o registro relacionado não reescreve o que a ocorrência exibe. A escola é derivada do aluno, ou do autor, e nula apenas na ocorrência de unidade registrada por `Nutricionista`. O histórico é append-only na mesma transação da mudança de status.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/ocorrencias` | Lista paginada com filtros `tipo`, `status`, `alunoId`, `unidadeId`, `escolaId`, `de`, `ate`, `pagina` e `tamanho` | Sessão; escopo por papel e vínculo |
| `GET` | `/api/ocorrencias/{id}` | Detalhe com vínculos resolvidos (aluno, cardápio, refeição e alimento) e histórico | Sessão; escopo por papel e vínculo |
| `POST` | `/api/ocorrencias` | Cria `Aluno`, `Refeicao`, `Alimento` ou `Outro` conforme o papel | `Merendeira`, `AdminEscola`, `Nutricionista` ou `Responsavel` vinculado |
| `POST` | `/api/ocorrencias/{id}/status` | Muda o status com `observacao` (2–500); transições inválidas devolvem `409` | `Nutricionista` da unidade ou `AdminEscola` da escola |
| `POST` | `/api/ocorrencias/{id}/cancelar` | `Aberta → Cancelada` apenas no mesmo dia civil de `America/Sao_Paulo` | Somente o autor |

A ocorrência não é apagada; o encerramento é `Arquivada`. O "mesmo dia" do cancelamento é o dia civil de `America/Sao_Paulo` (UTC-3), não o dia UTC. Filtro de `escolaId`/`unidadeId` fora do alcance da sessão devolve `403`; recurso de outra unidade devolve `404`. A resposta nunca expõe identificador de autor além do nome (`RF10`).

### Feedback

O feedback fecha o ciclo de comunicação: merendeira, responsável e escola criam; nutrição e escola respondem; o autor encerra o próprio feedback respondido. O status segue `Aberto → Respondido → Encerrado`. A escola é derivada do autor (quando tem escola) ou do aluno referenciado. O escopo de leitura respeita unidade, escola e vínculo (Spec 028).

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/feedbacks` | Lista paginada com filtro `status` | Sessão; escopo por papel e vínculo |
| `POST` | `/api/feedbacks` | Cria com `assunto` (5–120), `mensagem` (5–1000) e `alunoId` opcional | `Merendeira`, `Responsavel` ou `AdminEscola` |
| `POST` | `/api/feedbacks/{id}/responder` | `Aberto → Respondido` com `resposta` (5–1000); emite `FeedbackRespondido` ao autor | `Nutricionista` da unidade ou `AdminEscola` da escola |
| `POST` | `/api/feedbacks/{id}/encerrar` | `Respondido → Encerrado` | Somente o autor |

Feedback de outra unidade devolve `404`; de outra escola da mesma unidade, `403`. Responder feedback já respondido e encerrar feedback aberto devolvem `409`.

### Notificações

As notificações são emitidas por um único serviço, na mesma transação da mudança de origem. A leitura é sempre restrita ao destinatário da sessão; notificação de outro usuário devolve `404`. O conteúdo nunca inclui CPF, e-mail, telefone ou dado clínico além do nome do aluno quando necessário.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/notificacoes` | Lista paginada do usuário, com filtro `naoLidas` | Sessão |
| `POST` | `/api/notificacoes/{id}/ler` | Marca uma notificação como lida (idempotente) | Sessão; dono |
| `POST` | `/api/notificacoes/ler-todas` | Marca todas como lidas (idempotente) | Sessão |

### Geração automática de cardápios

Os parâmetros vivem em `ParametrosGeracao` (único por unidade) e a seleção é função pura
`SeletorDeRefeicao`/`GeradorCardapioAutomatico`, determinística pela semente e com até 5
tentativas por refeição. Cada item gerado corresponde a 100 g; o total calórico da refeição é
recalculado pelo `CalculoNutricional` da Spec 012 antes de persistir.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/unidades/{unidadeId}/parametros-geracao` | Parâmetros de geração da unidade | `AdminSecretaria` ou `Nutricionista` |
| `PUT` | `/api/unidades/{unidadeId}/parametros-geracao` | Cria ou altera os parâmetros de geração | Sim; só `Nutricionista` |
| `GET` | `/api/nutricionistas` | Lista as nutricionistas ativas (nome, e-mail, CPF e CRN) para escolher a autora do cardápio | Sim; só `Nutricionista` |
| `POST` | `/api/cardapios/gerar` | Gera o geral e as versões adaptadas por escola/conjunto na mesma transação | Sim; só `Nutricionista` |

A resposta traz `cardapioGeralId`, `seed`, `tentativasPorRefeicao`, `totaisPorRefeicao`,
`versaoAdaptadaIds` (com `escolaId`, `conjunto`, `status` e `motivo`), `avisos` e
`diagnostico`. A geração responde `400` sem parâmetros configurados, com categoria obrigatória
inexistente/inativa ou com nutricionista inválida (outra unidade ou sem CRN), e `422`
(`{ erro, detalhes }`) quando a geração é impossível — o `detalhes` traz o diagnóstico por
refeição (faixa alcançável, categoria ausente, candidatos e tentativas). Nenhum cardápio
parcial é salvo. `AdminEscola` recebe `403` por não ter escopo por unidade.

A rota pública responde `400` sem `unidadeId` e nunca devolve rascunho. Editar ou excluir cardápio que não está em `Rascunho` responde `409` — publicar, cancelar e revisar pertencem à Spec 015. Alimento inexistente responde `400`; alimento desativado só é aceito se já estava no cardápio, o que preserva o histórico de um item que saiu do catálogo. O filtro `refeicao` compara o nome sem diferenciar caixa, mas acento continua significativo. Excluir uma unidade com cardápio responde `409`.


### Papéis e escopo

O login grava as claims `NameIdentifier`, `Role`, `escola` e `unidade`. Um usuário sem papel não autentica (`401` com `Conta sem papel definido. Procure a secretaria.`), e o autocadastro cria a conta nesse estado: quem define o papel é um administrador.

| Papel | Escopo de dados |
| --- | --- |
| `Nutricionista` | Própria unidade |
| `AdminSecretaria` | Própria unidade |
| `AdminEscola` | Própria escola |
| `Merendeira` | Própria escola, somente leitura de aluno |
| `Responsavel` | Apenas alunos vinculados a ele |

Não existe perfil global: `AdminSecretaria` e `Nutricionista` atuam na própria unidade; a matriz da Spec 028 é a fonte autoritativa de permissão por recurso.

**Bootstrap por token (duas vias):**

**Token não versionado (setup por máquina):** o valor de `Bootstrap__Token` não está no
repositório. Distribua-o por canal privado e grave-o uma vez em cada máquina de
desenvolvimento:

```bash
dotnet user-secrets set "Bootstrap:Token" "<token>" --project backend
```

`dotnet run` usa o perfil `Development` (`backend/Properties/launchSettings.json`) e
carrega esse valor. Em produção/container, defina a variável de ambiente
`Bootstrap__Token`. O banco SQLite é local — usuários, unidades e o controle de
"um uso por unidade" não são compartilhados entre máquinas.

1. **Token de ambiente** — com `Bootstrap__Token` definido (mínimo **32 caracteres**), a página `/bootstrap` cria um `AdminSecretaria` informando o **nome exato de uma unidade**. Se a unidade não existir, ela é **criada junto** com o administrador; se existir, o admin é atribuído a ela. O mesmo token vale **uma vez por unidade**: cadastrar duas vezes na mesma unidade responde `409`. Unidade desativada ou sem nome responde `400`.
2. **Token temporário** — em **Secretaria → painel**, um `AdminSecretaria` vinculado a uma unidade gera um token de **30 minutos**, de **uso único**, exibido uma única vez. Enquanto ele vale, o botão de gerar fica desabilitado com contagem regressiva; ao expirar, o botão volta a liberar um novo token e o cadastro fica bloqueado.

`Bootstrap__Permitir=false` é a trava dura de ambiente: nega as duas vias mesmo com token válido (`403 Criação de administrador desativada.`). O token vai no corpo da requisição, nunca na URL — URL vaza em histórico, log e `Referer`. Sem nenhuma via disponível, a rota responde `404`, como se não existisse.

**Bootstrap por seed:** `AdminInicial__Email` e `AdminInicial__Senha` criam o administrador no startup, de forma idempotente. Sem essas variáveis o startup apenas registra um aviso.

Usuários criados antes da Spec 009 ficam sem papel e não autenticam até um administrador definir o papel.

### Bootstrap

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/bootstrap/status` | Informa se há via de cadastro disponível (`{ disponivel, motivo }`) | Não |
| `POST` | `/api/bootstrap/admin` | Cria um `AdminSecretaria` com token de ambiente ou token temporário | Token |
| `GET` | `/api/bootstrap/token` | Estado do token da unidade (`{ ativo, expiraEm }`) | Sim (`AdminSecretaria`) |
| `POST` | `/api/bootstrap/token` | Gera o token de 30 minutos da unidade (exibido uma única vez) | Sim (`AdminSecretaria`) |

`POST /api/bootstrap/admin` recebe `{ token, unidade?, nome, cpf, dataNascimento, email, senha }` e responde `201 { id }`. `unidade` é obrigatória apenas para o token de ambiente; com o token temporário a unidade vem de quem o gerou. Erros: `400` de validação (inclui unidade inexistente), `403` com `Token inválido ou expirado.`, `409` para e-mail repetido ou unidade que já consumiu o token de ambiente e `404` quando não há nenhuma via configurada.

`POST /api/bootstrap/token` responde `201 { token, expiraEm }`; com um token ainda válido na unidade responde `409`. A conta precisa estar **vinculada a uma unidade** (`403` caso contrário). O valor em claro nunca é persistido — o banco guarda apenas o hash SHA-256.

### Escolas

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/escolas` | Lista escolas no escopo; aceita `unidadeId` | `AdminSecretaria`, `AdminEscola`, `Nutricionista` ou `Merendeira` |
| `POST` | `/api/escolas` | Cria escola | `AdminSecretaria` |
| `PUT` | `/api/escolas/{id}` | Renomeia escola | `AdminSecretaria` |
| `POST` | `/api/escolas/{id}/desativar` | Desativa escola | `AdminSecretaria` |
| `POST` | `/api/escolas/{id}/reativar` | Reativa escola | `AdminSecretaria` |
| `DELETE` | `/api/escolas/{id}` | Exclui escola sem usuários vinculados (`409` caso contrário) | `AdminSecretaria` |

### Unidades da rede

Unidade é da secretaria, **não pertence a escola**: o nome é único na rede. A unidade **nasce no bootstrap do primeiro administrador**, não há rota de criação.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/unidades/ativas` | Consulta pública mínima (id e nome) de unidades ativas para o cadastro | Não |
| `GET` | `/api/unidades` | Lista as unidades da rede | `AdminSecretaria`, `AdminEscola`, `Nutricionista` ou `Merendeira` |
| `PUT` | `/api/unidades/{id}` | Renomeia unidade | `AdminSecretaria` |
| `POST` | `/api/unidades/{id}/desativar` | Desativa unidade | `AdminSecretaria` |
| `POST` | `/api/unidades/{id}/reativar` | Reativa unidade | `AdminSecretaria` |
| `DELETE` | `/api/unidades/{id}` | Exclui unidade sem usuário, aluno ou token vinculado (`409` caso contrário) | `AdminSecretaria` |

### Diagnóstico da hierarquia

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/diagnostico/hierarquia` | Conta escolas sem unidade, alunos sem escola e usuários sem vínculo obrigatório; não corrige dados | `AdminSecretaria` |

### Gestão de usuários

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/usuarios` | Lista usuários com filtros `papel`, `escolaId`, `unidadeId`, `ativo`, `busca`; expõe `crn` e `crnValido` | `AdminSecretaria` (própria unidade) ou `AdminEscola` (leitura da própria escola) |
| `GET` | `/api/usuarios/diagnostico-crn` | Lista nutricionistas ativas sem CRN ou com CRN inválido (diagnóstico de legado) | `AdminSecretaria` |
| `POST` | `/api/usuarios` | Cria usuário com papel; CRN obrigatório para `Nutricionista` | `AdminSecretaria` |
| `PUT` | `/api/usuarios/{id}` | Atualiza dados do próprio perfil; papel e escopo só pelo ADM | Dono do perfil (dados próprios) ou `AdminSecretaria` (papel/escopo) |
| `POST` | `/api/usuarios/{id}/ativar` | Reativa usuário | `AdminSecretaria` |
| `POST` | `/api/usuarios/{id}/desativar` | Desativa usuário e invalida a sessão na requisição seguinte | `AdminSecretaria` |
| `DELETE` | `/api/usuarios/{id}` | Exclui conta sem vínculo nem token emitido; bloqueia própria conta e último admin (`409`) | `AdminSecretaria` |

`PUT /api/usuarios/{id}` não permite alterar o próprio papel. O escopo por papel é: `AdminSecretaria` e `Nutricionista` → unidade; `AdminEscola` e `Merendeira` → escola; `Responsavel` → unidade de origem, sem escola. Papel e escopo viram claims **no login**: mudança de papel/unidade/escola invalida a sessão na requisição seguinte.

### Vínculo responsável↔aluno

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/aluno/{alunoId}/responsaveis` | Lista responsáveis do aluno | `AdminSecretaria`, `AdminEscola` ou `Responsavel` vinculado ao aluno |
| `POST` | `/api/aluno/{alunoId}/responsaveis/{usuarioId}` | Vincula responsável | `AdminSecretaria` |
| `DELETE` | `/api/aluno/{alunoId}/responsaveis/{usuarioId}` | Desvincula responsável | `AdminSecretaria` |

O vínculo é único por par `(aluno, responsável)`: repetir devolve `409`. O responsável não carrega escola; o vínculo é a única autorização de leitura do aluno.

Erros seguem um contrato único: corpo `{ "erro": "mensagem" }` em `400` (validação de domínio, modelo inválido ou JSON malformado), `401` (sem sessão), `403` (acesso negado), `404` (recurso ou rota inexistente), `405` (verbo não permitido), `409` (conflito de estado), `415` (content-type não suportado) e `500` (erro interno, sem detalhe técnico). As exceções são traduzidas por `ErrorHandlingMiddleware`; modelo inválido, `404` de rota inexistente, `405` e `415` são traduzidos por `API/Middleware/ContratoDeErro.cs`. Nenhum controller monta resposta de erro fora desse formato.

As rotas de criação (`POST /api/usuario/registrar` e `POST /api/aluno/registrar`) respondem `201 Created` com o cabeçalho `Location` apontando para o recurso criado e o recurso no corpo. `DELETE` e `logout` respondem `204 No Content`. Toda rota com `id` usa a constraint `{id:int}`.

O perfil de usuário devolve `id`, `nome`, `email`, `telefone`, `foto`, `papel`, `escolaId`, `unidadeId`, `ativo` e `crn`; `cpf` e `senha` nunca aparecem no corpo.

## Testes

A suíte automatizada fica em `backend.Tests` (xUnit) e cobre as regras de domínio de `Usuario` e `Aluno`, os serviços `UsuarioService` e `AlunoService` (com repositórios em memória) e os contratos HTTP via `WebApplicationFactory` com SQLite em memória.

```bash
dotnet test backend.Tests/backend.Tests.csproj
```

Para rodar apenas um grupo: `dotnet test backend.Tests/backend.Tests.csproj --filter Domain.AlunoTests`.

## Pendências conhecidas

- O frontend possui chamadas para operações de cardápio, mas não há controller de cardápio no backend atual.
- Não há um arquivo de exemplo de ambiente no estado atual do repositório; crie `frontend/.env` com `VITE_API_URL=http://localhost:5001` para que o proxy tenha seu destino.

## Banco de dados

O backend usa SQLite e executa `Database.Migrate()` ao iniciar. As migrations ficam em `backend/Migrations/`. O caminho SQLite é relativo ao diretório de execução (`database.db`); mantenha isso em mente ao executar a API e ao localizar o arquivo criado.


<img width="100%" src="https://capsule-render.vercel.app/api?type=slice&height=150&color=D92567&reversal=false&section=footer"/>