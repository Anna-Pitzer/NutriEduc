---
name: cardapio-com-dias
status: discarded
references: Spec 037
---

# Tasks — Cardápio com dias

> **Estado:** descartada em 2026-11-18 por decisão do dono. Nenhuma tarefa será executada.

## Backend — modelo

- [ ] **T1 — Entidade e migration de dias.**
  - Arquivos: `backend/Domain/Entities/DiaDeCardapio.cs`,
    `backend/Domain/Entities/Cardapio.cs`, `backend/Domain/Entities/Refeicao.cs`,
    `backend/Infrastructure/DataBase/ConnectionContext.cs`,
    `backend/Migrations/<timestamp>_DiasDeCardapio.cs`.
  - Ação: criar `DiaDeCardapio`, mover `Refeicao` para o dia, adicionar `Horario` e remover
    `RefeicoesPadrao` de `ParametrosGeracao`; migration limpa os cardápios de teste e cria a
    nova estrutura, sem backfill.
  - Critério: base com cardápios de teste é limpa; a nova estrutura é criada sem resíduo.

- [ ] **T2 — Invariantes do agregado.**
  - Arquivos: `backend/Domain/Entities/Cardapio.cs`,
    `backend/Domain/Entities/DiaDeCardapio.cs`.
  - Ação: validar período, ao menos um dia, datas únicas e pertencentes ao período; as três
    refeições fixas presentes e na ordem canônica; ordem derivada da data; horário válido
    quando informado.
  - Critério: cada caso de borda da spec tem teste de domínio falhando/passando.

## Backend — casos de uso

- [ ] **T3 — Leitura e escrita por dia.**
  - Arquivos: `backend/API/Services/CardapioService.cs`,
    `backend/Application/CardapioDTOs.cs`, `backend/Application/CardapioMapper.cs`,
    `backend/Infrastructure/Repositories/CardapioRepository.cs`.
  - Ação: `CardapioDTO.dias[]`; criação e edição aceitam dias; `Include` de dias →
    refeições → itens; remover `refeicoes` do nível do cardápio.
  - Critério: detalhe devolve dias; criar/editar com duas datas grava as duas; data
    repetida devolve 400.

- [ ] **T4 — Geração por dia.**
  - Arquivos: `backend/API/Services/GeracaoCardapioService.cs`,
    `backend/Application/ParametroGeracaoDTOs.cs`.
  - Ação: `dias` opcional no DTO; sem datas, gerar os dias úteis do período (máximo 15 dias
    corridos); gerar as três refeições fixas em cada dia; semente derivada por data;
    validação por dia; montagem de `Dias[]`.
  - Critério: mesma semente e datas → mesmo resultado; dias diferentes → conteúdos
    diferentes; período acima de 15 dias → 400.

- [ ] **T5 — Adaptação, publicação e portal.**
  - Arquivos: `backend/API/Services/AdaptarCardapioService.cs`,
    `backend/API/Services/PublicacaoCardapioService.cs`,
    `backend/API/Services/CardapioServidoService.cs`.
  - Ação: adaptar por dia; snapshot de publicação com dias; portal busca o dia pela data.
  - Critério: uma versão adaptada por dia; publicação preserva dias; data sem dia → 404.

## Frontend

- [ ] **T6 — Detalhe e formulário com dias.**
  - Arquivos: `frontend/src/services/CardapioApi.ts`,
    `frontend/src/services/GeracaoApi.ts`,
    `frontend/src/pages/nutricao/cardapios/DetalheCardapio.tsx`,
    `frontend/src/pages/nutricao/cardapios/FormularioCardapio.tsx`,
    `frontend/src/pages/nutricao/cardapios/ParametrosGeracao.tsx`.
  - Ação: exibir dias em abas/seções com as três refeições fixas (Café, Almoço e Lanche),
    horário opcional e itens; permitir adicionar e remover dia, nunca refeição.
  - Critério: criar, editar e remover dia pela tela; build e lint sem erro.

- [ ] **T7 — Portal do responsável.**
  - Arquivos: `frontend/src/pages/pais/CardapiosPais.tsx`,
    `frontend/src/components/CardapioDoAluno.tsx`.
  - Ação: consumir `dias[]` e mostrar o dia consultado.
  - Critério: data com dia exibe o conteúdo; data sem dia exibe mensagem.

## Relatório

- [ ] **T8 — Relatório de cardápios por data.**
  - Arquivos: `backend/Infrastructure/Repositories/RelatorioRepository.cs`.
  - Ação: montar colunas a partir das datas dos dias do cardápio.
  - Critério: cada coluna corresponde a uma data real; dia sem refeição aparece vazio.

## Fechamento

- [ ] **T9 — Testes e validação.**
  - Ação: cobrir domínio, migration, geração, adaptação, portal, relatório e integração;
    executar os gates.
  - Critério: todos os critérios de aceitação da spec atendidos; `dotnet build`,
    `dotnet test`, `npm run build` e `npm run lint` sem erro.
