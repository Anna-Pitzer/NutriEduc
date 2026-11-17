---
name: funcionalidades-permissoes-merendeira
status: audited
references: V26; V14; V17; Spec 009, Spec 012, Spec 015, Spec 016, Spec 017, Spec 018, Spec 025, Spec 027, Spec 028, Spec 029, Spec 031, Spec 033; ADR-002, ADR-004, ADR-020, ADR-021
---

# Requirements — Funcionalidades e Permissões do Perfil de Merendeira

## Objetivo

Corrigir e disponibilizar as funcionalidades do perfil **Merendeira**, respeitando o vínculo de
cada profissional com a sua escola: visualizar apenas os cardápios da própria escola, visualizar
e criar solicitações vinculadas automaticamente à escola e ao usuário autenticado, e receber os
feedbacks destinados ao seu perfil/escola. O isolamento entre escolas deve valer no backend e no
frontend, inclusive contra requisições diretas à API.

## Diagnóstico (por que as funcionalidades não aparecem ou não funcionam)

A análise do código em 2026-11-17 mostra que **a autorização no backend já cobre o papel
Merendeira** (matriz, escopo por escola e derivação automática de escola na criação de
solicitação/feedback), mas há falhas de **navegação no frontend** e uma **invariante de escopo
implícita e não testada** que impedem o uso real.

1. **Navegação ausente (causa principal).** O componente `Navbar` — que contém os itens de menu
   da merendeira (Cardápio, Solicitações, Feedback, Ocorrências, Catálogo de alimentos) — só é
   montado em `HomeNutricao.tsx` (nutricionista) e `layoutSecretaria.tsx` (secretaria). A página
   inicial da merendeira, `HomeMerendeiras.tsx` (`/merendeiras`), monta apenas `Header` +
   `AlertaAlunosIndisponiveis` + `Footer`, **sem `Navbar`**. A merendeira cai numa tela sem menu e
   não alcança as telas de Cardápio/Solicitações/Feedback/Ocorrências, embora as rotas existam em
   `routes.tsx` e o backend as autorize. As páginas de destino (`SolicitacoesPage`,
   `FeedbackPage`, `OcorrenciasPage`) também não montam `Header`/`Navbar`, então não há
   navegação de volta.

2. **Invariante de escopo implícita (risco de 403).** `Usuario.DefinirPapel(Merendeira, …)` só
   exige `escolaId`; o `UnidadeId` só fica preenchido porque os dois caminhos de criação
   (`AprovarAcessoService.Aprovar` e `UsuarioService` via `ResolucaoEscopo`) passam
   `escola.UnidadeId`. Porém `CardapioService`, `SolicitacaoService` e `FeedbackService` exigem
   `escopo.UnidadeId` **e** `escopo.EscolaId` para Merendeira. Uma merendeira criada/importada sem
   `UnidadeId` (caminho legado ou edição que só grava escola) recebe 403 em todas as leituras e
   escritas escopadas. Não há teste de conformidade que garanta “Merendeira sempre tem
   `EscolaId` + `UnidadeId` derivada da escola”.

3. **Higiene de guarda de rota (defesa em profundidade).** Em `routes.tsx`, as rotas
   `/alimentos/novo` e `/alimentos/editar/:id` estão sob `ProtegidaPorPapel(["Nutricionista",
   "Merendeira"])`, mas a matriz dá `R` (somente leitura) à Merendeira em `CatalogoDeAlimentos`.
   O backend devolve 403 em `POST`/`PUT`, mas a UI de escrita não deveria ser exposta à merendeira.

4. **Lacuna funcional herdada (PEND-025).** A merendeira não tem tela de leitura do
   **histórico/trilha de restrições de um aluno específico** — só a contagem agregada por item no
   painel. A API `GET /api/aluno/{id}/restricoes/historico` já responde 200 para a merendeira da
   unidade, mas nenhuma tela a consome.

### O que já funciona (reuso — não reimplementar)

- **Matriz (Spec 028):** `CardapioGeral` R, `CardapioAdaptado` R, `PublicacaoDeCardapio` R,
  `Solicitacao` RC, `Feedback` RC, `Ocorrencia` RC, `Notificacao` R, `Painel` R,
  `CatalogoDeAlimentos` R, `CatalogoDeRestricoes` R, `Aluno`/`RestricaoDoAluno`/`HistoricoDoAluno` R.
