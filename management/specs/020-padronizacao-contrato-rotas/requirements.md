---
name: padronizacao-contrato-rotas
status: created
references: V9, V8, ADR-005, L6, L7
---

# Requirements — Padrão de contrato das rotas HTTP

## Objetivo

As 11 rotas de negócio existentes seguem três padrões diferentes: duas devolvem corpo
`string` no sucesso, o `id` é `{id:int}` em duas rotas e `{id}` em três, e o formato
`{ "erro": "mensagem" }` do Spec 007 vaza em três situações — validação de modelo do
`[ApiController]` (responde `ProblemDetails`), `405` e `415` (respondem corpo vazio).
Além disso `GET /api/usuario/perfil/{id}` é anônimo e devolve CPF de qualquer usuário
(PEND-001).

Esta spec fixa **um padrão único e verificável** para todas as rotas e alinha o código
existente a ele, sem criar rota nova, sem mudar nome de rota e sem schema novo.

## Escopo

- Contrato de sucesso: `201` + `Location` + corpo objeto nas duas criações; nenhuma
  resposta de sucesso é string JSON.
- Contrato de erro: `{ erro }` em validação de modelo, JSON malformado, `405`, `415` e
  rota inexistente — hoje fora do middleware.
- Uniformidade de rota: `{id:int}` em todas as rotas com `id`.
- Autorização: `GET /api/usuario/perfil/{id}` passa a exigir sessão e dono (PEND-001),
  sem expor `cpf` nem `senha`.
- Código morto: remoção de `AlterarSenhaDTO` e `UsuarioService.TrocarSenha`, que não têm
  rota nem consumidor (PEND-012).
- Documentação: tabela de endpoints do `README.md` atualizada na mesma mudança (V7/L6).
- Fora de escopo: papéis, escola/unidade e vínculos (Spec 009); DTOs separados de
  entrada/saída do aluno (PEND-007, Spec 012); rotas de cardápio (Specs 011–015);
  paginação, filtros e versionamento de API.

## Requisitos funcionais

- RF1: `POST /api/aluno/registrar` e `POST /api/usuario/registrar` devolvem `201`, o
  cabeçalho `Location` apontando para o recurso criado e um corpo JSON objeto que
  inclui `id`. Nenhuma das duas devolve string.
- RF2: nenhuma rota devolve corpo `string` JSON em resposta de sucesso; sucesso com
  corpo é sempre objeto e remoção continua `204` sem corpo.
- RF3: toda rota com `id` usa a constraint `{id:int}`: `GET /api/aluno/perfil/{id:int}`,
  `GET /api/usuario/perfil/{id:int}` e `PUT /api/usuario/perfil/{id:int}`. Requisição
  com `id` não numérico não alcança o service.
- RF4: `GET /api/usuario/perfil/{id:int}` exige sessão; quando o `id` da rota difere do
  `id` da sessão, responde `403` com `{ erro }`; quando o usuário não existe, `404` com
  `{ erro }`.
- RF5: o corpo de `GET /api/usuario/perfil/{id:int}` contém `id`, `nome`, `email`,
  `telefone` e `foto` — ou seja, os campos que a tela de perfil consome — e **não**
  contém `cpf` nem `senha`.
- RF6: erro de validação de modelo (`[Required]`, `[StringLength]`, `[EmailAddress]`)
  responde `400` com `{ erro }`, nunca `ProblemDetails`/`ValidationProblemDetails`.
- RF7: corpo JSON malformado ou tipo incompatível responde `400` com
  `{ erro = "Requisição inválida." }`.
- RF8: `405` responde `{ erro }` com mensagem em português e `415` responde
  `{ erro }` com mensagem em português.
- RF9: requisição para rota inexistente sob `/api/` responde `404` com `{ erro }`.
- RF10: o contrato do Spec 007 não regride: `400` validação de domínio, `401` sem
  sessão, `403` dono diferente, `404` não encontrado, `409` conflito e `500` interno
  continuam com `{ erro }` e sem stack trace.
- RF11: `AlterarSenhaDTO` e `UsuarioService.TrocarSenha` são removidos, junto dos dois
  testes que os exercitam.
- RF12: o `README.md` descreve os status novos (`201`, `405`, `415`, regra de dono do
  perfil) na mesma mudança, e `DocumentationTests` continua verde.
- RF13: nenhuma tela quebra — cadastro de aluno, cadastro de usuário, login, perfil,
  listagem, edição e exclusão de aluno mantêm o fluxo atual.

## Requisitos não funcionais

- RNF1: nenhuma dependência nova no backend nem no frontend.
- RNF2: nenhuma migration e nenhuma mudança de schema; `database.db` existente continua
  válido.
- RNF3: nenhuma regra de negócio nova no controller; validação continua no domínio
  (ADR-004).
- RNF4: o formato de erro tem uma única autoridade no código — um ponto decide
  status e corpo; controller não monta erro fora do contrato.
- RNF5: mensagens de erro em português, sem nome de exceção, sem stack e sem caminho
  interno.
- RNF6: gates verdes — `dotnet build`, `dotnet test`, `npm run build`, `npm run lint`.

## Restrições

- Não renomear rota existente: o frontend já as consome e a Spec 008 acabou de fixar a
  tabela do `README.md`.
- Não implementar `403` por papel; a regra desta spec é apenas "id da rota = id da
  sessão". Papel e escola pertencem à Spec 009.
- Não alterar `DadosAlunoDTO` nem `DadosUsuarioDTO` como entrada: a separação de DTOs
  de entrada/saída é PEND-007 e pertence às Specs 009/012.
- Não expor rota de diagnóstico fora do ambiente `Testing`.

## Critérios de aceitação

- [x] `POST /api/aluno/registrar` devolve `201`, `Location` com o id criado e corpo com
      `id`, `nome`, `escola`, `serie`, `restricoes` e `anafilaxia`.
- [x] `POST /api/usuario/registrar` devolve `201`, `Location` e corpo com `id`, `nome`
      e `email`, sem `senha`.
- [x] Nenhum teste nem consumidor depende de corpo string nas duas rotas de criação.
- [x] `GET /api/aluno/abc` não alcança o service (rota não casa com `{id:int}`).
- [x] `GET /api/usuario/perfil/{id}` sem cookie devolve `401` com `{ erro }`.
- [x] `GET /api/usuario/perfil/{id}` com cookie de outro usuário devolve `403` com
      `{ erro }`.
- [x] `GET /api/usuario/perfil/{id}` do próprio usuário devolve `200` sem `cpf` e sem
      `senha`.
- [x] `PUT /api/usuario/perfil` com `nome` vazio devolve `400` com `{ erro }` em
      português.
- [x] `POST /api/usuario/login` com `{"email":` truncado devolve `400` com
      `{ erro = "Requisição inválida." }`.
- [x] `PATCH /api/aluno` devolve `405` com `{ erro }`.
- [x] `POST /api/aluno/registrar` com `Content-Type: text/plain` devolve `415` com
      `{ erro }`.
- [x] `GET /api/rota-que-nao-existe` devolve `404` com `{ erro }`.
- [x] `DELETE /api/aluno/{id}` continua `204` sem corpo.
- [x] `AlterarSenhaDTO.cs` e `TrocarSenha` não existem mais; nenhuma referência
      remanescente no backend ou nos testes.
- [x] `README.md` documenta `201`, `405`, `415` e a regra de dono do perfil.
- [x] `dotnet test` verde com os testes atualizados; `npm run build` e `npm run lint`
      sem regressão.
