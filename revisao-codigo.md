# Revisão de Código — NutriEduc

Revisão técnica independente, com foco em **Design e POO** e em pontos que comprometem a entrega.
Complementa `correcoes.md` (avaliação da régua) e o conjunto `management/`.

**Estado revisado:** commit `653114d`, branch `branch-kayke`.

### Escopo lido

| Fonte | Cobertura |
|---|---|
| Código backend | 14 arquivos (entidades, DTOs, serviços, repositórios, controllers, `Program.cs`, csproj) |
| Código frontend | serviços, contexto, rotas, `vite.config.ts`, `package.json`; páginas por métrica |
| `correcoes.md`, `implementacoes.md`, `README.md` | integral |
| `management/` | `arch.md`, `adr.md` (6 ADRs), `lessons.md` (9 lições), `vision.md` (V1–V7), `PLANEJAMENTO.md`, `roadmap.md`, `pending-items.md`, `cross-session/README.md` |
| `management/specs/` | as 5 specs completas — `requirements.md`, `design.md` e `task.md` de cada uma (15 arquivos) |

---

## 1. Resumo executivo

O projeto está em um estado **melhor documentado do que implementado** — e essa é a chave da revisão.

Existe um contrato de arquitetura escrito, com regras explícitas (`arch.md` §2–§8), seis ADRs
aceitos e nove lições derivadas do código. O código, porém, **viola esse contrato em oito pontos
verificáveis**. Não é um projeto sem disciplina: é um projeto com disciplina documentada e execução
atrasada.

| Frente | Situação |
|---|---|
| Arquitetura | 4 camadas corretas, DIP real — mas 8 violações do próprio `arch.md` |
| Domínio | `Usuario` rico; `Aluno` anêmico, sem uma única invariante |
| Segurança | Senha em claro; `AlunoController` público; IDOR de CPF em `GET /usuario/perfil/{id}` |
| Testes | Zero. Dimensão H = 0,0 |
| CRUD | Falta o `U` de aluno — maior peso da régua (I = 50%) |
| Planejamento | 12 specs definidas; 5 com documentos; **0 implementadas**; ordem de execução com ciclo |

**Conclusão:** a base conceitual é sólida e defensável na banca. O que falta é **execução**, e ela
está bloqueada por um defeito de sequenciamento no plano (§7.1), não por falta de clareza sobre o
que fazer.

---

## 2. O contrato de arquitetura existe e está sendo violado

Esta é a seção mais acionável da revisão. `management/arch.md` não é decorativo: ele declara regras
objetivas. Todas as linhas abaixo são verificáveis no código atual.

| # | Regra declarada em `arch.md` | Violação no código |
|---|---|---|
| 1 | §5 — *"Nenhum repositório instancia `ConnectionContext` por conta própria."* | `UsuarioRepository.cs:9` — `private readonly ConnectionContext _context = new();` |
| 2 | §5 — *"Services são `Scoped`, alinhados ao ciclo de vida do `ConnectionContext`."* | `Program.cs:12` — `AddTransient<UsuarioService>()` |
| 3 | §5 — *"DTOs de entrada e de saída são distintos. Campo de senha nunca aparece em DTO de saída."* | `DadosUsuarioDTO.cs:10` expõe `Senha` e é devolvido por `ObterUsuarioPorId`; `DadosAlunoDTO` serve às duas direções |
| 4 | §5 — *"Coleções expostas pelo domínio são `IReadOnlyList`… com operação de domínio."* | `Aluno.cs:11` — `List<string> Restricoes { get; private set; }` |
| 5 | §3 — *"Toda chamada autenticada carrega `credentials: "include"`. Uma chamada sem esse atributo é um defeito, não um detalhe."* | `AlunoApi.ts:21,31,39` — nenhuma das três chamadas envia `credentials` |
| 6 | §6 — *"Formato único de erro: `{ "erro": "mensagem legível" }`"* | Não existe handler global; controllers devolvem string crua (`AlunoController.cs:23,25,40`; `UsuarioController.cs:28,52`) |
| 7 | §2 — Domain *"Não pode: conhecer HTTP, EF Core, DTO"* | `Usuario.cs:87-126` valida prefixos `data:image/jpeg;base64,` — formato de payload HTTP dentro da entidade |
| 8 | §8.2 — *"Nenhum endpoint novo é aceito sem teste de integração do caminho feliz e do caminho de erro."* | Nenhum teste existe no repositório |

