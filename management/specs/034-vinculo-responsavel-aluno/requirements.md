---
name: vinculo-responsavel-aluno
status: created
references: V23; V17; ADR-020; ADR-021; Spec 027; Spec 028; Spec 033
---

# Requirements — Vínculo responsável↔aluno e busca de alunos

## Objetivo

Fechar o módulo de pais/responsáveis: toda conta `Responsavel` nasce com ao menos um aluno
vinculado, o ADM encontra o aluno por busca e a página de alunos exibe e mantém os vínculos.
O vínculo é a única autorização de leitura do responsável (Spec 027/028).

## Escopo

- Backend: busca de alunos por nome/escola/série; vínculo obrigatório no cadastro direto e na
  definição de papel; validação de vínculo na criação.
- Frontend: seletor de alunos com busca na aprovação de acesso e na tela de usuários; painel de
  responsáveis na página de alunos (exibir, vincular e desvincular).
- Correção de acesso: a área de pais deixa de depender de rota autenticada que o papel não pode ler.
- Fora de escopo: tela "meus filhos"/painel da família (Spec 025), sincronização automática entre
  contato manual do aluno e conta vinculada, alteração de dados do aluno pelo responsável.

## Requisitos funcionais

- RF1: `GET /api/aluno` aceita `busca`, `escolaId` e `serie`, sempre estreitando o escopo do
  solicitante; nenhum filtro amplia o alcance.
- RF2: criar `Responsavel` por `POST /api/usuarios` exige `alunosIds` com ao menos um aluno da
  unidade; sem aluno responde `400` e nada é gravado.
- RF3: definir papel `Responsavel` por `PUT /api/usuarios/{id}` também exige vínculo quando a
  conta ainda não possui nenhum; a validação acontece antes de qualquer mutação.
- RF4: cada aluno de `alunosIds` precisa existir e pertencer à unidade do responsável; aluno de
  outra unidade responde `404` e a operação inteira é desfeita.
- RF5: usuário + vínculos + trilha são gravados na mesma transação; o vínculo usa o id gerado
  pelo `Salvar()` da unidade de trabalho.
- RF6: vínculo duplicado é idempotente no cadastro/definição de papel e continua respondendo
  `409` na rota dedicada `POST /api/aluno/{alunoId}/responsaveis/{usuarioId}`.
- RF7: `GET /api/aluno/{alunoId}/responsaveis` lista nome e e-mail dos responsáveis do aluno.
- RF8: a página de alunos exibe o contato manual (`nomeResponsavel`/`telefoneResponsavel`) e a
  lista de contas vinculadas, sem sincronizar os dois.
- RF9: o ADM com permissão (`AdminSecretaria`) vincula e desvincula pela página de alunos,
  buscando responsáveis por nome/e-mail; a escrita continua validada no backend.
- RF10: a área `/pais` carrega as unidades por `GET /api/unidades/ativas` (anônima), corrigindo o
  `403` que impedia a seleção de unidade.
- RF11: o responsável continua vendo apenas solicitações, ocorrências, feedback e dados dos
  próprios alunos vinculados; relatórios permanecem negados pela matriz.

## Requisitos não funcionais

- RNF1: busca e filtros executados no SQL (`AlunoRepository`), nunca em memória.
- RNF2: nenhuma rota nova; os contratos existentes são estendidos (`alunosIds`, `busca`, `serie`).
- RNF3: validação de vínculo no serviço, não no controller; o domínio continua validando o papel.
- RNF4: toda escrita confinada ao escopo do solicitante (unidade/escola), com `404` fora da
  fronteira de unidade e `403` dentro dela.

## Casos de borda

- Responsável com vários alunos: todos são vinculados no mesmo ato.
- Aluno com mais de um responsável: o vínculo N:N permanece único por par.
- Responsável antigo sem vínculo: ao definir o papel novamente, o serviço exige aluno.
- Definição de papel para outro papel: `alunosIds` é ignorado e os vínculos existentes são
  preservados.
- Usuário desativado: não pode ser vinculado (`409`).
- Aluno sem escola/unidade: não entra em busca de unidade e falha a validação de vínculo.

## Critérios de aceitação

- [x] Criar `Responsavel` sem `alunosIds` responde `400`.
- [x] Criar `Responsavel` com aluno da unidade responde `201` e cria o vínculo.
- [x] Criar `Responsavel` com aluno de outra unidade responde `404` sem gravar.
- [x] `GET /api/aluno?busca=&serie=&escolaId=` devolve apenas o recorte correspondente.
- [x] `PUT /api/usuarios/{id}` para `Responsavel` sem vínculo responde `400`; com aluno, `200`.
- [x] Página de alunos exibe contato e contas vinculadas; ADM vincula/desvincula.
- [x] Aprovação de acesso usa busca de alunos em vez de lista completa.
- [x] `HomePais` carrega unidades sem `403`.
- [x] `dotnet test` 792/792, `npm run build` e `npm run lint` sem erro.

## Validação

- `env APPDATA=... ProgramFiles=... dotnet test backend.Tests/backend.Tests.csproj`
- `cd frontend && npm run build && npm run lint`
- Manual: criar responsável na secretaria com busca de aluno; conferir o nome na página de
  alunos; entrar como responsável e conferir que só os filhos aparecem.
