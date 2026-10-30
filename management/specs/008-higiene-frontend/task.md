---
name: higiene-frontend
status: created
references: V7, L6, L8
---

# Tasks — Higiene do frontend e documentação viva

## Pré-condição

- [ ] T0: Specs 006 e 007 implementadas (sessão e contrato de erro definidos).
  - Validação: `dotnet test` verde; tabela do README sem ambiguidade de status.

## Frontend

- [ ] T1: remover a rota `/colors`, o import de `PaletaDeCor` e o arquivo
  `pages/dev/colorsTESTE.tsx`.
  - Validação: busca textual não encontra `colors` em `frontend/src`.
- [ ] T2: remover `buscarEstatisticas` de `Perfil.ts` e o bloco correspondente em
  `Perfil.tsx`.
  - Validação: busca textual não encontra `buscarEstatisticas`; `tsc -b` verde.
- [ ] T3: remover de `CardapioApi.ts` as quatro funções sem consumidor e tipos órfãos.
  - Validação: `npm run build` e `npm run lint` sem erro.
- [ ] T4: adicionar `buscarOuVazio` e o estado vazio nas telas de cardápio para 404.
  - Validação: com a API sem as rotas, a tela mostra a mensagem e o console não
  registra erro não tratado.
- [ ] T5: adicionar `credentials: "include"` nas chamadas internas de cardápio.
  - Validação: busca textual confirma as ocorrências.

## Documentação

- [ ] T6: atualizar a tabela de endpoints do `README.md`, incluindo coluna de sessão.
  - Validação: inspeção manual contra os controllers.
- [ ] T7: criar `Integration/DocumentationTests.cs` que extrai pares método+rota dos
  controllers e confere presença no README, e o inverso.
  - Validação: `dotnet test --filter DocumentationTests` verde.
- [ ] T8: remover uma linha do README e confirmar que o teste falha nomeando o par.
  - Validação: execução controlada, revertida em seguida.

## Fechamento

- [ ] T9: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Nenhuma rota de desenvolvimento em produção.
- Nenhuma chamada a endpoint inexistente.
- README verificado por teste automatizado.
- Gates verdes.
