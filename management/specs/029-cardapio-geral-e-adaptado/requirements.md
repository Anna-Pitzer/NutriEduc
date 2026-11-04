---
name: cardapio-geral-e-adaptado
status: created
references: V18, V20, ADR-022, ADR-011, ADR-009, L7
---

# Requirements — Cardápio geral e cardápio adaptado

## Objetivo

Entregar as duas versões exigidas pela regra de negócio: o cardápio **geral** da unidade e as
versões **adaptadas** por escola, geradas a partir dele e vinculadas ao mesmo cardápio de
origem. Hoje a Spec 012 monta um único cardápio sem autor, a Spec 014 apenas **detecta**
incompatibilidade e a criação manual não mostra alunos nem restrições. Nenhuma delas produz a
refeição que o aluno com restrição realmente recebe.

## Escopo

- Backend: `Tipo`, `OrigemId`, `EscolaId` e `ConjuntoRestricoesChave` em `Cardapio`; adaptação
  determinística por escola; substituição com tolerância nutricional; regra de servir por
  aluno; autoria obrigatória com snapshot de nome e CRN.
- Frontend: contexto de restrições por escola na montagem manual; seleção de nutricionista com
  nome e CRN; detalhe com as duas versões, origem, conjunto, substituições e status.
- Fora de escopo: estrutura de refeições e itens (Spec 012), compatibilidade (Spec 014),
  publicação e histórico (Spec 015), pipeline da geração automática (Spec 032), CRN como
  cadastro de usuário (Spec 031), sugestão por IA (Spec 026, opcional).

### Cobertura de `implementacoes.md`

- **Nutricionista:** criar cardápio informando o responsável técnico; ver alunos e restrições
  por escola; gerar e publicar as versões; ver o que foi substituído e por quê.
- **Merendeira:** ver o cardápio do dia e a versão aplicável a cada aluno com restrição.
- **Pais:** ver a versão publicada aplicável ao próprio filho.
- **Secretaria:** ver o par geral↔adaptado da própria unidade, sem editar.

## Decisões e interpretações

| # | Regra recebida | Interpretação | Origem |
|---|---|---|---|
| D1 | "Cardápio padrão da escola" | O cardápio pertence à **unidade** (decisão do dono em 2026-11-04); a **adaptação é por escola** dentro dela (decisão do dono em 2026-11-04) | ADR-022, ADR-020 |
| D2 | "Versão adaptada para os alunos com restrições" | Uma versão por `(escola, conjunto distinto de restrições)` | ADR-022 |
| D3 | "As duas versões vinculadas ao mesmo cardápio de origem" | `OrigemId` no adaptado aponta sempre para o geral; não existe adaptado de adaptado | ADR-022 |
| D4 | "Impedir a seleção de alimentos incompatíveis" | O geral **não** considera restrições individuais. Na montagem manual: alimento incompatível **com substituto seguro** gera aviso e exige confirmação; alimento incompatível **sem substituto seguro** para algum grupo é bloqueado. No adaptado, alimento bloqueante é proibido sem exceção | Regra do dono + §1.1 |
| D5 | "Preservar, sempre que possível, os valores nutricionais" | Tolerância explícita e testável; fora dela a versão fica `PendenteDeRevisao` | RF7 |
| D6 | Limite de combinações | Máximo de 50 versões adaptadas por origem, somando escolas e conjuntos; acima disso 422 com a contagem e nada é gravado | Bounded execution |
| D7 | "CRN obrigatório para nutricionista" | Obrigatório no cadastro e na operação; a regra completa é da Spec 031 | Spec 031 |
| D8 | "Nunca substituir uma versão pela outra silenciosamente" | Geral e adaptado são registros distintos, com ids próprios; nenhuma operação sobrescreve um com o outro | RF14 |

## Requisitos funcionais

- RF1: `Cardapio.Tipo` é `Geral` ou `Adaptado`. `OrigemId` é nulo em `Geral` e obrigatório em
  `Adaptado`; o destino é sempre um `Geral`. `EscolaId` é nulo em `Geral` e obrigatório em
  `Adaptado`.
