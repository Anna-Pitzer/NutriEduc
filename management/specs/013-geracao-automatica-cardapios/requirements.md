---
name: geracao-automatica-cardapios
status: created
references: V6, ADR-004, L7
---

# Requirements — Geração automática de cardápios

## Objetivo

O produto prevê parâmetros de geração e geração automática de cardápio; hoje o
nutricionista montaria tudo manualmente. A geração precisa ser **determinística e
testável**: mesma entrada e mesma semente produzem exatamente o mesmo cardápio.

## Escopo

- Backend: `ParametroGeracao` por unidade e serviço de domínio de geração que produz
  um `Cardapio` em `Rascunho` (Spec 012).
- Frontend: tela de parâmetros e ação "Gerar cardápio" com pré-visualização do
  rascunho antes de salvar.
- Fora de escopo: publicação (Spec 015), análise de compatibilidade (Spec 014 — esta
  spec **consome** o resultado), cálculo nutricional (Spec 012), edição manual do
  resultado (Spec 012 já cobre).

### Cobertura de `implementacoes.md`

- **Nutrição (3):** definir parâmetros da geração, alterar parâmetros e gerar
  cardápios automaticamente a partir dos parâmetros.

## Requisitos funcionais

- RF1: `ParametroGeracao` é único por unidade e contém:
  `CaloriasMinimasRefeicao`, `CaloriasMaximasRefeicao` (kcal, `>= 0`,
  `min <= max`), `OpcoesPorRefeicao` (1–10), `JanelaRepeticaoDias` (0–30),
  `CategoriasObrigatorias` (ids de `CategoriaAlimento`) e
  `ExcluirAlimentosIncompativeis` (bool).
- RF2: `Nutricionista` cria, consulta e altera parâmetros; alteração não afeta
  cardápios já gerados.
- RF3: `POST /api/cardapios/gerar` recebe `unidadeId`, `nome`, `periodo` e `seed`
  opcional. Sem `seed`, o servidor gera uma e a devolve na resposta.
- RF4: a geração produz um `Cardapio` `Rascunho` com as refeições definidas pela
  unidade (estrutura de refeições padrão) e, em cada uma, `OpcoesPorRefeicao`
  alimentos distintos.
- RF5: cada refeição gerada tem total calórico dentro de
  `[CaloriasMinimasRefeicao, CaloriasMaximasRefeicao]`. Se não for possível, a
  geração falha com 422 e mensagem que indica a refeição e o motivo — nenhum
  cardápio parcial é salvo.
- RF6: ao menos um item de cada `CategoriaObrigatoria` aparece no cardápio; ausência
  de candidato para uma categoria obrigatória também gera 422.
- RF7: são candidatos apenas alimentos ativos. Se `ExcluirAlimentosIncompativeis`
  for verdadeiro, ficam de fora alimentos com vínculo `Alergenico` com restrições
  presentes nos alunos da unidade (Spec 014).
- RF8: `JanelaRepeticaoDias > 0` exclui alimentos usados em cardápios `Publicado`
  da mesma unidade cujo período termine nos últimos N dias.
- RF9: a seleção usa apenas `Random(seed)`; nenhuma fonte de aleatoriedade global,
  nenhuma ordenação dependente de hash ou de cultura.
- RF10: a resposta inclui `cardapioId`, `seed`, totais por refeição e `avisos`
  (ex.: categoria obrigatória atendida por único alimento).
- RF11: geração é exclusiva de `Nutricionista`; parâmetros são legíveis por
  `AdminEscola` da unidade, sem edição.
- RF12: o frontend mostra os parâmetros atuais, permite editá-los e, após gerar,
  exibe o rascunho com opção de abrir no editor da Spec 012.

## Requisitos não funcionais

- RNF1: o algoritmo é uma função pura de domínio
  (`GeradorCardapioAutomatico.Gerar`) sem acesso a banco, relógio ou `Guid`.
- RNF2: teste de determinismo: duas execuções com a mesma semente e o mesmo
  conjunto de entrada produzem cardápios iguais item a item.
- RNF3: complexidade limitada: no máximo três passagens sobre os candidatos por
  refeição; sem busca exaustiva.
- RNF4: contrato `{ erro }`; 422 é usado apenas para impossibilidade de geração,
  com `{ erro, detalhes }`.
- RNF5: nenhuma regra no controller; serviço apenas orquestra consulta + domínio.

## Restrições

- Migration nova `ParametrosGeracao`.
- Não alterar o modelo de `Cardapio` (Spec 012) além do necessário para receber o
  resultado.
- Nenhuma dependência nova.

## Dependências

- Spec 012 (cardápio) e Spec 014 (compatibilidade) implementadas **antes** desta.
- Spec 011 (alimentos e categorias) e Spec 009 (unidade).

## Casos de borda

- Parâmetros ausentes para a unidade → 400 com mensagem explícita.
- `min > max` → 400.
- Nenhum alimento ativo → 422.
- Candidatos insuficientes para `OpcoesPorRefeicao` → 422 com a refeição.
- Faixa calórica impossível com os candidatos → 422.
- Categoria obrigatória sem candidato → 422.
- `seed` negativa ou ausente → aceita; a resposta sempre devolve a semente usada.
- Geração repetida com mesma semente após mudança de catálogo → resultado diferente;
  isso é esperado porque a entrada mudou.

## Critérios de aceitação

- [ ] Parâmetros inválidos devolvem 400 e não persistem.
- [ ] Gerar duas vezes com a mesma semente e mesma base produz o mesmo cardápio.
- [ ] Cardápio gerado é `Rascunho` e visível na listagem da Spec 012.
- [ ] Toda refeição respeita a faixa calórica em teste com base fixa.
- [ ] Alimento com alergênico de aluno da unidade não aparece quando
      `ExcluirAlimentosIncompativeis = true`.
- [ ] Alimento usado em publicado recente não aparece com `JanelaRepeticaoDias > 0`.
- [ ] Impossibilidade devolve 422 e não cria cardápio.
- [ ] `POST /api/cardapios/gerar` por `AdminEscola` devolve 403.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Configurar parâmetros para uma unidade com catálogo fixo, gerar com semente
conhecida, conferir determinismo e faixas, e repetir com um aluno alérgico na
unidade para confirmar a exclusão. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
