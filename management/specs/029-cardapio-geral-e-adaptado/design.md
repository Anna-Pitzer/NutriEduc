---
name: cardapio-geral-e-adaptado
status: created
references: V18, ADR-022, ADR-011, ADR-019
---

# Design — Cardápio geral e cardápio adaptado

## Princípio

O geral é a verdade editorial; o adaptado é uma **projeção verificável** dele para um conjunto
de restrições **de uma escola**. Adaptar não é editar à mão: é executar uma função
determinística e provar que o resultado não contém item bloqueante e preserva a refeição dentro
da tolerância. Quando a prova falha, a versão fica explicitamente pendente — nunca "quase
pronta".

## Estrutura

```
backend/
  Domain/Entities/
    Cardapio.cs                 (+ Tipo, OrigemId, EscolaId, ConjuntoRestricoesChave, StatusAdaptacao)
    ConjuntoDeRestricoes.cs     (novo — objeto de valor: normaliza, ordena e compara)
  Domain/Services/
    AdaptacaoDeCardapio.cs      (novo — função pura geral + escola + conjunto → adaptado | pendência)
    SubstituicaoNutricional.cs  (novo — escolha do candidato por desvio)
  Application/Cardapios/
    AdaptarCardapioService.cs   (novo — orquestra, persiste e audita)
    CardapioServidoService.cs   (novo — resolve a versão de um aluno)
    ContextoDeRestricoesService.cs (novo — alunos e restrições por escola)
  API/Controllers/CardapioController.cs (+ adaptar, adaptados, contexto, servido)
  API/Controllers/NutricionistaController.cs (existente — nome + CRN)
frontend/src/pages/nutricao/cardapios/
  FormularioCardapio.tsx        (select de nutricionista + painel de restrições por escola)
  DetalheCardapio.tsx           (origem, escola, tipo, conjunto, status, substituições)
  ListarCardapios.tsx           (par geral↔adaptado)
frontend/src/pages/merendeiras/  (cardápio do dia + alerta de indisponível)
```

## Domínio

`Cardapio`:

| Campo | Regra |
|---|---|
| `Tipo` | `Geral` \| `Adaptado`; obrigatório |
| `OrigemId` | nulo se `Geral`; obrigatório se `Adaptado`; alvo precisa ser `Geral` |
| `EscolaId` | nulo se `Geral`; obrigatório se `Adaptado`; escola precisa pertencer à unidade |
| `ConjuntoRestricoesChave` | nulo se `Geral`; não vazio se `Adaptado`; imutável após criação |
| `StatusAdaptacao` | nulo se `Geral`; `Atualizado` \| `Desatualizado` \| `PendenteDeRevisao` |
| Snapshot do nutricionista | nome, e-mail, CPF e CRN, já existente (PEND-038, ADR-009) |

Invariantes no construtor e nos métodos de transição: adaptado sem origem lança; origem do
tipo `Adaptado` lança; conjunto vazio lança; `MarcarDesatualizado` só sai de `Atualizado`;
`MarcarPendente` grava o motivo por item.

`ConjuntoDeRestricoes`: value object com `Ids` ordenados, `Chave` canônica (`"3,7,12"`) e
`Contem`/`Equals`. A identidade da versão é `(OrigemId, EscolaId, Chave)`; o conjunto sozinho
não identifica a versão, porque duas escolas com o mesmo conjunto têm versões distintas.

`ContextoDeRestricoesService.Listar(unidadeId, escolaId?)` devolve, por escola, os alunos ativos
e suas restrições (nome, categoria, severidade) em uma consulta com filtro no banco. É o insumo
do painel da tela manual e do cálculo dos conjuntos.

## Algoritmo de adaptação

```
Adaptar(geral, escola, conjunto):
  pendentes = []
  alunos = alunos ativos de EscolaId == escola (Spec 027)
  para cada refeição r do geral:
    para cada item i de r:
      se NãoBloqueia(i.Alimento, conjunto):
        mantém i
      senão:
        candidatos = alimentos ativos
          .Onde(a => a.Categoria == i.Alimento.Categoria)
          .Onde(a => a.Id != i.Alimento.Id)
          .Onde(a => NãoBloqueia(a, conjunto))
          .Onde(a => |Kcal(a, i.Gramas) - Kcal(i.Alimento, i.Gramas)| <= 0,20 * Kcal(i.Alimento, i.Gramas))
          .Onde(a => macros dentro de ±30%)
        escolhido = candidatos.OrdenarPor(DesvioNormalizado(i, a)).Primeiro()
        se escolhido == null: pendentes += (r, i, motivo)
        senão: substitui i por escolhido com as mesmas gramas
    se |Kcal(r.adaptada) - Kcal(r.geral)| > 0,10 * Kcal(r.geral):
      pendentes += (r, motivo = "refeição fora da tolerância")
  StatusAdaptacao = pendentes.vazio ? Atualizado : PendenteDeRevisao
```

Antes de salvar, a mesma função calcula a **adaptabilidade do geral**: para cada alimento e cada
conjunto da unidade, existe substituto seguro? Alimento sem substituto para algum conjunto é
bloqueado na montagem manual (RF12), com o grupo afetado na mensagem.

`DesvioNormalizado` soma as diferenças relativas de kcal, proteína, lipídio e fibra, cada uma
com peso igual. Empate é resolvido por `AlimentoId` crescente, para determinismo (RNF2).

