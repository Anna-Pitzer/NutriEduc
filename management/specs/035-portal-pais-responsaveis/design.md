---
name: portal-pais-responsaveis
status: created
references: Spec 035; Spec 010; Spec 016; Spec 017; Spec 018; Spec 025; Spec 029; Spec 033; Spec 034; ADR-009; ADR-021
---

# Design — Portal dos Pais/Responsáveis

## Design Approach

A implementação é incremental e reutiliza os contratos existentes. O backend já resolve vínculo,
cardápio servido, ocorrências, solicitações e feedback; o trabalho novo é de interface, extração
de componentes e uma extensão pontual do feedback.

1. **Backend primeiro:** persistir `AlunoId`/`AlunoNome` no feedback e expor os campos nos DTOs.
2. **Estrutura da área:** `LayoutPais`, rotas e `SeletorDeFilho`.
3. **Páginas por funcionalidade:** cardápios, ocorrências, solicitações e feedback, consumindo os
   serviços existentes.
4. **Fechamento:** testes de integração, gates e validação manual.

Alternativas rejeitadas:

- **Endpoint agregado `GET /api/painel/familia`:** pertence à Spec 025; criá-lo aqui duplicaria
  contrato e escopo.
- **Texto livre para alergia:** exigiria criação de item de catálogo por solicitação, mudando o
  domínio da Spec 010.
- **Nova tabela para ocorrências/feedback:** desnecessária; as entidades existentes já guardam
  autor, aluno e snapshots.
- **Biblioteca de estado global:** o volume de estado é local a cada página; uma dependência nova
  violaria NFR2 e KISS.

Premissas declaradas:

- O backend da Spec 029 está disponível e permanece estável; o frontend pendente da 029 não
  bloqueia a 035.
- A Spec 025 não será implementada antes da 035; se for, a troca de fonte é local ao painel.
- O catálogo ativo de restrições é a fonte única para pedidos de alergia.

## Architecture Decisions

| ID | Decisão | Motivo | Princípio |
| --- | --- | --- | --- |
| D1 | Não criar endpoint agregado; usar contratos por aluno | Evita duplicar a Spec 025 e mantém baixo acoplamento | KISS, DRY |
| D2 | `pages/pais/` própria + componentes compartilhados em `components/` | Evita copiar a lógica de ocorrências/solicitações/feedback | DRY, SRP |
| D3 | `SeletorDeFilho` único e controlado | Uma responsabilidade, interface estreita e reutilizável | SRP, baixo acoplamento |
| D4 | Cardápio servido, nunca o público, na visão do filho | O público não representa a versão adaptada e induziria erro | Correção |
| D5 | Ocorrência de responsável somente tipo `Aluno` | O backend já impõe; a UI não oferece caminho inválido | Defesa em profundidade |
| D6 | Alergia somente catálogo ativo | Catálogo é dado de referência da Nutricionista | KISS, integridade |
| D7 | Feedback persiste `AlunoId` + `AlunoNome` | Requisito de associação e preservação histórica (ADR-009) | Correção, rastreabilidade |
| D8 | Nenhuma permissão nova | Matriz 028 permanece autoritativa | Segurança, baixo acoplamento |
| D9 | Um único migration | Mudança de schema mínima e reversível | Cirurgia |
| D10 | Estado local por página, sem store global | Volume pequeno; menos acoplamento | KISS |
| D11 | Erros seguem o contrato `{ erro }` já existente | Consistência e tratamento único | DRY |
| D12 | Datas sempre explícitas na API (`yyyy-MM-dd`) | Evita dependência do default UTC do backend | Determinismo |

## Component / Module Breakdown

### Frontend components

