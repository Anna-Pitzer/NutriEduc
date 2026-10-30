---
name: compatibilidade-cardapio-restricoes
status: created
references: V6, ADR-004, L5
---

# Requirements — Compatibilidade entre cardápio e restrições

## Objetivo

O produto exige verificar se um cardápio é seguro para os alunos da unidade,
identificar alimentos incompatíveis e alertar antes da publicação. Sem isso, a
publicação (Spec 015) seria uma decisão visual, não uma regra de domínio testada.

## Escopo

- Backend: serviço de domínio puro que cruza itens do cardápio (Spec 012) com os
  vínculos alimento↔restrição (Spec 011) e as restrições dos alunos da unidade
  (Spec 010 e 009); endpoint de consulta.
- Frontend: painel de compatibilidade no detalhe do cardápio, com alimentos
  permitidos e não permitidos e alunos afetados.
- Fora de escopo: publicação (Spec 015), geração automática (Spec 013 consome este
  serviço), cadastro de restrições e alimentos (Specs 010 e 011).

### Cobertura de `implementacoes.md`

- **Nutrição (4):** verificar compatibilidade dos alimentos com as restrições dos
  alunos; separar alimentos prejudiciais ou não; identificar alimentos incompatíveis
  com alergias/restrições; alertar quando um cardápio possuir alimento incompatível.

## Requisitos funcionais

- RF1: a análise é derivada e não persistida: recebe cardápio, seus itens, os
  vínculos de cada alimento e a lista de alunos da unidade com suas restrições.
- RF2: uma incompatibilidade é **bloqueante** quando o vínculo do alimento é
  `Alergenico` e a restrição correspondente pertence a pelo menos um aluno da
  unidade. É **alerta** quando o vínculo é `Restricao`.
- RF3: a comparação de restrições usa a normalização única de
  `RestricaoAlimentar.Normalizar`; nenhuma segunda normalização é criada (L9).
- RF4: `GET /api/cardapios/{id}/compatibilidade` devolve:
  `compativel` (falso se houver qualquer bloqueante), `bloqueantes[]`, `alertas[]`,
  `alimentosPermitidos[]`, `alimentosNaoPermitidos[]` e `totalAlunosAnalisados`.
- RF5: cada achado identifica refeição, alimento, restrição, severidade e os alunos
  afetados (`id` e `nome`); alunos são ordenados por nome e alimentos por refeição e
  nome, para resposta determinística.
- RF6: cardápio sem alunos na unidade devolve `compativel = true`,
  `totalAlunosAnalisados = 0` e um aviso explícito de que não houve análise.
- RF7: alimento sem vínculo é `permitido` para todas as restrições; alimento com
  vínculo cuja restrição não existe em nenhum aluno é `permitido` para o grupo.
- RF8: a rota é acessível a `Nutricionista`, `Merendeira` e `AdminEscola` da unidade
  do cardápio; `Responsavel` não acessa a visão agregada (vê apenas o próprio aluno).
- RF9: o frontend exibe contagem de bloqueantes e alertas no detalhe e na listagem,
  com destaque visual; "publicar" fica desabilitado quando há bloqueante (Spec 015).
- RF10: o serviço é reutilizável pela geração automática (Spec 013) e pela
  publicação (Spec 015) sem duplicar a regra.

## Requisitos não funcionais

- RNF1: função pura de domínio, sem EF, sem relógio, sem I/O; recebe dados já
  carregados.
- RNF2: complexidade linear em (itens × vínculos) + (alunos × restrições); uma
  consulta por agregado no serviço de aplicação.
- RNF3: resultado determinístico para a mesma entrada (ordenação com desempate).
- RNF4: nenhuma regra no controller; o controller apenas monta a entrada e devolve o
  resultado.
- RNF5: contrato `{ erro }` para 400/403/404.

## Restrições

- Sem alteração de schema nesta spec.
- Não alterar os modelos de `Cardapio`, `Alimento` ou `Aluno`; apenas ler.
- Sem dependência nova no frontend.

## Dependências

- Specs 009, 010, 011 e 012 implementadas.
- Spec 015 consome o resultado; implementar 014 antes de 015.
- Spec 013 consome o resultado; implementar 014 antes de 013.

## Casos de borda

- Cardápio sem itens → `compativel = true`, listas vazias, sem erro.
- Aluno sem restrições → não gera achado.
- Dois alunos com a mesma restrição e o mesmo alimento → um achado com dois alunos.
- Mesmo alimento em duas refeições → dois achados (um por refeição).
- Restrição desativada no catálogo → continua valendo para o aluno que a possui.
- Cardápio de unidade sem alunos → aviso de ausência de análise.
- Aluno sem unidade (legado) → não participa da análise; contagem registra apenas os
  alunos da unidade.
- Bloqueante em qualquer item → `compativel = false`, mesmo com alertas presentes.

## Critérios de aceitação

- [ ] Alimento com vínculo `Alergenico` para restrição de aluno da unidade produz
      bloqueante.
- [ ] Alimento com vínculo `Restricao` produz alerta, não bloqueante.
- [ ] Alimento sem vínculo aparece em `alimentosPermitidos`.
- [ ] Dois alunos com a mesma restrição aparecem no mesmo achado.
- [ ] Refeições diferentes geram achados distintos para o mesmo alimento.
- [ ] Unidade sem alunos devolve `compativel = true` com aviso.
- [ ] `Merendeira` de outra unidade recebe 403.
- [ ] `Responsavel` recebe 403.
- [ ] Resultado é idêntico em duas chamadas consecutivas (ordenação estável).
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Montar cardápio com um alimento alergênico e um restrito, ter dois alunos com essas
restrições e conferir bloqueantes, alertas, listas de permitidos e a matriz de
acesso. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