**Por que isso importa para a nota:** a régua avalia coesão e responsabilidades (dimensão D, 20%).
Um projeto que declara uma regra e a viola na mesma camada é mais frágil, aos olhos da avaliação,
do que um projeto que nunca declarou nada — porque demonstra que a regra não foi aplicada.

**A boa notícia:** os itens 1, 2 e 4 são correções de poucas linhas, e o item 5 é uma linha por
função. Nenhum exige redesenho.

---

## 3. Pontos fortes (defender na apresentação)

1. **Camadas com direção de dependência correta.** `Domain` não conhece `Infrastructure`; as
   interfaces estão em `backend/Domain/Interface/`. É o DIP aplicado, não apenas citado.
2. **Interfaces com consumidor abstrato real.** `Program.cs:11,15` ligam contrato → implementação
   e os serviços dependem só da abstração.
3. **`Usuario` é entidade de verdade.** `private set` em tudo, comportamento próprio e a sequência
   correta do cap. 4: **valida tudo, depois atribui** (`Usuario.cs:35-56`, `58-78`, `79-126`).
4. **Serviço que coordena sem invadir.** `UsuarioService.AtualizarPerfil` (`:77-108`) carrega,
   delega a regra à entidade e persiste. A regra não vaza.
5. **Erro de domínio mapeado para HTTP sem acoplamento.** `ArgumentException` → `400` em
   `UsuarioController.cs:198-201`. A entidade não conhece HTTP; o controller não conhece a regra.
6. **Cookie `HttpOnly`** (`Program.cs:22`) e `OnRedirectToLogin` → `401` (`:29-33`), adequado a API
   consumida por SPA.
7. **Documentação de engenharia acima da média.** `arch.md` com camadas, fluxos e regras de
   evolução; 6 ADRs com contexto e consequências negativas declaradas; 9 lições rastreáveis (`L1`–`L9`)
   referenciadas pelas specs. Isso é raro em trabalho de disciplina e deve ser apresentado.
8. **Planejamento em fases com gates verificáveis.** `PLANEJAMENTO.md` define F1–F6, cada uma com
   estado-alvo observável, e gates de build/test/lint.
9. **Frontend com separação por responsabilidade.** `pages/`, `components/`, `services/`, `context/`,
   `routes/`, um serviço por recurso e `ProtectedRoutes` como layout route.
10. **Contexto de autenticação correto no cliente.** `authContext.tsx:41-72` recupera sessão com
    `AbortController` e não faz `setState` após desmontagem.

---

## 4. Segurança

Não pontua na régua de POO, mas é o que uma banca externa encontra primeiro.

### 4.1 Senha em texto claro — `UsuarioService.cs:57`

```csharp
if (usuario != null && dto.Senha == usuario.Senha)
```

`UsuarioRepository.Add` grava a senha crua. Qualquer acesso ao `database.db` expõe todas as senhas.
Já existe decisão tomada: **ADR-003** define PBKDF2-SHA256, ≥100.000 iterações, formato
`pbkdf2-sha256$<iteracoes>$<salt>$<hash>`, `CryptographicOperations.FixedTimeEquals`, sem dependência
nova, com migração de legado no primeiro login bem-sucedido.

> **Correção em relação à revisão anterior:** não usar `PasswordHasher<T>` do Identity. O ADR-003
> rejeitou explicitamente essa dependência; a solução do projeto é PBKDF2 via
> `System.Security.Cryptography`.

### 4.2 `AlunoController` totalmente público — `AlunoController.cs:4-54`