| Componente | Responsabilidade | Interface principal |
| --- | --- | --- |
| `LayoutPais` | Cabeçalho (`Header` + `SinoNotificacoes`), abas e `Outlet` | sem props |
| `SeletorDeFilho` | Selecionar um filho; pré-selecionar quando houver um | `{ filhos: AlunoResposta[]; value: number \| null; onChange: (id: number) => void; disabled?: boolean; id?: string }` |
| `PainelPais` | Carregar filhos e exibir cartões/atalhos | sem props |
| `CardapiosPais` | Filho + data + cardápio servido + filtro de refeição | sem props |
| `OcorrenciasPais` | Lista, filtros, paginação e detalhe | sem props |
| `NovaOcorrenciaPais` | Formulário de ocorrência tipo `Aluno` | sem props |
| `SolicitacoesPais` | Lista e formulário de `InclusaoAlergia` | sem props |
| `FeedbackPais` | Lista própria e formulário com aluno opcional | sem props |
| `CardapioDoAluno` | Apresentação de `CardapioServidoDTO` | `{ resultado: CardapioServido; refeicao: number \| "todas"; onRefeicao: (valor: number \| "todas") => void }` |
| `ListaOcorrencias` | Tabela/lista reutilizável | `{ itens: OcorrenciaResumo[]; onSelecionar: (id: number) => void; carregando: boolean }` |
| `FormularioOcorrencia` | Campos filho/descrição e submit | `{ filhos: AlunoResposta[]; alunoId: number \| null; onAluno: (id: number) => void; descricao: string; onDescricao: (valor: string) => void; enviando: boolean; erro?: string; onSubmit: () => void }` |
| `ListaSolicitacoes` | Tabela/lista reutilizável | `{ itens: SolicitacaoResumo[]; onSelecionar: (id: number) => void; carregando: boolean }` |
| `FormularioSolicitacao` | Filho/restrição/descrição | `{ filhos: AlunoResposta[]; restricoes: Restricao[]; alunoId: number \| null; onAluno: (id: number) => void; restricaoNome: string; onRestricao: (valor: string) => void; descricao: string; onDescricao: (valor: string) => void; enviando: boolean; erro?: string; onSubmit: () => void }` |
| `FormularioFeedback` | Assunto/mensagem/filho opcional | `{ filhos: AlunoResposta[]; alunoId: number \| null; onAluno: (id: number \| null) => void; assunto: string; onAssunto: (valor: string) => void; mensagem: string; onMensagem: (valor: string) => void; enviando: boolean; erro?: string; onSubmit: () => void }` |
| `ListaFeedback` | Lista própria | `{ itens: FeedbackResumo[]; onEncerrar: (id: number) => void; carregando: boolean }` |

### Services (frontend)

```ts
// CardapioApi.ts (novo)
export async function buscarCardapioDoAluno(
  alunoId: number,
  data: string,
): Promise<CardapioServido>;

// FeedbackApi.ts (estendido)
export async function criarFeedback(
  assunto: string,
  mensagem: string,
  alunoId?: number,
): Promise<FeedbackDetalhe>;

export async function listarFeedbacks(
  filtro?: { status?: StatusFeedback | ""; alunoId?: number; pagina?: number; tamanho?: number },
): Promise<Pagina<FeedbackResumo>>;

// OcorrenciaApi.ts (estendido: campos já suportados pelo backend)
export interface FiltrosOcorrencia {
  tipo?: TipoOcorrencia | "";
  status?: StatusOcorrencia | "";
  alunoId?: number;
  de?: string;   // yyyy-MM-dd
  ate?: string;  // yyyy-MM-dd
  pagina?: number;
  tamanho?: number;
}
```

### Backend changes

| Arquivo | Mudança |
| --- | --- |
| `backend/Domain/Entities/Feedback.cs` | `AlunoId` (`int?`), `AlunoNome` (`string?`), parâmetros no construtor e validação do snapshot |
| `backend/Application/FeedbackDTOs.cs` | `FeedbackResumoDTO.AlunoId`/`Aluno` |
| `backend/Domain/EscopoUsuario.cs` (`FiltroFeedback`) | `AlunoId` (`int?`) |
| `backend/API/Services/FeedbackService.cs` | persistir aluno; validar vínculo; `Listar(string? status, int? alunoId, int? pagina, int? tamanho, EscopoUsuario escopo)`; no `Criar`, copiar `aluno.Nome` para `AlunoNome` |
| `backend/API/Controllers/FeedbackController.cs` | `[FromQuery] int? alunoId` na listagem |
| `backend/Infrastructure/Repositories/FeedbackRepository.cs` | aplicar `AlunoId` no filtro |
| `backend/Infrastructure/DataBase/ConnectionContext.cs` | mapear FK `SetNull` e índice |
| `backend/Migrations/<timestamp>_FeedbackAluno.cs` | migration nova |

