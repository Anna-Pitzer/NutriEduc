---
name: solicitacao-alteracao-dados
status: created
references: V19, ADR-004, ADR-016, ADR-019
---

# Design — Solicitação de alteração de dados do aluno

## Princípio

A solicitação é uma **proposta imutável**; quem escreve no aluno continua sendo
`Aluno.AtualizarDados`. Aprovar não é "aplicar o que o pai mandou": é compor o estado final,
validar no domínio e provar que o valor atual não mudou desde o pedido. Divergiu, não aplica.

## Estrutura

```
backend/
  Domain/Entities/
    Solicitacao.cs              (existente — + alterações do tipo DadosCadastrais)
    TipoSolicitacao.cs          (+ DadosCadastrais)
    AlteracaoDeDado.cs          (novo — campo, valorSolicitado, valorNoPedido)
    CampoDeDadoDoAluno.cs       (novo — enum fechado dos cinco campos)
  Application/Solicitacoes/
    SolicitarAlteracaoDeDadosService.cs  (novo — cria com snapshot)
    DecidirSolicitacaoDeDadosService.cs  (novo — aprova/recusa atômico)
  API/Controllers/SolicitacaoController.cs (+ tipo e payload)
frontend/src/pages/pais/
  SolicitacoesDoFilho.tsx       (novo — criar e acompanhar)
frontend/src/pages/secretaria/
  FilaSolicitacoes.tsx          (+ diff e aprovar/recusar com motivo)
```

## Domínio

`CampoDeDadoDoAluno`: `Nome`, `DataNascimento`, `NomeResponsavel`, `TelefoneResponsavel`,
`Observacao`. O enum é a lista fechada de RF3; qualquer outro valor é rejeitado na desserialização.

`AlteracaoDeDado` (value object): `Campo`, `ValorSolicitado`, `ValorNoPedido` (snapshot). É
imutável; não existe setter nem método de edição.

`Solicitacao` (extensão da Spec 017):

- construtor de `DadosCadastrais` exige `alteracoes` não vazias, `justificativa` 10–500 e pelo
  menos um campo distinto; campo repetido é erro;
- `Aprovar(autor, alunoAtual)` não conhece repositório: recebe o aluno e devolve o estado final
  ou lança `ConflitoDeSolicitacaoException` com os campos divergentes;
- `Recusar(motivo)` exige motivo;
- transições continuam as da Spec 017; nenhuma transição nova.

A validação de valor reutiliza as regras de `Aluno` (`ValidacaoDeCampo` / construtor). Não há
segunda cópia da regra de nome, data ou telefone (L9).

## Aplicação

`SolicitarAlteracaoDeDadosService.Criar`:

1. resolve o aluno pelo escopo do responsável (Spec 028); fora → 403;
2. verifica aluno ativo e ausência de pedido aberto para o mesmo campo (RF13);
3. lê o valor atual de cada campo e grava o snapshot;
4. persiste a solicitação `Aberta` na unidade de trabalho (ADR-019).

`DecidirSolicitacaoDeDadosService.Aprovar`:

1. resolve a solicitação no escopo do ADM (unidade atual do aluno);
2. compara valor atual × snapshot; divergência → 409, sem escrita;
3. compõe o estado final do aluno e chama `Aluno.AtualizarDados` (ADR-004);
4. grava o evento na trilha do aluno com `Origem = SolicitacaoId`, autor e valores;
5. muda o status para `Aprovada` e depois `Concluida` (Spec 017);
6. emite `SolicitacaoStatus` (Spec 018) na mesma transação.

Qualquer exceção desfaz tudo: não existe aluno alterado com solicitação ainda `EmAnalise`.

## Persistência

- `Solicitacoes.Tipo` já é inteiro; o novo valor é `DadosCadastrais` (sem migration de enum).
- Nova tabela `SolicitacaoAlteracoes`: `Id`, `SolicitacaoId` (FK `Cascade`), `Campo` (int),
  `ValorSolicitado` (texto 500), `ValorNoPedido` (texto 500). Sem update: append-only por
  natureza (a solicitação é imutável).
- Índice `(SolicitacaoId)` e índice parcial para RF13 em `Solicitacoes(AlunoId, Status)` não é
  suficiente porque o campo está na tabela filha; a verificação é feita por consulta
  `EXISTS` sobre `SolicitacaoAlteracoes` com `Status IN (Aberta, EmAnalise)`, sob transação.
- Migration `SolicitacaoDadosCadastrais`.

## Contrato de API

| Rota | Regra | Papel |
|---|---|---|
| `POST /api/solicitacoes` | tipo `DadosCadastrais`; `alteracoes[]` + `justificativa` | Responsavel |
| `GET /api/solicitacoes?tipo=DadosCadastrais` | escopo da matriz 028 | ADM, AdminEscola, Responsavel |
| `GET /api/solicitacoes/{id}` | detalhe com diff e snapshot | mesmos |
| `POST /api/solicitacoes/{id}/analisar` | `Aberta → EmAnalise` | ADM |
| `POST /api/solicitacoes/{id}/aprovar` | atômico; 409 em divergência | ADM |
| `POST /api/solicitacoes/{id}/recusar` | motivo obrigatório | ADM |
| `POST /api/solicitacoes/{id}/cancelar` | autor, enquanto `Aberta` | Responsavel |

`403` fora do escopo, `404` para outra unidade, `409` para estado/divergência/duplicidade,
`400` para campo ou motivo inválido. Corpo de erro `{ erro }` (ADR-005).

## Testes obrigatórios

| Teste | Prova |
|---|---|
| `DadosCadastrais_CampoForaDaLista_400` | RF3 |
| `DadosCadastrais_SemVinculo_403` | RF4 |
| `DadosCadastrais_AdminEscolaNaoCria_403` | RF4, matriz |
| `Aprovar_AplicaTodosOsCampos_EmUmaTransacao` | RF6 |
| `Aprovar_CampoInvalido_NaoAplicaNada` | RF6 |
| `Aprovar_ValorDivergente_409SemEscrita` | RF7 |
| `Aprovar_DuasVezes_409SemDuplicarTrilha` | RF8 |
| `Criar_SegundoPedidoMesmoCampo_409` | RF13 |
| `Recusar_SemMotivo_400` | RF5 |
| `Trilha_EventoComOrigemDaSolicitacao` | RF6, RNF2 |
| `Listagem_NutricionistaEMerendeira_NaoVeem` | RF9 |

## Riscos

- **Duplicação do fluxo da 017:** mitigada por extensão do agregado, não por um fluxo paralelo.
- **Concorrência na aprovação:** duas aprovações simultâneas. Mitigação: `ConcurrencyCheck`
  (ADR-010) na `Solicitacao` e checagem de status; a segunda recebe 409.
- **Snapshot como fonte de verdade:** o snapshot é informativo; a autorização de escrita é o
  estado atual do aluno. Mitigação: RF7 compara e recusa.
- **Campo sensível:** CPF e escola ficaram fora de propósito; incluí-los exigiria regra própria
  de identidade e de transferência.
