---
name: correcao-geracao-automatica-cardapios
status: created
references: V21, V18, ADR-022, ADR-011, ADR-004, L7
---

# Requirements — Correção da geração automática de cardápios

## Objetivo

A geração automática falha de forma intermitente e, quando falha, não deixa diagnóstico útil.
Esta spec documenta o fluxo completo (parâmetros → candidatos → seleção → cálculo → validação →
persistência), os defeitos observados no código atual e as regras que o pipeline deve cumprir.
Ela **corrige** o comportamento da Spec 013; não reescreve o documento dela, que está
`implemented` e é imutável.

## Escopo

- Backend: seleção com tentativas limitadas e determinísticas, validação pré-persistência,
  escopo de nutricionista (unidade + CRN), geração do geral e das versões adaptadas na mesma
  transação, contrato de resposta com diagnóstico.
- Frontend: exibir motivo por refeição, estado das versões adaptadas e impedir duplo envio.
- Fora de escopo: estrutura de cardápio e cálculo nutricional (Spec 012), compatibilidade
  (Spec 014), publicação e histórico (Spec 015), modelo das duas versões (Spec 029), CRN como
  cadastro (Spec 031).

### Relação com a Spec 013

Esta spec **supera** os seguintes pontos da Spec 013, mantendo os demais:

| Spec 013 | Passa a valer |
|---|---|
| RF4 (uma seleção gulosa por refeição) | RF5 e RF6 desta spec (tentativas limitadas + validação por refeição) |
| RF7 (`ExcluirAlimentosIncompativeis` filtra candidatos do geral) | RF3 e RF4 desta spec (o geral não considera restrições; a adaptação aplica) |
| RF10 (resposta só com id e totais) | RF12 desta spec (ids do geral e dos adaptados + diagnóstico) |
| RF11 (nutricionista da rede) | RF2 desta spec (mesma unidade + CRN válido) |

RF1, RF2, RF3, RF5, RF8, RF9 e RF12 da Spec 013 continuam válidos.

## Problemas observados no código atual (2026-11-04)

| # | Evidência | Efeito observável | Regra que corrige |
|---|---|---|---|
| P1 | `backend/Domain/Services/GeradorCardapioAutomatico.cs:51-55,96-104` — uma única passagem gulosa; se a faixa calórica não fechar, lança exceção sem nova tentativa | Geração viável falha de forma aparentemente aleatória, dependendo do `seed` | RF5, RF6 |
| P2 | `GeradorCardapioAutomatico.cs:19,52,107` — `usados` é global entre refeições e cada refeição exige `OpcoesPorRefeicao` alimentos distintos | Com muitas refeições, o catálogo se esgota e devolve "alimentos insuficientes" | RF5, RF14 |
| P3 | `backend/API/Services/GeracaoCardapioService.cs:128-129` — quando `ExcluirAlimentosIncompativeis` é verdadeiro, bloqueia todo alimento incompatível para **qualquer** aluno da unidade | O cardápio geral nasce filtrado por restrições individuais, contrariando a definição do geral e reduzindo candidatos | RF3, RF4 |
| P4 | `GeracaoCardapioService.cs:153-178` — a análise de compatibilidade roda para todos os alimentos × todos os alunos da unidade | Custo alto e uso do contexto de unidade, não de escola | RF4, RF14 |
| P5 | `GeracaoCardapioService.cs:102-114` — valida papel e `Ativo`, mas não valida unidade nem CRN | Cardápio pode ser assinado por nutricionista de outra unidade ou sem CRN | RF2 |
| P6 | `GeracaoCardapioService.cs:180-207` — `MontarCardapio` cria apenas um cardápio | Não existe versão adaptada no fluxo automático | RF8 |
| P7 | `GeracaoCardapioService.cs:139-148` — persiste o resultado sem revalidar totais nem vínculos já persistidos | Cardápio inconsistente pode ser salvo se o gerador mudar | RF7, RF10 |
| P8 | `backend/Domain/Entities/ParametroGeracao.cs:138-146` — categoria obrigatória só é checada como positiva e não repetida | Categoria inexistente ou inativa só falha na geração, com 422 tardio | RF1 |
| P9 | `GeradorCardapioAutomatico.cs:66,92,101` — a mensagem cita apenas a refeição e um motivo genérico | Usuário não sabe qual faixa era alcançável nem qual categoria faltou | RF15 |
| P10 | `GeracaoCardapioService.cs:137` — `Random.Shared` quando o `seed` é nulo | Duas chamadas iguais produzem cardápios diferentes; aceitável, mas precisa ser explícito na resposta | RF13 |

