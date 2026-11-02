---
name: substituicao-assistida-ia
status: planned
references: V15, ADR-011, ADR-004
---

# Tasks — Substituição assistida por IA

## Pré-condição

- [ ] T0: Specs 011, 012 e 014 implementadas.
  - Validação: catálogo com categoria e valores; cardápio com itens;
    `AnaliseCompatibilidade` disponível no domínio.

## Domínio

- [ ] T1: criar `SelecaoDeCandidatos` (função pura): mesma categoria, ativo, sem restrição
  vinculada de categoria `Alergia`, faixa de valores, ordenação por proximidade.
  - Validação: testes de exclusão por alergênico, faixa e lista vazia.
- [ ] T2: criar `SugestaoDeSubstituicao`, `OrigemDaSugestao` e `RegistroDeSugestao`
  (append-only).
  - Validação: teste de snapshot de autor e de decisão aceita/recusada.
- [ ] T3: criar a porta `ISugestorDeSubstituicao` com `ContextoDeSubstituicao` sem campo de
  aluno.
  - Validação: teste de contrato garante que o contexto não carrega dado pessoal.

## Persistência

- [ ] T4: mapear `RegistroDeSugestao` com índice `(CardapioId, CriadoEmUtc)` e migration
  nova.
  - Validação: teste de modelo confirma tabela, colunas e índice.

## Aplicação

- [ ] T5: `SubstituicaoService` — montar candidatos, chamar o sugestor, revalidar e
  registrar.
  - Validação: teste com provedor falso devolvendo id inexistente confirma descarte.
- [ ] T6: implementar `SugestorDeterministico` e o fallback em timeout, erro e teto.
  - Validação: teste de timeout devolve sugestões determinísticas sem exceção.
- [ ] T7: implementar `SugestorPorIa` com timeout, parsing estrito, teto diário e chave em
  configuração segura.
  - Validação: teste garante que prompt não contém nome, CPF, e-mail, telefone nem id de
    aluno.
- [ ] T8: aplicar substituição com revalidação, `ConcurrencyCheck` do item e registro da
  decisão.
  - Validação: aplicar sem aprovação não grava; item de outro cardápio → 404/403.

## API

- [ ] T9: adicionar as rotas de sugestão e de aplicação em `CardapioController`, restritas a
  `Nutricionista`.
  - Validação: testes HTTP de matriz de acesso e do formato da resposta.

## Frontend

- [ ] T10: ação "sugerir substituição" na montagem do cardápio, com candidatos, origem,
  justificativa e confirmação explícita.
  - Validação: `npm run build`; aplicar só depois do aceite; falha do provedor não trava a
    tela.

## Fechamento

- [ ] T11: atualizar a tabela de endpoints do `README.md`.
  - Validação: `DocumentationTests` verde.
- [ ] T12: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Nenhuma sugestão inválida é oferecida; o motor determinístico continua sendo a autoridade.
- Sem provedor, a funcionalidade permanece utilizável pelo fallback.
- Toda sugestão aceita ou recusada fica registrada com autor e data.
- Nenhum dado de aluno sai do sistema.
- Gates verdes.
