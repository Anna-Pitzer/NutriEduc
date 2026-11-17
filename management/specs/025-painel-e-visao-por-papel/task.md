---
name: painel-e-visao-por-papel
status: audited
references: V14, V6, ADR-002
---

# Tasks — Painel e visão por papel

## Pré-condição

- [x] T0: Specs 009, 010, 012, 014, 016, 017, 023, 029 e 035 implementadas.
  - Validação: `dotnet test` verde.

## Domínio e leitura

- [x] T1: criar os records de contagem e a porta `IPainelRepository`.
  - Validação: build.
- [x] T2: implementar `PainelRepository` com `COUNT`/`GROUP BY` para os seis indicadores.
  - Validação: teste de integração com banco vazio devolve zeros.

## Aplicação

- [x] T3: `PainelService.Secretaria` — acesso, unidade da sessão e montagem (RF1, RF2).
  - Validação: serviço sem unidade devolve 403; painel vazio devolve zeros.
- [x] T4: `PainelService.Merendeira` — cardápio do dia e contagem por item (RF3, RF4).
  - Validação: duas unidades confirmam que só a do escopo aparece; contagem não expõe nomes.
- [x] T5: `PainelService.Familia` — filhos, restrições e cardápio do dia (RF5).
  - Validação: dois filhos recebem blocos distintos; filho com restrição não recebe refeições.
- [x] T6: `PainelService.RelatorioDoFilho` — vínculo, período e dados do filho (RF6, RF7).
  - Validação: não vinculado e id inexistente → 403; vinculado → apenas os dados dele.

## API

- [x] T7: criar `PainelController` com as quatro rotas.
  - Validação: testes HTTP de matriz de acesso (200/401/403).
- [x] T8: criar as políticas `Merendeira` e `Responsavel` no `Program.cs`.
  - Validação: teste de papel errado devolve 403.
- [x] T9: garantir que nenhuma rota aceita `escolaId`/`unidadeId` do requisitante.
  - Validação: teste enviando esses parâmetros confirma que são ignorados.

## Frontend

- [x] T10: `PainelApi.ts` e indicadores na `HomeSecretaria`.
  - Validação: `npm run build`; números vêm do backend.
- [x] T11: `HomeMerendeiras` com cardápio do dia e selo de restrição.
  - Validação: nenhum botão de edição na tela.
- [x] T12: `PainelPais` com cardápio/aviso e `RelatorioPais` para o filho.
  - Validação: trocar de filho troca os dados; nenhum dado de outro aluno aparece.

## Fechamento

- [x] T13: atualizar a tabela de endpoints do `README.md`.
  - Validação: `DocumentationTests` verde.
- [x] T14: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test` (855/855 em Release); `npm run build`; `npm run lint` (0 erros).

## Definição de pronto

- Cada papel entra na sua área e vê apenas o que lhe cabe.
- Nenhuma resposta do painel da secretaria contém dado pessoal de aluno.
- O relatório da família é sempre do próprio filho; aluno alheio responde 403.
- Gates verdes.

## Observação de estado

A implementação está completa e validada, mas a promoção de `planned` para `implemented`
no `roadmap.md` é exclusiva do pipeline SDD (`/spec-implement 025`). O frontmatter dos
documentos permanece `verified` até essa promoção.
