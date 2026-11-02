---
name: painel-e-visao-por-papel
status: planned
references: V14, V6, ADR-002
---

# Design — Painel e visão por papel

## Estrutura

```text
backend/Application/
  PainelSecretariaResposta.cs      (indicadores agregados)
  CardapioDoDiaResposta.cs         (refeições, itens e alertas por item)
  FamiliaResposta.cs               (filhos, cardápio do dia e restrições de cada filho)
  RelatorioDoFilhoResposta.cs      (restrições, alterações e ocorrências do filho)
backend/API/Services/
  PainelService.cs                 (única leitura agregada)
backend/API/Controllers/
  PainelController.cs
frontend/src/services/
  PainelApi.ts
frontend/src/pages/
  secretaria/HomeSecretaria.tsx
  merendeiras/HomeMerendeiras.tsx
  pais/HomePais.tsx
```

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/painel/secretaria` | `AdminSecretaria` |
| `GET` | `/api/painel/merendeira` | `Merendeira` |
| `GET` | `/api/painel/familia` | `Responsavel` |
| `GET` | `/api/painel/familia/{alunoId}/relatorio?de=&ate=` | `Responsavel` (filho vinculado) |

Resposta da secretaria:

```json
{ "alunosAtivos": 312, "alunosPorUnidade": [ { "unidade": "EM Vila Nova", "total": 120 } ],
  "usuariosAtivosPorPapel": [ { "papel": "Merendeira", "total": 14 } ],
  "cardapiosPublicadosNaSemana": 5, "ocorrenciasAbertas": 3, "solicitacoesPendentes": 2 }
```

Resposta da merendeira:

```json
{ "data": "2026-11-01", "unidade": "EM Vila Nova",
  "refeicoes": [ { "nome": "Almoço", "itens": [
      { "alimento": "Arroz integral", "gramas": 120, "alunosComRestricao": 2 } ] } ] }
```

`alunosComRestricao` é a contagem de alunos da unidade cujo conjunto de restrições incide
sobre o alimento (restrição vinculada de categoria `Alergia` ou `Restricao`, Spec 014). A
merendeira vê **quantos**, nunca **quem**.

## Regras de escopo

- A secretaria é global: indicadores de toda a rede.
- A merendeira usa o filtro de unidade da própria sessão; sem unidade, `403`.
- O responsável usa `FiltrosDeConsulta.ParaAluno` (Spec 024, F5) — o vínculo é o filtro.
  Aluno fora do vínculo responde `403` sem revelar existência (mesma regra do módulo de
  aluno).
- Nenhum endpoint desta spec aceita `escolaId`/`unidadeId` como parâmetro: o escopo vem da
  sessão, nunca da requisição.

## Consultas

| Bloco | Origem | Observação |
|---|---|---|
| Alunos ativos / por unidade | `Alunos` + `Unidades` | `GROUP BY` na unidade |
| Usuários por papel | `Usuarios` | `GROUP BY Papel` com `Ativo` |
| Cardápios publicados na semana | `Cardapios` (Spec 012/015) | filtro por `PublicadoEm` |
| Ocorrências abertas | `Ocorrencias` (Spec 016) | `status` aberto |
| Solicitações pendentes | `Solicitacoes` (Spec 017) | `status` pendente |
| Cardápio do dia | `Cardapios` publicados | data = hoje, unidade do escopo |
| Alertas por item | `AlimentoRestricao` + restrições dos alunos da unidade | contagem, sem identificar aluno |
| Relatório do filho | restrições + `HistoricosAlunos` + `Ocorrencias` | escopado ao `alunoId` validado |

Cada bloco é uma consulta independente; a falha de um bloco não impede os demais (RNF5).

## Frontend

- `HomeSecretaria`: grade de indicadores + atalhos para escolas, unidades e usuários
  (telas da Spec 021, já existentes).
- `HomeMerendeiras`: seletor de refeição do dia; cada item mostra a contagem de alunos com
  restrição; modo somente leitura, sem botão de edição.
- `HomePais`: seletor de filho (quando houver mais de um), cardápio do dia da unidade do
  filho, restrições do filho e link para o relatório do período.
- Menu: cada entrada declara os papéis que a enxergam (`Navbar` já suporta).

## Testes obrigatórios

| Camada | Caso |
|---|---|
| HTTP | matriz de acesso das quatro rotas (200/403) |
| HTTP | merendeira de outra unidade não vê o cardápio alheio |
| HTTP | responsável recebe 403 no relatório de aluno não vinculado |
| HTTP | responsável com dois filhos recebe dados separados por filho |
| Serviço | painel sem cardápio publicado devolve zeros, não erro |
| Domínio | nenhum DTO do painel contém nome, CPF, e-mail, telefone ou foto |

## Riscos

- **Dependência de features não implementadas**: enquanto 012/014/016/017 não existirem, as
  consultas correspondentes devolvem zero e os blocos ficam vazios — comportamento previsto
  por RNF5, não erro.
- **Custo das agregações**: `GROUP BY` em tabelas grandes exige índice por unidade/papel; os
  índices já existentes (Spec 009) cobrem o caso atual.
- **Vazamento por mensagem**: negativa de escopo nunca diz "existe, mas não é seu".