- RF2: cada `Adaptado` tem `ConjuntoRestricoesChave` com identificadores distintos, ordenados,
  normalizados e separados por vírgula. Conjunto vazio não gera versão adaptada.
- RF3: existe no máximo uma versão `Adaptado` vigente por
  `(OrigemId, EscolaId, ConjuntoRestricoesChave)`. Readaptar atualiza o rascunho existente; uma
  versão publicada só é substituída pelo fluxo de revisão da Spec 015.
- RF4: o `Geral` é da unidade, é a versão servida a aluno **sem restrição** e a base de cálculo
  de todas as versões adaptadas.
- RF5: a adaptação é determinística e por escola. Para cada item cujo alimento tem vínculo
  **bloqueante** (Spec 014) com alguma restrição do conjunto, o item é substituído por alimento
  ativo, da mesma categoria, sem vínculo bloqueante com nenhuma restrição do conjunto, escolhido
  pelo menor desvio nutricional normalizado entre gramas equivalentes.
- RF6: os alunos considerados são exclusivamente os da `EscolaId` da versão, com
  `Aluno.EscolaId` igual (Spec 027); aluno de outra escola, ou sem escola, não entra no conjunto
  e não influencia a substituição.
- RF7: tolerância nutricional: a refeição adaptada fica dentro de ±10% das kcal da refeição do
  geral; o substituto fica dentro de ±20% das kcal e ±30% de proteína, lipídio e fibra do item
  original. Fora da tolerância, ou sem candidato, a versão é `PendenteDeRevisao` e o motivo é
  registrado por item.
- RF8: versão `PendenteDeRevisao` não pode ser publicada nem servida. O detalhe lista refeição,
  alimento original, restrição bloqueante, candidatos considerados e motivo da recusa.
- RF9: criar cardápio manual exige `nutricionistaId` de `Nutricionista` ativo, da mesma unidade
  e com CRN válido (Spec 031). A resposta inclui `nutricionistaNome` e `nutricionistaCrn`; o
  snapshot é persistido (ADR-009) e não muda quando o cadastro muda depois.
- RF10: a criação manual produz o `Geral` e, na mesma transação, uma versão adaptada por
  `(escola, conjunto distinto)` existente entre os alunos ativos **daquela escola**. Sem nenhum
  conjunto, nenhuma versão adaptada é criada e a resposta declara `semRestricoes = true`.
- RF11: a tela de montagem manual carrega, por escola da unidade, os alunos ativos e suas
  restrições/alergias (nome da restrição, categoria e severidade), e atualiza o painel a cada
  item incluído ou removido.
- RF12: na montagem manual, alimento incompatível com algum conjunto e **com** substituto
  seguro gera aviso nominal (aluno, restrição, escola) e exige `confirmarIncompatibilidades =
  true` para salvar; sem confirmação devolve 400. Alimento incompatível **sem** substituto
  seguro para algum conjunto é bloqueado com 400 e a resposta indica o grupo afetado.
- RF13: a confirmação de RF12 é registrada de forma append-only com autor, data UTC, alimento,
  escolas e conjuntos afetados; não é um "aceite" reutilizável em outra edição.
- RF14: publicação é individual por cardápio (Spec 015). O `Geral` não pode ser publicado
  enquanto existir versão adaptada da mesma origem, de escola com alunos, em
  `PendenteDeRevisao` ou `Desatualizado`. Publicar o geral não publica nem altera as versões
  adaptadas, e vice-versa.
- RF15: servir: aluno sem restrição recebe o `Geral` publicado; aluno com restrições recebe a
  versão publicada de `(origem, escola do aluno, conjunto exato)`. Sem versão publicada para o
  conjunto, o cardápio do aluno é `Indisponivel` e a merendeira recebe alerta; o geral nunca é
  usado como fallback e item bloqueante nunca é servido.
