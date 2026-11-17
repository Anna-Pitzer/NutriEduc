---
name: painel-e-visao-por-papel
status: verified
references: V14, V6, ADR-002
---

# Design — Painel e visão por papel

## Estrutura

```text
backend/Domain/Painel/
  IndicadoresDaUnidade.cs          (records de contagem agregada)
backend/Domain/Interface/
  IPainelRepository.cs             (porta de leitura agregada)
backend/Infrastructure/Repositories/
  PainelRepository.cs              (COUNT/GROUP BY em SQL)
backend/Application/
  PainelDTOs.cs                    (contrato das quatro respostas)
backend/API/Services/
  PainelService.cs                 (acesso, escopo, período e montagem)
backend/API/Controllers/
  PainelController.cs              (só traduz em HTTP)
frontend/src/services/
  PainelApi.ts
frontend/src/pages/
  secretaria/HomeSecretaria.tsx    (indicadores + o que já existe)
  merendeiras/HomeMerendeiras.tsx  (cardápio do dia + alertas)
  pais/PainelPais.tsx              (filhos, restrições e cardápio do dia)
  pais/RelatorioPais.tsx           (relatório do filho)
```

`HomePais` não existe como arquivo: a área da família é `PainelPais`, entregue pela Spec
035. Esta spec preenche o painel, não cria uma segunda tela inicial.

## Decisões registradas

| # | Decisão | Por quê |
|---|---|---|
| D1 | Painel da secretaria escopado à unidade da sessão | `AdminSecretaria` é escopo de unidade (Spec 027/024) em todas as rotas; painel global seria ampliação de privilégio |
| D2 | Indicador "alunos" = cadastrados | `Aluno` não tem flag de atividade; a exclusão é física |
| D3 | Alunos por **escola** da unidade | Uma linha por unidade seria o próprio total |
| D4 | Filho com restrição não recebe o cardápio geral | O geral não é a versão servida (Spec 029); devolver o geral informaria alimento bloqueante |
| D5 | Negativa de vínculo sempre `403` | RF7: a resposta não pode revelar existência |
| D6 | Contagem por item reutiliza `AnaliseCompatibilidade` | Regra única (ADR-011, L9/L15); `Aluno.Restricoes` é coleção primitiva (JSON), sem SQL trivial |
| D7 | Porta `IPainelRepository` em vez de serviço chamando serviço | `arch.md` §2 proíbe service→service; o precedente de read model é `RelatorioRepository` (Spec 019) |

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `GET` | `/api/painel/secretaria` | `AdminSecretaria` |
| `GET` | `/api/painel/merendeira?data=` | `Merendeira` |
| `GET` | `/api/painel/familia?data=` | `Responsavel` |
| `GET` | `/api/painel/familia/{alunoId:int}/relatorio?de=&ate=` | `Responsavel` (filho vinculado) |

Nenhuma rota aceita `escolaId`/`unidadeId` como parâmetro: o escopo vem da sessão.

Resposta da secretaria:

```json
{ "unidade": "EM Vila Nova", "alunos": 312,
  "alunosPorEscola": [ { "escola": "EM Vila Nova A", "total": 120 } ],
  "usuariosAtivosPorPapel": [ { "papel": "Merendeira", "total": 14 } ],
  "cardapiosPublicadosUltimosSeteDias": 5,
  "ocorrenciasAbertas": 3, "solicitacoesPendentes": 2 }
```

Resposta da merendeira:

```json
{ "data": "2026-11-01", "unidade": "EM Vila Nova", "motivo": null,
  "cardapio": { "nome": "Cardápio de novembro", "publicadoEm": "2026-10-30T12:00:00Z",
    "refeicoes": [ { "nome": "Almoço", "itens": [
      { "alimento": "Arroz integral", "gramas": 120, "alunosComRestricao": 2 } ] } ] } }
```

Sem cardápio publicado: `cardapio` nulo, `motivo` preenchido e 200.

Resposta da família:

```json
{ "data": "2026-11-01", "filhos": [ {
    "id": 7, "nome": "Ana", "escola": "EM Vila Nova A", "unidade": "EM Vila Nova",
    "restricoes": [ { "nome": "Lactose", "categoria": "Alergia" } ],
    "requerAdaptacao": true, "cardapio": null,
    "motivo": "Aluno com restrição: consulte a versão servida no cardápio do filho." } ] }
```

Filho sem restrição: `requerAdaptacao: false`, `motivo: null` e `cardapio` com refeições e
itens, no mesmo formato do painel da merendeira (sem `alunosComRestricao`).

Resposta do relatório do filho:

