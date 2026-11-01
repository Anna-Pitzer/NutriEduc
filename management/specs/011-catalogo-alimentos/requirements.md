---
name: catalogo-alimentos
status: created
references: V6, ADR-004, L2, L9
---

# Requirements — Catálogo de alimentos

## Objetivo

Não existe domínio de alimento no backend, embora `frontend/src/services/CardapioApi.ts`
já chame `/api/alimentos` e `MontarCardapio.tsx` opere sobre uma lista falsa. Sem alimento
não há composição nutricional, alergênicos nem vínculo com restrições — e o cálculo
nutricional do cardápio (Spec 012) e o alerta de incompatibilidade (Spec 014) não têm
sobre o que operar.

## Escopo

- Backend: categorias, alimentos, valores nutricionais por 100 g e vínculos com
  restrições; CRUD, desativação e pesquisa paginada.
- Frontend: tela de catálogo em `pages/nutricao/alimentos/`, substituindo a chamada
  órfã de `CardapioApi.ts` e os dados falsos de `MontarCardapio.tsx`.
- Fora de escopo: cardápio (Spec 012), cálculo por refeição (Spec 012), verificação de
  compatibilidade (Spec 014), importação de tabelas nutricionais externas, medidas
  caseiras (colher, porção) e volume em ml.

### Cobertura de `implementacoes.md`

- **Nutrição (10):** cadastrar, editar, desativar, pesquisar e categorizar
  alimentos; cadastrar composição e valores nutricionais; cadastrar alergênicos;
  relacionar alimentos às restrições; definir alimentos permitidos/não permitidos.

## Requisitos funcionais

- RF1: `CategoriaAlimento` tem nome único (2–60, sem diferenciar caixa) e estado ativo;
  `Nutricionista` cria, edita, desativa e reativa. Categoria desativada não recebe
  alimento novo nem aceita que um alimento existente seja movido para ela.
- RF2: `Alimento` tem nome (2–100), `NomeNormalizado` único **global**, categoria,
  estado ativo e `ValoresNutricionais` obrigatórios.
- RF3: `ValoresNutricionais` é um objeto de valor com seis medidas por 100 g:
  `CaloriasKcal`, `ProteinasG`, `CarboidratosG`, `GordurasG`, `FibrasG`, `SodioMg`.
  Todas obrigatórias, `>= 0`, com até duas casas decimais; valor negativo, ausente ou com
  três casas devolve 400, sem arredondamento silencioso.
- RF4: `Alimento` mantém vínculos com `RestricaoAlimentar` (Spec 010). O vínculo é
  apenas o par `(AlimentoId, RestricaoId)` — **não existe motivo próprio no vínculo**:
  a severidade é sempre a `Categoria` da restrição (`Alergia` bloqueia, `Restricao`
  alerta), e reclassificar a restrição muda a severidade de todos os alimentos
  vinculados, sem tocar nos vínculos. O mesmo par não pode aparecer duas vezes.
- RF5: alimento sem vínculo é permitido para todas as restrições; "não permitido"
  é derivado dos vínculos, nunca de um campo booleano paralelo.
- RF6: desativar alimento é soft delete (`Ativo = false`); ele continua legível e
  referenciável por cardápios já publicados (Spec 012/015), mas não pode entrar em
  cardápio novo — regra que pertence à Spec 012.
- RF7: `GET /api/alimentos` aceita `busca`, `categoriaId`, `pagina` (>= 1, padrão 1),
  `tamanho` (1–100, padrão 20) e `incluirInativos` (exclusivo de `Nutricionista`), e
  devolve `PaginaDTO<AlimentoDTO>` com `pagina`, `tamanho`, `total` e `itens`. A busca
  é por nome sobre `NomeNormalizado` (sem caixa e sem acento): **prefixo primeiro** e,
  somente quando o prefixo não devolve nenhum item, a mesma busca é repetida por
  "contém" com os demais filtros preservados.
- RF8: `GET /api/alimentos/{id}` devolve o alimento com valores nutricionais, categoria
  (id e nome) e vínculos resolvidos (id, nome e categoria da restrição).
- RF9: nenhuma rota de escrita do catálogo é acessível a `Merendeira`,
  `Responsavel`, `AdminEscola` ou `AdminSecretaria`; leitura é liberada a
  `Nutricionista` e `Merendeira`. A leitura usa a política de atributo `SessaoValida` e
  a recusa por papel acontece no serviço via `EscopoService`, devolvendo 403 — mesmo
  padrão da Spec 010 (`RestricaoService.Listar`).
- RF10: o frontend lista com filtro de categoria, busca e paginação; o formulário tem os
  seis valores e seleção múltipla de restrições (apenas `restricaoId`).
