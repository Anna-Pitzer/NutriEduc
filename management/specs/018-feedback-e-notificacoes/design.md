---
name: feedback-e-notificacoes
status: created
references: V6, ADR-004, L7
---

# Design — Feedback e notificações

## Estrutura

```text
backend/Domain/Entities/
  Feedback.cs
  StatusFeedback.cs          (enum)
  Notificacao.cs
  TipoNotificacao.cs         (enum)
backend/Domain/Interface/
  IFeedbackRepository.cs
  INotificacaoRepository.cs
backend/API/Services/
  FeedbackService.cs
  NotificacaoService.cs      (único emissor)
backend/API/Controllers/
  FeedbackController.cs
  NotificacaoController.cs
```

## Domínio — feedback

```csharp
public enum StatusFeedback { Aberto, Respondido, Encerrado }

public class Feedback
{
    public int Id { get; private set; }
    public string Assunto { get; private set; } = string.Empty;
    public string Mensagem { get; private set; } = string.Empty;
    public int AutorId { get; private set; }
    public string AutorNome { get; private set; } = string.Empty;
    public PapelUsuario PapelAutor { get; private set; }
    public int UnidadeId { get; private set; }
    public StatusFeedback Status { get; private set; }
    public string? Resposta { get; private set; }
    public string? RespondidoPor { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime AtualizadoEmUtc { get; private set; }

    public Feedback(string assunto, string mensagem, int autorId, string autorNome,
                    PapelUsuario papel, int unidadeId, DateTime utc) { /* valida 5..120 / 5..1000 */ }

    public void Responder(string resposta, string autorNome, DateTime utc)
    {
        if (Status != StatusFeedback.Aberto)
            throw new TransicaoInvalidaException("Este feedback já foi respondido.");
        resposta = (resposta ?? string.Empty).Trim();
        if (resposta.Length is < 5 or > 1000)
            throw new ArgumentException("A resposta deve ter entre 5 e 1000 caracteres.");
        Status = StatusFeedback.Respondido;
        Resposta = resposta; RespondidoPor = autorNome.Trim();
        AtualizadoEmUtc = utc;
    }

    public void Encerrar(string autorNome, DateTime utc)
    {
        if (Status != StatusFeedback.Respondido)
            throw new TransicaoInvalidaException("Só feedback respondido pode ser encerrado.");
        Status = StatusFeedback.Encerrado;
        AtualizadoEmUtc = utc;
    }
}
```

## Domínio — notificação

```csharp
public enum TipoNotificacao
{
    CardapioPublicado, CardapioAlterado, SolicitacaoStatus,
    OcorrenciaStatus, AlunoAtualizado, FeedbackRespondido
}

public class Notificacao
{
    public int Id { get; private set; }
    public int DestinatarioId { get; private set; }
    public TipoNotificacao Tipo { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Mensagem { get; private set; } = string.Empty;
    public string? ReferenciaTipo { get; private set; }
    public int? ReferenciaId { get; private set; }
    public bool Lida { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }

    public void MarcarLida() => Lida = true;   // idempotente
}
```

## Emissão centralizada

Único ponto de criação:

```csharp
public interface INotificacaoService
{
    Task NotificarAsync(TipoNotificacao tipo, IEnumerable<int> destinatarios,
                        string titulo, string mensagem,
                        string? referenciaTipo = null, int? referenciaId = null);
}
```

Regras de destinatário (RF9) ficam em `NotificacaoService`, não nos chamadores:

| Origem | Destinatários |
|---|---|
| `CardapioPublicado`/`CardapioAlterado` | merendeiras ativas da unidade do cardápio |
| `SolicitacaoStatus` | solicitante da solicitação |
| `OcorrenciaStatus` | autor da ocorrência + responsáveis vinculados aos alunos |
| `AlunoAtualizado` | responsáveis vinculados ao aluno |
| `FeedbackRespondido` | autor do feedback |

Chamadas nas specs de origem:

```text
015 PublicacaoCardapioService  → after Publicar/MarcarSubstituido
016 OcorrenciaService          → after MudarStatus
017 SolicitacaoService         → after Aprovar/Recusar/Concluir
010 AlunoService               → after atualizar aluno com responsáveis
018 FeedbackService            → after Responder
```

Todas usam o mesmo `ConnectionContext` Scoped e a transação já aberta pelo serviço de
origem. Se a emissão falhar, a operação inteira falha (RNF2).

## Escopo

| Papel | Feedback visível |
|---|---|
| autor | próprios |
| `AdminEscola` | unidade(s) da escola |
| `Nutricionista` | todos |
| `AdminSecretaria` | todos (leitura) |

Notificações são sempre por `DestinatarioId == usuarioId` da sessão. Não existe
endpoint administrativo para ler notificação alheia.

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/feedbacks?status=&pagina=&tamanho=` | sessão com escopo |
| `POST` | `/api/feedbacks` | `Merendeira`, `Responsavel`, `AdminEscola` |
| `POST` | `/api/feedbacks/{id}/responder` | `Nutricionista`, `AdminEscola` (escola) |
| `POST` | `/api/feedbacks/{id}/encerrar` | autor |
| `GET` | `/api/notificacoes?naoLidas=&pagina=&tamanho=` | sessão |
| `POST` | `/api/notificacoes/{id}/ler` | dono |
| `POST` | `/api/notificacoes/ler-todas` | sessão |

```json
{ "id": 41, "tipo": "SolicitacaoStatus", "titulo": "Solicitação aprovada",
  "mensagem": "Sua solicitação de inclusão de alergia foi aprovada.",
  "referenciaTipo": "solicitacao", "referenciaId": 14,
  "lida": false, "criadoEm": "2026-10-30T13:00:00Z" }
```

## Persistência

- `Feedbacks`: índices `(UnidadeId, CriadoEmUtc)` e `(AutorId, CriadoEmUtc)`.
- `Notificacoes`: índice `(DestinatarioId, Lida, CriadoEmUtc)`; FK
  `DestinatarioId` (`Cascade` quando usuário é removido — na prática desativado).
- Migration `FeedbackNotificacoes`.

## Frontend

- `components/SinoNotificacoes.tsx`: contador de não lidas, lista, link para o
  recurso, marcação de leitura ao abrir.
- `pages/merendeiras/Feedback.tsx`, `pages/pais/Feedback.tsx`,
  `pages/escolas/Feedback.tsx`: criar e acompanhar.
- `pages/nutricao/feedback/FilaFeedback.tsx`: responder e encerrar.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | responder duas vezes e encerrar `Aberto` lançam |
| Domínio | assunto/mensagem/resposta fora dos limites lançam |
| Serviço | publicação notifica todas as merendeiras da unidade |
| Serviço | solicitação notifica apenas o solicitante |
| Serviço | ocorrência notifica autor e todos os responsáveis |
| Serviço | usuário desativado não recebe notificação |
| HTTP | notificação de outro usuário → 404 |
| HTTP | `ler-todas` é idempotente e restrito ao dono |

## Riscos

- **Explosão de notificações**: publicar uma vez notifica N merendeiras; é o
  comportamento desejado, e o índice por destinatário mantém a leitura barata.
- **Transação longa**: emissão síncrona dentro da transação de origem aumenta o tempo
  da operação; o volume por transição é limitado ao número de usuários da unidade.
- **Privacidade**: título e mensagem não carregam dado sensível; o link leva ao
  recurso, onde o escopo é aplicado novamente.
