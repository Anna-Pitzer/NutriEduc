---
name: telas-secretaria-e-escola
status: created
references: V10, V6, ADR-002
---

# Requirements — Telas de secretaria

## Objetivo

Dar à `AdminSecretaria` uma interface mínima para operar a rede sem HTTP manual:
cadastrar escolas, criar usuários com papel, atribuir papel a contas já existentes e
ativar/desativar contas. Toda a autorização já existe no backend (Spec 009); esta spec
consome as rotas e não cria nenhuma rota nova.

## Requisitos funcionais

- **RF1 — Painel**: `/secretaria` mostra atalhos para escolas e usuários e o total de
  escolas e de contas listadas.
- **RF2 — Escolas**: listar, criar, renomear, desativar, reativar e **excluir** escolas.
  Excluir é definitivo e só acontece sem usuários (ou unidades legadas) vinculados; caso
  contrário o painel mostra o `409` do backend (ADR-014).
- **RF3 — Papel de conta existente**: atribuir ou trocar o papel de um usuário já
  criado, com escola quando o papel exigir; a conta sem papel passa a autenticar.
- **RF4 — Usuários**: listar com filtro por papel, escola, situação (ativa/desativada) e
  busca textual; criar
  usuário com nome, CPF, nascimento, e-mail, senha, papel e escola; ativar, desativar e
  **excluir**. Excluir bloqueia a própria conta, o último `AdminSecretaria`, contas com
  vínculo de responsável e contas que já emitiram token (ADR-014).
- **RF5 — Formulário coerente com o domínio**: o campo escola aparece apenas para
  `AdminEscola`, `Merendeira` e `Responsavel`, tanto na criação quanto na troca de papel.
- **RF6 — Erro do backend visível**: 409 (nome/e-mail duplicado) e 403 (fora do
  escopo) mostram a mensagem de `{ erro }`, não um texto genérico.
- **RF7 — Sessão expirada**: 401 encerra a sessão e o `ProtectedRoutes` leva ao login.
- **RF8 — Interruptor do bootstrap**: o painel mostra o estado da criação de admin por
  token e permite gerar o token de 30 minutos. **Ajustado pela Spec 022 v2:** o
  interruptor no painel foi removido; bloquear a criação é configuração de ambiente
  (`Bootstrap__Permitir=false`), e o token de 30 minutos é a única via pelo painel.
- **RF9 — Unidades**: `/secretaria/unidades` lista as unidades da rede, permite
  renomear, desativar/reativar e excluir. **Não cria**: a unidade nasce no cadastro do
  primeiro administrador (Spec 022).

## Requisitos não funcionais

- **RNF1 — Superfície HTTP**: `GET/POST/PUT/DELETE /api/escolas*`,
  `GET/PUT/DELETE /api/unidades*` e `GET/POST/PUT/DELETE /api/usuarios*`. Unidade não tem
  `POST`: ela nasce no bootstrap (Spec 022).
- **RNF2 — Sem biblioteca nova**: `fetch` + `useState/useEffect`, como o resto do app.
- **RNF3 — Papel como fonte de navegação**: a área é de `AdminSecretaria`; a guarda já
  existe (`ProtegidaPorPapel`), o backend continua sendo a autoridade.

## Critérios de aceitação

- [x] `/secretaria` renderiza painel com atalhos (não é mais `return null`).
- [x] Criar escola pela tela aparece na lista sem recarregar a página.
- [x] Nome de escola duplicado exibe a mensagem do backend.
- [x] Definir papel de uma conta sem papel devolve 200 e libera o login dela.
- [x] Criar merendeira exige escola; criar nutricionista esconde a escola.
- [x] Desativar e reativar escola refletem no estado da linha.
- [x] Desativar usuário reflete no estado da linha.
- [x] Filtro de situação envia `ativo=true|false` e separa ativas de desativadas.
- [x] Excluir escola/unidade/usuário sem referência devolve `204` e some da lista;
  com referência, o `409` do backend aparece na tela.
- [x] Tela de unidades lista a rede e não oferece criação.
- [x] `npm run build` e `npm run lint` sem erro.

## Fora de escopo

- Tela de unidades — existe, mas **somente leitura + renomear/ativar/excluir**: a
  criação acontece no bootstrap do primeiro administrador (Spec 022), não na tela.
- Vínculo responsável↔aluno (exige escolher aluno; entra em spec própria).
- Telas da `AdminEscola` (`/escolas`).
- Edição de nome, e-mail, CPF ou senha de outro usuário; paginação das listas.