## Data Flow

### 1. Painel dos filhos

1. `PainelPais` monta e chama `GET /api/aluno` com `credentials: "include"`.
2. API responde `200` com `AlunoResposta[]` já escopado por vínculo.
3. UI ordena por `nome` com `localeCompare("pt-BR")` e renderiza cartões; se vazio, renderiza
   estado vazio e desabilita ações.
4. `401` → logout; erro de rede → mensagem com botão “Tentar novamente”.

### 2. Cardápio servido

1. `SeletorDeFilho` fornece `alunoId`; o input de data é montado com `getFullYear()`,
   `getMonth()+1` e `getDate()` (local) e enviado como `yyyy-MM-dd`.
2. `CardapiosPais` chama `buscarCardapioDoAluno(alunoId, data)`.
3. API responde `200` com `CardapioServidoDTO`.
4. Se `disponivel=true`, `CardapioDoAluno` renderiza `cardapio.refeicoes`; o filtro local usa
   `refeicao === "todas"` ou `refeicao.id`.
5. Se `disponivel=false`, renderiza `motivo` + `alerta`; não renderiza refeições.
6. `403`/`404` → mensagem de acesso/não encontrado; `400` de data → erro no campo.

### 3. Ocorrências — leitura

1. Página carrega filhos (`GET /api/aluno`) e a primeira página
   (`GET /api/ocorrencias?alunoId=&tipo=&status=&de=&ate=&pagina=1&tamanho=20`).
2. API responde `PaginaDTO<OcorrenciaResumoDTO>`.
3. `ListaOcorrencias` renderiza os itens; filtros disparam nova consulta com `pagina=1`.
4. Selecionar item chama `GET /api/ocorrencias/{id}` e abre `OcorrenciaDTO`.
5. `403` → mensagem genérica; `404` → estado não encontrado.
6. A ocorrência criada entra na listagem dos profissionais pelo escopo existente (matriz 028);
   nenhum endpoint novo é necessário.

### 4. Ocorrências — relato

1. `NovaOcorrenciaPais` carrega filhos e exige `alunoId` + `descricao` (5–1000).
2. Submit chama `POST /api/ocorrencias` com `{ tipo: "Aluno", alunoId, descricao }`.
3. `201` → confirmação, limpeza do formulário e navegação para a lista atualizada.
4. `400` → erro no campo, preservando o texto; `403` → aluno fora do vínculo.

### 5. Alergia/restrição

1. Página carrega filhos e `GET /api/restricoes`; mantém apenas itens ativos.
2. Submit chama `POST /api/solicitacoes` com `{ tipo: "InclusaoAlergia", alunoId,
   restricaoNome, descricao }`.
3. `201` → confirmação e recarga da lista `GET /api/solicitacoes?alunoId=&tipo=&status=`.
4. Acompanhamento lê `SolicitacaoResumoDTO`; `podeCancelar` não existe no DTO atual, então a UI
   habilita cancelamento apenas em `Aberta` e trata `409` para `EmAnalise`/estado final.
5. Aprovação/recusa é feita fora da área de pais; a mudança chega por notificação e recarga.

### 6. Feedback

1. Página carrega filhos e `GET /api/feedbacks?pagina=1&tamanho=20`.
2. Submit chama `POST /api/feedbacks` com `{ assunto, mensagem, alunoId? }`.
3. `201` → confirmação e recarga da lista.
4. `podeEncerrar=true` → botão chama `POST /api/feedbacks/{id}/encerrar`; `200` atualiza o item.
5. `403` (aluno fora do vínculo) ou `400` (limites) → mensagem e preservação do formulário.

### 7. Cancelamentos

1. Ocorrência `Aberta`, `podeCancelar=true` e mesmo dia civil da criação em `America/Sao_Paulo`:
   `POST /api/ocorrencias/{id}/cancelar`.
2. `204` → confirmação e recarga do detalhe/lista; `409` → mensagem e recarga; `403` → não exibe
   a ação e trata como acesso negado se chamada.
