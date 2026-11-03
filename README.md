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
| `POST` | `/api/aluno/registrar` | Cadastra um aluno | Sim |
| `GET` | `/api/aluno` | Lista alunos | Sim |
| `GET` | `/api/aluno/perfil/{id}` | Consulta um aluno pelo ID | Sim |
| `PUT` | `/api/aluno/{id}` | Atualiza um aluno existente | Sim |
| `DELETE` | `/api/aluno/{id}` | Exclui um aluno | Sim |
| `GET` | `/api/aluno/{id}/historico` | Trilha de alterações do aluno (criação, campos e restrições), paginada e em ordem decrescente | Sim; `Nutricionista`, `AdminEscola`, `Merendeira` da unidade e `Responsavel` vinculado |
| `GET` | `/api/aluno/{id}/restricoes/historico` | Histórico de restrições do aluno, paginado e em ordem decrescente (derivado da trilha; rota mantida por compatibilidade) | Sim; `Nutricionista`, `AdminEscola`, `Merendeira` da unidade e `Responsavel` vinculado |

O campo de nascimento do DTO é `DateOnly`; envie uma data no formato `yyyy-MM-dd`. `anafilaxia` deve ser um booleano JSON (`true` ou `false`). As restrições alimentares são armazenadas como uma coleção JSON na tabela `Alunos`.

Cada aluno pode ser vinculado a uma unidade (`UnidadeId`). Sem unidade, o aluno só é visível para `Nutricionista` e `AdminSecretaria`; `Escola` continua sendo apenas rótulo de exibição.

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
| `GET` | `/api/alimentos` | Lista paginada; aceita `busca`, `categoriaId`, `pagina`, `tamanho`, `incluirInativos` e `ordenar` (`nome`, `nome-desc`, `categoria` ou `recentes`) | Sim; `Nutricionista` ou `Merendeira` |
| `GET` | `/api/alimentos/{id}` | Detalhe com composição, categoria e vínculos resolvidos | Sim; `Nutricionista` ou `Merendeira` |
| `POST` | `/api/alimentos` | Cria alimento com os seis valores nutricionais e os vínculos | Sim; só `Nutricionista` |
| `PUT` | `/api/alimentos/{id}` | Edita nome, categoria, composição e vínculos | Sim; só `Nutricionista` |
| `POST` | `/api/alimentos/{id}/desativar` | Desativa (soft delete); continua legível por id | Sim; só `Nutricionista` |
| `POST` | `/api/alimentos/{id}/reativar` | Reativa um alimento desativado | Sim; só `Nutricionista` |
| `GET` | `/api/categorias-alimento` | Lista categorias; `incluirInativas` é exclusivo da nutrição | Sim; `Nutricionista` ou `Merendeira` |
| `POST` | `/api/categorias-alimento` | Cria categoria | Sim; só `Nutricionista` |
| `PUT` | `/api/categorias-alimento/{id}` | Renomeia categoria | Sim; só `Nutricionista` |
| `POST` | `/api/categorias-alimento/{id}/desativar` | Desativa a categoria; ela não recebe alimento novo | Sim; só `Nutricionista` |
| `POST` | `/api/categorias-alimento/{id}/reativar` | Reativa a categoria | Sim; só `Nutricionista` |

A busca usa o nome normalizado: tenta prefixo primeiro e, somente quando não há resultado, repete por "contém" — por isso `leite` encontra `Leite de coco`. `Merendeira`, `AdminEscola` e `AdminSecretaria` recebem `403` na leitura do catálogo; `incluirInativos=true` fora da nutrição também responde `403`. Ordenação fora de `nome`, `nome-desc`, `categoria` e `recentes` responde `400` (o padrão é `nome`). Nome duplicado responde `409`; valor nutricional negativo ou com três casas decimais responde `400` sem persistir; categoria desativada não aceita alimento novo (`400`); o mesmo vínculo repetido responde `400`. Desativar o alimento não o esconde da leitura por id, e desativar a categoria não esconde os alimentos dela da listagem.

### Cardápio

