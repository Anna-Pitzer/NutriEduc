---
name: ocorrencias
status: created
references: V6, ADR-004, L7
---

# Design — Ocorrências

## Estrutura

```text
backend/Domain/Entities/
  Ocorrencia.cs
  StatusOcorrencia.cs        (enum)
  TipoOcorrencia.cs          (enum)
  HistoricoOcorrencia.cs
backend/Domain/Interface/
  IOcorrenciaRepository.cs
backend/API/Services/
  OcorrenciaService.cs
backend/API/Controllers/
  OcorrenciaController.cs
```

## Domínio

```csharp
public enum TipoOcorrencia { Aluno, Refeicao, Alimento, Outro }

public enum StatusOcorrencia { Aberta, EmAnalise, Resolvida, Arquivada, Cancelada }

public class Ocorrencia
{
    private readonly List<HistoricoOcorrencia> _historico = new();

    public int Id { get; private set; }
    public TipoOcorrencia Tipo { get; private set; }
    public int UnidadeId { get; private set; }
    public int? AlunoId { get; private set; }
    public int? CardapioId { get; private set; }
    public int? RefeicaoId { get; private set; }
    public int? AlimentoId { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public StatusOcorrencia Status { get; private set; }
    public int AutorId { get; private set; }
    public string AutorNome { get; private set; } = string.Empty;
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime AtualizadoEmUtc { get; private set; }
    public IReadOnlyList<HistoricoOcorrencia> Historico => _historico.AsReadOnly();

    public Ocorrencia(TipoOcorrencia tipo, int unidadeId, int autorId, string autorNome,
                      string descricao, int? alunoId, int? cardapioId, int? refeicaoId,
                      int? alimentoId, DateTime utc)
    {
        descricao = (descricao ?? string.Empty).Trim();
        if (descricao.Length is < 5 or > 1000)
            throw new ArgumentException("A descrição deve ter entre 5 e 1000 caracteres.");
        ValidarVinculos(tipo, alunoId, cardapioId, refeicaoId, alimentoId);

        Tipo = tipo; UnidadeId = unidadeId; AutorId = autorId;
        AutorNome = autorNome.Trim(); Descricao = descricao;
        Status = StatusOcorrencia.Aberta;
        CriadoEmUtc = AtualizadoEmUtc = utc;
        _historico.Add(new HistoricoOcorrencia(StatusOcorrencia.Aberta, autorNome, "Criação", utc));
    }

    public void MudarStatus(StatusOcorrencia destino, string observacao, string autorNome, DateTime utc)
    {
        observacao = (observacao ?? string.Empty).Trim();
        if (observacao.Length is < 2 or > 500)
            throw new ArgumentException("A observação deve ter entre 2 e 500 caracteres.");

        bool permitido = (Status, destino) switch
        {
            (StatusOcorrencia.Aberta, StatusOcorrencia.EmAnalise) => true,
            (StatusOcorrencia.EmAnalise, StatusOcorrencia.Resolvida) => true,
            (StatusOcorrencia.Resolvida, StatusOcorrencia.Arquivada) => true,
            (StatusOcorrencia.EmAnalise, StatusOcorrencia.Aberta) => true,
            _ => false
        };
        if (!permitido) throw new TransicaoInvalidaException("Transição de status inválida.");

        Status = destino;
        AtualizadoEmUtc = utc;
        _historico.Add(new HistoricoOcorrencia(destino, autorNome, observacao, utc));
    }

    public void Cancelar(string autorNome, DateTime utc)
    {
        if (Status != StatusOcorrencia.Aberta)
            throw new TransicaoInvalidaException("Somente ocorrência aberta pode ser cancelada.");
        if (utc.Date != CriadoEmUtc.Date)
            throw new TransicaoInvalidaException("Cancelamento só é permitido no dia da criação.");
        Status = StatusOcorrencia.Cancelada;
        AtualizadoEmUtc = utc;
        _historico.Add(new HistoricoOcorrencia(StatusOcorrencia.Cancelada, autorNome, "Cancelada pelo autor", utc));
    }
}
```

