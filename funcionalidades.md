# Funcionalidades — NutriEduc

Catálogo do que cada perfil de usuário consegue fazer no sistema hoje, com a tela,
a rota de API e a spec de origem.

**Fonte dos dados:** `backend/Application/Seguranca/MatrizDePermissoes.cs` (matriz executável),
`backend/API/Services/` (regras efetivas), `frontend/src/routes/routes.tsx` e
`frontend/src/components/Navbar.tsx` (telas e menus), tabela de endpoints do `README.md`.
Levantado em **2026-11-17**.

**Como ler:** cada perfil tem três blocos — *telas*, *o que pode fazer* e *limites*.
A coluna "Spec" indica qual documento de `management/specs/` entregou a funcionalidade.
O `roadmap.md` é a fonte de verdade do status das specs; este arquivo descreve
comportamento observado no código.

---

## 1. Perfis e escopo de dados

`PapelUsuario` tem cinco valores (`backend/Domain/Entities/PapelUsuario.cs`):

| Papel | Escopo de dados | Área inicial |
| --- | --- | --- |
| `AdminSecretaria` | Toda a unidade da sessão | `/secretaria` |
| `Nutricionista` | Toda a unidade da sessão | `/` (HomeNutricao) |
| `AdminEscola` | Apenas a própria escola | `/escolas` |
| `Merendeira` | A escola da sessão; unidade derivada dela | `/merendeiras` |
| `Responsavel` | Apenas os alunos vinculados | `/pais` |

**Isolamento:** recurso de outra unidade responde `404` (não revela existência); filtro de
`escolaId`/`unidadeId` fora do alcance da sessão responde `403` (Spec 028).

---

## 2. Matriz de permissões (referência)

Níveis: `N` nenhum · `R` leitura · `W` escrita · `RC` ler+criar · `RW` ler+escrever ·
`S` solicitar (escrita só via solicitação, nunca direta). Extraído de
`MatrizDePermissoes.Documentada` (Spec 028).

| Recurso | AdminSecretaria | AdminEscola | Nutricionista | Merendeira | Responsavel |
| --- | --- | --- | --- | --- | --- |
| Unidade | RW | R | R | R | N |
| Escola | RW | R | R | R | N |
| Conta de usuário | RW | R | N | N | N |
| Papel do usuário | W | N | N | N | N |
| Perfil próprio | RW | RW | RW | RW | RW |
| Vínculo responsável↔aluno | RW | R | N | N | R |
| Aluno | RW | R | R | R | S |
| Restrição do aluno | RW | R | R | R | S |
| Histórico do aluno | R | R | R | R | R |
| Catálogo de alimentos | R | R | RW | R | N |
| Catálogo de restrições | R | R | RW | R | R |
| Cardápio geral | R | R | RW | R | R |
| Cardápio adaptado | R | R | RW | R | R |
| Parâmetro de geração | R | N | RW | N | N |
| Publicação de cardápio | R | R | RW | R | N |
| Ocorrência | R | RW | R | RC | RC |
| Solicitação | RW | RC | RW | RC | RC |
| Feedback | R | RW | RW | RC | RC |
| Notificação | R | R | R | R | R |
| Relatório | R | R | R | N | N |
| Painel | R | R | R | R | N |

> A matriz é a referência declarada, mas alguns serviços aplicam recorte adicional —
> ver §10 *Divergências conhecidas*.

---

## 3. Público (sem sessão)

| Tela | Rota | API | Spec |
| --- | --- | --- | --- |
| Login | `/login` | `POST /api/usuario/login` | 002/006 |
| Autocadastro com intenção | `/login` (formulário) | `POST /api/usuario/registrar` | 033 |
| Bootstrap do primeiro admin | `/bootstrap` | `GET /api/bootstrap/status`, `POST /api/bootstrap/admin` | 022 |

Também abertos: `GET /api/unidades/ativas` (id e nome, para o autocadastro) e
`GET /api/cardapios/publicados?unidadeId=` (vitrine de cardápios publicados na data).

O autocadastro **não** dá acesso imediato: cria conta sem papel e uma
`SolicitacaoDeAcesso` pendente; a conta só autentica após aprovação do `AdminSecretaria`
da unidade (Spec 033).

---

## 4. Comum a todos os perfis autenticados

| Tela | Rota | API | Spec |
| --- | --- | --- | --- |
| Perfil próprio | `/perfil` | `GET /api/usuario/me`, `GET/PUT /api/usuario/perfil/{id}` | 006/031 |
| Termos de uso | `/termosdeuso` | — | 008 |
| Política de privacidade | `/politicadeprivacidade` | — | 008 |
| Suporte | `/suporte` | — | 008 |
| Notificações | (sino na `Navbar`) | `GET /api/notificacoes`, `POST /api/notificacoes/{id}/ler`, `POST /api/notificacoes/ler-todas` | 018 |
| Solicitações | `/solicitacoes` | ver §9 | 017 |

