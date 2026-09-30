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
| `/colors` | Página de teste de cores | Público |

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
| `GET` | `/api/usuario/perfil/{id}` | Consulta perfil pelo ID | Não |
| `PUT` | `/api/usuario/perfil/{id}` | Atualiza o perfil do usuário autenticado | Sim; o ID deve corresponder à sessão |

A autenticação usa cookie HTTP-only chamado `NutriEduc.Auth`, com validade de oito horas. As chamadas feitas diretamente pelo navegador devem incluir credenciais (`credentials: "include"`).

### Alunos

| Método | Caminho | Descrição |
| --- | --- | --- |
| `POST` | `/api/aluno/registrar` | Cadastra um aluno |
| `GET` | `/api/aluno` | Lista alunos |
| `GET` | `/api/aluno/perfil/{id}` | Consulta um aluno pelo ID |
| `DELETE` | `/api/aluno/{id}` | Exclui um aluno |

O campo de nascimento do DTO é `DateOnly`; envie uma data no formato `yyyy-MM-dd`. `anafilaxia` deve ser um booleano JSON (`true` ou `false`). As restrições alimentares são armazenadas como uma coleção JSON na tabela `Alunos`.

## Pendências conhecidas

- O frontend possui chamadas para operações de cardápio, mas não há controller de cardápio no backend atual.
- Não há um arquivo de exemplo de ambiente no estado atual do repositório; crie `frontend/.env` com `VITE_API_URL=http://localhost:5001` para que o proxy tenha seu destino.

## Banco de dados

O backend usa SQLite e executa `Database.Migrate()` ao iniciar. As migrations ficam em `backend/Migrations/`. O caminho SQLite é relativo ao diretório de execução (`database.db`); mantenha isso em mente ao executar a API e ao localizar o arquivo criado.


<img width="100%" src="https://capsule-render.vercel.app/api?type=slice&height=150&color=D92567&reversal=false&section=footer"/>