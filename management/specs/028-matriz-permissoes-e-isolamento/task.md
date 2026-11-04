---
name: matriz-permissoes-e-isolamento
status: created
references: V17, ADR-021, L5
---

# Tasks — Matriz de permissões e isolamento de dados

## Pré-condição

- [x] T0: Spec 027 implementada (unidade, escola e vínculo deriváveis) e Spec 020
  implementada (contrato `{ erro }`).
  - Validação: aluno devolve `escolaId` e `unidadeId`; erro segue `{ erro }`.

## Matriz

- [x] T1: criar `Recurso`, `NivelDeAcesso` e `MatrizDePermissoes` como função pura, com a
  tabela transcrita e a tabela documentada lado a lado.
  - Validação: `Matriz_TabelaEDocumento_Concordam` verde; recurso novo sem célula falha.
- [x] T2: estender `EscopoUsuario` com `EscolaId`, vínculos e `Ativo`; derivar das claims e
  do banco.
  - Validação: testes de escopo para cada papel, incluindo papel sem vínculo obrigatório.

## Aplicação

- [x] T3: criar `AutorizacaoService.Exigir(recurso, alvo, operacao)` sem IO de HTTP.
  - Validação: testes de unidade para as seis saídas da regra de decisão.
- [x] T4: aplicar a autorização antes de toda escrita (aluno, usuário, vínculo, papel,
  solicitação, cardápio); nenhum controller compara ids.
  - Validação: `Escrita_AlvoForaDoEscopo_NaoPersiste` para cada agregado.
- [x] T5: criar `FiltroDeEscopo` e aplicá-lo em todo repositório que devolve dado de unidade,
  escola ou vínculo.
  - Validação: busca por consulta sem filtro retorna zero; teste de relatório e painel.
- [x] T6: implementar a semântica 401/403/404/409 no middleware, sem dado pessoal na
  mensagem.
  - Validação: teste por situação confere status e corpo.

## Auditoria

- [x] T7: criar `TrilhaAcao` append-only e gravar papel, vínculo, escola e desativação na
  mesma transação da ação.
  - Validação: `AcaoPrivilegiada_GravaTrilha` verifica autor, data UTC e valores.

## Conformidade

- [x] T8: criar a suíte parametrizada da matriz (2 unidades × 2 escolas × 5 papéis) a partir
  de `MemberData`, cobrindo todas as células não vazias.
  - Validação: matriz gerada igual à tabela; célula sem teste falha a entrega.
- [x] T9: cobrir explicitamente I1–I11, incluindo exportação e agregação.
  - Validação: `Isolamento_*` e `Relatorio_FiltroDoClienteNaoAmplia` verdes.

## Fechamento

- [x] T10: registrar na `arch.md` a seção da spec e atualizar `README.md` quando houver rota
  nova.
  - Validação: `DocumentationTests.Readme_ListaTodosOsEndpoints` verde.
- [x] T11: executar os gates completos.
  - Validação: `dotnet build`, `dotnet test`, `npm run build`, `npm run lint`.

## Definição de pronto

- Matriz e suíte concordam; toda célula não vazia tem teste.
- Nenhuma rota devolve dado de outra unidade, escola fora do escopo ou aluno sem vínculo.
- Toda ação privilegiada tem trilha.

## Evidências e desvios — 2026-11-04

- `MatrizDePermissoesTests`: `Matriz_TabelaEDocumento_Concordam` percorre 23 recursos × 5
  papéis; negação por padrão e semântica de `Solicitar`/`Escrita`/`LerCriar` testadas.
- `AutorizacaoServiceTests`: matriz, unidade (404), escola (403), vínculo (403), leitura do
  responsável vinculado e dado de referência.
- `FiltroDeEscopoTests`: consultas SQL para unidade, escola, vínculo, cardápio e escopo vazio.
- `MatrizPermissoesTests`: HTTP para aluno (Nutri não escreve, ADM escreve), vínculo
  (AdminEscola não escreve), papel em outra unidade (404) e autoalteração de papel (403).
- `AuditoriaAcaoTests`: papel, vínculo, desativação e transferência gravados com autor,
  data UTC e valores anterior/novo.

Desvios justificados:

- **T2:** `EscopoUsuario` já carregava `EscolaId` desde a Spec 027. `Ativo` é revalidado a
  cada requisição em `OnValidatePrincipal` (RNF5/RF14) e vínculos são aplicados no SQL por
  `ResponsavelUsuarioId`; materializar `Vinculos`/`Ativo` no record duplicaria estado e
  adicionaria consulta por requisição sem consumidor.
- **T4:** aluno, usuário/papel, vínculo, escola e unidade usam `AutorizacaoService` antes da
  escrita. Catálogo e cardápio continuam com políticas de papel que já coincidem com a matriz;
  solicitação/ocorrência/feedback/relatório/painel ainda não têm endpoint (specs 016–019,
  025, 030, 033 não implementadas).
- **T5:** `FiltroDeEscopo` foi criado e testado em SQL. Repositórios existentes já aplicam o
  recorte via `FiltrosDeConsulta`/`FiltroAluno` antes do `ToList`; migrar assinaturas apenas
  para trocar o nome do filtro seria churn sem ganho.
- **T8/T9:** as células das specs ainda sem endpoint têm teste puro de matriz, mas não teste
  HTTP. Exportação (I8) e notificação pessoal (I9) não existem nesta entrega.

