---
name: substituicao-assistida-ia
status: planned
references: V15, ADR-011, ADR-004
---

# Design — Substituição assistida por IA

## Princípio

O determinístico decide **o que pode**; a IA decide **a ordem e o argumento**. Qualquer
resultado do provedor é proposta descartável: se falhar, o fluxo continua.

```text
item bloqueado
  → CandidatosValidos (domínio, determinístico)      ← Spec 014, ADR-011
  → ISugestorDeSubstituicao.Sugerir(candidatos)      ← IA opcional
  → Revalidar escolha (Spec 014)
  → nutricionista aprova
  → ItemRefeicao passa a apontar o novo alimento     ← Spec 012
  → evento append-only de sugestão                   ← padrão Spec 023
```

## Estrutura

```text
backend/Domain/Entities/
  SugestaoDeSubstituicao.cs        (record: origem, candidato, justificativa, confiança)
  RegistroDeSugestao.cs            (trilha append-only)
backend/Domain/Interface/
  ISugestorDeSubstituicao.cs       (porta)
backend/Domain/Services/
  SelecaoDeCandidatos.cs           (função pura: equivalentes já válidos)
backend/API/Services/
  SubstituicaoService.cs           (orquestra candidatos, sugestão, revalidação, auditoria)
backend/API/Controllers/
  CardapioController.cs            (+ rota de sugestão e de aplicação)
backend/Infrastructure/IA/
  SugestorPorIa.cs                 (provedor externo)
  SugestorDeterministico.cs        (fallback)
```

## Porta e contrato interno

```csharp
public enum OrigemDaSugestao { Determinística, Ia }

public sealed record SugestaoDeSubstituicao(
    int AlimentoSugeridoId,
    string Justificativa,
    double Confianca,
    OrigemDaSugestao Origem);

public interface ISugestorDeSubstituicao
{
    Task<IReadOnlyList<SugestaoDeSubstituicao>> SugerirAsync(
        ContextoDeSubstituicao contexto,
        CancellationToken cancelamento);
}
```

`ContextoDeSubstituicao` carrega apenas: alimento de origem (nome, categoria, valores),
lista de candidatos **já validados** e as restrições agregadas aplicáveis ao cardápio
(ex.: `["lactose", "gluten"]`). Nenhum `AlunoId`, nome ou ocorrência entra no contexto.

## Seleção de candidatos (determinística)

Regra pura, testável sem infraestrutura:
1. mesmo `CategoriaId` do alimento de origem;
2. `Ativo` e diferente do próprio;
3. sem restrição vinculada de categoria `Alergia` aplicável;
4. diferença de calorias e de proteínas dentro da faixa configurada (padrão ±25%);
5. ordenação por proximidade de valores, desempate por nome.

Sem candidato, a resposta é lista vazia com motivo — nunca uma sugestão inválida.

## Provedor de IA

- Configuração: `IA:Provedor`, `IA:Modelo`, `IA:ApiKey` (user-secrets/variável de ambiente),
  `IA:TimeoutSegundos` (padrão 8), `IA:MaximoSugestoesPorDia`.
- Prompt com JSON fechado: alimento de origem + candidatos (id, nome, valores) + restrições
  agregadas. Instrução explícita: **só** ranquear os candidatos recebidos.
- Resposta parseada de forma estrita; candidato fora da lista é descartado e registrado.
- Timeout, erro HTTP, resposta malformada ou teto atingido → `SugestorDeterministico`.
- O log guarda contagem, duração, origem e resultado — nunca o prompt inteiro.

## Aplicação da substituição

- `POST /api/cardapios/{id}/itens/{itemId}/substituicoes/sugerir` — devolve sugestões (não
  grava).
- `POST /api/cardapios/{id}/itens/{itemId}/substituicoes/aplicar` — recebe `alimentoId`
  aprovado, revalida (Spec 014 + item do cardápio pertence ao cardápio + cardápio em
  rascunho) e grava, com `ConcurrencyCheck` do item (ADR-010).
- Ambas as rotas: `Nutricionista` apenas; merendeira e responsável → 403.

## Persistência

Tabela `SugestoesDeSubstituicao` (append-only, padrão ADR-009):

| Coluna | Conteúdo |
|---|---|
| `AlunoId`-free | nunca referencia aluno |
| `CardapioId`, `ItemId` | origem |
| `AlimentoOrigemId`, `AlimentoSugeridoId` | o que saiu e o que entrou |
| `Origem` | `Deterministica` ou `Ia` |
| `Justificativa` | texto curto |
| `Decisao` | `Aceita` ou `Recusada` |
| `AutorNome`, `CriadoEmUtc` | snapshot, UTC |

Índice `(CardapioId, CriadoEmUtc)`. Nenhum evento é atualizado ou removido.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | seleção exclui candidato vinculado a restrição de categoria `Alergia` |
| Domínio | faixa de valores respeitada; sem candidato → lista vazia |
| Serviço | provedor devolve id inexistente → descartado |
| Serviço | timeout do provedor → fallback determinístico, sem erro para o usuário |
| Serviço | aplicar sem aprovação não grava; aplicar com item de outro cardápio → 403/404 |
| HTTP | merendeira e responsável recebem 403 nas duas rotas |
| HTTP | sugestão aceita e recusada aparecem na trilha com autor e data |

## Riscos

- **Prompt com dado pessoal**: mitigado por contrato fechado (`ContextoDeSubstituicao` não
  tem campo de aluno) e teste que inspeciona o payload enviado pelo provedor falso.
- **Latência**: timeout curto + fallback; a tela nunca espera mais que o configurado.
- **Custo**: teto diário e registro de consumo; atingido o teto, o sistema continua útil.
- **Confiança do usuário na IA**: a justificativa e a origem da sugestão ficam visíveis; a
  aprovação é sempre humana e o evento registra qual origem foi aceita.