Sem `[Authorize]`. Qualquer requisição anônima pode `GET /api/aluno` — devolvendo nome, escola,
série, telefone, nome e telefone do responsável, **`Anafilaxia` e `Restricoes`** — e
`DELETE /api/aluno/{id}`. `Anafilaxia` e `Restricoes` são **dados de saúde** (categoria sensível).

`ProtectedRoutes` no frontend protege a tela, não a API: a chamada funciona direto por `curl`.
Já está mapeado como **V5 / Spec 006**.

### 4.3 IDOR com exposição de CPF — `UsuarioController.cs:33-42`

`GET /api/usuario/perfil/{id}` **sem `[Authorize]`**. Anônimo enumera `id = 1, 2, 3…` e recebe
**Nome, CPF, DataNascimento, Email, Telefone, Foto** via `UsuarioService.ObterUsuarioPorId`.

O próprio arquivo tem o padrão correto em `AtualizarPerfil` (`:139-156`): compara o `id` da rota com
o `id` da sessão. O `GET` não faz essa verificação. **Não está coberto por nenhuma spec das 12
planejadas** — a Spec 006 trata de `AlunoController`.

### 4.4 `UsuarioRepository` viola a injeção de dependência — `UsuarioRepository.cs:9`

```csharp
private readonly ConnectionContext _context = new();
```

`AlunoRepository.cs:10-13` recebe o contexto pelo construtor; `UsuarioRepository` cria o seu.
Consequências: o registro `AddScoped<IUsuarioRepository, UsuarioRepository>()` (`Program.cs:11`) é
uma ficção; há **dois `DbContext` no mesmo request**; não é possível transação entre operações; o
contexto nunca é descartado pelo container.

Já está catalogado como **L4** e viola `arch.md` §5, mas **não está atribuído a nenhuma spec** —
a Spec 007 (`contrato-erro-e-infra-di`, status `planned`, sem documentos) é a candidata natural.

### 4.5 Connection string hardcoded — `ConnectionContext.cs:11-14`

`options.UseSqlite("Data Source = database.db")` dentro de `OnConfiguring`, com
`AddDbContext<ConnectionContext>()` **sem opções** (`Program.cs:45`). Funciona apenas porque a classe
de infraestrutura se autoconfigura; a configuração de ambiente deveria vir de fora.

### 4.6 Cookie sem `Secure` fora de desenvolvimento — `Program.cs:24`