## Persistência

Migration `CardapioAdaptado`:

1. `Cardapios.Tipo` inteiro, `default 0` (`Geral`), `not null`.
2. `Cardapios.OrigemId` inteiro anulável, FK `Restrict`.
3. `Cardapios.EscolaId` inteiro anulável, FK `Restrict`; obrigatório quando `Tipo = Adaptado`.
4. `Cardapios.ConjuntoRestricoesChave` texto(500) anulável.
5. `Cardapios.StatusAdaptacao` inteiro anulável.
6. Índice único filtrado `(OrigemId, EscolaId, ConjuntoRestricoesChave)` para
   `Tipo = 1 AND Situacao <> 'Substituido'` (a `Situacao` é a da Spec 015).
7. Nenhuma coluna existente é removida; o snapshot do nutricionista já existe.

## Contrato de API

| Rota | Regra | Papel |
|---|---|---|
| `POST /api/cardapios` | `nutricionistaId` obrigatório; CRN válido; gera geral + adaptados | Nutricionista |
| `GET /api/cardapios/contexto-restricoes?unidadeId=&escolaId=` | alunos ativos e restrições por escola | Nutricionista, ADM |
| `POST /api/cardapios/{id}/adaptar` | idempotente por `(origem, escola, conjunto)`; 422 acima de 50 | Nutricionista |
| `GET /api/cardapios/{id}/adaptados` | lista versões com escola, conjunto e status | matriz 028 |
| `GET /api/cardapios/{id}` | inclui `tipo`, `origemId`, `escolaId`, `conjunto`, `statusAdaptacao`, `substituicoes` | matriz 028 |
| `GET /api/aluno/{id}/cardapio` | versão servida ou `Indisponivel` | matriz 028 |
| `GET /api/nutricionistas` | `id`, `nome`, `crn` da própria unidade | Nutricionista, ADM |

Erros seguem a ADR-005. `409` para conflito de versão/publicação; `422` para excesso de
versões; `400` para CRN ausente, alimento sem substituto ou falta de confirmação.

## Resolução da versão servida

```
conjunto = aluno.Restricoes.Ativas.Ids.Ordenados.Chave
escola = aluno.EscolaId
se conjunto vazio: versão = geral publicado vigente
senão: versão = adaptado publicado vigente de (origem = geral, escola = escola, chave = conjunto)
se ausente: resultado = Indisponivel (com motivo e alerta)
```

A lista de alunos por versão é **derivada** na leitura, nunca persistida; assim uma mudança de
restrição não deixa lista velha.

## Testes obrigatórios

| Teste | Prova |
|---|---|
| `Adaptado_SemOrigem_Lanca` / `Adaptado_ComOrigemAdaptada_Lanca` | RF1 |
| `Conjunto_ChaveCanonica_IgnoraOrdemEDuplicata` | RF2, RNF5 |
| `Adaptar_SubstituiSoItensBloqueantes` | RF5 |
| `Adaptar_UsaSomenteAlunosDaEscola` | RF6 |
| `Adaptar_RespeitaToleranciaDaRefeicao` | RF7, RNF1 |
| `Adaptar_SemCandidato_MarcaPendente` | RF8 |
| `Criacao_ProduzGeralECadaEscolaEConjunto` | RF10 |
| `Criacao_SemRestricoes_NaoCriaAdaptado` | RF10, RF21 |
| `Montagem_BloqueiaAlimentoSemSubstituto` | RF12 |
| `Montagem_SemConfirmacao_400` | RF12, RF13 |
| `EditarGeral_MarcaAdaptadosDesatualizados` | RF20 |
| `PublicarGeralComAdaptadoPendente_409` | RF14 |
| `AlunoSemVersaoPublicada_Indisponivel` | RF15 |
| `Responsavel_SoVeVersaoDoFilho` | RF16 |
| `NutricionistaSemCrn_NaoCria` | RF17 |
| `AdaptarDuasVezes_NaoDuplica` | RNF5 |

## Riscos

- **Explosão combinatória:** cresce com `escolas × conjuntos`. Mitigação: teto de 50 versões
  por origem, contagem exibida antes de gerar e geração idempotente.
- **Bloqueio de publicação:** um adaptado pendente impede publicar o geral. É regra de
  segurança alimentar, não fricção administrativa; a tela mostra qual versão bloqueia e o
  caminho de correção (readaptar ou ajustar o geral).
- **Calibração da tolerância:** ±20% de kcal por item e ±10% por refeição são pontos de
  partida verificáveis; se a nutrição exigir outro número, muda em RF6 e no teste, não no
  código espalhado.
- **Rascunho editado muitas vezes:** cada edição invalida adaptados. Mitigação: readaptar é
  uma chamada idempotente e a UI mostra o status antes de publicar.
- **Snapshot do nutricionista:** já implementado em PEND-038; esta spec exige que a exibição
  use o snapshot, não o cadastro atual, e que o CRN seja válido na criação (Spec 031).
- **Geral sem substituto:** um alimento do geral sem substituto seguro para algum conjunto não
  pode ser salvo; o erro indica escola e conjunto afetados. Isso evita descobrir o problema só
  na adaptação.
- **Escola sem versão:** aluno de escola que ainda não tem adaptado publicado fica
  `Indisponivel`; a merendeira vê o alerta. Não existe fallback silencioso para o geral.
