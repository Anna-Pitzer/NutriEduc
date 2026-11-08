---
name: correcao-geracao-automatica-cardapios
status: created
references: V21, V18, ADR-022
---

# Tasks — Correção da geração automática de cardápios

## Pré-condição

- [x] T0: Specs 027, 028, 029 e 031 implementadas; Spec 013 implementada (ponto de partida).
  - Validação: aluno tem escola; matriz de escopo ativa; adaptação por escola disponível;
    CRN obrigatório para nutrição.

## Domínio

- [x] T1: extrair `SeletorDeRefeicao` com até 5 tentativas determinísticas e diagnóstico por
  refeição, reutilizando a ordem estável dos candidatos.
  - Validação: mesmo seed → mesmo resultado; faixa apertada usa tentativa 2+; falha devolve
  faixa alcançável.
- [x] T2: criar `ValidadorDeCardapioGerado` com as invariantes de RF6/RF7.
  - Validação: cada erro tem código e mensagem; cardápio válido passa sem erro.
- [x] T3: ampliar `ParametroGeracao` para validar categorias obrigatórias existentes e ativas
  no salvamento.
  - Validação: categoria inexistente devolve 400 no `PUT`, sem esperar a geração.
- [x] T4: remover do caminho do geral o bloqueio por restrições individuais e manter o
  conjunto de bloqueados apenas para a adaptação.
  - Validação: gerar com `ExcluirAlimentosIncompativeis = true` não reduz candidatos do geral.

## Aplicação e API

- [x] T5: validar nutricionista (papel, ativo, mesma unidade, CRN) antes de carregar catálogo.
  - Validação: 400 para outra unidade e para CRN inválido; 404/403 conforme matriz.
- [x] T6: reestruturar `GeracaoCardapioService.Gerar` no pipeline de 13 passos, com validação
  antes da transação.
  - Validação: falha de validação não abre transação e não grava nada.
- [x] T7: persistir geral + adaptados + trilha na mesma transação, com snapshot de autoria em
  todas as versões.
  - Validação: falha simulada no meio não deixa cardápio nem versão órfã.
- [x] T8: ampliar o DTO de resposta com ids, seed, tentativas, totais, status/motivo dos
  adaptados e diagnóstico.
  - Validação: contrato coberto por teste; 422 traz diagnóstico por refeição.
- [x] T9: garantir que adaptado pendente bloqueia a publicação do geral (Spec 029, RF14).
  - Validação: 409 com a versão que bloqueia.

## Frontend

- [x] T10: exibir por refeição o motivo da falha e o estado das versões adaptadas; desabilitar
  o envio enquanto a requisição estiver em curso.
  - Validação: `npm run build` e `npm run lint` sem erro novo; duplo clique não dispara duas
  gerações.

## Fechamento

- [x] T11: atualizar `README.md` (resposta e erros) e a seção da spec em `arch.md`.
  - Validação: `DocumentationTests.Readme_ListaTodosOsEndpoints` verde.
- [ ] T12: executar os gates completos.
  - Validação: `dotnet build`, `dotnet test`, `npm run build`, `npm run lint`.

## Definição de pronto

- Geração válida produz geral + adaptados por escola e conjunto na mesma transação.
- Falha devolve 422 acionável e não persiste nada.
- O geral não é filtrado por restrições individuais.
- O resultado é reproduzível pelo seed devolvido.
- O diagnóstico identifica refeição, categoria e faixa alcançável sem expor aluno.
