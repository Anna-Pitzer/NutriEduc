---
name: telas-secretaria-e-escola
status: implemented
references: V10, V6, ADR-002
---

# Tasks — Telas de secretaria

## Serviços

- [x] T1: criar `services/EscolaApi.ts` com os métodos de escola (listar, criar,
  renomear, ativar/desativar).
  - Validação: `npm run build` compila; nenhuma URL duplicada em componentes.
- [x] T2: criar `services/UsuarioAdminApi.ts` com listagem filtrada, criação, troca de
  papel e ativação.
  - Validação: filtros viram query string (`papel`, `escolaId`, `busca`).

## Componentes e telas

- [x] T3: criar `components/TabelaDeNomes.tsx` (criar, renomear inline, ativar/desativar).
- [x] T4: criar `pages/secretaria/layoutSecretaria.tsx` com Header, Navbar, abas, Footer.
- [x] T5: substituir `HomeSecretaria` (era `return null`) por painel com atalhos e totais.
- [x] T6: criar `pages/secretaria/Escolas.tsx`.
- [x] T8: criar `pages/secretaria/Usuarios.tsx` com filtros e formulário condicional.
- [x] T12: adicionar troca de papel inline na tabela de usuários (papel + escola).
  - Validação: `PUT /api/usuarios/{id}` devolve 200 e a conta sem papel passa a
    autenticar; o telefone é preservado.

## Remoção de escopo

- [x] T7 (reaberta e concluída na Spec 022): a tela de unidades foi removida na execução
  original e reintroduzida em `pages/secretaria/Unidades.tsx` pela Spec 022 — lista,
  renomeia, ativa/desativa e exclui; **não cria** (unidade nasce no bootstrap).

## Rotas e fechamento

- [x] T9: registrar as rotas em `routes/routes.tsx` sob `AdminSecretaria`
  (`/secretaria`, `/secretaria/escolas`, `/secretaria/unidades`, `/secretaria/usuarios`);
  a de unidades voltou com a Spec 022.
- [x] T10: gates — `npm run build` e `npm run lint`.
  - `npm run build`: OK (tsc + Vite).
  - `npm run lint`: 0 erros, 5 avisos (baseline preservada).
- [x] T11: registrar evidência neste arquivo e atualizar `arch.md`/`inicial.md`.

## Evidência

Reauditado em 2026-10-31 contra o código e os gates (`npm run build` OK, `npm run lint`
0 erros / 5 avisos). Verificação dos payloads exatos que a UI envia, contra uma
instância com banco temporário (o `database.db` real não foi tocado):

| Ação da tela | Requisição | Resultado |
| --- | --- | --- |
| Login | `POST /api/usuario/login` | 200 |
| Criar escola | `POST /api/escolas { nome }` | 201 `{ id, nome, ativa }` |
| Nome duplicado | `POST /api/escolas` (mesmo nome) | **409** `{ erro }` |
| Criar merendeira | `POST /api/usuarios` com `papel`, `escolaId` | 201 `{ id }` |
| Filtrar | `GET /api/usuarios?papel=Merendeira&escolaId=1&busca=...` | 200 com a conta |
| Desativar escola | `POST /api/escolas/1/desativar` | 200 `{ ativa: false }` |
| Autocadastro → login | `POST /api/usuario/registrar` + login | 201 e **401** `Conta sem papel definido` |
| **Definir papel** | `PUT /api/usuarios/2` com `papel`, `escolaId` e `telefone` | **200** com `papel: "Merendeira"` e telefone preservado |
| Login após o papel | `POST /api/usuario/login` | **200** com `papel` e `escolaId` |
| Filtro de situação | `GET /api/usuarios?ativo=true|false` | ativas e desativadas separadas (teste `Listar_ComFiltroAtivo_SeparaAtivasDeDesativadas`) |
| Botão ativar/desativar | `POST /api/usuarios/{id}/desativar` | 200; a linha muda de situação |

## Desvios registrados

| # | Desvio | Motivo |
| --- | --- | --- |
| D1 | Navegação por abas dentro de `/secretaria`, sem itens novos no `Navbar` | O menu é uma lista plana; vários itens de secretaria o deixariam ruidoso |
| D2 | Sem edição de nome, e-mail, CPF ou senha de outra conta | Não há caso de uso definido; papel e situação cobrem a operação |
| D3 | Listas sem paginação | O contrato atual do backend não pagina; a rede escolar cabe em uma tela |
| D4 | Carregamento por `finally` em vez de `setState` síncrono no efeito | A regra `set-state-in-effect` gerava avisos novos; a baseline de lint foi preservada em 5 |
| D5 | Telefone sempre enviado no `PUT` de papel | `AtualizarTelefone(null)` limpa o campo; omitir apagaria o telefone do usuário |
| D6 | Tela de unidades removida na execução original | Revertida pela Spec 022, que precisava do nome da unidade para o bootstrap; hoje existe sem criação |

## Observações

- `GET /api/escolas` exige apenas sessão válida (Spec 009); a restrição de área é da rota
  (`ProtegidaPorPapel`) e o backend continua sendo a autoridade nas escritas.
- Escola desativada ainda aceita criação de usuário no backend — comportamento herdado da
  Spec 009, registrado como candidato a pendência.
- Conta sem papel não autentica; a mensagem do backend ("Conta sem papel definido.
  Procure a secretaria.") é exibida na tela de login.