3. Solicitação com status `Aberta`: `POST /api/solicitacoes/{id}/cancelar`.
4. `204` → confirmação e recarga da lista; `409` → mensagem e recarga; `403` → ação não exibida.

## File / Module Layout

### Create

- `frontend/src/pages/pais/LayoutPais.tsx`
- `frontend/src/pages/pais/PainelPais.tsx`
- `frontend/src/pages/pais/CardapiosPais.tsx`
- `frontend/src/pages/pais/OcorrenciasPais.tsx`
- `frontend/src/pages/pais/NovaOcorrenciaPais.tsx`
- `frontend/src/pages/pais/SolicitacoesPais.tsx`
- `frontend/src/pages/pais/FeedbackPais.tsx`
- `frontend/src/components/SeletorDeFilho.tsx`
- `frontend/src/components/CardapioDoAluno.tsx`
- `frontend/src/components/ListaOcorrencias.tsx`
- `frontend/src/components/FormularioOcorrencia.tsx`
- `frontend/src/components/ListaSolicitacoes.tsx`
- `frontend/src/components/FormularioSolicitacao.tsx`
- `frontend/src/components/FormularioFeedback.tsx`
- `frontend/src/components/ListaFeedback.tsx`
- `backend/Migrations/<timestamp>_FeedbackAluno.cs`

### Modify

- `frontend/src/routes/routes.tsx`
- `frontend/src/pages/pais/HomePais.tsx` (substituir conteúdo/rota por `LayoutPais`/`PainelPais`)
- `frontend/src/components/Navbar.tsx`
- `frontend/src/services/CardapioApi.ts`
- `frontend/src/services/FeedbackApi.ts`
- `frontend/src/services/OcorrenciaApi.ts`
- `frontend/src/pages/ocorrencias/OcorrenciasPage.tsx`
- `frontend/src/pages/solicitacoes/SolicitacoesPage.tsx`
- `frontend/src/pages/feedback/FeedbackPage.tsx`
- `backend/Domain/Entities/Feedback.cs`
- `backend/Application/FeedbackDTOs.cs`
- `backend/Domain/EscopoUsuario.cs` (`FiltroFeedback`)
- `backend/API/Services/FeedbackService.cs`
- `backend/API/Controllers/FeedbackController.cs`
- `backend/Infrastructure/Repositories/FeedbackRepository.cs`
- `backend/Infrastructure/DataBase/ConnectionContext.cs`
- `backend.Tests/Integration/FeedbackApiTests.cs`
- `backend.Tests/Domain/FeedbackTests.cs`

### Read-only reuse

- `frontend/src/services/AlunoApi.ts`, `SolicitacaoApi.ts`, `RestricaoApi.ts`,
  `NotificacaoApi.ts`
- `frontend/src/components/Header.tsx`, `Footer.tsx`, `SinoNotificacoes.tsx`
- `frontend/src/context/authContext.tsx`

## Data Structures

### TypeScript (new)

```ts
type StatusFeedback = "Aberto" | "Respondido" | "Encerrado";

interface CardapioServido {
  alunoId: number;
  alunoNome: string;
  escolaId: number | null;
  conjuntoRestricoesChave: string;
  disponivel: boolean;
  motivo: string | null;
  alerta: boolean;
  cardapio: Cardapio | null;
}

interface Cardapio {
  id: number;
  nome: string;
  tipo: "Geral" | "Adaptado";
  origemId: number | null;
  escolaId: number | null;
  refeicoes: RefeicaoCardapio[];
  restricoes: RestricaoDaVersao[];
  substituicoes: SubstituicaoCardapio[];
}

interface RefeicaoCardapio {
  id: number;
  nome: string;
  ordem: number;
  itens: ItemCardapio[];
  total: ValoresNutricionais;
}

interface ItemCardapio {
  alimentoId: number;
  alimento: string;
  gramas: number;
  valores: ValoresNutricionais;
}

interface RestricaoDaVersao {
  id: number;
  nome: string;
  categoria: "Alergia" | "Restricao";
}

interface SubstituicaoCardapio {
  refeicaoOrdem: number;
  refeicaoNome: string;
  alimentoOriginalId: number;
  alimentoOriginal: string;
  alimentoSubstitutoId: number;
  alimentoSubstituto: string;
  gramas: number;
  restricoesBloqueantes: string;
}
```

