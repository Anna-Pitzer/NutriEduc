# Arquitetura — NutriEduc

Documento de arquitetura vigente. Descreve componentes, responsabilidades,
dependências e as regras que impedem a degradação estrutural.

---

## 1. Visão de componentes

```text
Navegador
  React 19 + TypeScript + Vite 8
  ├── routes/         Rotas e proteção por sessão
  ├── pages/          Telas divididas por área de usuário
  │   ├── comum/          login, Perfil, TermosDeUso, PoliticaDePrivacidade, Suporte
  │   ├── nutricao/       HomeNutricao + alunos/ + cardapios/
  │   ├── merendeiras/    HomeMerendeiras (em construção)
  │   ├── pais/           HomePais (em construção)
  │   ├── secretaria/     HomeSecretaria (em construção)
  │   ├── escolas/        HomeEscolas (em construção)
  │   └── dev/            colorsTESTE (paleta de cores, fora de produção)
  ├── components/     Header, Navbar, Footer, Button, CardapiosDiv, Notificacoes
  ├── context/        authContext (sessão via cookie)
  └── services/       AlunoApi, CardapioApi, Perfil  → fetch /api via proxy Vite

Regra de organização das telas: cada tela nova entra na pasta da área que a usa.
`comum/` é reservado a telas compartilhadas por mais de uma área; tela de área única
não entra em `comum/`.

API  ASP.NET Core 10 — http://localhost:5001
  ├── API/Controllers  UsuarioController, AlunoController
  ├── API/Services     UsuarioService, AlunoService
  ├── Application      DTOs de entrada e saída
  ├── Domain           Usuario, Aluno + IUsuarioRepository, IAlunoRepository
  ├── Infrastructure   ConnectionContext (EF Core), UsuarioRepository, AlunoRepository
  └── Migrations       4 migrations, aplicadas no startup

Persistência
  SQLite — database.db (tabelas Usuarios, Alunos)
```

## 2. Camadas e responsabilidades

| Camada | Pode | Não pode |
| --- | --- | --- |
| Controller | Traduzir HTTP ↔ DTO, decidir status code | Conter regra de negócio, acessar `DbContext` |
| Service | Orquestrar caso de uso, consultar repositório | Validar invariante de entidade (isso é do domínio) |
| Domain | Validar e proteger invariantes, expor comportamento | Conhecer HTTP, EF Core, DTO |
| Infrastructure | Persistir, mapear schema | Conter regra de negócio |
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
- Services são `Scoped`, alinhados ao ciclo de vida do `ConnectionContext`.
- DTOs de entrada e de saída são distintos. Campo de senha nunca aparece em DTO de
  saída.
- Coleções expostas pelo domínio são `IReadOnlyList`/`IReadOnlyCollection`
  acompanhadas de operação de domínio (`AdicionarRestricao`, `RemoverRestricao`).

## 6. Tratamento de erro

Formato único de erro:

```json
{ "erro": "mensagem legível" }
```

| Origem | Status |
| --- | --- |
| `ArgumentException` no domínio | 400 |
| não autenticado | 401 |
| autenticado sem o papel exigido | 403 |
| recurso inexistente | 404 |
| exceção não tratada | 500, sem stack trace no corpo |

## 7. Persistência

- EF Core 10 + SQLite; `Database.Migrate()` no startup.
- Migrations são versionadas e nunca editadas após aplicadas.
- `Restricoes` é armazenada como coluna `TEXT` (JSON). A normalização em tabela
  própria depende da entidade `Restricao` (Spec 010).
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
| Sem domínio de cardápio/alimento | Frontend chama 6 rotas inexistentes | Spec 010 |
| Um único tipo de usuário | Cinco perfis em `implementacoes.md` | Spec 009 |
| `Restricoes` como JSON | Impede consulta por restrição | Spec 010 |
| Sem paginação em `/api/aluno` | Listagem cresce sem limite | Spec 010 |
| Sem versionamento de API | Contrato muda sem sinal | Pós-F2 |
