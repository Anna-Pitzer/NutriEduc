---
name: catalogo-alimentos
status: created
references: V6, ADR-004, L2, L9
---

# Requirements — Catálogo de alimentos

## Objetivo

Não existe domínio de alimento no backend, embora `frontend/src/services/CardapioApi.ts`
já chame `/api/alimentos`. Sem alimento não há composição nutricional, alergênicos
nem vínculo com restrições — e o cálculo nutricional do cardápio (Spec 012) e o
alerta de incompatibilidade (Spec 014) não têm sobre o que operar.

## Escopo

- Backend: categorias, alimentos, valores nutricionais por 100 g, alergênicos e
  vínculo com restrições; CRUD, desativação e pesquisa.
- Frontend: tela de catálogo em `pages/nutricao/alimentos/`, substituindo a chamada
  órfã de `CardapioApi.ts`.
- Fora de escopo: cardápio (Spec 012), cálculo por refeição (Spec 012), verificação de
  compatibilidade (Spec 014), importação de tabelas nutricionais externas.

### Cobertura de `implementacoes.md`

- **Nutrição (10):** cadastrar, editar, desativar, pesquisar e categorizar
  alimentos; cadastrar composição e valores nutricionais; cadastrar alergênicos;
  relacionar alimentos às restrições; definir alimentos permitidos/não permitidos.

## Requisitos funcionais

- RF1: `CategoriaAlimento` tem nome único (2–60) e estado ativo; `Nutricionista`
  cria, edita e desativa. Categoria desativada não aceita novos alimentos.
- RF2: `Alimento` tem nome (2–100, único sem diferenciar caixa), categoria ativa,
  estado ativo e `ValoresNutricionais` obrigatórios.
- RF3: `ValoresNutricionais` é um objeto de valor com seis medidas por 100 g:
  `CaloriasKcal`, `ProteinasG`, `CarboidratosG`, `GordurasG`, `FibrasG`,
  `SodioMg`. Todas obrigatórias, `>= 0`, com até duas casas decimais; valor negativo
  ou ausente devolve 400.
- RF4: `Alimento` mantém vínculos com `RestricaoAlimentar` (Spec 010), cada um com
  `Motivo`: `Alergenico` (o alimento contém o alergênico) ou `Restricao` (o alimento
  é incompatível por decisão nutricional). O mesmo par alimento/restrição não pode
  aparecer duas vezes, mesmo com motivos diferentes.
- RF5: alimento sem vínculo é permitido para todas as restrições; "não permitido"
  é derivado dos vínculos, nunca de um campo booleano paralelo.
- RF6: desativar alimento é soft delete (`Ativo = false`); ele continua legível e
  referenciável por cardápios já publicados (Spec 012/015), mas não pode entrar em
  cardápio novo.
- RF7: `GET /api/alimentos` aceita `busca`, `categoriaId`, `pagina`, `tamanho`
  (1–100) e `incluirInativos` (restrito a `Nutricionista`); a busca é por nome,
  sem diferenciar caixa.
- RF8: `GET /api/alimentos/{id}` devolve o alimento com valores nutricionais,
  categoria e vínculos resolvidos (id e nome da restrição).
- RF9: nenhuma rota de escrita do catálogo é acessível a `Merendeira`,
  `Responsavel`, `AdminEscola` ou `AdminSecretaria`; leitura é liberada a
  `Nutricionista` e `Merendeira`.
- RF10: frontend lista com filtro de categoria e busca, formulário com os seis
  valores e seleção múltipla de restrições com o motivo.

## Requisitos não funcionais

- RNF1: valores nutricionais são `decimal(10,2)` no banco e `decimal` no domínio;
  nunca `double` nem `string` formatada.
- RNF2: invariantes de alimento no construtor e em `AtualizarDados`, seguindo
  ADR-004; nenhuma regra no controller.
- RNF3: a coleção de vínculos é protegida (`IReadOnlyList`), como na Spec 003.
- RNF4: consultas paginadas; nenhuma rota devolve catálogo inteiro sem paginação.
- RNF5: contrato de erro único `{ erro }` (ADR-005) em 400/404/409.

## Restrições

- Migration nova `CatalogoAlimentos`; migrations 001–005 não são reescritas.
- Nenhuma dependência nova de frontend.
- Nomenclatura em português, conforme padrão do código.

## Dependências

- Spec 009 (papéis) para autorização.
- Spec 010 (catálogo de restrições) para os vínculos.
- Spec 007 (contrato de erro) para 400/409.

## Casos de borda

- Nome duplicado com caixa diferente → 409.
- Categoria desativada no momento da gravação → 400.
- Vínculo com restrição inexistente ou inativa → 400.
- Vínculo duplicado (mesmo par, motivos diferentes) → 400.
- Alimento desativado em cardápio publicado → continua resolvido na leitura do
  cardápio; somente novos cardápios são bloqueados.
- Valores com três casas decimais → 400, sem arredondamento silencioso.
- Alimento inexistente → 404.
- Categoria em uso não pode ser apagada; só desativada.

## Critérios de aceitação

- [ ] `POST /api/alimentos` cria alimento com os seis valores e devolve 201 com o id.
- [ ] Valor negativo devolve 400 `{ erro }` e não persiste.
- [ ] Nome duplicado devolve 409.
- [ ] `POST /api/alimentos` por `AdminEscola` devolve 403.
- [ ] `GET /api/alimentos?busca=` encontra por nome sem diferenciar caixa.
- [ ] `incluirInativos=true` por `Merendeira` devolve 403 (ou é ignorado e retorna
      apenas ativos; a decisão é 403).
- [ ] Alimento desativado não aparece na listagem padrão.
- [ ] Vínculo `(alimento, restrição)` duplicado devolve 400.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Criar duas categorias, um alimento com composição completa, uma restrição do tipo
alergia e o vínculo. Consultar, editar, desativar e confirmar que o alimento
desativado some da listagem padrão e permanece acessível por id. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
