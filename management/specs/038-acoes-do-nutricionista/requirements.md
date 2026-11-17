---
name: acoes-do-nutricionista
status: audited
references: V6; V17; Spec 016; Spec 017; Spec 028; ADR-021
---

# Requirements — Ações do nutricionista em solicitações e ocorrências

## Objetivo

O nutricionista da unidade é o decisor das solicitações de cardápio e ingrediente, mas a
implementação anterior permitia que ele criasse solicitações, iniciasse análise, concluísse
e marcasse anafilaxia; em ocorrências, permitia criar, mudar status e cancelar. O produto
exige um recorte mais estreito: em solicitações ele apenas **aprova ou recusa**; em
ocorrências ele apenas **lê**. Esta spec revisa as Specs 016 e 017 e a célula correspondente
da matriz da Spec 028.

## Escopo

- Backend: `SolicitacaoService`, `OcorrenciaService` e `MatrizDePermissoes`.
- Frontend: `SolicitacoesPage` e `OcorrenciasPage`.
- Testes de unidade e de integração das duas áreas.
- Fora de escopo: mudar o fluxo de outros papéis, criar transições novas no agregado,
  alterar o escopo de leitura, notificações, painéis ou relatórios.

## Revisões declaradas

| Spec | Regra anterior | Regra nova |
|---|---|---|
| 017 | RF3 permitia `Nutricionista` criar `MudancaCardapio`, `Ingrediente` e `FaltaIngrediente`; RF5 permitia análise e conclusão pelo decisor | Nutricionista não cria e não executa `EmAnalise`/`Concluida` como ação separada; apenas aprova ou recusa |
| 016 | Criação e mudança de status permitiam `Nutricionista` | Nutricionista não cria, não muda status e não cancela; leitura permanece |
| 028 | Célula `Ocorrência × Nutricionista = RW` | Célula passa a `R`; `Solicitação × Nutricionista` permanece `RW` (lê e decide, não cria) |

## Requisitos funcionais

- RF1: criação de solicitação do tipo `MudancaCardapio`, `Ingrediente` ou
  `FaltaIngrediente` por `Nutricionista` devolve **403** e nada é persistido. `Merendeira` e
  `AdminEscola` continuam criando na própria escola. (Revisa 017 RF3.)
- RF2: `Nutricionista` só executa `Aprovar` e `Recusar`. `Analisar`, `Concluir` e
  `MarcarAnafilaxia` devolvem **403** para esse papel. (Revisa 017 RF5.)
- RF3: `Aprovar` e `Recusar` funcionam com a solicitação em `Aberta` ou `EmAnalise`. Quando
  ela está `Aberta`, o serviço registra `EmAnalise` no histórico e aplica a decisão na mesma
  transação, nessa ordem. A tabela de transições do agregado não muda.
- RF4: `Aprovar` mantém o efeito atual: aplica a restrição quando o tipo é de restrição e
  conclui a solicitação (`Concluida`) para os demais tipos. A recusa encerra em `Recusada`.
- RF5: criação de ocorrência por `Nutricionista` devolve **403** e nada é persistido.
  `Merendeira`, `AdminEscola` e `Responsavel` continuam criando conforme a Spec 016.
- RF6: `MudarStatus` e `Cancelar` de ocorrência por `Nutricionista` devolvem **403**.
  Leitura (`GET` de lista e detalhe) e escopo por unidade permanecem inalterados.
  (Revisa 016.)
- RF7: `AdminEscola` continua sendo o único papel que muda status de ocorrência; o autor
  continua sendo o único que cancela, no dia da criação. Nenhuma regra desses papéis muda.
- RF8: a matriz declarativa passa a declarar `(Ocorrencia, Nutricionista) = Ler` nas duas
  representações (`Documentada` e `Nivel`), e o teste de concordância continua verde.
- RF9: a tela de solicitações não renderiza o formulário de criação para `Nutricionista` e
  exibe apenas `Aprovar`/`Recusar` quando ele é o decisor. A tela de ocorrências não
  renderiza criação, mudança de status nem cancelamento para ele.
- RF10: papéis sem decisão continuam recebendo **403** nas ações que não lhes pertencem; os
  critérios de escopo por unidade/escola/vínculo não mudam.

## Restrições

- RNF1: a regra é aplicada no serviço (fonte da verdade); esconder botão na UI não é
  suficiente.
- RNF2: nenhuma transição nova é criada no agregado `Solicitacao`; a máquina de estados e o
  histórico append-only continuam válidos.
- RNF3: nenhuma alteração de schema, migration ou contrato de rota.
- RNF4: respostas mantêm o contrato de erro existente (`403` para papel sem permissão).

## Dependências

- Spec 016 (ocorrências) e Spec 017 (solicitações), ambas `implemented` e imutáveis: esta
  spec é a revisão autorizada.
- Spec 028 (matriz de permissões), `audited`: a célula de ocorrência é revisada aqui.
- ADR-021 (matriz declarativa) e ADR-004 (domínio dono das invariantes).

## Comportamento esperado

| Papel | Solicitação | Ocorrência |
|---|---|---|
| Nutricionista | lê, aprova, recusa | apenas lê |
| Merendeira | cria na escola, lê | cria na escola, lê, cancela a própria no dia |
| AdminEscola | cria na escola, lê | cria na escola, lê, muda status, cancela a própria |
| AdminSecretaria | lê, analisa, aprova, recusa, conclui, marca anafilaxia | lê |
| Responsavel | cria e cancela as próprias, lê as dos filhos | cria e cancela as próprias, lê as dos filhos |

## Casos de borda

- Solicitação `Aberta` aprovada diretamente pelo nutricionista: o histórico contém `Aberta`,
  `EmAnalise`, `Aprovada` e `Concluida`.
- Recusa direta: o histórico contém `Aberta`, `EmAnalise` e `Recusada`.
- Solicitação antiga criada por nutricionista antes desta revisão: continua legível e pode
  ser aprovada ou recusada; ele não pode mais criar novas.
- Ocorrência antiga criada por nutricionista: continua legível; ele não pode mudar status
  nem cancelar.
- Tentativa de criação ou alteração via chamada direta à API: `403`, sem persistência.

## Critérios de aceitação

- [ ] Testes de serviço cobrem criação proibida, análise proibida, conclusão proibida,
  aprovação direta de `Aberta` com histórico completo e recusa direta.
- [ ] Testes de integração cobrem `403` para criação e status de ocorrência e `403` para
  criação de solicitação.
- [ ] Teste de anafilaxia por nutricionista devolve `403` e o aluno permanece sem a marca.
- [ ] Matriz `Documentada` e `Nivel` concordam e o teste de conformidade passa.
- [ ] Gates: `dotnet build -c Release`, `dotnet test -c Release`, `npm run build` e
  `npm run lint` sem erro.

## Validação

- `dotnet build backend.Tests/backend.Tests.csproj -c Release --no-restore`
- `dotnet test backend.Tests/backend.Tests.csproj -c Release --no-build`
- `cd frontend && npm run build && npm run lint`
