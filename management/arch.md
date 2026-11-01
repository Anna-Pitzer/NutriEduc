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
  │   ├── secretaria/     HomeSecretaria, Escolas, Unidades, Usuarios
  │   └── escolas/        HomeEscolas (em construção)
  ├── components/     Header, Navbar, Footer, Button, CardapiosDiv, Notificacoes
  ├── context/        authContext (sessão via cookie)
  └── services/       AlunoApi, CardapioApi, Perfil  → fetch /api via proxy Vite

Regra de organização das telas: cada tela nova entra na pasta da área que a usa.
`comum/` é reservado a telas compartilhadas por mais de uma área; tela de área única
não entra em `comum/`.

API  ASP.NET Core 10 — http://localhost:5001
  ├── API/Controllers  UsuarioController, AlunoController, UsuarioAdminController,
  │                    EscolaController, UnidadeController, VinculoController,
  │                    BootstrapController (+ RespostaController, base)
  ├── API/Services     UsuarioService, AlunoService, EscolaService, VinculoService,
  │                    BootstrapService, EscopoService
  ├── Application      DTOs de entrada e saída
  ├── Domain           Usuario, Aluno, Escola, UnidadeEscolar, ConsumoBootstrap,
  │                    TokenDeCadastro + contratos de repositório
  ├── Infrastructure   ConnectionContext (EF Core) e repositórios
  └── Migrations       8 migrations, aplicadas no startup

Persistência
  SQLite — database.db (Usuarios, Alunos, Escolas, Unidades, Vinculos,
  ConsumosBootstrap, TokensDeCadastro)
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
| Um único tipo de usuário | Cinco papéis implementados na Spec 009 | Resolvido em 2026-10-30 |
| `Restricoes` como JSON | Impede consulta por restrição e não há histórico | Spec 010 |
| Sem paginação em `/api/aluno` | Listagem cresce sem limite | Spec 009 (listagens escopadas e paginadas) |
| Sem versionamento de API | Contrato muda sem sinal | Pós-F2 |
| Bootstrap grava dois agregados sem transação | Usuário e consumo em `SaveChanges` separados | Unidade de trabalho transacional (Spec 022, limitação registrada) |
| `AdminEscola` sem vínculo aluno→escola | Unidade deixou de pertencer a escola (ADR-013) | PEND-024: adicionar `Aluno.EscolaId` |
| Exclusão definitiva sem auditoria | `DELETE` remove o registro; resta só o log da requisição (ADR-014) | Registrar exclusões em histórico se auditoria for exigida |

## 10. Estado atual verificado (2026-10-30)

| Verificação | Resultado |
| --- | --- |
| `dotnet test backend.Tests` | 246 testes, 246 aprovados, 0 falhas (157 unitários + 89 de integração HTTP) |
| Warnings de build | 0 (o `Usuario` reescrito na Spec 009 eliminou os 7 `CS8604`) |
| `npm run build` / `npm run lint` | passam: build Vite OK (1855 módulos); lint 0 erros e 5 avisos |
| Migrations | 9, aplicadas no startup por `Database.Migrate()`: duas do bootstrap (`BootstrapPorUnidade`, `UnidadesDaRede`) e a anterior `BootstrapPainel` |
| Domínio implementado | `Aluno`, `Usuario` (hash PBKDF2 + papel/escopo), `Escola`, `UnidadeEscolar` (da rede, ADR-013), `VinculoResponsavelAluno`, `ConsumoBootstrap`, `TokenDeCadastro` |
| Domínio especificado e não implementado | Specs 010–019 (30 documentos, status `created`) |
| Superfície HTTP | 8 controllers (`Aluno`, `Usuario`, `UsuarioAdmin`, `Escola`, `Unidade`, `Vinculo`, `Bootstrap`, `Resposta` base), com `DELETE` em escola, unidade e usuário |
| Índice canônico | `management/roadmap.md` com 22 linhas, todas `planned`, criadas por `roadmap-plan-commit` |

Estado formal por spec: no roadmap, as 22 estão `planned` e a promoção para
`verified`/`implemented` é feita pelos comandos do pipeline (`/spec-new`,
`/spec-implement`, `/spec-test`). 001–009 e 020–022 têm os três documentos e as tasks
executadas (a 009 com T13 transferida para a 021); 010–019 têm os três documentos e
nenhuma task executada.

## 11. Mapa de módulos especificados (009–019) e telas (021)

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
| 021 | Telas de secretaria (V10) | nenhuma entidade nova | consome `/api/escolas`, `/api/unidades` e `/api/usuarios`, incluindo os `DELETE` |
| 022 | Bootstrap de admin por token (V11) | `ConsumoBootstrap` (1 uso por unidade), `TokenDeCadastro` (30 min, uso único), `BootstrapService`; unidade da rede (ADR-013) | `/api/bootstrap/status`, `POST /api/bootstrap/admin`, `GET`/`POST /api/bootstrap/token`, `GET`/`POST /api/unidades` |

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
