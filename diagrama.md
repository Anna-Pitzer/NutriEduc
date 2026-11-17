# Diagramas — NutriEduc

Diagramas do sistema derivados do **código atual**, não da intenção das specs.

**Fontes:** `backend/Program.cs` (DI), `backend/Infrastructure/DataBase/ConnectionContext.cs`
(DbSets, FKs, índices), enums e métodos de transição em `backend/Domain/Entities/`,
`backend/API/Services/`, `frontend/src/routes/routes.tsx`.
Levantado em **2026-11-17**.

**Como renderizar:** os blocos são [Mermaid](https://mermaid.js.org/) — renderizam direto no
GitHub/GitLab e no preview do VS Code. Não há imagem versionada; o texto é a fonte.

---

## 1. Camadas e dependências

**Fato que importa:** o backend é **um único projeto** (`backend/backend.csproj`,
`net10.0`), não quatro assemblies. As camadas são pastas e namespaces, e a fronteira é
mantida por convenção + teste — `backend/Domain` tem **zero** `using` de
`Microsoft.EntityFrameworkCore`, e quem mapeia é `Infrastructure`.

```mermaid
flowchart TD
    subgraph browser["Navegador"]
        UI["React 19 + TypeScript<br/>frontend/src/pages, components"]
        SVC["Serviços HTTP<br/>frontend/src/services/*.ts"]
        UI --> SVC
    end

    subgraph backend["backend.csproj — net10.0 (um projeto, quatro camadas por pasta)"]
        API["API<br/>Controllers · Middleware · Services"]
        APP["Application<br/>DTOs · Mappers · Seguranca/MatrizDePermissoes"]
        DOM["Domain<br/>Entities · Services (puros) · Interface · Filtros"]
        INFRA["Infrastructure<br/>ConnectionContext · Repositories · Exportadores"]
        API --> APP
        API --> DOM
        APP --> DOM
        INFRA --> DOM
        API -.->|"usa via interface"| INFRA
    end

    DB[("SQLite<br/>database.db / :memory: nos testes")]
    TESTS["backend.Tests<br/>Domain · Services · Integration"]

    SVC -->|"HTTP + cookie de sessão"| API
    INFRA --> DB
    TESTS --> backend
    TESTS -.->|"InternalsVisibleTo"| backend
```

**Regras que o diagrama representa**

| Regra | Onde está garantida |
| --- | --- |
| `Domain` não conhece EF nem HTTP | ausência de `using Microsoft.EntityFrameworkCore` em `backend/Domain` |
| `Controller` não calcula regra de papel | `EscopoUsuario` + `MatrizDePermissoes` (Spec 028) |
| Repositório não abre conexão própria | `IUnidadeDeTrabalho` com `ConnectionContext` Scoped (L4) |
| Erro de domínio vira HTTP em um só lugar | middleware em `API/Middleware` (Spec 007) |

---

## 2. Mapa de módulos e dependências

Regra declarada em `management/arch.md` §11: nenhum módulo reimplementa regra de outro.

```mermaid
flowchart LR
    S009["009<br/>papéis/escola"]
    S010["010<br/>restrições"]
    S011["011<br/>catálogo"]
    S012["012<br/>cardápio"]
    S013["013<br/>geração"]
    S014["014<br/>compatibilidade"]
    S015["015<br/>publicação"]
    S016["016<br/>ocorrências"]
    S017["017<br/>solicitações"]
    S018["018<br/>feedback/notificações"]
    S019["019<br/>relatórios"]

    S027["027<br/>hierarquia"]
    S028["028<br/>matriz"]
    S033["033<br/>acesso"]
    S034["034<br/>vínculo"]
    S025["025<br/>painel"]
    S029["029<br/>geral/adaptado"]
    S030["030<br/>dados cadastrais"]
    S032["032<br/>correção da geração"]
    S035["035<br/>portal dos pais"]
    S039["039<br/>merendeira"]

    S009 --> S010 --> S011 --> S012 --> S014
    S012 --> S013
    S014 --> S015
    S015 --> S016 --> S017 --> S018 --> S019
    S027 --> S028 --> S033
    S027 --> S034
    S033 --> S034
    S010 --> S029
    S011 --> S029
    S012 --> S029
    S014 --> S029
    S015 --> S029
    S013 --> S032
    S029 --> S032
    S017 --> S030
    S016 --> S028
    S025 --> S028
    S016 --> S035
    S017 --> S035
    S018 --> S035
    S029 --> S035
    S034 --> S035
    S009 --> S039
    S029 --> S039
    S025 --> S039
```

---

## 3. Modelo de dados (ER por agregado)

Extraído de `ConnectionContext.cs`. Cardápio e catálogo aparecem em diagrama próprio
porque concentram a maior parte das relações.

### 3.1 Identidade, escola e aluno

```mermaid
erDiagram
    UNIDADE_ESCOLAR ||--o{ ESCOLA : "possui"
    UNIDADE_ESCOLAR ||--o{ USUARIO : "escopo administrativo"
    ESCOLA ||--o{ USUARIO : "escopo da escola"
    UNIDADE_ESCOLAR ||--o{ ALUNO : "derivada da escola"
    ESCOLA ||--o{ ALUNO : "matrícula"
    ALUNO ||--o{ HISTORICO_ALUNO : "trilha"
    ALUNO ||--o{ VINCULO : "responsáveis"
    USUARIO ||--o{ VINCULO : "responsável"
    UNIDADE_ESCOLAR ||--o{ TOKEN_CADASTRO : "emite"
    USUARIO ||--o{ TOKEN_CADASTRO : "cria"
    UNIDADE_ESCOLAR ||--o{ CONSUMO_BOOTSTRAP : "primeiro admin"

    USUARIO {
        int Id PK
        string Nome
        string Cpf
        string Email "índice único"
        string Telefone
        string Crn "índice"
        int Papel "enum PapelUsuario"
        bool Ativo
        int EscolaId FK
        int UnidadeId FK
        string HashSenha "VO HashSenha"
    }

    ALUNO {
        int Id PK
        string Nome
        DateOnly Nascimento
        string Telefone
        string Observacao
        bool Anafilaxia
        string Restricoes "coluna JSON de nomes"
        string Turma "tipo complexo: Escola + Serie texto"
        string Responsavel "tipo complexo: nome + telefone"
        int EscolaId FK "anulável no modelo; exigido na criação"
        int UnidadeId FK "anulável; derivado da escola"
    }

    VINCULO {
        int Id PK
        int AlunoId FK
        int ResponsavelUsuarioId FK
    }

    HISTORICO_ALUNO {
        int Id PK
        int AlunoId FK
        string Tipo "enum TipoEventoAluno"
        string Campo
        string ValorAnterior
        string ValorNovo
        string AutorNome
        datetime CriadoEmUtc
    }
```

### 3.2 Catálogo e cardápio

```mermaid
erDiagram
    CATEGORIA_ALIMENTO ||--o{ ALIMENTO : "classifica"
    ALIMENTO ||--o{ ALIMENTO_RESTRICAO : "bloqueado por"
    RESTRICAO_ALIMENTAR ||--o{ ALIMENTO_RESTRICAO : "bloqueia"
    UNIDADE_ESCOLAR ||--o{ CARDAPIO : "pertence"
    ESCOLA ||--o{ CARDAPIO : "versão adaptada"
    CARDAPIO ||--o{ CARDAPIO : "OrigemId (geral → adaptados)"
    CARDAPIO ||--o{ REFEICAO : "3 refeições fixas"
    REFEICAO ||--o{ ITEM_REFEICAO : "composição"
    ALIMENTO ||--o{ ITEM_REFEICAO : "usado em"
    CARDAPIO ||--o{ HISTORICO_CARDAPIO : "eventos"
    CARDAPIO ||--o{ SUBSTITUICAO_ITEM : "trocas registradas"
    CARDAPIO ||--o{ PENDENCIA_ADAPTACAO : "sem substituto"
    CARDAPIO ||--o{ CONFIRMACAO_INCOMPATIBILIDADE : "aceite do nutricionista"
    UNIDADE_ESCOLAR ||--o{ PARAMETRO_GERACAO : "parâmetros"

    ALIMENTO {
        int Id PK
        string Nome
        string NomeNormalizado "índice"
        int CategoriaId FK
        string ValoresNutricionais "tipo complexo: 6 medidas por 100 g"
        bool Ativo
    }

    CARDAPIO {
        int Id PK
        string Nome
        int Tipo "enum TipoCardapio: Geral ou Adaptado"
        string Situacao "enum SituacaoCardapio"
        int UnidadeId FK
        int OrigemId FK "self, versão de origem"
        int EscolaId FK "só em adaptado"
        string ConjuntoRestricoesChave "índice único (OrigemId, EscolaId, Chave) filtrado"
        string StatusAdaptacao "enum, só em adaptado"
        string Periodo "tipo complexo Periodo_Inicio, Periodo_Fim"
        int Versao
        string NutricionistaResponsavel "snapshot: nome, email, cpf, crn"
    }

    REFEICAO {
        int Id PK
        int CardapioId FK
        string Nome
        int Ordem "índice (CardapioId, Ordem) único"
    }

    ITEM_REFEICAO {
        int Id PK
        int RefeicaoId FK
        int AlimentoId FK
        decimal QuantidadeGramas "máx 5000, 2 casas"
    }

    SUBSTITUICAO_ITEM {
        int Id PK
        int CardapioId FK
        int ItemOriginalId
        int ItemNovoId
    }
```

### 3.3 Solicitações, ocorrências, feedback, notificações

```mermaid
erDiagram
    UNIDADE_ESCOLAR ||--o{ SOLICITACAO : "todas têm unidade"
    ALUNO ||--o{ SOLICITACAO : "quando o tipo refere aluno"
    CARDAPIO ||--o{ SOLICITACAO : "quando o tipo refere cardápio"
    USUARIO ||--o{ SOLICITACAO : "solicitante"
    SOLICITACAO ||--o{ HISTORICO_SOLICITACAO : "trilha de status"
    SOLICITACAO ||--o{ SOLICITACAO_ALTERACAO : "tipo DadosCadastrais"
    UNIDADE_ESCOLAR ||--o{ OCORRENCIA : "escopo"
    ALUNO ||--o{ OCORRENCIA : "aluno"
    CARDAPIO ||--o{ OCORRENCIA : "refeição"
    OCORRENCIA ||--o{ HISTORICO_OCORRENCIA : "trilha"
    UNIDADE_ESCOLAR ||--o{ FEEDBACK : "escopo"
    ALUNO ||--o{ FEEDBACK : "opcional"
    USUARIO ||--o{ NOTIFICACAO : "destinatário"
    USUARIO ||--o{ TRILHA_ACAO : "autor"
    USUARIO ||--|| SOLICITACAO_ACESSO : "conta criada sem papel"
    UNIDADE_ESCOLAR ||--o{ SOLICITACAO_ACESSO : "fila da unidade"

    SOLICITACAO {
        int Id PK
        string Tipo "enum TipoSolicitacao (6 valores)"
        string Status "enum StatusSolicitacao"
        int UnidadeId FK
        int EscolaId FK
        int AlunoId FK
        int CardapioId FK
        int SolicitanteId FK
        string SolicitanteNome "snapshot"
        string Resposta
    }

    OCORRENCIA {
        int Id PK
        string Tipo "enum TipoOcorrencia"
        string Status "enum StatusOcorrencia"
        int UnidadeId FK
        int EscolaId FK
        int AlunoId FK
        int CardapioId FK
        int RefeicaoId FK
        int AlimentoId FK
        int AutorId FK
    }

    FEEDBACK {
        int Id PK
        string Status "enum StatusFeedback"
        int AutorId FK
        int UnidadeId FK
        int EscolaId FK
        int AlunoId FK "Spec 035"
        string AlunoNome "snapshot"
        string Resposta
        string RespondidoPor
    }

    SOLICITACAO_ACESSO {
        int Id PK
        int UsuarioId FK
        int UnidadeId FK
        string Tipo "enum TipoCadastroSolicitado"
        string Status "enum StatusSolicitacaoAcesso"
        int DecididoPorId FK
        int EscolaConcedidaId FK
    }

    NOTIFICACAO {
        int Id PK
        int DestinatarioId FK
        string Tipo "enum TipoNotificacao"
        int ReferenciaId "id do recurso de origem"
        bool Lida
    }
```

**Nuances que o ER não mostra:**

- `Turma`, `Responsavel` (do aluno), `Periodo` e `ValoresNutricionais` são **tipos
  complexos** (`Owned`/complex type), não tabelas — aparecem como colunas prefixadas.
- `Aluno.Restricoes` é **coluna JSON** com nomes de restrição; o catálogo
  (`RestricaoAlimentar`) é normalizado. É a dívida registrada em `arch.md` §9 e o candidato
  C8 de `classes-candidatos.md`.
- Enums são persistidos como **inteiro**; acrescentar valor ao fim é alteração aditiva sem
  migração de dados (o próprio `SituacaoCardapio` documenta isso).
- `TrilhaAcao.AlvoId` é polimórfico (guarda id de recursos diferentes).

---

## 4. Fluxo de criação, adaptação e publicação de cardápio

```mermaid
sequenceDiagram
    autonumber
    actor N as Nutricionista
    participant API as CardapioController
    participant CS as CardapioService
    participant CR as ContextoDeRestricoesService
    participant AD as AdaptarCardapioService
    participant DOM as AdaptacaoDeCardapio (puro)
    participant PB as PublicacaoCardapioService
    participant DB as ConnectionContext
    actor M as Merendeira
    actor R as Responsável

    N->>API: POST /api/cardapios (nome, período, refeições)
    API->>CS: Criar(dto, escopo)
    CS->>AD: resolver grupos (escola, conjunto de restrições)
    AD->>CR: alunos e restrições por escola
    loop cada (escola, conjunto)
        AD->>DOM: Adaptar(geral, conjunto, catálogo)
        DOM-->>AD: substituições, pendências, StatusAdaptacao
        Note over DOM: Atualizado se achou substituto seguro<br/>PendenteDeRevisao se não achou
    end
    AD->>DB: grava geral + adaptados na MESMA transação
    CS-->>API: detalhe com tipo, origem, escola, conjunto, status
    API-->>N: 201 Created

    Note over N,DOM: incompatibilidade bloqueante exige<br/>confirmarIncompatibilidades = true

    N->>API: POST /api/cardapios/{id}/publicar
    API->>PB: Publicar(id, escopo)
    PB->>PB: CRN válido? (Spec 031)
    PB->>PB: incompatibilidade (422), sobreposição de período (409)
    PB->>PB: adaptado PendenteDeRevisao/Desatualizado? (409)
    PB->>DB: Situacao = Publicado, anterior = Substituido
    PB->>DB: HistoricoCardapio + Notificacao
    Note over PB: geral notifica merendeiras da unidade;<br/>adaptado notifica as da escola da versão
    API-->>N: 200 OK

    M->>API: GET /api/painel/merendeira
    API-->>M: cardápio geral do dia + alunosComRestricao por item

    R->>API: GET /api/aluno/{id}/cardapio?data=
    API->>API: CardapioServidoService escolhe por (escola, conjunto)
    API-->>R: cardapioGeral + cardapioAdaptado (ou alerta de indisponível)
```

**Leitura do diagrama**

| Etapa | Regra que pode barrar | Código |
| --- | --- | --- |
| Criar | nutricionista sem CRN → 400 | `ValidacaoDeNutricionista`, `PoliticaDeCrn` |
| Adaptar | limite de 50 versões por origem → 422 | `LimiteDeAdaptacoesException` |
| Adaptar | sem substituto → versão `PendenteDeRevisao` (não publicável) | `AdaptacaoDeCardapio` |
| Publicar | item bloqueante sem confirmação → 422 | `AnaliseCompatibilidade` |
| Publicar | período sobreposto → 409 | `PeriodoVigencia`, `PublicacaoSobrepostaException` |
| Publicar | adaptado pendente/desatualizado → 409 | `PublicacaoCardapioService` |
| Editar geral | adaptados existentes → `Desatualizado` | `Cardapio.MarcarDesatualizado` |

---

## 5. Máquinas de estado

### 5.1 Cardápio (`SituacaoCardapio`)

```mermaid
stateDiagram-v2
    [*] --> Rascunho : POST /api/cardapios
    Rascunho --> Publicado : Publicar() — exige CRN e sem bloqueio
    Rascunho --> Cancelado : Cancelar(motivo)
    Publicado --> Cancelado : Cancelar(motivo)
    Publicado --> Substituido : revisão publicada sobre ele
    Cancelado --> [*]
    Substituido --> [*]
    note right of Rascunho
        Só rascunho publica.
        Editar um cardápio com
        adaptados os marca
        Desatualizado.
    end note
```

Transições conferidas em `Cardapio.Publicar` e `Cardapio.Cancelar`: só rascunho publica;
`Cancelado` e `Substituido` não cancelam de novo.

### 5.2 Versão adaptada (`StatusAdaptacao`)

```mermaid
stateDiagram-v2
    [*] --> Atualizado : adaptação achou substituto seguro dentro da tolerância
    [*] --> PendenteDeRevisao : sem substituto ou fora da tolerância
    Atualizado --> Desatualizado : cardápio geral foi editado
    PendenteDeRevisao --> Atualizado : readaptar
    Desatualizado --> Atualizado : readaptar
    Atualizado --> [*] : publicável
    note right of PendenteDeRevisao
        Não pode ser publicada nem servida
    end note
    note right of Desatualizado
        Bloqueia publicação do geral (409)
    end note
```

### 5.3 Solicitação (`StatusSolicitacao`)

```mermaid
stateDiagram-v2
    [*] --> Aberta : POST /api/solicitacoes
    Aberta --> EmAnalise : IniciarAnalise()
    EmAnalise --> Aprovada : Aprovar(resposta)
    EmAnalise --> Recusada : Recusar(resposta)
    Aprovada --> Concluida : Concluir()
    Aberta --> Cancelada : Cancelar() — só o solicitante
    Recusada --> [*]
    Concluida --> [*]
    Cancelada --> [*]
    note right of Aberta
        Nutricionista decide direto de Aberta
        (a análise é gravada no mesmo ato)
    end note
```

Tabela executável em `Solicitacao.TransicoesPermitidas` (linhas 20–37). Efeito colateral:
aprovar restrição aplica a alteração ao aluno na mesma transação (Spec 017/030).

### 5.4 Ocorrência (`StatusOcorrencia`)

```mermaid
stateDiagram-v2
    [*] --> Aberta : POST /api/ocorrencias
    Aberta --> EmAnalise
    EmAnalise --> Resolvida
    Resolvida --> Arquivada
    EmAnalise --> Aberta : reabrir
    Aberta --> Cancelada : autor, no mesmo dia civil (America/Sao_Paulo)
    Cancelada --> [*]
    Arquivada --> [*]
```

Pares válidos literais em `Ocorrencia.MudarStatus` (linhas 135–138). Mudança de status é
exclusiva do `AdminEscola` da escola; o registro não é apagado — encerrar é `Arquivada`.

### 5.5 Feedback (`StatusFeedback`)

```mermaid
stateDiagram-v2
    [*] --> Aberto : POST /api/feedbacks
    Aberto --> Respondido : POST /{id}/responder
    Respondido --> Encerrado : POST /{id}/encerrar — só o autor
    Encerrado --> [*]
```

### 5.6 Solicitação de acesso (`StatusSolicitacaoAcesso`)

```mermaid
stateDiagram-v2
    [*] --> Pendente : POST /api/usuario/registrar
    Pendente --> Aprovada : AdminSecretaria aprova (papel, escopo, CRN, vínculos)
    Pendente --> Recusada : AdminSecretaria recusa com motivo
    Aprovada --> [*]
    Recusada --> [*]
    note right of Pendente
        Transição monotônica: decide uma única vez.
        Conta sem papel não autentica.
    end note
```

---

## 6. Notas de leitura

1. **Fonte de verdade.** Este arquivo descreve o código; status de spec e pendências vivem em
   `management/roadmap.md` e `management/pending-items.md`. Divergência entre diagrama e
   código deve ser corrigida no diagrama.
2. **O que foi omitido de propósito:** índices secundários, colunas de auditoria
   (`CriadoEmUtc`, `AtualizadoEmUtc`), detalhes de `HistoricoCardapio`/`EventoCardapio` e as
   54 linhas de registro de DI em `Program.cs`.
3. **Cardápio com dias (Spec 037)** não aparece nos diagramas de dados: `DiaDeCardapio` não
   existe no código — `Refeicao` continua presa diretamente ao `Cardapio`. O mesmo vale para
   sugestão por IA (Spec 026). Ver a ressalva de rastreabilidade no `roadmap.md`.
4. **Frontend:** um diagrama de rotas por papel já existe de forma textual em
   `funcionalidades.md` §1–§9; não foi duplicado aqui.