O nutricionista pode informar o próprio CRN em `PUT /api/usuario/perfil/{id}`; papel e
escopo só mudam pelo `AdminSecretaria` (Spec 031).

**Notificações** cobrem estes eventos (`TipoNotificacao`): cardápio publicado e alterado,
mudança de status de solicitação e de ocorrência, aluno atualizado, feedback respondido,
nova solicitação, nova ocorrência registrada e nova solicitação de acesso. Toda listagem é
do próprio usuário e o campo `total` serve de contador de não lidas (Spec 018).

---

## 5. AdminSecretaria

### Telas

`/secretaria` (painel), `/secretaria/escolas`, `/secretaria/unidades`,
`/secretaria/usuarios`, `/secretaria/solicitacoes-acesso`, `/cardapios` e
`/cardapios/:id` (leitura), `/aluno/registrar`, `/aluno/vizualizar`,
`/alunos/editar/:id`, `/solicitacoes`, `/ocorrencias` (leitura), `/feedbacks` (leitura),
`/relatorios`.

### O que pode fazer

| Funcionalidade | API | Spec |
| --- | --- | --- |
| Cadastrar aluno (com restrições e escola obrigatória) | `POST /api/aluno/registrar` | 001/010/027 |
| Editar aluno e transferir de escola na mesma unidade | `PUT /api/aluno/{id}`, `POST /api/aluno/{id}/transferir-escola` | 001/027 |
| Excluir aluno | `DELETE /api/aluno/{id}` | 001 |
| Consultar trilha de alterações do aluno | `GET /api/aluno/{id}/historico` | 023 |
| Criar/editar/ativar/desativar/excluir usuários; definir papel e escopo | `/api/usuarios*` | 009/031/033 |
| Listar contas e diagnosticar CRN inválido | `GET /api/usuarios`, `GET /api/usuarios/diagnostico-crn` | 031 |
| Vincular/desvincular responsável a aluno | `POST/DELETE /api/aluno/{alunoId}/responsaveis/{usuarioId}` | 034 |
| Criar, renomear, desativar, reativar e excluir escolas e unidades | `/api/escolas*`, `/api/unidades*` | 009/027 |
| Emitir token de cadastro de 30 min da unidade | `GET/POST /api/bootstrap/token` | 022 |
| Diagnosticar hierarquia inconsistente (leitura) | `GET /api/diagnostico/hierarquia` | 027 |
| Aprovar ou recusar pedidos de acesso (fila da unidade) | `/api/solicitacoes-acesso*` | 033 |
| Analisar, aprovar, recusar e concluir solicitações | `POST /api/solicitacoes/{id}/{analise,aprovar,recusar,concluir}` | 017/030 |
| Marcar anafilaxia em pedido de inclusão de alergia | `POST /api/solicitacoes/{id}/anafilaxia` | 035 |
| Ver painel da unidade (contagens) | `GET /api/painel/secretaria` | 025 |
| Gerar relatórios e exportar XLSX | `/api/relatorios/{tipo}/*` | 019 |
| Ler cardápios, ocorrências e feedbacks da unidade | rotas de leitura correspondentes | 012/016/018 |

### Limites

- **Não escreve cardápio, alimento, categoria de alimento nem restrição** — a matriz dá
  apenas `R` nesses recursos; a escrita é exclusiva do `Nutricionista`.
- Não registra ocorrência nem muda status de ocorrência.
- Não responde feedback (apenas lê).

---

## 6. Nutricionista

### Telas

`/` (HomeNutricao), `/aluno/vizualizar`, `/alunos/editar/:id`, `/cardapios`,
`/cardapios/:id`, `/cardapios/novo`, `/cardapios/:id/editar`,
`/cardapios/parametros-geracao`, `/cardapios/alteracoes`, `/alimentos`,
`/alimentos/novo`, `/alimentos/editar/:id`, `/aluno/registrar` (ver §10),
`/solicitacoes`, `/ocorrencias` (leitura), `/feedbacks`, `/relatorios`.

### O que pode fazer

