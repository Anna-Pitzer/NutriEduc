---
name: perfil-crn-nutricionista
status: created
references: V20, ADR-004, PEND-038
---

# Tasks — Perfil e CRN da nutricionista

## Pré-condição

- [x] T0: Specs 009, 020 e 028 implementadas (papéis, contrato de erro e matriz).
  - Validação: criação de usuário por papel funciona; erro segue `{ erro }`; trilha de ação
    privilegiada disponível.

## Domínio

- [x] T1: criar `CrnDeNutricionista` com normalização, `TryParse` e igualdade.
  - Validação: variações de separador e caixa normalizam; região/número inválidos recusam.
- [x] T2: aplicar a regra por papel em `Usuario` (construtor, `DefinirCrn`, `DefinirPapel`).
  - Validação: nutrição sem CRN lança; outro papel com CRN lança; troca de papel limpa CRN.
- [x] T3: criar `PoliticaDeCrn.ExigirParaCardapio` com mensagem única.
  - Validação: retorna erro para nutrição com CRN nulo ou inválido e sucesso com válido.

## Persistência

- [x] T4: criar a migration `CrnUnico` com índice único filtrado e verificação prévia de
  duplicidade.
  - Validação: duplicidade faz a migration falhar com relatório; banco limpo aplica o índice.
- [x] T5: garantir que `Cardapio.NutricionistaCrn` receba apenas valor canônico.
  - Validação: snapshot com CRN inválido é impossível pelo domínio.

## Aplicação e API

- [x] T6: validar `crn` em `CriarUsuarioDTO`/`AtualizarUsuarioDTO` e nas regras de
  `UsuarioService` (criação, edição e mudança de papel); no autocadastro, guardar em
  `SolicitacaoAcesso.CrnSolicitado` (Spec 033) e nunca em `Usuario.Crn`; impedir que o dono
  altere o próprio CRN.
  - Validação: 400/409 com as mensagens da tabela; dono recebe 403 ou campo ignorado; nada é
  gravado em erro.
- [x] T7: expor `crn` e `crnValido` em `me`, listagem e `NutricionistaDTO`; filtrar a seleção
  de cardápio.
  - Validação: lista de seleção só devolve ativos da unidade com CRN válido.
- [x] T8: criar `GET /api/usuarios/diagnostico-crn` e gravar a trilha de alteração de CRN.
  - Validação: legado listado; trilha contém valor anterior e novo com autor e data.
- [x] T9: aplicar o bloqueio nas operações de cardápio (Specs 029 e 032) reutilizando
  `PoliticaDeCrn`.
  - Validação: criar/gerar/adaptar/publicar sem CRN devolve 400 com a mensagem única.

## Frontend

- [x] T10: campo CRN condicional ao papel em `secretaria/Usuarios.tsx`, exibição somente
  leitura em `Perfil.tsx` e marcador de CRN inválido na listagem.
  - Validação: `npm run build` e `npm run lint` sem erro novo; máscara não substitui a
  validação do servidor.

## Fechamento

- [x] T11: atualizar `README.md` e a seção da spec em `arch.md`.
  - Validação: `DocumentationTests.Readme_ListaTodosOsEndpoints` verde.
- [ ] T12: executar os gates completos.
  - Validação: `dotnet build`, `dotnet test`, `npm run build`, `npm run lint`.

## Definição de pronto

- Nenhum `Nutricionista` é criado ou promovido sem CRN válido.
- CRN normalizado é único e só aparece no escopo autorizado.
- Nutricionista legado sem CRN é diagnosticada e bloqueada de operar cardápio.
- Nenhum fluxo de cardápio reimplementa a regra de CRN.