> `Cardapio` é o subconjunto de `CardapioDTO` consumido pela tela; o DTO completo do backend
> continua autoritativo e não é alterado.

`FeedbackResumo` (estendido) e `FeedbackDetalhe`:

```ts
interface FeedbackResumo {
  id: number;
  assunto: string;
  mensagem: string;
  alunoId: number | null;
  aluno: string | null;
  status: StatusFeedback;
  resposta: string | null;
  respondidoPor: string | null;
  criadoEm: string;
  podeEncerrar: boolean;
  podeResponder: boolean;
}

interface FeedbackDetalhe extends FeedbackResumo {
  atualizadoEm: string;
}
```

### C# (backend)

```csharp
// Feedback.cs
public int? AlunoId { get; private set; }
public string? AlunoNome { get; private set; }

// Construtor atual preservado (delega com aluno nulo)
public Feedback(
    string? assunto,
    string? mensagem,
    int autorId,
    string? autorNome,
    PapelUsuario papel,
    int unidadeId,
    int? escolaId,
    DateTime utc);

// Novo construtor com aluno opcional
public Feedback(
    string? assunto,
    string? mensagem,
    int autorId,
    string? autorNome,
    PapelUsuario papel,
    int unidadeId,
    int? escolaId,
    int? alunoId,
    string? alunoNome,
    DateTime utc);

// FeedbackResumoDTO.cs
public int? AlunoId { get; set; }
public string? Aluno { get; set; }

// FiltroFeedback (EscopoUsuario.cs)
public sealed record FiltroFeedback(
    StatusFeedback? Status = null,
    int? UnidadeId = null,
    int? EscolaId = null,
    int? AutorId = null,
    int? AlunoId = null);
```

Regras de dados:

- `AlunoId` só é preenchido quando o vínculo é válido e `alunoId > 0`; `AlunoNome` é o `Nome` do
  aluno no momento da criação, com 2–100 caracteres.
- `Feedback.AlunoId` é `null` quando o feedback não referencia aluno; nesse caso `AlunoNome` é
  `null`. Se `alunoId` for informado, `alunoNome` é obrigatório.
- Ao persistir, o serviço copia `aluno.Nome` para `AlunoNome`; o cliente não envia o nome.
- FK `OnDelete(DeleteBehavior.SetNull)`; índice `IX_Feedbacks_AlunoId`.
- `AlunoNome` é `TEXT` nullable; não é atualizado quando o aluno é renomeado.

## Error Handling

| Componente | Condição | Comportamento |
| --- | --- | --- |
| `PainelPais` | `401` | `logout()` e redireciona para `/login` |
| `PainelPais` | rede/500 | mensagem “Não foi possível carregar seus filhos” + botão repetir |
| `SeletorDeFilho` | lista vazia | renderiza select desabilitado com “Nenhum aluno vinculado” |
| `CardapiosPais` | `400` data | erro no campo; não envia |
| `CardapiosPais` | `403`/`404` | mensagem de acesso/não encontrado; mantém seleção |
| `OcorrenciasPais` | `400` filtro | erro no filtro; mantém lista anterior |
| `OcorrenciasPais` | `403` detalhe | mensagem genérica; fecha detalhe |
| `OcorrenciasPais` | `409` cancelamento | mensagem; recarrega detalhe/lista |
| `NovaOcorrenciaPais` | `400` | erro no campo; preserva texto |
| `NovaOcorrenciaPais` | `403` | mensagem de vínculo; não limpa formulário |
| `SolicitacoesPais` | `400` restrição | erro no select; recarrega catálogo |
| `SolicitacoesPais` | `409` duplicidade | mensagem com id existente; link para o pedido |
| `SolicitacoesPais` | `409` cancelamento | mensagem; recarrega lista |
| `FeedbackPais` | `400` limites | erro no campo; preserva texto |
| `FeedbackPais` | `403` aluno | mensagem; não envia |
| `FeedbackPais` | `409` encerrar | botão não é exibido; mensagem se chamado |
| Todos | `401` em escrita | logout; não reenvia automaticamente |
| Todos | falha de rede | mensagem com repetir; não perde estado local |

