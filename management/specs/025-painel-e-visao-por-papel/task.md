---
name: painel-e-visao-por-papel
status: planned
references: V14, V6, ADR-002
---

# Tasks — Painel e visão por papel

## Pré-condição

- [ ] T0: Specs 009, 010 e 023 implementadas; 012/014/016/017 no mínimo com contrato
  acordado.
  - Validação: `dotnet test` verde; `/api/aluno` e `/api/restricoes` respondendo.

## Aplicação

- [ ] T1: criar `PainelService` com leitura agregada da secretaria (RF1, RF2).
  - Validação: teste de serviço com banco vazio devolve zeros, sem exceção.
- [ ] T2: montar o cardápio do dia por unidade com alertas por item (RF3, RF4).
  - Validação: teste com duas unidades confirma que só a unidade do escopo aparece.
- [ ] T3: montar a visão da família — filhos, cardápio do dia e restrições (RF5).
  - Validação: responsável com dois filhos recebe dois blocos distintos.
- [ ] T4: montar o relatório do filho escopado ao vínculo (RF6, RF7).
  - Validação: aluno não vinculado → 403; vinculado → apenas os dados dele.

## API

- [ ] T5: criar `PainelController` com as quatro rotas e as políticas do design.
  - Validação: testes HTTP de matriz de acesso (200/403) e do formato de resposta.
- [ ] T6: garantir que nenhuma rota aceita `escolaId`/`unidadeId` do requisitante.
  - Validação: teste enviando esses parâmetros confirma que são ignorados.

## Frontend

- [ ] T7: preencher `HomeSecretaria` com os indicadores e atalhos para as telas da 021.
  - Validação: `npm run build`; painel reflete os números do backend.
- [ ] T8: preencher `HomeMerendeiras` com o cardápio do dia e as contagens de restrição.
  - Validação: merendeira não vê botão de edição em nenhum item.
- [ ] T9: preencher `HomePais` com seletor de filho, cardápio, restrições e relatório.
  - Validação: trocar de filho troca os dados; nenhum dado de outro aluno aparece.

## Fechamento

- [ ] T10: atualizar a tabela de endpoints do `README.md`.
  - Validação: `DocumentationTests` verde.
- [ ] T11: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Cada papel entra na sua área e vê apenas o que lhe cabe.
- Nenhuma resposta do painel da secretaria contém dado pessoal de aluno.
- O relatório da família é sempre do próprio filho; aluno alheio responde 403.
- Gates verdes.