- RF11: a migration semeia seis categorias — Cereais, Leguminosas, Hortaliças, Frutas,
  Carnes e ovos, Laticínios — de forma idempotente. Nenhum alimento é semeado, porque
  valor nutricional real exige fonte externa, que está fora de escopo.

## Requisitos não funcionais

- RNF1: valores nutricionais são `decimal` no domínio e persistidos como tipo complexo
  com colunas `decimal(10,2)`. O SQLite não aplica precisão nem escala, então a garantia
  de duas casas vem da validação do domínio, nunca do banco. Nunca `double` nem `string`
  formatada, e nenhuma soma ou comparação numérica em SQL.
- RNF2: invariantes de alimento no construtor e em `AtualizarDados`, seguindo
  ADR-004; nenhuma regra no controller. Texto validado por `ValidacaoDeCampo.Texto` e
  medida numérica por uma única regra em `ValidacaoDeCampo` — não por `if` repetido.
- RNF3: a coleção de vínculos é protegida (`IReadOnlyList`), como na Spec 003.
- RNF4: consultas paginadas; nenhuma rota devolve catálogo inteiro. A regra de paginação
  tem um único dono reutilizado por `HistoricoAlunoService` e `AlimentoService`, sem
  cópia por serviço.
- RNF5: contrato de erro único `{ erro }` (ADR-005) em 400/403/404/409.

## Restrições

- Migration nova `CatalogoAlimentos` (tabelas + seed de categorias); migrations
  existentes não são reescritas.
- Restrições e alergias vêm da semente da Spec 010 e não são editáveis pela interface
  (ADR-015): esta spec apenas as referencia nos vínculos.
- Nenhuma dependência nova de frontend.
- Nomenclatura em português, conforme padrão do código.

## Dependências

- Spec 009 (papéis) para autorização.
- Spec 010 (catálogo de restrições) para os vínculos e para a `Categoria` que define a
  severidade.
- Spec 007 (contrato de erro) para 400/403/409.
- Spec 024 (padrões de DTO, `Resultado<T>`, `PaginaDTO<T>` e `EscopoService`).

## Casos de borda

- Nome duplicado com caixa ou acento diferente → 409 (unicidade global do
  `NomeNormalizado`).
- Categoria desativada no momento da gravação, ou alimento movido para categoria
  desativada → 400.
- Vínculo com restrição inexistente ou inativa → 400.
- Vínculo duplicado (mesmo par) → 400.
- Alimento de categoria desativada continua listado e legível; a listagem não o esconde.
- Busca sem resultado por prefixo cai para "contém"; sem resultado em ambas devolve
  página vazia com `total` 0.
- `busca` vazia ou ausente não dispara o fallback: lista tudo que os demais filtros
  permitem.
- Alimento desativado em cardápio publicado → continua resolvido na leitura do
  cardápio; somente novos cardápios são bloqueados.
- Valores com três casas decimais → 400, sem arredondamento silencioso.
- Alimento inexistente → 404.
- Categoria em uso não pode ser apagada; só desativada.
- Reclassificar restrição `Alergia` ↔ `Restricao` altera a severidade dos alimentos
  vinculados sem alterar nenhum vínculo.

## Critérios de aceitação

- [ ] `POST /api/alimentos` cria alimento com os seis valores e devolve 201 com o id.
- [ ] Valor negativo ou com três casas decimais devolve 400 `{ erro }` e não persiste.
- [ ] Nome duplicado (variação de caixa) devolve 409.
- [ ] `POST /api/alimentos` por `AdminEscola` devolve 403.
- [ ] `GET /api/alimentos?busca=` encontra por nome sem diferenciar caixa nem acento.
- [ ] Busca por prefixo acha "Arroz" com "arr"; "leite" acha "Leite de coco" pelo
      fallback de "contém".
- [ ] A resposta é `PaginaDTO` com `pagina`, `tamanho`, `total` e `itens`; `tamanho`
      fora de 1–100 devolve 400.
- [ ] `incluirInativos=true` por `Merendeira` devolve 403.
- [ ] Alimento desativado não aparece na listagem padrão; alimento de categoria
      desativada aparece.
- [ ] Vínculo `(alimento, restrição)` duplicado devolve 400.
- [ ] A resposta de vínculo traz `restricaoId`, `restricao` e `categoria`, e não traz
      motivo.
- [ ] A migration cria as seis categorias uma vez; reaplicar não duplica.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Criar um alimento com composição completa, duas restrições de categorias diferentes e os
vínculos. Consultar, editar, desativar e confirmar que o alimento desativado some da
listagem padrão e permanece acessível por id. Buscar "arr" (prefixo) e "leite" (fallback
de contém). Desativar uma categoria e confirmar que os alimentos dela continuam listados.
Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e
`cd frontend && npm run build && npm run lint`.
