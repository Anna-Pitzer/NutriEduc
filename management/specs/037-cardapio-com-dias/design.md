---
name: cardapio-com-dias
status: audited
references: V25, ADR-027, Spec 012, Spec 013, Spec 029, Spec 015, Spec 019
---

# Design — Cardápio com dias

## Princípio

O cardápio deixa de ser **um conjunto** e passa a ser **um período com dias**. O cabeçalho
continua sendo do cardápio (unidade, escola, período, nutricionista, situação, versão); o
conteúdo passa a ser do dia. Nenhuma regra de seleção ou adaptação é reescrita: elas são
chamadas uma vez por dia.

## Agregado

```
Cardapio
  ├─ Id, Nome, UnidadeId, Periodo, Situacao, Tipo, OrigemId, EscolaId, Versao, ...
  └─ Dias[]  (DiaDeCardapio)
       ├─ Data (DateOnly), Ordem
       └─ Refeicoes[]  (Refeicao)
            ├─ Nome ∈ {Café, Almoço, Lanche}, Ordem, Horario (TimeOnly?)
            └─ Itens[]  (ItemRefeicao: AlimentoId, GramasPorcao)
```

- `Refeicao` deixa de referenciar `Cardapio` e passa a referenciar `DiaDeCardapio`.
- `DiaDeCardapio` é entidade filha do agregado, com ciclo de vida do cardápio.
- `Cardapio` valida o conjunto: período, ao menos um dia, datas únicas e dentro do período.
- `DiaDeCardapio` valida a própria data e delega às refeições as invariantes já existentes.

## Refeições fixas

Todo dia contém exatamente três refeições, nesta ordem: **Café, Almoço e Lanche**. O
conjunto é fechado no domínio (constante), não é parâmetro da unidade. `ParametroGeracao`
perde `RefeicoesPadrao` — entidade, DTO, tela e coluna —, e a geração passa a usar sempre
as três. Nome fora do conjunto ou refeição faltante invalida o dia.

## Migration

1. Apagar os cardápios existentes — dados de teste confirmados pelo dono em 2026-11-16. A
   cascata remove refeições e itens.
2. Criar `DiasDeCardapio` (`Id`, `CardapioId`, `Data`, `Ordem`), FK para `Cardapios`.
3. Adicionar `DiaId` obrigatório em `Refeicoes` e `Horario` anulável.
4. Remover `Refeicoes.CardapioId` e o índice antigo.
5. Remover a coluna `RefeicoesPadrao` de `ParametrosGeracao`.

Não há backfill nem cardápio legado: a base de cardápios é recriada depois da migration.

## Geração

```
1. Resolver os dias: datas informadas no DTO ou todos os dias úteis do período
2. Para cada data (ordem crescente):
   a. semente do dia = seed + Data.DayNumber
   b. GeradorCardapioAutomatico.Gerar(entrada) por refeição fixa — sem alteração na função
   c. Validar o dia (ValidadorDeCardapioGerado por refeição/dia)
3. Montar Dias[] e validar o cardápio inteiro
4. Transação: geral + adaptados por dia + trilha
```

O conjunto de alimentos usados é por dia (a variedade entre dias é desejada). O período é
quinzenal (máximo 15 dias corridos), então a geração cobre no máximo 11 dias úteis; data
fora do período devolve 400.

## Adaptação, publicação e portal

- **Adaptação**: `AdaptacaoDeCardapio.Adaptar` é chamada por dia; a versão adaptada tem os
  mesmos dias da origem, com substituições e pendências por dia.
- **Publicação**: continua no nível do cardápio; o snapshot inclui os dias. Revisão e
  histórico não mudam de dono.
- **Portal**: `CardapioServidoService` localiza o dia cuja `Data` coincide com a consulta;
  ausência devolve 404 com mensagem.

## Contrato

```
GET /api/cardapios/{id}
200 {
  id, nome, unidadeId, periodo, situacao, tipo, escolaId, versao,
  dias: [
    { data, refeicoes: [ { nome, ordem, horario, itens: [...] } ], totais }
  ]
}
```

`refeicoes` no nível do cardápio é removido; todos os consumidores são atualizados nesta
spec (detalhe, formulário, portal e relatório).

## Estrutura

```
backend/
  Domain/Entities/DiaDeCardapio.cs            (novo)
  Domain/Entities/Cardapio.cs                 (Dias substitui Refeicoes)
  Domain/Entities/Refeicao.cs                 (+ Horario, DiaId)
  Domain/Services/ValidadorDeCardapioGerado.cs (por dia)
  API/Services/GeracaoCardapioService.cs      (gera por dia)
  API/Services/CardapioService.cs             (montagem/edição por dia)
  API/Services/CardapioServidoService.cs      (busca por data)
  Infrastructure/Repositories/CardapioRepository.cs
  Migrations/<timestamp>_DiasDeCardapio.cs
frontend/
  services/CardapioApi.ts                     (tipos com dias)
  pages/nutricao/cardapios/DetalheCardapio.tsx
  pages/nutricao/cardapios/FormularioCardapio.tsx
  pages/pais/CardapiosPais.tsx
  components/CardapioDoAluno.tsx
```

## Testes

- **Domínio**: data fora do período, data duplicada, cardápio sem dia, horário inválido.
- **Refeições fixas**: dia sem uma das três, nome diferente e ordem canônica.
- **Migration**: base antiga com refeições vira um dia por cardápio, sem perda.
- **Geração**: mesma semente e mesmas datas → mesmo resultado; dias diferentes → conteúdos
  diferentes; as três refeições em cada dia; período acima de 15 dias → 400.
- **Adaptação**: uma versão por dia, com substituições preservadas.
- **Portal**: data com dia → 200; data sem dia → 404.
- **Relatório**: colunas iguais às datas do cardápio.
- **Integração**: criar, editar, publicar e consultar com dias.

## Riscos

- **Migration**: apaga os cardápios existentes. Decisão explícita do dono (dados de teste);
  em base com dados reais, a estratégia precisa ser revista antes de rodar.
- **Contrato**: remover `refeicoes` quebra consumidores antigos; a spec atualiza todos os
  do repositório, mas clientes externos precisam de aviso.
- **Volume**: período longo com muitos dias aumenta o payload; o teto de geração e a
  paginação do relatório limitam o impacto.
