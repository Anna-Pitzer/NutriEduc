---
name: higiene-frontend
status: created
references: V7, L6, L8
---

# Tasks — Higiene do frontend e documentação viva

## Pré-condição

- [x] T0: Specs 006 e 007 implementadas (sessão e contrato de erro definidos).
  - Validação: `dotnet test` verde; tabela do README sem ambiguidade de status.

## Frontend

- [x] T1: remover a rota `/colors`, o import de `PaletaDeCor` e o arquivo
  `pages/dev/colorsTESTE.tsx`.
  - Validação: busca textual não encontra `colors` em `frontend/src`.
- [x] T2: remover `buscarEstatisticas` de `Perfil.ts` e o bloco correspondente em
  `Perfil.tsx`.
  - Validação: busca textual não encontra `buscarEstatisticas`; `tsc -b` verde.
- [x] T3: remover de `CardapioApi.ts` as quatro funções sem consumidor e tipos órfãos.
  - Validação: `npm run build` e `npm run lint` sem erro.
- [x] T4: adicionar `buscarOuVazio` e o estado vazio nas telas de cardápio para 404.
  - Validação: com a API sem as rotas, a tela mostra a mensagem e o console não
  registra erro não tratado.
- [x] T5: adicionar `credentials: "include"` nas chamadas internas de cardápio.
  - Validação: busca textual confirma as ocorrências.

## Documentação

- [x] T6: atualizar a tabela de endpoints do `README.md`, incluindo coluna de sessão.
  - Validação: inspeção manual contra os controllers.
- [x] T7: criar `Integration/DocumentationTests.cs` que extrai pares método+rota dos
  controllers e confere presença no README, e o inverso.
  - Validação: `dotnet test --filter DocumentationTests` verde.
- [x] T8: remover uma linha do README e confirmar que o teste falha nomeando o par.
  - Validação: execução controlada, revertida em seguida.

## Fechamento

- [x] T9: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Nenhuma rota de desenvolvimento em produção.
- Nenhuma chamada a endpoint inexistente.
- README verificado por teste automatizado.
- Gates verdes.

## Registro de execução

Executado em 2026-10-30. Sem migration e sem mudança de backend além do teste de
documentação.

**Concluído**

| Task | Evidência |
| --- | --- |
| T1 | `routes.tsx` sem `/colors` e sem import de `PaletaDeCor`; `pages/dev/colorsTESTE.tsx` removido e pasta `pages/dev/` apagada |
| T2 | `buscarEstatisticas` removida de `Perfil.ts`; estado, efeito e três cartões removidos de `Perfil.tsx`; `const ApiURL` órfão também removido |
| T3 | `CardapioApi.ts` mantém `buscarAlimentos`, `buscarCardapio`, `buscarHistoricoCardapios` e `salvarCardapio`; as quatro funções sem consumidor saíram |
| T4 | `buscarOuVazio` devolve `[]` em 404; `HomeNutricao` mostra "Nenhum cardápio criado ainda"; `Cardapio` renderiza cartões vazios; `MontarCardapio` mantém o catálogo local quando a API está indisponível (D1) |
| T5 | `credentials: "include"` em `buscarOuVazio` e em `salvarCardapio` |
| T6 | Tabela de alunos com coluna `Autenticação`; tabela de usuários já a tinha |
| T7 | `DocumentationTests.Readme_ListaTodosOsEndpoints` extrai `[Route]` + `[Http*]` por reflexão e compara nos dois sentidos |
| T8 | Removida a linha "Lista alunos" do README; o teste falhou com `Rotas sem linha no README: GET /api/aluno`; README restaurado em seguida |
| T9 | `dotnet test` → **161/161** (140 unitários + 21 integração); `npm run build` OK; `npm run lint` 0 erros e 5 avisos (baseline; um arquivo a menos) |

**Desvios registrados**

- D1: `MontarCardapio` continua usando `alimentosFake` quando `buscarAlimentos`
  devolve lista vazia. O builder é uma tela de montagem local; deixá-la vazia antes da
  Spec 012 a tornaria inutilizável. `Cardapio` e `HomeNutricao` mostram estado vazio.
- D2: `Cardapio` não exibe o texto "Cardápio disponível em breve"; os quatro cartões
  aparecem vazios. O texto explícito exigiria um estado de carregamento que a tela não
  tem hoje — fica para a Spec 012, quando as rotas passarem a existir.
- D3: o teste de documentação valida o par `MÉTODO /rota`, não a descrição textual da
  tabela. É o limite declarado no design.

**Pendências**

Nenhuma task pendente. O status no roadmap continua `planned`.