O cardápio vive em `Cardapios`, `Refeicoes` e `ItensRefeicao`. O período é um tipo complexo (`Periodo_Inicio` e `Periodo_Fim`), a ordem da refeição é única no cardápio e o item usa a chave `(RefeicaoId, AlimentoId)` — o mesmo alimento não repete na refeição, mas pode em outra. Os valores nutricionais são **derivados**: `valores do alimento × gramas / 100`, arredondados a duas casas por item e somados; nada disso é persistido.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/cardapios` | Lista paginada; aceita `unidadeId`, `data`, `situacao`, `pagina` e `tamanho` | Sim; `Nutricionista` ou `Merendeira` da própria unidade |
| `GET` | `/api/cardapios/publicados` | Publicados cujo período inclui a data (padrão: hoje); exige `unidadeId`; aceita `data`, `refeicao`, `pagina` e `tamanho` | Não |
| `GET` | `/api/cardapios/{id}` | Detalhe com refeições ordenadas, valores por item e totais | Sim; `Nutricionista` ou `Merendeira` da unidade |
| `GET` | `/api/cardapios/{id}/compatibilidade` | Análise de compatibilidade com as restrições dos alunos (bloqueantes e alertas) | Sim; `Nutricionista` ou `Merendeira` da unidade |
| `POST` | `/api/cardapios` | Cria rascunho com período (até 366 dias), refeições e itens | Sim; só `Nutricionista` |
| `PUT` | `/api/cardapios/{id}` | Substitui nome, período e refeições de um rascunho de forma atômica | Sim; só `Nutricionista` |
| `POST` | `/api/cardapios/{id}/duplicar` | Copia a composição para um novo período como rascunho | Sim; só `Nutricionista` |
| `DELETE` | `/api/cardapios/{id}` | Exclui um rascunho com refeições e itens | Sim; só `Nutricionista` |
| `POST` | `/api/cardapios/{id}/publicar` | Publica um rascunho; bloqueia por incompatibilidade (`422`) ou sobreposição (`409`) | Sim; só `Nutricionista` |
| `POST` | `/api/cardapios/{id}/cancelar` | Cancela com `motivo` obrigatório (2–500) | Sim; só `Nutricionista` |
| `POST` | `/api/cardapios/{id}/revisao` | Cria uma revisão (`Versao + 1`) de um cardápio publicado | Sim; só `Nutricionista` |
| `GET` | `/api/cardapios/{id}/historico` | Linha do tempo de eventos do cardápio, decrescente e paginada | Sim; `Nutricionista` ou `Merendeira` da unidade |
| `GET` | `/api/cardapios/alteracoes` | Feed de alterações no escopo a partir de `desde`, paginado | Sim; `Nutricionista` ou `Merendeira` da própria unidade |

### Geração automática de cardápios

Os parâmetros vivem em `ParametrosGeracao` (único por unidade) e a seleção é função pura
`GeradorCardapioAutomatico.Gerar`, determinística pela semente. Cada item gerado corresponde
a 100 g; o total calórico da refeição é a soma das calorias por 100 g dos itens selecionados.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/unidades/{unidadeId}/parametros-geracao` | Parâmetros de geração da unidade | Sim; só `Nutricionista` |
| `PUT` | `/api/unidades/{unidadeId}/parametros-geracao` | Cria ou altera os parâmetros de geração | Sim; só `Nutricionista` |
| `POST` | `/api/cardapios/gerar` | Gera um cardápio `Rascunho` a partir dos parâmetros e da semente | Sim; só `Nutricionista` |

A geração responde `400` sem parâmetros configurados e `422` (`{ erro, detalhes }`) quando a
geração é impossível (sem alimento ativo, categoria obrigatória sem candidato, candidatos
insuficientes ou faixa calórica inatingível) — nenhum cardápio parcial é salvo. `AdminEscola`
recebe `403` por não ter escopo por unidade.

A rota pública responde `400` sem `unidadeId` e nunca devolve rascunho. Editar ou excluir cardápio que não está em `Rascunho` responde `409` — publicar, cancelar e revisar pertencem à Spec 015. Alimento inexistente responde `400`; alimento desativado só é aceito se já estava no cardápio, o que preserva o histórico de um item que saiu do catálogo. O filtro `refeicao` compara o nome sem diferenciar caixa, mas acento continua significativo. Excluir uma unidade com cardápio responde `409`.


### Papéis e escopo

O login grava as claims `NameIdentifier`, `Role`, `escola` e `unidade`. Um usuário sem papel não autentica (`401` com `Conta sem papel definido. Procure a secretaria.`), e o autocadastro cria a conta nesse estado: quem define o papel é um administrador.