`ValidarVinculos` é estático e privado: verifica que o vínculo preenchido corresponde
ao `Tipo` e que pelo menos um existe. Não consulta banco.

`HistoricoOcorrencia` guarda `Status`, `AutorNome`, `Observacao` e `CriadoEmUtc` —
snapshots, sem FK para usuário.

## Escopo

`EscopoService` (Spec 009) é reutilizado. `OcorrenciaService.Listar` traduz o escopo
em filtro de consulta:

```text
Nutricionista     → sem filtro (pode filtrar por escola/unidade)
AdminSecretaria   → sem filtro
AdminEscola       → UnidadeId in (unidades da escola)
Merendeira        → UnidadeId == unidade da sessão
Responsavel       → AlunoId in (alunos vinculados ao usuário)
```

O filtro de responsável vem da tabela `Vinculos` (Spec 009), nunca de lista em
memória.

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/ocorrencias?status=&tipo=&alunoId=&unidadeId=&de=&ate=&pagina=&tamanho=` | sessão, escopo por papel |
| `GET` | `/api/ocorrencias/{id}` | sessão, escopo por papel |
| `POST` | `/api/ocorrencias` | `Merendeira`, `Responsavel`, `Nutricionista`, `AdminEscola` |
| `POST` | `/api/ocorrencias/{id}/status` | `Nutricionista`, `AdminEscola` |
| `POST` | `/api/ocorrencias/{id}/cancelar` | autor da ocorrência |

```json
{ "id": 5, "tipo": "Refeicao", "status": "EmAnalise",
  "unidadeId": 3, "alunoId": 9, "cardapioId": 12, "refeicaoId": 31,
  "alimentoId": null, "descricao": "Refeição servida com atraso.",
  "autor": "Carla", "criadoEm": "2026-10-30T11:00:00Z",
  "historico": [ { "status": "EmAnalise", "autor": "Maria",
                   "observacao": "Encaminhado à nutrição.", "criadoEm": "2026-10-30T12:00:00Z" } ] }
```

## Persistência

- `Ocorrencias`: FKs anuláveis para `Alunos`, `Cardapios`, `Refeicoes`, `Alimentos`
  com `OnDelete(SetNull)`; FKs `UnidadeId` (`Restrict`); índices
  `(UnidadeId, CriadoEmUtc)` e `(AlunoId, CriadoEmUtc)`.
- `HistoricoOcorrencias`: FK `OcorrenciaId` (`Cascade`), índice
  `(OcorrenciaId, CriadoEmUtc)`.
- Migration `Ocorrencias`.

## Frontend

- `pages/merendeiras/Ocorrencias.tsx`: formulário contextual (cardápio/refeição/
  alimento) e listagem da unidade.
- `pages/pais/Ocorrencias.tsx`: restrito aos alunos vinculados; botão "Relatar" a
  partir do aluno.
- `pages/escolas/Ocorrencias.tsx`: painel de acompanhamento com filtro de status.
- `pages/nutricao/alunos/` recebe link "Registrar ocorrência" no detalhe do aluno.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | descrição 4/1001 e vínculo incoerente lançam |
| Domínio | todas as transições válidas e inválidas |
| Domínio | cancelar em outro dia lança |
| Serviço | pai registrando para aluno não vinculado → 403 |
| Serviço | escopo de listagem por papel em base com duas unidades |
| HTTP | 400/403/404/409 e contrato `{ erro }` |
| HTTP | histórico preservado após aluno excluído |

## Riscos

- **Vazamento entre pais**: o filtro por vínculo é a única barreira; teste HTTP com
  dois responsáveis é obrigatório.
- **Histórico muda com o cadastro**: evitado por snapshots; nenhuma junção para
  exibir autor.
- **Cardápio/refeição excluídos**: `SetNull` preserva a ocorrência e o histórico;
  o detalhe mostra o vínculo como ausente.