## Requisitos funcionais

- RF1: os parâmetros são validados no salvamento e na geração: faixa calórica não negativa e
  `min <= max`, opções entre 1 e 10, janela entre 0 e 30 dias, ao menos uma refeição padrão,
  nomes de refeição únicos e **categorias obrigatórias existentes e ativas**. Categoria
  inexistente devolve 400 no salvamento, não 422 na geração.
- RF2: `nutricionistaId` é obrigatório; a profissional precisa ser `Nutricionista`, estar
  ativa, pertencer à **mesma unidade** do cardápio e ter **CRN válido** (Spec 031). Qualquer
  falha devolve 400 com mensagem específica, antes de carregar alimentos.
- RF3: o cardápio **geral** não considera restrições individuais: o pipeline não remove
  candidatos por causa de alunos. O filtro por restrições pertence exclusivamente às versões
  adaptadas (Spec 029). O parâmetro `ExcluirAlimentosIncompativeis` é lido como
  "aplicar restrições na adaptação" e não altera o geral.
- RF4: candidatos do geral: alimentos ativos, com valores nutricionais válidos e não usados
  dentro da `JanelaRepeticaoDias` (Spec 013, RF8). A ordem de entrada é estável (`AlimentoId`
  crescente) antes de qualquer embaralhamento.
- RF5: a seleção usa `Random(seed)` e até **5 tentativas** por refeição. A tentativa `n` usa
  `seed + n` derivado de forma determinística; cada tentativa embaralha os candidatos
  disponíveis e tenta fechar a refeição. Só falha quando as 5 tentativas falham. Nenhuma
  fonte de aleatoriedade global.
- RF6: uma refeição só é aceita quando tem exatamente `OpcoesPorRefeicao` alimentos distintos,
  todos os `CategoriasObrigatorias` presentes, nenhum alimento repetido na refeição e total
  calórico dentro de `[CaloriasMinimasRefeicao, CaloriasMaximasRefeicao]`.
- RF7: antes de persistir, o pipeline revalida o cardápio inteiro: todas as refeições de RF6,
  totais recalculados pelo mesmo cálculo da Spec 012, período válido, unidade existente e
  nenhum item com alimento nulo. Falha em qualquer ponto devolve 422 e **nada é gravado**.
- RF8: no sucesso, a mesma transação cria o geral e uma versão adaptada por
  `(escola, conjunto distinto)` existente naquela escola, reutilizando o serviço da Spec 029.
  O geral e os adaptados recebem o mesmo snapshot de autoria.
- RF9: a versão adaptada pode nascer `PendenteDeRevisao`; o geral ainda é salvo se passou em
  RF7. A resposta informa, por escola e conjunto, o status e o motivo. A publicação do geral
  fica bloqueada enquanto houver adaptado pendente ou desatualizado (Spec 029, RF14).
- RF10: atomicidade: geral, adaptados e trilha são persistidos na mesma unidade de trabalho
  (ADR-019). Falha em qualquer escrita desfaz tudo; não existe cardápio geral salvo sem as
  versões que a resposta declarou.
- RF11: toda versão (geral e adaptada) carrega autor com nome, e-mail, CPF e CRN válidos,
  no padrão da ADR-009.
- RF12: a resposta contém `cardapioGeralId`, `seed`, `tentativasPorRefeicao`, `totaisPorRefeicao`,
  `versaoAdaptadaIds[]` com `escolaId`, `conjunto`, `status` e `motivo`, `avisos[]` e
  `diagnostico[]`.
- RF13: determinismo: mesmos parâmetros, catálogo e `seed` produzem o mesmo geral. Quando o
  `seed` é nulo, o valor sorteado é devolvido na resposta para reprodução.
- RF14: escopo: apenas `Nutricionista` da própria unidade. Alunos, escolas e cardápios
  considerados são os da unidade da sessão; outra unidade devolve 404/403 (Spec 028).
- RF15: quando não for possível gerar, o 422 traz, por refeição, o motivo acionável: faixa
  alcançável com o catálogo atual, categoria ausente, quantidade de candidatos disponível e
  quantas tentativas foram feitas.
- RF16: não existe fallback silencioso: se a adaptação falhar, a versão fica pendente e não é
  servida; o geral nunca substitui a versão adaptada de um aluno com restrição (Spec 029).
- RF17: repetir a chamada cria um cardápio novo (não há idempotência por chave); o frontend
  desabilita o envio enquanto a requisição estiver em curso e o `seed` devolvido permite
  reproduzir o resultado.