| Papel | Escopo de dados |
| --- | --- |
| `Nutricionista` | Toda a rede |
| `AdminSecretaria` | Toda a rede |
| `AdminEscola` | Só a própria escola |
| `Merendeira` | Só a própria unidade, somente leitura de aluno |
| `Responsavel` | Só alunos vinculados a ele |

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
| `GET` | `/api/escolas` | Lista escolas | Sessão válida |
| `POST` | `/api/escolas` | Cria escola | `AdminSecretaria` |
| `PUT` | `/api/escolas/{id}` | Renomeia escola | `AdminSecretaria` |
| `POST` | `/api/escolas/{id}/desativar` | Desativa escola | `AdminSecretaria` |
| `POST` | `/api/escolas/{id}/reativar` | Reativa escola | `AdminSecretaria` |
| `DELETE` | `/api/escolas/{id}` | Exclui escola sem usuários vinculados (`409` caso contrário) | `AdminSecretaria` |

### Unidades da rede

Unidade é da secretaria, **não pertence a escola**: o nome é único na rede. A unidade **nasce no bootstrap do primeiro administrador**, não há rota de criação.

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/unidades` | Lista as unidades da rede | Sessão válida |
| `PUT` | `/api/unidades/{id}` | Renomeia unidade | `AdminSecretaria` |
| `POST` | `/api/unidades/{id}/desativar` | Desativa unidade | `AdminSecretaria` |
| `POST` | `/api/unidades/{id}/reativar` | Reativa unidade | `AdminSecretaria` |
| `DELETE` | `/api/unidades/{id}` | Exclui unidade sem usuário, aluno ou token vinculado (`409` caso contrário) | `AdminSecretaria` |

### Gestão de usuários

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/usuarios` | Lista usuários com filtros `papel`, `escolaId`, `unidadeId`, `ativo`, `busca` | `AdminSecretaria` ou `AdminEscola` (escopo da escola) |
| `POST` | `/api/usuarios` | Cria usuário com papel | `AdminSecretaria` ou `AdminEscola` (só `Responsavel`/`Merendeira` da própria escola) |
| `PUT` | `/api/usuarios/{id}` | Atualiza usuário; papel e unidade só por administrador | Dono do perfil ou administrador do escopo |
| `POST` | `/api/usuarios/{id}/ativar` | Reativa usuário | Administrador do escopo |
| `POST` | `/api/usuarios/{id}/desativar` | Desativa usuário e invalida a sessão na requisição seguinte | Administrador do escopo |
| `DELETE` | `/api/usuarios/{id}` | Exclui conta sem vínculo nem token emitido; bloqueia própria conta e último admin (`409`) | Administrador do escopo |

`PUT /api/usuarios/{id}` aceita `unidadeId` apenas quando o papel resultante é `AdminSecretaria` (atribuição de origem, não escopo); nos demais papéis a unidade continua restrita a `Merendeira` e `Responsavel`, que também exigem escola. Papel e unidade viram claims **no login**: ao alterar a própria conta, saia e entre novamente para o painel reconhecer a unidade.

### Vínculo responsável↔aluno

| Método | Caminho | Descrição | Autenticação |
| --- | --- | --- | --- |
| `GET` | `/api/aluno/{alunoId}/responsaveis` | Lista responsáveis do aluno | `EquipeEscolar` do aluno |
| `POST` | `/api/aluno/{alunoId}/responsaveis/{usuarioId}` | Vincula responsável | `AdminSecretaria` ou `AdminEscola` do aluno |
| `DELETE` | `/api/aluno/{alunoId}/responsaveis/{usuarioId}` | Desvincula responsável | `AdminSecretaria` ou `AdminEscola` do aluno |

O vínculo é único por par `(aluno, responsável)`: repetir devolve `409`. O responsável e o aluno precisam pertencer à mesma escola.

Erros seguem um contrato único: corpo `{ "erro": "mensagem" }` em `400` (validação de domínio, modelo inválido ou JSON malformado), `401` (sem sessão), `403` (acesso negado), `404` (recurso ou rota inexistente), `405` (verbo não permitido), `409` (conflito de estado), `415` (content-type não suportado) e `500` (erro interno, sem detalhe técnico). As exceções são traduzidas por `ErrorHandlingMiddleware`; modelo inválido, `404` de rota inexistente, `405` e `415` são traduzidos por `API/Middleware/ContratoDeErro.cs`. Nenhum controller monta resposta de erro fora desse formato.

As rotas de criação (`POST /api/usuario/registrar` e `POST /api/aluno/registrar`) respondem `201 Created` com o cabeçalho `Location` apontando para o recurso criado e o recurso no corpo. `DELETE` e `logout` respondem `204 No Content`. Toda rota com `id` usa a constraint `{id:int}`.

O perfil de usuário devolve apenas `id`, `nome`, `email`, `telefone` e `foto`; `cpf` e `senha` nunca aparecem no corpo.

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