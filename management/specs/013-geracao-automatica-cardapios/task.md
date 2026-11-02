---
name: geracao-automatica-cardapios
status: created
references: V6, ADR-004, L7
---

# Tasks — Geração automática de cardápios

## Pré-condição

- [ ] T0: Specs 009, 011, 012 e 014 implementadas.
  - Validação: `dotnet test` verde; `AnaliseCompatibilidade` disponível como serviço
  de domínio.

## Domínio

- [ ] T1: criar `ParametroGeracao` com faixa calórica, opções, janela, categorias
  obrigatórias, `RefeicoesPadrao` e `Atualizar`.
  - Validação: testes de `min > max`, opções 0/11, janela 31 e lista vazia de refeições.
- [ ] T2: criar `GeradorCardapioAutomatico` com `Random(seed)` e embaralhamento
  Fisher-Yates; sem relógio, sem `Guid`, sem ordenação sem desempate.
  - Validação: teste de determinismo com duas execuções e com base em ordem inversa.
- [ ] T3: implementar as etapas de seleção (bloqueados → categorias obrigatórias →
  faixa calórica) e `GeracaoImpossivelException` com refeição e motivo.
  - Validação: testes de faixa atingida, bloqueado nunca escolhido e 422 de
  impossibilidade.

## Persistência

- [ ] T4: mapear `ParametroGeracao` (único por unidade, coleções primitivas) e gerar
  migration `ParametrosGeracao`.
  - Validação: `dotnet ef migrations add ParametrosGeracao`; teste de modelo confirma
  índice único em `UnidadeId`.
- [ ] T5: criar `IParametroGeracaoRepository` e ampliar `ICardapioRepository` com
  `ObterAlimentosUsadosDesde(unidadeId, dataLimite)`.
  - Validação: teste em SQLite confirma a consulta da janela de repetição.

## Aplicação e API

- [ ] T6: criar `GeracaoCardapioService` orquestrando as sete etapas do design.
  - Validação: testes de parâmetros ausentes (400) e candidatos vazios (422).
- [ ] T7: criar `GeracaoController` com leitura/escrita de parâmetros e
  `POST /api/cardapios/gerar`.
  - Validação: testes HTTP 200/400/403/422 e formato `{ erro, detalhes }`.
- [ ] T8: integrar com Spec 014 para bloquear alimentos incompatíveis com as
  restrições dos alunos da unidade.
  - Validação: teste com aluno alérgico confirma exclusão do alimento.

## Frontend

- [ ] T9: criar tela de parâmetros em `pages/nutricao/cardapios/ParametrosGeracao.tsx`.
  - Validação: `npm run build`; salvar parâmetros e reabrir com os mesmos valores.
- [ ] T10: adicionar ação "Gerar" com campo de semente opcional, exibição de avisos e
  link para o editor do rascunho.
  - Validação: gerar duas vezes com a mesma semente mostra o mesmo resultado;
  - Validação: 422 exibe a mensagem e o detalhe do backend.

## Fechamento

- [ ] T11: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas documentadas coincidem com os controllers.
- [ ] T12: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Parâmetros persistidos por unidade e geração determinística comprovada por teste.
- Nenhum cardápio parcial é salvo quando a geração é impossível.
- Gates verdes.