## Security and Isolation

- Toda decisão de acesso é do backend; o frontend só oculta ações.
- `alunoId` de todas as operações é validado contra o vínculo pelo serviço.
- Nenhuma tela recebe lista global de alunos; `GET /api/aluno` já é escopado.
- Feedback não expõe dado clínico; cardápio servido não expõe outro aluno.
- Logs e mensagens não incluem CPF, senha, texto de ocorrência/feedback ou conteúdo clínico.
- O `AlunoNome` do feedback é snapshot; não é usado para autorização.

## Testing Strategy

- Backend (`backend.Tests/Integration/FeedbackApiTests.cs` e `backend.Tests/Domain/FeedbackTests.cs`):
  - domínio: `alunoId` nulo exige `alunoNome` nulo; `alunoId` válido exige `alunoNome` 2–100;
  - feedback com aluno vinculado → `201`, persiste `AlunoId`/`AlunoNome`, lista devolve;
  - feedback sem aluno → `201`, `AlunoId=null`;
  - feedback com aluno de outro responsável → `403`;
  - filtro `alunoId` devolve apenas o esperado;
  - `Feedback` sem aluno continua funcionando para merendeira/escola.
- Regressões existentes: ocorrência/solicitação/cardápio do responsável (já cobertas; manter).
- Frontend: `npm run build` e `npm run lint`; sem runner de testes (PEND-049).
- Manual: dois responsáveis, um com dois filhos; percorrer os seis fluxos e os edge cases.
- Gates: `dotnet build`, `dotnet test`, `npm run build`, `npm run lint`.

## Karpathy Compliance

| Princípio | Evidência no design |
| --- | --- |
| Think Before Coding | Análise de contratos existentes; alternativas rejeitadas; premissas explícitas |
| Simplicity First | Nenhum endpoint/aggregado novo; uma migration; estado local |
| Surgical Changes | Lista fechada de arquivos; nenhuma alteração em ocorrência/solicitação/cardápio |
| Goal-Driven Execution | Critérios de aceitação objetivos e gates por task |
| Single Responsibility | `SeletorDeFilho`, `CardapioDoAluno`, listas e formulários separados |
| Low Coupling / DRY | Serviços existentes reutilizados; componentes compartilhados extraídos uma vez |
| Explicit over implicit | DTOs, props, status codes e formatos de data declarados |

## Traceability (RF → component → task)

| RF | Componente | Task |
| --- | --- | --- |
| RF1, RF26 | `PainelPais`, `SeletorDeFilho` | T4, T5 |
| RF2–RF6 | `CardapiosPais`, `CardapioDoAluno`, `CardapioApi` | T6 |
| RF7–RF10 | `OcorrenciasPais`, `ListaOcorrencias` | T7 |
| RF11–RF14 | `NovaOcorrenciaPais`, `FormularioOcorrencia` | T8 |
| RF15–RF20 | `SolicitacoesPais`, `FormularioSolicitacao`, `ListaSolicitacoes` | T9 |
| RF21–RF25 | `FeedbackPais`, `FormularioFeedback`, `ListaFeedback`, backend | T1–T3, T10 |
| RF27–RF32 | layout, estados, erros e datas | T4, T11, T12 |
| NFR1–NFR12 | revisão, testes e gates | T12–T14 |

## Risks and Dependencies

- **Spec 025:** não bloqueia; a agregação futura troca apenas a fonte do painel.
- **PEND-042:** frontend da 029 pendente; a página de cardápios usa o backend pronto.
- **PEND-048:** divergência matriz × `CardapioService.PodeLer`; a 035 usa a rota por aluno, mas a
  divergência deve ser resolvida antes da certificação.
- **PEND-049:** sem testes de frontend; aceitação depende de validação manual.
- **Migration:** se a revisão rejeitar `AlunoId` persistido, RF22 e DA3 precisam ser renegociados.
- **Dados legados:** feedbacks antigos têm `AlunoId=null` e `AlunoNome=null`; a UI mostra
  “Sem aluno”.