| Funcionalidade | API | Spec |
| --- | --- | --- |
| Manter catálogo de alimentos (composição, categoria, vínculos, soft delete) | `/api/alimentos*`, `/api/categorias-alimento*` | 011 |
| Manter catálogo de restrições (alergia/restrição, desativar/reativar) | `/api/restricoes*` (escrita) | 010 |
| Criar cardápio geral + uma versão adaptada por `(escola, conjunto)` na mesma transação | `POST /api/cardapios` | 029 |
| Adaptar um geral para uma escola (idempotente, teto de 50 versões) | `POST /api/cardapios/{id}/adaptar` | 029 |
| Ver contexto de restrições por escola; analisar compatibilidade | `GET /api/cardapios/contexto-restricoes`, `GET /api/cardapios/{id}/compatibilidade` | 010/029 |
| Editar, duplicar e excluir rascunho | `PUT/DELETE /api/cardapios/{id}`, `POST /api/cardapios/{id}/duplicar` | 012/015 |
| Publicar, cancelar com motivo e criar revisão | `POST /api/cardapios/{id}/{publicar,cancelar,revisao}` | 015 |
| Gerar cardápio automático (geral + adaptados por escola) | `POST /api/cardapios/gerar` | 013/029/032 |
| Ler/gravar parâmetros de geração da unidade | `GET/PUT /api/unidades/{id}/parametros-geracao` | 013 |
| Histórico e feed de alterações do cardápio | `GET /api/cardapios/{id}/historico`, `GET /api/cardapios/alteracoes` | 015 |
| Listar nutricionistas da unidade com CRN | `GET /api/nutricionistas` | 031 |
| Decidir solicitação diretamente (aprovar/recusar) | `POST /api/solicitacoes/{id}/{aprovar,recusar}` | 017/038 |
| Responder feedback | `POST /api/feedbacks/{id}/responder` | 018 |
| Gerar relatórios e exportar XLSX | `/api/relatorios/{tipo}/*` | 019 |
| Ler alunos, histórico e cardápio servido ao aluno | rotas de leitura | 001/023/029 |

Publicar exige **CRN válido** (Spec 031) e bloqueia por incompatibilidade (`422`),
sobreposição de período (`409`) e versão adaptada pendente ou desatualizada (`409`) —
Specs 014/015/029.

### Limites

- **Ocorrências são somente leitura** para o nutricionista: não registra, não muda status,
  não cancela (`403`) — Spec 038.
- **Não cria solicitação** e **não usa análise nem conclusão**: decide direto de `Aberta`,
  com a análise registrada no histórico no mesmo ato (Specs 017/038).
- Não marca anafilaxia (ver §10).
- Não escreve aluno: a matriz dá `R` em `Aluno`, a escrita é do `AdminSecretaria`.
- Não administra usuários, papéis ou vínculos.

---

## 7. Merendeira

### Telas

`/merendeiras` (home com `Navbar` e 4 abas — Spec 039), `/cardapios`, `/cardapios/:id`,
`/cardapios/alteracoes`, `/solicitacoes`, `/ocorrencias`, `/feedbacks`.

A unidade da merendeira é **derivada da escola** da sessão (Spec 039); o DTO de solicitação
não expõe `escolaId`/`unidadeId`.

### O que pode fazer

| Funcionalidade | API | Spec |
| --- | --- | --- |
| Ver o cardápio geral do dia da unidade e a contagem de alunos com restrição por item | `GET /api/painel/merendeira` | 025 |
| Consultar cardápios, detalhe, histórico e feed de alterações da unidade | `GET /api/cardapios*` | 012/015 |
| Analisar compatibilidade do cardápio com as restrições | `GET /api/cardapios/{id}/compatibilidade` | 014 |
| Abrir solicitações (mudança de cardápio, ingrediente, falta de ingrediente) e acompanhar | `GET/POST /api/solicitacoes`, `POST /api/solicitacoes/{id}/cancelar` | 017 |
| Registrar ocorrência e cancelar a própria ocorrência no mesmo dia civil | `POST /api/ocorrencias`, `POST /api/ocorrencias/{id}/cancelar` | 016 |
| Enviar feedback e encerrar o próprio feedback respondido | `POST /api/feedbacks`, `POST /api/feedbacks/{id}/encerrar` | 018 |

### Limites

- Não escreve cardápio, alimento nem restrição (matriz `R`).
- **Não muda status de ocorrência** — só `AdminEscola` da escola faz isso (Specs 016/028).
- Não responde feedback (responder é `Nutricionista` ou `AdminEscola`).
- Sem acesso a relatórios, usuários, vínculos e painel de outras áreas.
- Sem tela de catálogo de alimentos: a API permite leitura, mas `/alimentos` é rota da
  nutrição (Spec 039).

---

## 8. AdminEscola

### Telas

`/escolas` (home), `/solicitacoes`, `/ocorrencias`, `/feedbacks`, `/relatorios`,
`/cardapios` e `/cardapios/:id` (leitura).

### O que pode fazer

| Funcionalidade | API | Spec |
| --- | --- | --- |
| Registrar ocorrência da escola e mudar seu status | `POST /api/ocorrencias`, `POST /api/ocorrencias/{id}/status` | 016 |
| Abrir solicitações e acompanhar | `GET/POST /api/solicitacoes` | 017 |
| Responder feedback da escola | `POST /api/feedbacks/{id}/responder` | 018 |
| Gerar relatórios da escola e exportar XLSX | `/api/relatorios/{tipo}/*` | 019 |
| Ler usuários da própria escola | `GET /api/usuarios` | 033 |
| Ler vínculos responsável↔aluno | `GET /api/aluno/{alunoId}/responsaveis` | 034 |
| Ler cardápios, alunos e histórico no escopo da escola | rotas de leitura | 012/001/023 |

