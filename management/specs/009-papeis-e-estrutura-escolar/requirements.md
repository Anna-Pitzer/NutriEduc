---
name: papeis-e-estrutura-escolar
status: created
references: V6, ADR-002, L4
---

# Requirements — Papéis e estrutura escolar

## Objetivo

Modelar quem é cada usuário e a que escola/unidade ele pertence. Hoje `Usuario`
não tem papel, `Escola` existe apenas como texto dentro de `Aluno.Turma` e
`Responsavel` é objeto de valor sem vínculo com um usuário. Sem isso, nenhuma
funcionalidade de `implementacoes.md` pode ser autorizada: 46 das 128
funcionalidades dependem de saber o papel do usuário e o alcance dos seus dados.

## Escopo

- Backend: entidades `Escola`, `UnidadeEscolar` e `VinculoResponsavelAluno`; papel
  e escopo em `Usuario`; políticas de autorização; endpoints de gestão.
- Frontend: áreas de secretaria e de escola; menu e rotas por papel; guardas de rota.
- Fora de escopo: catálogo de restrições e alimentos (Spec 010 e 011), ocorrências
  (Spec 016), solicitações (Spec 017), relatórios e indicadores (Spec 019).

### Cobertura de `implementacoes.md`

- **Secretaria (22):** login administrativo; CRUD e desativação de administradores;
  administradores de escolas; CRUD de escolas e unidades; associação de
  administradores às escolas; visualizar, pesquisar, filtrar e ativar/desativar
  usuários; informações gerais das escolas.
- **Escolas (19):** login administrativo da escola; informações da escola; gestão de
  unidades; cadastro, edição e desativação de pais e merendeiras; vínculo e remoção
  de vínculo responsável↔aluno; visualização de nutricionistas, merendeiras e pais;
  pesquisa e filtro por unidade; visualização de alunos da escola.
- **Merendeiras (4):** visualizar alunos, restrições, alergias e informações de preparo
  da própria unidade, em modo somente leitura.
- **Pais (1):** visualizar apenas os alunos vinculados ao próprio responsável.

## Requisitos funcionais

- RF1: `PapelUsuario` tem exatamente cinco valores: `Nutricionista`, `Merendeira`,
  `Responsavel`, `AdminSecretaria`, `AdminEscola`. Usuário sem papel, ou com papel
  desconhecido, não autentica.
- RF2: o login grava no cookie as claims `NameIdentifier` (id do usuário), `Role`
  (papel), `escola` e `unidade` (quando aplicáveis). `GET /api/usuario/me` devolve
  `id`, `nome`, `email`, `papel`, `escolaId`, `unidadeId`.
- RF3: `AdminSecretaria` cria, edita e desativa escolas e unidades. Escola tem nome
  único; unidade pertence a exatamente uma escola e tem nome único dentro dela.
- RF4: `AdminSecretaria` cria e edita `AdminSecretaria` e `AdminEscola`.
  `AdminEscola` cria e edita apenas `Responsavel` e `Merendeira` da própria escola.
  Nenhum outro papel cria usuários.
- RF5: ativar/desativar usuário é exclusivo de `AdminSecretaria` (qualquer usuário) e
  `AdminEscola` (usuários da própria escola). Usuário desativado: o próximo login
  devolve 401 e uma sessão já emitida é rejeitada na requisição seguinte.
- RF6: `AdminEscola` vincula e desvincula um `Responsavel` a um `Aluno` da própria
  escola. O vínculo é N:N e único por par (aluno, responsável).
- RF7: `AdminSecretaria` lista usuários com filtros `papel`, `escolaId`, `unidadeId`,
  `ativo` e `busca` (nome ou e-mail). `AdminEscola` vê apenas usuários da própria
  escola e filtra por unidade.
- RF8: `Merendeira` consulta alunos, restrições e alergias da própria unidade; a
  resposta não inclui CPF, e-mail, telefone ou foto de responsável.
- RF9: `Responsavel` consulta apenas alunos vinculados a ele; qualquer outro id
  devolve 403 (não 404), para não revelar existência.
- RF10: todas as decisões de acesso acontecem no backend; esconder item de menu no
  frontend é conveniência, nunca controle.

## Requisitos não funcionais

- RNF1: nenhuma regra de autorização no controller além de atributos/política; o
  escopo é resolvido em um serviço dedicado.
- RNF2: negar acesso devolve 403 com o contrato `{ erro }` da ADR-005; ausência de
  sessão devolve 401.
- RNF3: `Usuario` valida papel e escopo no construtor e em `DefinirPapel`
  (ADR-004); não existe `Usuario` válido sem papel.
- RNF4: consultas filtradas por escola/unidade aplicam o filtro no banco, não em
  memória.
- RNF5: nomes de escola e unidade são normalizados (trim + caixa única) antes de
  comparar ou gravar.

## Restrições

- Migration nova `PapeisEEscolas`; as migrations 001–005 não são reescritas.
- `Aluno.Turma.Escola` (texto) é preservado como rótulo de exibição; a fonte de
  verdade do vínculo institucional passa a ser `Aluno.UnidadeId`.
- Não alterar o formato do cookie existente de forma incompatível: apenas acrescentar
  claims.

## Dependências

- Spec 002 (hash de senha) e Spec 006 (autorização dos endpoints de aluno) precisam
  estar implementadas antes da RF5.
- Spec 007 (contrato de erro) define o formato das respostas 400/403.

## Casos de borda

- `Nutricionista` e `AdminSecretaria` não pertencem a escola: `escolaId` e
  `unidadeId` ficam nulos e o escopo é global (nutrição) ou global administrativo.
- `AdminEscola` tentando editar usuário de outra escola → 403.
- Sessão emitida antes da desativação → rejeitada na requisição seguinte.
- Escola com usuários ou alunos ativos → desativação, nunca exclusão física.
- Unidade desativada → usuários dela continuam autenticando, mas não recebem novos
  vínculos até reativação.
- Aluno sem `UnidadeId` (legado) → visível apenas para `Nutricionista` e
  `AdminSecretaria`; escola não o enxerga até que seja vinculado.
- Responsável com mais de um aluno → lista apenas os vinculados.
- Vínculo duplicado → 409, sem criar segunda linha.

## Critérios de aceitação

- [ ] `Usuario` possui `Papel`, `EscolaId`, `UnidadeId` e `Ativo`; não existe
      construtor público que produza usuário sem papel.
- [ ] Login de usuário desativado devolve 401.
- [ ] Sessão de usuário desativado após o login é rejeitada na requisição seguinte.
- [ ] `POST /api/escolas` por `AdminEscola` devolve 403.
- [ ] `POST /api/usuarios` por `AdminEscola` com papel `AdminSecretaria` devolve 403.
- [ ] `GET /api/alunos` por `Merendeira` devolve somente alunos da própria unidade.
- [ ] `GET /api/alunos/{id}` por `Responsavel` não vinculado devolve 403.
- [ ] `POST /api/alunos/{id}/responsaveis/{usuarioId}` duplicado devolve 409.
- [ ] Menu e rotas do frontend exibem apenas áreas permitidas ao papel da sessão.
- [ ] `dotnet build backend/backend.csproj` e `dotnet test` concluem sem erro.
- [ ] `npm run build` e `npm run lint` no frontend concluem sem erro.

## Validação

Criar uma escola, duas unidades, um `AdminEscola`, uma `Merendeira`, um
`Responsavel` e um aluno vinculado. Autenticar com cada papel e confirmar a matriz
de acesso: 200 no próprio escopo, 403 fora dele, 401 sem sessão. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
