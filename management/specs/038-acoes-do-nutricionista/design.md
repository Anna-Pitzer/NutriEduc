---
name: acoes-do-nutricionista
status: audited
references: V6; V17; Spec 016; Spec 017; Spec 028; ADR-021
---

# Design — Ações do nutricionista em solicitações e ocorrências

## Princípio

Quem decide não precisa criar nem administrar o fluxo. O nutricionista é o dono da decisão
de cardápio e ingrediente, mas a única ação dele é a decisão final. Em ocorrências, ele é
leitor. A regra vive no serviço; a interface apenas reflete o que o serviço autoriza.

## Decisão de implementação

### Decisão direta sem nova transição

`Solicitacao` mantém a tabela `Aberta → EmAnalise → Aprovada/Recusada → Concluida`. Em vez
de abrir `Aberta → Aprovada/Recusada` no agregado — o que ampliaria a regra para todos os
papéis —, o serviço registra `IniciarAnalise` e a decisão na mesma transação quando o autor
é `Nutricionista` e o status é `Aberta`. Vantagens:

- a máquina de estados e o histórico append-only continuam sendo a única fonte de verdade;
- a análise fica registrada com autor e data, sem um clique extra;
- `AdminSecretaria` (restrições e dados cadastrais) mantém exatamente o fluxo anterior.

### Bloqueios no serviço

- `SolicitacaoService.PodeCriar`: remove `Nutricionista` do conjunto de cardápio/ingrediente.
- `SolicitacaoService.Analisar` e `Concluir`: `Nutricionista` → `403` antes de carregar.
- `SolicitacaoService.MarcarAnafilaxia`: remove `Nutricionista`; apenas `AdminSecretaria`
  (unidade) e `Responsavel` (própria família) permanecem.
- `OcorrenciaService.PodeCriar`: remove `Nutricionista`.
- `OcorrenciaService.PodeMudarStatus`: apenas `AdminEscola`.
- `OcorrenciaService.Cancelar`: `Nutricionista` → `403` (somente leitura).
- `MatrizDePermissoes`: `(Ocorrencia, Nutricionista)` de `RW` para `R` em `Documentada` e
  em `Nivel`.

### Frontend

- `SolicitacoesPage`: `tiposDeCriacao` não inclui `Nutricionista`; `podeMarcarAnafilaxia`
  apenas `AdminSecretaria`; `Iniciar análise` e `Concluir` ocultos para nutricionista;
  `Aprovar`/`Recusar` visíveis em `Aberta` e `EmAnalise`.
- `OcorrenciasPage`: `tiposDeCriacao` não inclui `Nutricionista`; `podeMudarStatus` apenas
  `AdminEscola`; cancelamento oculto para nutricionista.

## Arquivos

- `backend/API/Services/SolicitacaoService.cs`
- `backend/API/Services/OcorrenciaService.cs`
- `backend/Application/Seguranca/MatrizDePermissoes.cs`
- `frontend/src/pages/solicitacoes/SolicitacoesPage.tsx`
- `frontend/src/pages/ocorrencias/OcorrenciasPage.tsx`
- `backend.Tests/Services/SolicitacaoServiceTests.cs`
- `backend.Tests/Integration/OcorrenciaApiTests.cs`
- `backend.Tests/Integration/SolicitacaoAnafilaxiaApiTests.cs`
- `backend.Tests/Integration/NotificacaoEventosApiTests.cs`

## Consequências

- Uma aprovação do nutricionista gera mais de um evento de histórico; é o registro honesto
  da análise e da decisão, sem etapa manual.
- Solicitações e ocorrências antigas criadas por nutricionista continuam legíveis; as ações
  de escrita deixam de existir para o papel.
- Nenhum outro papel perde ou ganha ação.