### Limites

- Não cria, edita ou exclui escola, usuário ou vínculo (escrita é do `AdminSecretaria`).
- Não escreve cardápio nem catálogos.
- Não aprova pedidos de acesso.
- **Não pode** mudar status de ocorrência de outra escola (escopo por escola).

---

## 9. Responsavel

### Telas

`/pais` (painel), `/pais/cardapios`, `/pais/ocorrencias`, `/pais/ocorrencias/nova`,
`/pais/solicitacoes`, `/pais/feedback`, `/pais/relatorio`.

### O que pode fazer

| Funcionalidade | API | Spec |
| --- | --- | --- |
| Ver os filhos vinculados, com escola, unidade, restrições e cardápio do dia | `GET /api/painel/familia` | 025 |
| Relatório do filho: restrições vigentes, alterações e ocorrências (padrão 30 dias, máx. 366) | `GET /api/painel/familia/{alunoId}/relatorio` | 025 |
| Ver o cardápio servido ao filho (geral e adaptado) na data | `GET /api/aluno/{id}/cardapio` | 029/035 |
| Registrar ocorrência do filho (tipo `Aluno`) e cancelar a própria no mesmo dia | `POST /api/ocorrencias*` | 016/035 |
| Pedir inclusão de alergia ou alteração de restrição, informando anafilaxia | `POST /api/solicitacoes` | 017/035 |
| Marcar anafilaxia no próprio pedido de alergia | `POST /api/solicitacoes/{id}/anafilaxia` | 035 |
| Pedir alteração de dados cadastrais do filho | `POST /api/solicitacoes` (`DadosCadastrais`) | 030 |
| Enviar feedback associado ao filho | `POST /api/feedbacks` | 018/035 |
| Ler histórico do filho | `GET /api/aluno/{id}/historico` | 023 |

### Limites

- Escrita em aluno e restrição é sempre **via solicitação** (matriz `S`); o cadastro só
  muda após aprovação do `AdminSecretaria`.
- Não acessa relatórios de gestão (`/relatorios`) nem dados de alunos não vinculados.
- Não muda status de ocorrência.
- Aluno fora do vínculo responde `403` sem revelar existência.

---

## 10. Divergências conhecidas (documentadas no projeto)

Registradas aqui porque afetam o que o usuário vê versus o que o backend aceita:

1. **Cardápio para responsável (PEND-048).** A matriz dá `R` a `Responsavel` em
   `CardapioGeral`/`CardapioAdaptado`, mas `CardapioService.PodeLer` devolve `403` em
   `GET /api/cardapios`. O acesso real do responsável é `GET /api/aluno/{id}/cardapio`.
2. **Menu de cadastro de aluno para nutricionista.** `/aluno/registrar` está liberado para
   `Nutricionista` na guarda de rota e no `Navbar`, mas a matriz dá `R` em `Aluno`: a tela
   abre e a gravação é recusada.
3. **Anafilaxia.** O `README.md` (seção de solicitações) indica `Nutricionista` da unidade
   como autorizado; em `SolicitacaoService.MarcarAnafilaxia` apenas `Responsavel` (do próprio
   pedido) e `AdminSecretaria` (da unidade) passam — os demais recebem `403`.
4. **Painel por papel.** A matriz dá `R` em `Painel` para quatro papéis, mas existem só três
   rotas: `secretaria`, `merendeira` e `familia`.
5. **Frontend sem testes automatizados (PEND-049).** As telas são validadas por
   `npm run build` e `npm run lint`; não há teste de componente ou de fluxo.

---

## 11. Recursos transversais

| Recurso | Descrição | Specs |
| --- | --- | --- |
| Relatórios | Tipos `Cardapios`, `AlunosRestricoes`, `Ocorrencias`, `SolicitacoesFeedbacks`, `IndicadoresAdministrativos`; prévia de 200 linhas e exportação XLSX (limite 50.000 linhas → `413`); janela padrão de 30 dias | 019 |
| Trilha do aluno | `HistoricoAluno` append-only com autor e data; criação, campos alterados e restrições | 023 |
| Histórico do cardápio | `HistoricoCardapio` com eventos, versão e feed de alterações | 015 |
| Notificações | Por evento, com leitura individual e em lote | 018 |
| Matriz de permissões | Fonte executável da autorização, com teste de concordância entre tabela documentada e nível | 028 |
| Bootstrap | Primeiro admin por token de ambiente ou token temporário de 30 min por unidade | 022 |