- RF16: leitura: `AdminSecretaria` e `Nutricionista` leem o par da própria unidade;
  `AdminEscola` e `Merendeira` leem o par aplicável à própria escola; `Responsavel` lê apenas a
  versão publicada aplicável ao filho (Spec 025). Escrita é exclusiva de `Nutricionista`
  (Spec 028).
- RF17: nutricionista sem CRN válido não cria, gera, adapta nem publica cardápio (400 com
  mensagem que aponta o cadastro); o login permanece permitido (Spec 031).
- RF18: a tela de criação escolhe o nutricionista exibindo **nome e CRN**; o detalhe mostra
  tipo, origem, escola, conjunto de restrições, status de adaptação, substituições e alertas; a
  listagem mostra o par geral↔adaptado e permite navegar entre eles.
- RF19: toda criação, duplicação, revisão e adaptação registra autor (snapshot) e data UTC,
  reutilizando a trilha append-only da Spec 015 e o padrão da ADR-009.
- RF20: alterar o conteúdo de um `Geral` em rascunho marca todas as versões adaptadas dele como
  `Desatualizado`. Alterar um `Geral` publicado exige o fluxo de revisão da Spec 015, e a nova
  versão nasce sem adaptações herdadas.
- RF21: escola sem alunos ativos ou sem nenhuma restrição não recebe versão adaptada; a
  resposta e o detalhe informam `semRestricoes` por escola.
- RF22: uma versão adaptada nunca contém alimento bloqueante para o próprio conjunto, mesmo que
  ele exista no geral. Não existe "aproximação" silenciosa.

## Requisitos não funcionais

- RNF1: cálculo nutricional em C#, com arredondamento de duas casas e comparação por tolerância,
  nunca igualdade de ponto flutuante (PEND-029, PEND-030).
- RNF2: determinismo: a mesma entrada produz a mesma saída; nenhuma fonte de aleatoriedade na
  adaptação (Spec 013, RF9).
- RNF3: a compatibilidade é a função pura da Spec 014 (ADR-011); nenhuma regra de severidade é
  reimplementada.
- RNF4: nenhuma dependência de IA. A Spec 026 é opcional e só sugere entre candidatos válidos.
- RNF5: adaptação é idempotente por `(OrigemId, EscolaId, ConjuntoRestricoesChave)`; repetir a
  chamada não cria versão nova nem duplica itens.
- RNF6: o custo é `escolas × conjuntos × itens × candidatos`; o catálogo e os alunos são
  carregados uma vez por requisição.
- RNF7: o contexto de restrições por escola é lido no banco, sem carregar a unidade inteira em
  memória quando há filtro de escola.

## Restrições

- Specs 012, 013, 014 e 015 permanecem donas da estrutura, geração, compatibilidade e
  versionamento. Esta spec adiciona a camada derivada, sem reescrever aquelas regras.
- A Spec 013 está `implemented` e é imutável; a correção do pipeline automático é da Spec 032.
- Nenhum editor novo: reutilizar `FormularioCardapio`, `DetalheCardapio` e
  `CompatibilidadeService`.
- Cardápio publicado não é excluído (Spec 015, RF7); o adaptado segue a mesma regra.
- Migration nova; `Tipo` entra com padrão `Geral`, `OrigemId` e `EscolaId` nulos para os
  cardápios existentes.
- O escopo do cardápio é decisão do dono: unidade. Mudar para escola exige revisar RF4, RF10,
  RF15 e a matriz da Spec 028 na mesma mudança.

## Dependências

- Spec 010 (restrições e alergias) — insumo dos conjuntos.
- Spec 011 (catálogo de alimentos) — categorias e valores nutricionais.
- Spec 012 (domínio de cardápio) — refeições, itens e cálculo.
- Spec 014 (compatibilidade) — severidade e vínculo alimento↔restrição.
- Spec 015 (publicação e histórico) — versão, revisão e vigência.
- Spec 027 (hierarquia) — `Aluno.EscolaId` e `Escola.UnidadeId`.
- Spec 028 (matriz) — escopo de leitura e escrita.
- Spec 031 (CRN) — obrigatoriedade e formato do CRN da nutricionista.
- Spec 032 (geração automática) — produtor do par geral+adaptado no fluxo automático.
- Spec 025 (painel da família) — exibição ao responsável.