`CookieSecurePolicy.SameAsRequest` envia o cookie por HTTP puro quando a requisição é HTTP.
**Já reconhecido no ADR-002** como pendência para produção ("precisa se tornar `Always` em
produção") — é decisão consciente, não descuido. Registrar como tal.

### 4.7 Sem proteção CSRF explícita

Autenticação por cookie + `POST`/`PUT`/`DELETE` sem validação antiforgery. `SameSite=Lax`
(`Program.cs:23`) mitiga o caso clássico. Aceitável no escopo, **se documentado como decisão**.

---

## 5. Design e POO (o que a régua mede)

### 5.1 `Aluno` anêmico — dimensão A (10%)

`Aluno.cs:18-30` aceita 10 parâmetros e **não valida nenhum**; nenhum método de domínio. É o oposto
de `Usuario`. Catalogado como **L1**, alvo da **Spec 004** (que define invariantes completas:
nome/escola/série/responsável com limites, nascimento entre 1900 e hoje, telefone 10–11 dígitos
normalizado, observação ≤500, `ArgumentException` por campo).

### 5.2 Coleção mutável de fora — dimensão C (20%)

`Aluno.cs:11` — `private set` protege a referência, não o conteúdo: `aluno.Restricoes.Add("x")`
funciona. Catalogado como **L2**, alvo da **Spec 003 / ADR-006**.

### 5.3 `SenhaValida()` estoura em execução — dimensão C

`Usuario.cs:129-136` — `Senha.Length` sem guard clause; `Senha` é `string?`. **L3**.

### 5.4 `AtualizarSenha` valida depois de usar — dimensão C

`Usuario.cs:140` usa `novaSenha.Length` **antes** do `IsNullOrWhiteSpace` da linha 145. A ordem é
parte da regra. **L3**; a Spec 002 (T3) já prevê a correção.

### 5.5 DTO com duas direções — dimensão D (20%)

- `DadosAlunoDTO` serve para criar, listar e detalhar, e carrega `Id` no corpo de entrada.
- `DadosUsuarioDTO.cs:10` expõe `Senha` e é devolvido como resposta de perfil.

Contradiz `arch.md` §5 (item 3 da §2) — e a própria **Spec 001 justifica manter assim** ("não contém
dado sensível"), justificativa que é **incorreta**: `Anafilaxia` e `Restricoes` são dados de saúde.
Ver §7.6.

### 5.6 Construtores telescópicos criam objeto inválido — dimensão B (15%)

`Usuario.cs:12-33` — quatro construtores, incluindo `Usuario(string email)` e um vazio público, que
produzem um `Usuario` sem nome, CPF ou senha. O construtor deveria ser o guardião do estado válido.
**Não está coberto por nenhuma spec.**

### 5.7 Validação só no caminho de atualização — dimensões A e C

`CriarUsuario` (`UsuarioService.cs:14-31`) valida apenas e-mail duplicado e tamanho da senha.
`AtualizarPerfil` (`Usuario.cs:35-56`) valida nome, formato de e-mail e limites. Resultado: **é
possível cadastrar um usuário com nome de 1 caractere e e-mail inválido, e depois nunca conseguir
editá-lo** — a atualização rejeita o que a criação aceitou.

**Não está coberto por nenhuma spec das 12.** É a violação mais direta do **ADR-004** ("invariante
mora no domínio"), que existe justamente para impedir isso.

### 5.8 Domínio conhecendo formato de transporte — dimensão A

`Usuario.cs:87-126` valida `data:image/jpeg;base64,` e faz `Convert.FromBase64String` dentro da
entidade. É formato de payload HTTP, não conceito de negócio. Viola `arch.md` §2 (item 7 da §2).

### 5.9 Namespaces — dimensão D

Sem `namespace`: `AlunoService`, `UsuarioService`, `AlunoController`, `LoginUsuarioDTO`,
`AlterarSenhaDTO`. E vários não correspondem à pasta:

| Pasta | Namespace |
|---|---|
| `Domain/Entities/` | `Ntc.Domain.Entity` |
| `Infrastructure/DataBase/` | `Ntc.Database` |
| `Infrastructure/Repositories/` | `Ntc.Database` |

### 5.10 Código morto

| Local | Problema |
|---|---|
| `AlunoService.cs:30` | `CriarAluno` sempre retorna `true` |
| `AlunoController.cs:21-24` | `if (!value) return BadRequest(...)` nunca executa |
| `AlunoRepository.cs:31-35` | `UpdateAluno` nunca chamado — é o `U` que falta |
| `IAlunoRepository.cs:7` | `List<Aluno>? GetAlunos()` — coleção nulável sem motivo; força `?? new()` |
| `UsuarioService.cs:109-127` | `TrocarSenha` sem endpoint |
| `Application/AlterarSenhaDTO.cs` | DTO sem consumidor |
| `Perfil.ts:13-19` | `buscarEstatisticas` chama `/usuarios/{id}/estatisticas` — sem `/api/`, inexistente |

Catalogado como **L9** — mas a lição está **incorreta** (§7.9).

### 5.11 Consultas redundantes — dimensão D

`UsuarioController.cs:55-57` e `:105-107` fazem **duas** consultas para a mesma linha
(`ObterNomePeloEmail` + `ObterIdPeloEmail`); `AtualizarPerfil` faz três (`:146` mais as duas de
`ObterUsuarioPorId`). O repositório já tem `GetByEmail`, que devolve o agregado — falta um
`ObterPorEmail` na camada de serviço.

### 5.12 `SaveChanges` por método de repositório

Cada `Add`/`Update`/`Delete` chama `SaveChanges()` internamente, impedindo compor duas operações em
uma transação (unit of work). Funciona no escopo atual; registrar como decisão consciente.

### 5.13 Lifetimes inconsistentes

`Program.cs:12` (`UsuarioService` como `AddTransient`) versus `Program.cs:14` (`AlunoService` como
`AddScoped`), sem justificativa. Viola `arch.md` §5 (item 2 da §2).

---

## 6. Frontend

### 6.1 Falta o `U` do CRUD — dimensão I (50% do bloco)

`AlunoApi.ts` tem `cadastrarAlunos`, `buscarAlunos` e `excluirAluno`; **não há `atualizarAluno`**.
O backend também não tem `PUT /api/aluno/{id}`. O único Update do sistema é o de **perfil**, que é
outro recurso. É o único requisito combinado que o grupo não fecha — `I = 6,0`.

**Já endereçado pela Spec 001**, completamente especificada (contrato de API, código de controller,
serviço, `atualizarAluno`, tela, 9 tasks, critérios de aceitação).

### 6.2 Endpoints de cardápio inexistentes

`CardapioApi.ts` chama `/api/alimentos`, `/api/cardapios`, `/api/cardapios/historico`,
`/api/cardapios/historico/{id}`. Só existem `AlunoController` e `UsuarioController`. As telas
`Cardapio.tsx` e `MontarCardapio.tsx` falham em runtime. **README reconhece**; **`arch.md` §9**
registra; **Spec 010** é o encaminhamento.

### 6.3 `credentials` ausente nas chamadas de aluno — `AlunoApi.ts:21,31,39`

Funciona hoje porque `AlunoController` não exige autenticação. **Assim que o `[Authorize]` da Spec
006 entrar, as três funções quebram** — a Spec 006 não lista esse ajuste. Defeito latente de
sequenciamento (viola `arch.md` §3).

### 6.4 Rota duplicada e rota de teste pública — `routes.tsx`

```tsx
<Route path="/cardapio" element={<Cardapio />} />   // linha 28
<Route path="/cardapio" element={<Cardapio />}/>    // linha 30 — duplicada
```

E `routes.tsx:19` expõe `/colors` → `colorsTESTE.tsx` **fora** do `ProtectedRoutes`.

### 6.5 Typo de rota — `routes.tsx:31`

`path="/vizualunos"` — deveria ser `/visualizar-alunos` ou `/alunos`.

### 6.6 `ApiURL` duplicado em três serviços

`AlunoApi.ts:1`, `CardapioApi.ts:1`, `Perfil.ts:1` repetem `const ApiURL = ""`. Extrair um cliente
HTTP único, que também centralize erro e `credentials`.

### 6.7 Nomenclatura misturada pt-BR / inglês

`authContext.tsx:4-21` — `UserLogin { password }` e `UserRegister { name, password, birthDate, cpf }`
contra a API `nome`, `senha`, `dataNascimento`. O mapeamento manual (`:80-83`, `:106-112`) é ponto de
erro silencioso: renomear no backend não gera erro de compilação no frontend.

### 6.8 Páginas grandes demais — dimensão K (25%)

| Arquivo | Linhas |
|---|---|
| `MontarCardapio.tsx` | 577 |
| `Perfil.tsx` | 517 |
| `CadastrarAlunos.tsx` | 491 |
| `Cardapio.tsx` | 422 |
| `VisualizarAlunos.tsx` | 419 |

Formulário, validação, estado, efeitos e layout no mesmo componente. Extrair hooks e componentes.

### 6.9 Projeto não sobe em máquina limpa

`vite.config.ts:7-11` **lança exceção** se `VITE_API_URL` não existir, e não há
`frontend/.env.example` versionado. `.env` está no `.gitignore` (linha 10) — correto — mas falta o
exemplo. Quem clonar não consegue rodar `npm run dev`.

---

## 7. Defeitos no plano (specs e documentos)

Esta seção não existia na revisão anterior e é a mais valiosa: o planejamento tem erros que
**bloqueariam a execução** antes de chegar a código.

> **Status em 2026-10-27:** os itens desta seção **foram corrigidos** nos documentos do
> projeto. Cada subseção mantém a descrição do defeito original — o que segue é o
> registro de auditoria — e o texto final de cada uma indica onde a correção está.
> Itens sem spec atribuída ficaram em `management/pending-items.md` como `PEND-001` a
> `PEND-012`.

### 7.1 Ciclo de dependência entre a Spec 001 e a Spec 005

- **Spec 001, task T3:** *"cobrir o caso de uso com teste de serviço… Validação: `dotnet test` verde
  (projeto criado na Spec 005)."*
- **Spec 005, requisito RF6:** *"testes de integração verificam… `PUT /api/aluno/{id}`"* — endpoint
  criado pela Spec 001.

E `PLANEJAMENTO.md`, no grafo de dependências, ordena `001 → 005`. As duas não podem vir antes uma
da outra como estão escritas. Na prática, 001 T3 não roda (não existe `backend.Tests/`), e 005 RF6
não tem o que testar.

**Correção:** extrair de 005 apenas a criação do projeto (`T1`–`T3`: `backend.Tests.csproj`, `Program`
como `partial`, fakes) e executá-la antes de 001. O restante de 005 continua depois de 001.

### 7.2 `AtualizarDados` recebe restrições e não as aplica

- **Spec 001, `design.md`:** o serviço chama
  `aluno.AtualizarDados(dto.Nome, …, dto.Restricoes, dto.Anafilaxia, …)` — passa restrições.
- **Spec 004, `design.md`:** o corpo de `AtualizarDados` atribui nome, nascimento, escola, série,
  telefone, observação, responsável e anafilaxia — **não atribui `Restricoes`**, e o `ValidarDados`
  devolve uma tupla sem restrições.

Resultado, se implementado como escrito: **editar um aluno descarta silenciosamente toda alteração
de restrição alimentar.** Em uma aplicação cujo propósito é segurança alimentar, é o pior defeito
possível — e ele nasce no design, não no código.

**Correção:** `AtualizarDados` deve sincronizar a coleção (limpar e reaplicar via
`AdicionarRestricao`, ou calcular diferença), com teste nominal em 005.

### 7.3 `Restricoes` readonly contornável por cast

`003/design.md` propõe `public IReadOnlyList<string> Restricoes => _restricoes;`. O critério
*"`aluno.Restricoes.Add("x")` não compila"* é atendido, mas
`((List<string>)aluno.Restricoes).Add("x")` continua funcionando.

**Correção:** `=> _restricoes.AsReadOnly();` — encapsulamento real, custo de uma chamada.

### 7.4 Construtor assumido pela Spec 002 não existe

`002/design.md` usa `new Usuario(dto.Nome, dto.Cpf, dto.DataNascimento, dto.Email)` — quatro
parâmetros. O construtor atual tem cinco (`…, string senha`), e nenhuma task de 002 prevê criar o
de quatro parâmetros.

### 7.5 Caminho de arquivo errado na Spec 002

`002/task.md` T1 manda criar `backend/Domain/Entity/HashSenha.cs`. A pasta real é
`backend/Domain/Entities/` (plural). Detalhe pequeno, mas o executor segue o caminho escrito.

### 7.6 A justificativa da Spec 001 sobre DTO é incorreta

`001/design.md` — *"Reutilizar `DadosAlunoDTO` em entrada e saída: não contém dado sensível."*
`DadosAlunoDTO` carrega `Anafilaxia` e `Restricoes`, que são dados de saúde. Além disso, a decisão
contradiz `arch.md` §5 ("DTOs de entrada e de saída são distintos"). É preciso escolher: ou o
`arch.md` abre exceção explícita, ou a Spec 001 separa os contratos.

### 7.7 Achado sem spec: autorização do `GET /usuario/perfil/{id}`

As 12 specs cobrem `AlunoController` (006), erro/DI (007), higiene de frontend (008), papéis (009),
cardápios (010–011) e operações (012). **Nenhuma cobre o IDOR de CPF da §4.3.** É um item órfão.

### 7.8 Achados sem spec: construtores inválidos e criação sem validação

§5.6 e §5.7 não aparecem em nenhuma das 12 specs, embora contrariem diretamente o ADR-004.

### 7.9 A lição L9 está factualmente errada

`lessons.md` L9 afirma que `Validacao.tsx` e `Sanitizacao.tsx` *"não são importados por nenhum
arquivo"*. **Falso:** `login.tsx:11` e `:17` importam ambos. Seguir a lição ao pé da letra leva a
apagar código em uso. A lição precisa ser reescrita para o fato verificável — se há duplicação, ela
está nas demais páginas, não na ausência de consumidores.

### 7.10 `PLANEJAMENTO.md` descreve um fluxo SDD que não existe mais

O documento cita, no passo a passo de promoção:

- `/spec-init` — não existe no fluxo atual;
- `/spec-review` — não existe (não há estágio de review; `/spec-new` sela `verified`);
- `/spec-audit` — é identidade aposentada, nunca roteável.

E a escada de status `planned → created → verified → implemented → audited` usa **`created`**, que
foi aposentado. A tabela "Critérios de transição" repete o mesmo fluxo obsoleto.

### 7.11 O passo 1 do `PLANEJAMENTO.md` aponta para uma pasta inexistente

O documento diz que o pipeline enxerga `NutriEduc/Dscode/management/` e não
`NutriEduc/management/`. **A pasta `Dscode/` não existe** no repositório. O diagnóstico do bloqueio
está desatualizado — e é a razão declarada de o `roadmap.md` canônico nunca ter sido gerado.

---

## 8. Documentação e processo

1. **`roadmap.md` está vazio** (`management/roadmap.md:19-20`). A causa está documentada
   (`PLANEJAMENTO.md`: o índice é propriedade do pipeline), mas o diagnóstico está errado (§7.11).
   Hoje a fonte de verdade real é `PLANEJAMENTO.md`.
2. **São 12 specs, não 5.** As specs 001–005 têm documentos; **006–012 existem apenas como linha na
   tabela** de `PLANEJAMENTO.md`, sem pasta. A revisão anterior contou só as pastas.
3. **Nenhuma spec foi implementada.** Todas as cinco com documentos estão em `status: created`.
4. **`pending-items.md` está vazio** ("none yet"), embora existam pendências conhecidas e não
   atribuídas a spec — §4.3, §5.6, §5.7, §4.4, os dead codes da §5.10. É exatamente para isso que o
   arquivo existe.
5. **`cross-session/README.md` é só o template** — nenhuma memória durável registrada, apesar de
   haver decisões com racional que sobreviveram apenas em ADRs.
6. **`.dscode/` não está no `.gitignore`.** O diretório contém `settings.json` (634 B),
   `state.db` e `file-index.db`. Está untracked hoje, mas um `git add .` o commitaria — e
   `settings.json` é o arquivo de configuração local que pode conter chaves. **`.env` já está coberto**
   (`.gitignore:10`), `.dscode/` não.
7. **`management/`, `AGENTS.md`, `correcoes.md`, `implementacoes.md` e `.dscode/` estão untracked** na
   branch `branch-kayke`. Se a entrega é por repositório, o planejamento inteiro não está no histórico.

---

## 9. Ordem correta de execução

Substitui a ordem da revisão anterior (que começava pela 001 e ignorava a dependência de 004).

```text
Fase A — independentes, em paralelo
  004 (invariantes de Aluno)          ← pré-requisito de 001 e 003
  002 (hash de senha)                 ← isolada; corrige L3

Fase B — depende de 004
  003 (restrições protegidas)
  005 T1–T3 (projeto de teste + fakes)  ← quebra o ciclo da §7.1

Fase C — depende de 004 e de 005 T1
  001 (CRUD de aluno: PUT + tela de edição)

Fase D — fecha a rede
  005 T4–T9 (suíte completa de testes)

Fase E — segurança e higiene
  006 (autorização) + ajuste de `credentials` em AlunoApi.ts (§6.3)
  007 (erro global + DI do UsuarioRepository) + IDOR de CPF (§4.3)
```

**Racional:** 004 antes de 001 e 003 porque `AtualizarDados` e as restrições vivem na mesma entidade.
005 T1–T3 antes de 001 porque 001 T3 exige `dotnet test` executável. 002 é totalmente independente e
pode rodar em qualquer ponto.

---

## 10. Prioridade por impacto na nota

| # | Ação | Dimensão | Esforço |
|---|---|---|---|
| 1 | `PUT /api/aluno/{id}` + `atualizarAluno` + tela de edição (Spec 001) | **I (50%)** — hoje 6,0 | M |
| 2 | Projeto de teste + casos de `Usuario`/`Aluno` (Spec 005) | **H (5%)** — hoje **0,0** | M |
| 3 | `IReadOnlyList` + `AdicionarRestricao`/`RemoverRestricao` (Spec 003) | **C (20%)** | S |
| 4 | Invariantes do construtor de `Aluno` (Spec 004) | **A (10%)** | M |
| 5 | `SenhaValida()` → guard clause; ordem de `AtualizarSenha` (Spec 002) | C | S |
| 6 | `UsuarioRepository` receber o `DbContext` (§4.4) | **D (20%)**, E (15%) | S |
| 7 | Unificar validação de criação e atualização de `Usuario` (§5.7) | A, C | M |
| 8 | Remover construtores que geram objeto inválido (§5.6) | B (15%) | S |
| 9 | Namespaces padronizados (§5.9) | D | S |
| 10 | Remover código morto (§5.10) | D | S |
| 11 | Associação real entre objetos (`E` = 15%) | **E (15%)** | L |
| 12 | Hash de senha (Spec 002) | segurança | M |

`S` ≈ até 1 arquivo · `M` ≈ 2–5 arquivos · `L` ≈ mudança de modelagem

---

## 11. Checklist para a apresentação

- [ ] CRUD de aluno completo e demonstrável (C, R, U, D)
- [ ] `dotnet test` verde com contagem reportada
- [ ] Nenhuma senha em texto claro no banco
- [ ] Nenhum endpoint de dado pessoal acessível sem autenticação
- [ ] `credentials: "include"` em toda chamada autenticada
- [ ] `/colors` e rota `/cardapio` duplicada removidas
- [ ] `frontend/.env.example` versionado; projeto sobe em máquina limpa
- [ ] `.dscode/` no `.gitignore`
- [ ] `roadmap.md` (ou `PLANEJAMENTO.md` corrigido) refletindo as 12 specs
- [ ] `pending-items.md` com os achados órfãos (§4.3, §5.6, §5.7)
- [ ] `arch.md` §5 e o código na mesma página — ou a regra revista, ou o código corrigido

---

## Errata da revisão anterior

Registro do que estava errado na versão anterior deste documento, para rastreabilidade.

| Afirmação anterior | Correção |
|---|---|
| "As 5 specs cobrem exatamente as lacunas" — dito por inferência dos nomes das pastas | Verificado agora. Cobrem 5 das lacunas, mas **deixam de fora** §4.3 (IDOR de CPF), §5.6 (construtores) e §5.7 (criação sem validação) |
| "Recomendo `PasswordHasher<T>` do Identity" | Contraria o **ADR-003**, que rejeita a dependência. Correto é PBKDF2 com `System.Security.Cryptography` |
| "Começar pela Spec 001 e pela 005" | 001 depende de 004 (T0); 001 e 005 têm ciclo (§7.1). Ordem correta na §9 |
| "`Validacao.tsx`/`Sanitizacao.tsx` não verificados" | São importados por `login.tsx:11,17`. A lição **L9**, que os declara mortos, está errada (§7.9) |
| "5 specs" | São **12** planejadas; 5 com documentos |
| "Não li `arch.md`/`adr.md`/`lessons.md`/specs" | A §2 e a §7 inteiras dependem deles — é onde está o maior valor da revisão |
| "Uma falha de segurança não apontada" (IDOR) | Mantido: é órfão no plano, confirmado contra as 12 specs |