```json
{ "alunoId": 7, "alunoNome": "Ana", "de": "2026-10-18", "ate": "2026-11-16",
  "restricoes": [ { "nome": "Lactose", "categoria": "Alergia" } ],
  "alteracoes": [ { "tipo": "RestricaoAdicionada", "restricao": "Lactose",
                    "autor": "Maria", "origem": null, "criadoEm": "2026-11-02T10:00:00Z" } ],
  "ocorrencias": [ { "id": 3, "tipo": "Aluno", "status": "Aberta",
                     "descricao": "...", "criadaEm": "2026-11-03T09:00:00Z" } ] }
```

## Regras de escopo

- Secretaria: unidade da sessão; sem unidade, `403`.
- Merendeira: unidade da sessão; sem unidade, `403`.
- Responsável: `FiltroAluno(ResponsavelUsuarioId)` — o vínculo é o filtro. Relatório de
  aluno fora do vínculo responde `403`, exista ou não.
- Datas: `DateOnly` na data local do servidor, comparada com o período do cardápio.

## Consultas

| Bloco | Origem | Observação |
|---|---|---|
| Alunos da unidade | `Alunos` | `COUNT` |
| Alunos por escola | `Alunos` + `Escolas` | `GROUP BY EscolaId`; sem escola vira "Sem escola" |
| Usuários ativos por papel | `Usuarios` | `GROUP BY Papel` com `Ativo` e `UnidadeId` |
| Cardápios publicados | `Cardapios` | `Situacao = Publicado` e `PublicadoEmUtc >= hoje-7d` |
| Ocorrências abertas | `Ocorrencias` | `Aberta` + `EmAnalise` |
| Solicitações pendentes | `Solicitacoes` | `Aberta` + `EmAnalise` |
| Cardápio do dia | `Cardapios` publicados | `ObterGeralPublicado(unidadeId, data)` |
| Alertas por item | `AlimentoRestricao` + alunos da unidade | `AnaliseCompatibilidade` (D6), só contagem |
| Relatório do filho | `HistoricosAlunos` + `Ocorrencias` | período em UTC |

Cada bloco é consultado de forma independente; origem vazia devolve zero ou lista vazia.
Exceção de banco não é convertida em zero (RNF5).

## Regra da contagem por item

1. Montar os itens do cardápio (`RefeicaoOrdem`, `RefeicaoNome`, `AlimentoId`, `AlimentoNome`).
2. Carregar os vínculos alimento↔restrição dos alimentos usados.
3. Carregar os alunos da unidade com seus nomes de restrição.
4. Chamar `AnaliseCompatibilidade.Analisar` (função pura da Spec 014).
5. Agrupar os achados por `(RefeicaoOrdem, AlimentoId)` e contar **ids distintos** de alunos.

Nenhum nome de aluno é projetado no DTO.

## Período do relatório

- `de`/`ate` ausentes → últimos 30 dias, terminando hoje.
- Intervalo maior que 366 dias, ou `de > ate` → `400`.
- `de`/`ate` são convertidos para UTC no início do dia e no início do dia seguinte.

## Frontend

- `HomeSecretaria`: grade de indicadores da unidade, mantendo token, escolas e usuários.
- `HomeMerendeiras`: data (padrão hoje), refeições e itens com selo de restrição; somente
  leitura, sem botão de edição.
- `PainelPais`: filhos com escola, unidade e restrições; cardápio do dia quando não há
  restrição; aviso e atalho para a versão servida quando há; link para o relatório.
- `RelatorioPais`: seletor de filho, período, restrições, alterações e ocorrências.
- Menu: cada entrada declara os papéis que a enxergam (`Navbar` já suporta).

## Testes obrigatórios

| Camada | Caso |
|---|---|
| HTTP | matriz de acesso das quatro rotas (200/401/403) |
| HTTP | secretaria de uma unidade não vê números de outra |
| HTTP | merendeira sem unidade → 403 |
| HTTP | merendeira de outra unidade não vê o cardápio alheio |
| HTTP | responsável recebe 403 no relatório de aluno não vinculado e no de id inexistente |
| HTTP | responsável com dois filhos recebe dados separados por filho |
| HTTP | filho com restrição não recebe refeições no painel |
| Serviço/DB | painel sem cardápio publicado devolve zeros, não erro |
| Contrato | nenhum DTO do painel da secretaria contém nome, CPF, e-mail, telefone ou foto |

## Riscos

- **Custo das agregações**: `GROUP BY` em tabelas grandes exige índice por unidade/papel;
  os índices existentes cobrem o volume atual.
- **Vazamento por mensagem**: negativa de escopo nunca diz "existe, mas não é seu".
- **Duas fontes de cardápio**: o painel usa `ObterGeralPublicado`; a versão servida continua
  sendo exclusiva da Spec 029. O painel não reproduz nem substitui essa decisão.
- **Spec 037 (dias de cardápio)**: quando entrar, o bloco "cardápio do dia" passa a filtrar
  pelo dia do agregado; o contrato do painel não muda.