## Casos de borda

- Aluno sem restrição: recebe o `Geral`; nenhuma versão adaptada é necessária para ele.
- Aluno com restrição sem versão publicada para `(escola, conjunto)`: `Indisponivel` + alerta;
  nunca servir o geral nem item bloqueante.
- Dois alunos da mesma escola com o mesmo conjunto: compartilham a mesma versão.
- Dois alunos de escolas diferentes com o mesmo conjunto: **não** compartilham; cada escola tem
  sua versão (RF6).
- Versão adaptada sem nenhum item bloqueante: é criada idêntica ao geral, com
  `substituicoes = 0` e status `Atualizado`.
- Restrição **informativa** (Spec 014): não bloqueia, não gera substituição, mas aparece no
  alerta da versão.
- Alimento desativado após a publicação: o cardápio publicado permanece histórico; nova
  adaptação considera apenas alimentos ativos.
- Restrição removida do aluno: ele passa ao conjunto menor; sem versão publicada, fica
  `Indisponivel` até a adaptação.
- Escola sem alunos: nenhuma versão adaptada; a resposta declara `semRestricoes` para ela.
- Unidade sem alunos: nenhum conjunto; apenas o geral é criado.
- Mudança de restrição de um aluno após a publicação: as versões existentes permanecem; a lista
  de alunos por versão é derivada e muda na leitura.
- Duas publicações simultâneas da mesma origem: uma 200 e uma 409 (Spec 015, RF4).
- Acima de 50 versões adaptadas por origem: 422 com a contagem e nada é gravado.
- Aluno sem `EscolaId` (legado): não entra em nenhum conjunto; leitura fica restrita ao ADM até
  o backfill da Spec 027.

## Critérios de aceitação

- [ ] Criar cardápio sem `nutricionistaId` devolve 400; com nutricionista sem CRN devolve 400.
- [ ] A resposta de criação traz `nutricionistaNome` e `nutricionistaCrn`.
- [ ] Unidade com duas escolas e dois conjuntos produz 1 geral + até 4 adaptados, com
  `escolaId` e `origemId` corretos.
- [ ] Nenhum item de versão `Atualizado` tem vínculo bloqueante com o próprio conjunto.
- [ ] Refeição adaptada fica dentro de ±10% das kcal da refeição do geral.
- [ ] Item sem substituto válido marca a versão como `PendenteDeRevisao` e impede publicação.
- [ ] A tela de montagem lista alunos e restrições por escola e bloqueia alimento sem
  substituto seguro.
- [ ] Salvar geral com incompatibilidade e sem confirmação devolve 400.
- [ ] A confirmação fica registrada com autor, data e grupos afetados.
- [ ] Editar o geral em rascunho marca os adaptados como `Desatualizado`.
- [ ] Publicar o geral com adaptado pendente ou desatualizado devolve 409.
- [ ] Aluno com restrição sem adaptado publicado recebe `Indisponivel`, não o geral.
- [ ] Responsável vê apenas a versão publicada aplicável ao próprio filho (403 fora).
- [ ] Repetir a adaptação não cria versão duplicada.
- [ ] Mais de 50 versões adaptadas devolve 422 sem gravar.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro novo.

## Validação

Cadastrar, na mesma unidade, duas escolas e alunos com nenhuma restrição, restrição A, restrição
B e A+B em cada escola. Criar um cardápio manual com nutricionista e CRN. Conferir: 1 geral +
adaptados por `(escola, conjunto)`; painel de alunos/restrições na montagem; bloqueio de
alimento sem substituto; confirmação registrada; ausência de item bloqueante por conjunto;
totais dentro da tolerância; versão pendente impedindo publicação; responsável de cada aluno
vendo somente a versão aplicável. Executar os gates do projeto.
