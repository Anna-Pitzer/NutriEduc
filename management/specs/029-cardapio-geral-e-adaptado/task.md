---
name: cardapio-geral-e-adaptado
status: created
references: V18, V20, ADR-022, L7
---

# Tasks — Cardápio geral e cardápio adaptado

## Pré-condição

- [ ] T0: Specs 012, 014 e 015 implementadas ou verificadas; Spec 027 implementada
  (`Aluno.EscolaId`); Spec 031 implementada (CRN obrigatório).
  - Validação: `Cardapio` cria com `nutricionistaId`; aluno devolve `escolaId`;
    nutricionista sem CRN é recusada no cadastro.

## Domínio

- [ ] T1: criar `ConjuntoDeRestricoes` com chave canônica, igualdade e identidade composta
  `(origem, escola, chave)`.
  - Validação: ordem e duplicata não mudam a chave; escolas diferentes não colidem.
- [ ] T2: adicionar `Tipo`, `OrigemId`, `EscolaId`, `ConjuntoRestricoesChave` e
  `StatusAdaptacao` em `Cardapio`, com as invariantes de RF1–RF3.
  - Validação: adaptado sem origem ou sem escola lança; origem adaptada lança; geral com
    escola lança.
- [ ] T3: implementar `AdaptacaoDeCardapio` como função pura e `SubstituicaoNutricional` com
  desvio normalizado e desempate determinístico; calcular também a adaptabilidade do geral.
  - Validação: testes de substituição, tolerância, ausência de candidato e de alimento sem
    substituto.

## Persistência

- [ ] T4: criar a migration `CardapioAdaptado` com `EscolaId`, `OrigemId`, chave e índice
  único filtrado `(OrigemId, EscolaId, ConjuntoRestricoesChave)`.
  - Validação: duas versões para a mesma tripla violam o índice; versões substituídas não
    bloqueiam.
- [ ] T5: persistir substituições, confirmação de incompatibilidade e motivos de pendência.
  - Validação: detalhe devolve `substituicoes`, `confirmacoes` e `pendentes` por refeição.

## Aplicação e API

- [ ] T6: criar `AdaptarCardapioService` idempotente por `(origem, escola, conjunto)`, com teto
  de 50 versões por origem e auditoria.
  - Validação: segunda chamada não duplica; 51 versões devolve 422 sem gravar.
- [ ] T7: exigir `nutricionistaId` e CRN na criação manual e produzir geral + adaptados na
  mesma transação, com snapshot de autoria em todas as versões.
  - Validação: criação sem CRN devolve 400; duas escolas com dois conjuntos geram os
  adaptados corretos.
- [ ] T8: criar `ContextoDeRestricoesService` e a rota de contexto (alunos e restrições por
  escola); aplicar bloqueio de alimento sem substituto e exigir confirmação registrada.
  - Validação: 400 sem confirmação; 400 para alimento sem substituto; confirmação grava
  autor, data, alimento e grupos.
- [ ] T9: criar `CardapioServidoService` e a rota `GET /api/aluno/{id}/cardapio` com escola +
  conjunto.
  - Validação: aluno sem restrição recebe o geral; com restrição sem versão publicada recebe
  `Indisponivel`; aluno de outra escola não usa a versão da primeira; 403 fora do escopo.
- [ ] T10: marcar adaptados como `Desatualizado` na edição do geral e bloquear publicação com
  pendente ou desatualizado.
  - Validação: 409 com a lista de versões que bloqueiam.
- [ ] T11: incluir tipo, origem, escola, conjunto, status, substituições e confirmações no
  detalhe e na listagem.
  - Validação: teste de contrato do detalhe e da listagem.

## Frontend

- [ ] T12: na montagem manual, select de nutricionista com nome e CRN e painel de alunos e
  restrições por escola; marcar alimento incompatível, bloquear o sem substituto e pedir
  confirmação explícita.
  - Validação: `npm run build` e `npm run lint` sem erro novo; fluxo manual de criar, receber
  aviso, confirmar e publicar.
- [ ] T13: detalhe com o par geral↔adaptado por escola, diffs de substituição e alerta de
  aluno indisponível na tela da merendeira.
  - Validação: navegar entre versões e conferir cada grupo.

## Fechamento

- [ ] T14: atualizar `README.md` (rotas novas) e a seção da spec em `arch.md`.
  - Validação: `DocumentationTests.Readme_ListaTodosOsEndpoints` verde.
- [ ] T15: executar os gates completos.
  - Validação: `dotnet build`, `dotnet test`, `npm run build`, `npm run lint`.

## Definição de pronto

- Todo cardápio geral criado tem versões adaptadas para cada `(escola, conjunto)` existente.
- Nenhuma versão publicada contém item bloqueante para o próprio conjunto.
- A montagem manual mostra alunos/restrições por escola e impede alimento sem substituto.
- A relação origem↔adaptado é navegável na API e na tela.
- Versão pendente impede publicação com mensagem acionável.