- **Cardápio:** `CardapioService.Listar/Obter/ListarAdaptados` escopam a merendeira (geral da
  unidade + versão adaptada da própria escola via `EscolaVisivelId`, Spec 029 RF16).
- **Solicitação:** `SolicitacaoService.Listar/Criar` filtram por unidade+escola, excluem
  `DadosCadastrais`; `Criar` deriva a escola de `escopo.EscolaId` (o DTO **não** tem campo de
  escola/unidade), e `NotificarNovaSolicitacao` encaminha para a Nutricionista (cardápio/ingrediente).
- **Feedback:** `FeedbackService.Listar/Criar` filtram por unidade+escola e derivam
  unidade/escola da sessão.
- **Frontend:** `SolicitacoesPage` já tem `tiposDeCriacao` para Merendeira (`MudancaCardapio`,
  `Ingrediente`, `FaltaIngrediente`); `FeedbackPage` já habilita criação para Merendeira;
  `ListarCardapios` já oculta ações de escrita e escopa por unidade.

## Escopo

- Frontend: área da merendeira com navegação (Navbar/layout), correção da guarda de rota de
  alimentos, e (opcional) tela de leitura da trilha de restrições por aluno (PEND-025).
- Backend: garantir por teste a invariante de escopo da merendeira (derivação de `UnidadeId` a
  partir da escola, já existente); nenhuma mudança de matriz, de regra de negócio nem de código
  de produção.
- Banco: nenhuma mudança estrutural — a modelagem já guarda `EscolaId` e `UnidadeId` em `Usuario`.
- Fora de escopo: alterar a matriz de permissões, criar novos papéis, novos tipos de solicitação,
  ou migração de provedor de banco.

## Requisitos funcionais

- **RF1 — Visualizar cardápios da própria escola.** A merendeira lista e abre apenas os cardápios
  disponibilizados para a sua escola: o geral da sua unidade e as versões adaptadas da própria
  escola, nunca de outra escola. A listagem exibe período (datas), refeições e itens/alimentos de
  cada cardápio, e o histórico/feed de alterações no escopo (Spec 015 RF11).
- **RF2 — Visualizar solicitações da própria escola.** A merendeira lista e abre apenas as
  solicitações cuja `escolaId` é a sua, com detalhes, datas e status. Solicitações de outra escola
  e solicitações `DadosCadastrais` ficam fora da visão.
- **RF3 — Realizar solicitações.** A merendeira cria solicitações de `MudancaCardapio`,
  `Ingrediente` e `FaltaIngrediente`. A escola de destino é sempre a da merendeira autenticada e o
  responsável (`SolicitanteId`/`SolicitanteNome`) é o usuário da sessão, sem escolha manual.
- **RF4 — Encaminhamento automático.** A solicitação criada pela merendeira notifica, na mesma
  transação, o(s) Nutricionista(s) ativo(s) da unidade (dono da decisão dos tipos de
  cardápio/ingrediente).
- **RF5 — Receber feedback.** A merendeira visualiza os feedbacks correspondentes ao seu
  perfil/escola (com comentário e resposta) e pode criar feedbacks destinados à nutrição, sempre
  no escopo da própria unidade+escola.
- **RF6 — Isolamento também por requisição direta.** Qualquer acesso a cardápio/solicitação/
  feedback de outra escola responde 403 (mesma unidade, outra escola) ou 404 (outra unidade),
  sem vazar existência entre escolas; criação forjando outra escola é impossível porque o DTO não
  expõe `escolaId`/`unidadeId`.

## Requisitos não funcionais

- **RNF1 — Negação por padrão.** A matriz continua sendo a fonte única e executável; nenhum
  recurso novo ganha permissão sem declaração explícita (Spec 028 RNF1).
- **RNF2 — Escopo decidido no serviço, nunca no controller/payload.** Os filtros de escola/unidade
  são derivados de `EscopoUsuario` e aplicados no banco (Spec 024 F5; Spec 027 RF13).
- **RNF3 — Sem migração e sem dependência nova.** A entrega não altera o schema nem adiciona
  pacotes.