## Requisitos não funcionais

- RNF1: a seleção e a validação são funções puras no domínio, sem IO; o serviço só carrega,
  orquestra e persiste (Spec 013, RNF5).
- RNF2: determinismo por `(seed, tentativa)`; nenhuma dependência de hash, cultura ou relógio
  dentro do domínio.
- RNF3: cálculo em C#, com arredondamento de duas casas e comparação por faixa; nunca
  igualdade de ponto flutuante (PEND-029, PEND-030).
- RNF4: uma carga de catálogo e uma de alunos por requisição; o conjunto de bloqueados da
  adaptação é derivado de vínculos já carregados, sem análise `alimento × aluno` no caminho do
  geral.
- RNF5: tentativas limitadas a 5 por refeição; nenhuma busca exaustiva.
- RNF6: o log registra `unidadeId`, `seed`, tentativas e motivo da falha, sem dado pessoal de
  aluno.
- RNF7: a transação envolve todas as escritas e não deixa versão órfã.

## Restrições

- A Spec 013 está `implemented` e é imutável; esta spec é o documento de correção e deve ser
  lida junto com ela.
- A Spec 029 é dona do modelo `Tipo`/`OrigemId`/`EscolaId`/`ConjuntoRestricoesChave` e do
  algoritmo de substituição; esta spec não reimplementa a adaptação.
- O cardápio continua sendo da **unidade**; a adaptação é **por escola** (decisão do dono em
  2026-11-04).
- Não alterar `ParametroGeracao` para aceitar categoria inexistente; a validação nova é
  aditiva.
- Nenhuma dependência nova.

## Dependências

- Spec 027 (`Aluno.EscolaId`) e Spec 028 (escopo e unidade).
- Spec 031 (CRN válido e `PoliticaDeCrn`).
- Spec 029 (modelo e serviço de adaptação).
- Spec 011 (catálogo e categorias), Spec 012 (cálculo), Spec 014 (compatibilidade).
- Spec 015 (publicação) para a trava de RF9.

## Casos de borda

- Faixa calórica impossível com o catálogo atual → 422 com faixa alcançável, nada gravado.
- Categoria obrigatória sem alimento ativo → 422 nomeando a categoria.
- Catálogo insuficiente para `refeições × opções` → 422 com a conta explícita.
- Refeição padrão repetida → 400 no salvamento (já garantido no domínio).
- Categoria obrigatória inexistente/inativa → 400 no salvamento.
- Nutricionista de outra unidade → 400/404 conforme o escopo, sem carregar catálogo.
- Nutricionista sem CRN → 400 (Spec 031).
- Seed fixo repetido → mesmo cardápio; seed nulo → seed devolvido.
- Unidade sem escola → geral criado, nenhum adaptado, aviso `semEscolas`.
- Escola sem alunos → nenhum adaptado para ela, aviso `semAlunos`.
- Adaptação impossível para um conjunto → geral salvo, adaptado `PendenteDeRevisao`, publicação
  do geral bloqueada.
- Falha de banco no meio → nenhuma versão persiste; resposta 500 sem id.
- Duas requisições simultâneas com o mesmo período → duas gerações distintas; conflito de
  publicação é tratado pela Spec 015.

## Critérios de aceitação

- [ ] Geração válida produz 1 geral + adaptados por `(escola, conjunto)` na mesma transação.
- [ ] Mesmo `seed` e mesmos dados produzem o mesmo geral.
- [ ] Nenhuma refeição fica fora da faixa calórica quando a resposta é 201.
- [ ] Todas as categorias obrigatórias aparecem em cada refeição.
- [ ] Categoria inexistente no salvamento devolve 400.
- [ ] Nutricionista de outra unidade ou sem CRN devolve 400 antes de gerar.
- [ ] O geral não perde candidatos por restrições individuais.
- [ ] Falha de validação devolve 422 e não cria cardápio.
- [ ] Adaptado pendente é retornado com motivo e bloqueia publicação do geral.
- [ ] Resposta traz ids, seed, tentativas, totais e diagnóstico por refeição.
- [ ] Nenhum item bloqueante aparece em versão adaptada publicada.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro novo.

## Validação

Com um catálogo fixo, uma unidade com duas escolas e alunos com restrições distintas: gerar
com faixa apertada, com categoria sem alimento, com catálogo insuficiente e com seed fixo.
Conferir 201/422, totais, categorias, determinismo, versões adaptadas e bloqueio de publicação.
Executar os gates do projeto.
