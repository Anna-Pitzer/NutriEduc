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
  └── Migrations       5 migrations, aplicadas no startup

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
| Sem domínio de cardápio/alimento | Frontend chama 6 rotas inexistentes | Specs 011 (alimentos) e 012 (cardápio) |
| Um único tipo de usuário | Cinco perfis em `implementacoes.md` | Spec 009 |
| `Restricoes` como JSON | Impede consulta por restrição e não há histórico | Spec 010 |
| Sem paginação em `/api/aluno` | Listagem cresce sem limite | Spec 009 (listagens escopadas e paginadas) |
| Sem versionamento de API | Contrato muda sem sinal | Pós-F2 |

## 10. Estado atual verificado (2026-10-30)

| Verificação | Resultado |
| --- | --- |
| `dotnet test backend.Tests` | 152 testes, 152 aprovados, 0 falhas (12 de integração HTTP) |
| Warnings de build | 8 `CS8604` em `UsuarioService`/`UsuarioController` (propriedades anuláveis de `Usuario`) |
| `npm run build` / `npm run lint` | passam: build Vite OK (1846 módulos); lint 0 erros e 5 avisos |
| Migrations | 5, aplicadas no startup por `Database.Migrate()` (a última é documental: `HashSenhaUsuario`) |
| Domínio implementado | `Aluno` (invariantes, `Turma`, `Responsavel`, restrições protegidas) e `Usuario` (hash PBKDF2), sem papel |
| Domínio especificado e não implementado | Specs 009–019 (33 documentos, status `created`) |
| Superfície HTTP | 2 controllers (`AlunoController`, `UsuarioController`) |
| Índice canônico | `management/roadmap.md` com 19 linhas, todas `planned`, criadas por `roadmap-plan-commit` em 2026-10-30 |

Estado formal por spec: no roadmap, as 19 estão `planned` e a promoção para
`verified`/`implemented` é feita pelos comandos do pipeline (`/spec-new`,
`/spec-implement`, `/spec-test`). 001–005 têm os três documentos e todas as tasks
executadas; 006–008 têm os três documentos e nenhuma task executada; 009–019 têm os
três documentos e nenhuma task executada.

## 11. Mapa de módulos especificados (009–019)

| Spec | Módulo | Entidades / tipos principais | Superfície HTTP |
| --- | --- | --- | --- |
| 009 | Identidade e estrutura escolar | `Escola`, `UnidadeEscolar`, `VinculoResponsavelAluno`, `PapelUsuario` | `EscolaController`, `UnidadeController`, `VinculoController`, ampliação de `UsuarioController` |
| 010 | Restrições e alergias | `RestricaoAlimentar`, `HistoricoRestricaoAluno` | `RestricaoController`, histórico em `AlunoController` |
| 011 | Catálogo de alimentos | `CategoriaAlimento`, `Alimento`, `AlimentoRestricao`, `ValoresNutricionais` | `AlimentoController`, `CategoriaAlimentoController` |
| 012 | Cardápio | `Cardapio`, `Refeicao`, `ItemRefeicao`, `PeriodoVigencia` | `CardapioController` (interno + público) |
| 013 | Geração automática | `ParametroGeracao`, `GeradorCardapioAutomatico` | `GeracaoController` |
| 014 | Compatibilidade | `AnaliseCompatibilidade` (função pura) | rota em `CardapioController` |
| 015 | Publicação e histórico | `HistoricoCardapio`, `EventoCardapio`, transições de `Cardapio` | `CardapioController` |
| 016 | Ocorrências | `Ocorrencia`, `HistoricoOcorrencia` | `OcorrenciaController` |
| 017 | Solicitações | `Solicitacao`, `HistoricoSolicitacao` | `SolicitacaoController` |
| 018 | Feedback e notificações | `Feedback`, `Notificacao` | `FeedbackController`, `NotificacaoController` |
| 019 | Relatórios | `RelatorioGerado`, `IRelatorio`, exportadores | `RelatorioController` |

**Regra de dependência entre módulos:** o fluxo de dados é
`009 → 010 → 011 → 012 → 014 → {013, 015} → 016 → 017 → 018 → 019`. A seta
significa "o módulo à direita consome contrato do módulo à esquerda". Nenhum módulo
reimplementa regra de outro: compatibilidade (014) é a única fonte de severidade, e
notificação (018) é a única fonte de `Notificacao`.

**Regra de escrita entre agregados:** operações que tocam dois agregados (ex.: aprovar
solicitação de restrição, em 017) usam o mesmo `ConnectionContext` Scoped e uma
transação explícita. Não existe repositório que abre conexão própria (L4).

## Spec 007 — contrato-erro-e-infra-di

**Canonical creation path:** `backend/API/Middleware/`, `backend/Program.cs`.

O contrato único de erro (§6) e a injeção de dependência por contrato (§5) ganham um
dono explícito: um middleware de exceção registrado em `Program.cs`, que traduz
`ArgumentException` em 400 e elimina os `try/catch` repetidos nos controllers, e a
correção do `UsuarioRepository`, que hoje instancia o próprio `DbContext` (PEND-004).