- **RNF4 — Gates verdes.** `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Regras de negócio e permissões

- A merendeira **lê** `CardapioGeral`, `CardapioAdaptado`, `PublicacaoDeCardapio`,
  `CatalogoDeAlimentos`, `CatalogoDeRestricoes`, `Aluno`, `RestricaoDoAluno`, `HistoricoDoAluno`,
  `Notificacao` e `Painel`.
- A merendeira **lê e cria** `Solicitacao` (não decide, não edita registro existente) e `Feedback`
  (cria, não responde — resposta é de Nutricionista/AdminEscola) e `Ocorrencia` (RC).
- O escopo da merendeira é a **escola** (`EscolaId`); a unidade é derivada da escola
  (`escola.UnidadeId`). Cardápio geral pertence à unidade; versão adaptada pertence à escola.
- Toda solicitação da merendeira é gravada com `EscolaId` e `UnidadeId` derivados da sessão e com
  `SolicitanteId`/`SolicitanteNome` do usuário autenticado.
- A escola de destino **não** pode ser escolhida nem alterada manualmente (o contrato de criação
  não expõe escola/unidade).

## Dependências

- **Visão:** V26 (valor entregue por esta spec), V14 (cada papel vê o seu) e V17 (isolamento
  verificável) são a origem de valor; cada RF deriva de uma dessas.
- Spec 009 (papéis e vínculo escola), Spec 012/015/029 (cardápio, publicação, geral/adaptado),
  Spec 016/017/018 (ocorrências, solicitações, feedback), Spec 025 (painel por papel),
  Spec 027 (hierarquia unidade/escola/aluno), Spec 028 (matriz e isolamento), Spec 031/033
  (criação de usuário e vínculo de escola/unidade).
- ADR-002 (papéis e escopo), ADR-004 (invariantes no domínio), ADR-020/ADR-021 (escopo e matriz).

## Casos de borda

- Merendeira sem `EscolaId` na sessão → 401/403, nunca lista tudo.
- Merendeira com `EscolaId` mas sem `UnidadeId` (dado legado inconsistente) → nunca amplia
  escopo; como a derivação é garantida por `ResolucaoEscopo` nos três caminhos de criação, esse
  estado não é produzido por código novo e fica fora do escopo desta spec.
- Solicitação `MudancaCardapio` para cardápio de outra unidade → 404 “Cardápio não encontrado”.
- Solicitação de cardápio da própria unidade, mas com `escolaId` forjado → ignorado; escola = da
  sessão.
- `GET /api/solicitacoes` com `escolaId` de outra escola → 403; com `unidadeId` de outra unidade → 403.
- `GET /api/cardapios` para merendeira retorna geral da unidade + adaptados da própria escola,
  nunca adaptados de outra escola.

## Critérios de aceitação

- [ ] A merendeira acessa, a partir da própria área, um menu que leva a Cardápios, Solicitações,
  Feedback e Ocorrências (Navbar/layout presente na área da merendeira).
- [ ] A merendeira visualiza os cardápios da própria escola (datas, refeições e alimentos) e não
  vê cardápios de outras escolas.
- [ ] A merendeira visualiza as solicitações da própria escola (detalhes, datas e status) e não vê
  solicitações de outras escolas.
- [ ] A merendeira cria solicitações de `MudancaCardapio`, `Ingrediente` e `FaltaIngrediente`
  destinadas à própria escola; a escola é preenchida automaticamente.
- [ ] A merendeira não consegue enviar solicitação para outra escola (UI não expõe escola; API
  ignora/recusa escola forjada).
- [ ] A solicitação criada notifica a Nutricionista responsável.
- [ ] A merendeira recebe e visualiza os feedbacks do seu perfil/escola.
- [ ] Requisições diretas à API com outra escola/unidade retornam 403/404, sem vazar dados de
  outra escola.
- [ ] A guarda de `/alimentos/novo` e `/alimentos/editar/:id` deixa de expor escrita à merendeira.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Com uma base fixa (duas escolas na mesma unidade, cada uma com uma merendeira; um cardápio geral
publicado e uma versão adaptada por escola; solicitações e feedbacks de ambas as escolas),
percorrer: login da merendeira A → menu/navegação presente; cardápios listados contêm só o geral
da unidade e os adaptados da escola A; solicitações listadas contêm só a escola A; criar
`Ingrediente`/`FaltaIngrediente`/`MudancaCardapio` e conferir `escolaId` e `solicitante` gravados;
feedback criado aparece para a merendeira A e para a nutrição; trocar para a merendeira B e
confirmar que nada da escola A aparece. Forçar pela API (curl/ferramenta) uma solicitação com
escola de outra escola e conferir 403/404. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
