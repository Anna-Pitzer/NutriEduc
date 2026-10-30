---
name: restricoes-e-alergias
status: created
references: V1, V6, ADR-004, L2
---

# Requirements — Restrições e alergias alimentares

## Objetivo

Hoje `Aluno.Restricoes` é uma lista de texto livre: qualquer string é aceita, não há
catálogo, não há distinção entre restrição e alergia e não existe histórico. Isso
impede quatro funcionalidades de nutrição, duas de pais e a verificação de
compatibilidade dos cardápios (Spec 014), que precisa comparar nomes estáveis.

## Escopo

- Backend: catálogo `RestricaoAlimentar`, histórico append-only por aluno, validação
  dos nomes no caso de uso e endpoints de consulta.
- Frontend: tela de catálogo (nutrição) e histórico na tela do aluno.
- Fora de escopo: solicitação de inclusão/alteração pelos pais (Spec 017); vínculo
  alimento↔restrição (Spec 011); alerta de incompatibilidade (Spec 014).

### Cobertura de `implementacoes.md`

- **Nutrição (3):** cadastrar e gerenciar tipos de restrições; cadastrar alergias;
  visualizar histórico de restrições/alergias do aluno.
- **Base para pais (2, entregues pela Spec 017):** o catálogo e o histórico desta
  spec são o que a solicitação de inclusão de alergia e de alteração de restrição
  consome; a solicitação em si pertence à Spec 017.

## Requisitos funcionais

- RF1: `RestricaoAlimentar` tem `Id`, `Nome`, `Categoria` (`Alergia` ou `Restricao`)
  e `Ativa`. Nome obrigatório, 2–60 caracteres, único por categoria
  (comparação sem diferenciar maiúsculas e sem espaços nas pontas).
- RF2: `Nutricionista` cria e edita o catálogo. `AdminSecretaria` não edita catálogo.
- RF3: desativar uma restrição referenciada por aluno não a remove dos alunos e não
  apaga linhas; ela deixa de ser aceita em novos vínculos.
- RF4: ao criar ou atualizar um aluno, cada nome informado em `restricoes` precisa
  existir no catálogo e estar ativo; nome desconhecido devolve 400 com
  `{ erro = "Restrição desconhecida: <nome>." }`. O limite de 20 da Spec 003 é
  mantido.
- RF5: toda alteração efetiva do conjunto de restrições de um aluno gera um evento de
  histórico com: aluno, nome da restrição, ação (`Adicionada`/`Removida`), autor
  (nome) e data em UTC. Nenhum caminho atualiza ou apaga evento.
- RF6: `GET /api/alunos/{id}/restricoes/historico` devolve os eventos em ordem
  decrescente de data, paginado (`pagina`, `tamanho` de 1–100), autorizado por
  papel: `Nutricionista` (global), `AdminEscola` (escola do aluno), `Merendeira`
  (unidade do aluno), `Responsavel` (aluno vinculado).
- RF7: `GET /api/restricoes?categoria=&incluirInativas=` lista o catálogo;
  `incluirInativas=true` é restrito a `Nutricionista`.
- RF8: o frontend exibe o catálogo editável para nutrição e, na tela do aluno,
  multi-seleção a partir do catálogo ativo e a linha do tempo do histórico.
- RF9: merendeira e responsável veem restrições e histórico em modo somente leitura.

## Requisitos não funcionais

- RNF1: a regra de formato (nome, limite, duplicidade) vive na entidade; a existência
  no catálogo é regra de caso de uso, executada antes de chamar o domínio.
- RNF2: o histórico é gravado na mesma unidade de persistência do aluno — uma
  alteração bem-sucedida nunca fica sem evento.
- RNF3: datas em UTC no banco e ISO 8601 na API.
- RNF4: consultas de catálogo e histórico paginadas; nenhuma rota devolve lista
  ilimitada.
- RNF5: `Aluno.Restricoes` continua `IReadOnlyList<string>` (Spec 003 preservada);
  nenhuma alteração de contrato nas rotas existentes de aluno.

## Restrições

- Migration nova `RestricoesEAlergias`.
- Não reescrever as migrations 001–005.
- Nenhuma regra de negócio no controller.

## Dependências

- Spec 003 (coleção protegida) e Spec 004 (invariantes de `Aluno`) implementadas.
- Spec 009 (autorização por papel) para RF6 e RF7.

## Casos de borda

- Restrição já existente no aluno sendo adicionada novamente → não gera evento novo
  (conjunto não mudou).
- Remoção de restrição inexistente → sem evento, resposta 200.
- Nome com acento ou caixa diferente do catálogo → normalizado antes da comparação.
- Catálogo vazio e aluno com restrição legada → leitura funciona; nova gravação exige
  catálogo.
- Restrição desativada ainda presente no aluno → aparece na leitura marcada como
  inativa.
- Autor do evento removido depois → nome do autor preservado no evento (snapshot).
- Aluno inexistente → 404 no histórico e na atualização.

## Critérios de aceitação

- [ ] `POST /api/restricoes` com nome duplicado na mesma categoria devolve 400.
- [ ] `POST /api/restricoes` por `AdminEscola` devolve 403.
- [ ] `PUT /api/aluno/{id}` com restrição fora do catálogo devolve 400 e não altera
      o aluno.
- [ ] Criar aluno com duas restrições grava dois eventos `Adicionada`.
- [ ] Atualizar aluno removendo uma e adicionando outra grava um evento de cada.
- [ ] Atualizar aluno sem mudar restrições não grava evento.
- [ ] Histórico de aluno de outra unidade devolve 403 para `Merendeira`.
- [ ] Histórico de aluno não vinculado devolve 403 para `Responsavel`.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Criar o catálogo com uma alergia e uma restrição, cadastrar um aluno com ambas,
alterar, consultar o histórico com cada papel e confirmar a matriz de acesso. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
