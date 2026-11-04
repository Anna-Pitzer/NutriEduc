---
name: perfil-crn-nutricionista
status: created
references: V20, ADR-021, ADR-004, ADR-009, PEND-038
---

# Requirements — Perfil e CRN da nutricionista

## Objetivo

O CRN existe no cadastro desde PEND-038, mas é opcional, aceita qualquer texto de 3 a 20
caracteres e não bloqueia nada. `Usuario.DefinirCrn` normaliza e valida apenas o tamanho;
`CriarUsuarioDTO` limita a 20 caracteres; a geração de cardápio aceita nutricionista com
`Crn` nulo e grava o snapshot sem registro profissional. A regra de negócio exige CRN
obrigatório e exclusivo do papel `Nutricionista`, com formato verificável.

## Escopo

- Backend: objeto de valor do CRN, regras de obrigatoriedade por papel, unicidade,
  normalização, gate para operações de cardápio, diagnóstico de legado e auditoria.
- Frontend: campo condicional ao papel, formato e exibição de nome + CRN.
- Fora de escopo: consulta ao conselho para confirmar autenticidade, integração externa,
  emissão ou renovação de registro.

### Cobertura de `implementacoes.md`

- **Secretaria:** cadastrar e editar nutricionistas com CRN; ver o registro na listagem.
- **Nutricionista:** ver o próprio CRN no perfil, sem editá-lo.
- **Nutrição (cardápios):** selecionar apenas nutricionistas com CRN válido; exibir nome e
  CRN na autoria.

## Decisões e interpretações

| # | Ponto | Decisão | Origem |
|---|---|---|---|
| D1 | Quem tem CRN | Exclusivo de `Nutricionista`; qualquer outro papel com CRN é inválido | Regra do dono, 2026-11-04 |
| D2 | Obrigatoriedade | Obrigatório na criação, na edição e na mudança de papel para `Nutricionista`; obrigatório para criar, gerar, adaptar e publicar cardápio | Decisão do dono, 2026-11-04 |
| D3 | Formato | `CRN-<região>-<número>`; região numérica de 1 a 11 (configurável) e número de 3 a 8 dígitos; separadores `.`, espaço e hífen aceitos na entrada e normalizados | RF2 |
| D4 | Unicidade | Um CRN normalizado pertence a um único usuário | RF5 |
| D5 | Autenticidade | O sistema valida **formato**, não autenticidade; não há consulta ao conselho | RF12 |
| D6 | Legado | Nutricionista sem CRN continua autenticando, mas fica bloqueada de operar cardápio e aparece no diagnóstico até regularizar | RF10 |
| D7 | Quem edita | `AdminSecretaria` da própria unidade; a nutricionista só lê o próprio CRN (Spec 028) | ADR-021 |

## Requisitos funcionais

- RF1: `Usuario.Crn` é texto anulável, armazenado normalizado em maiúsculas, sem espaços
  internos e sem separadores redundantes.
- RF2: o formato canônico é `CRN-<região>-<número>`:
  - região inteira de 1 a 11, configurável por `Crn__RegioesValidas`;
  - número com 3 a 8 dígitos;
  - entrada aceita `CRN 3 12345`, `crn-3-12345`, `CRN.3.12345` e normaliza para
    `CRN-3-12345`;
  - qualquer outro formato devolve 400 com exemplo do formato aceito.
- RF3: criar ou editar usuário com papel `Nutricionista` exige CRN válido. Sem CRN devolve
  400 e não grava o usuário.
- RF4: mudar o papel de um usuário para `Nutricionista` exige CRN válido na mesma operação.
  Mudar de `Nutricionista` para outro papel limpa o CRN e registra o valor anterior na trilha
  de ação privilegiada (Spec 028, RF11).
- RF5: CRN normalizado é único entre usuários. Duplicidade devolve 409 com o usuário
  existente, sem gravar.
- RF6: usuário com papel diferente de `Nutricionista` não pode receber CRN; payload com CRN
  nesse caso devolve 400.
- RF7: o CRN é lido em `GET /api/usuario/me`, na listagem administrativa de usuários e nos
  DTOs de nutricionista; nunca em resposta de outro papel que não seja o dono ou o ADM da
  unidade.
- RF8: `GET /api/nutricionistas` (seleção para cardápio) devolve apenas nutricionistas
  ativos, da própria unidade e com CRN válido. A listagem administrativa devolve todos e
  informa `crnValido`.
- RF9: criar, gerar, adaptar ou publicar cardápio com nutricionista sem CRN válido devolve
  400 com mensagem que aponta o cadastro a corrigir. A regra de negócio fica nas Specs 029
  e 032; esta spec expõe o predicado e a mensagem.
- RF10: o diagnóstico de legado lista nutricionistas ativos sem CRN ou com CRN inválido,
  com id, nome, unidade e motivo. Não corrige dados e não inventa registro.
- RF11: toda alteração de CRN é registrada na trilha append-only com autor, data UTC, valor
  anterior e valor novo (ADR-009).
- RF12: a validação de formato **não** confirma autenticidade. O sistema não consulta o CRN,
  não valida situação do registro e não garante que o número pertence à pessoa; isso é
  responsabilidade declarada do ADM que cadastra.
- RF13: o campo CRN aparece no frontend apenas quando o papel selecionado é `Nutricionista`,
  com máscara e mensagem de formato; o servidor é a fonte de verdade e rejeita valor
  inválido mesmo que o cliente envie.
- RF14: autorização de leitura e escrita segue a matriz da Spec 028: ADM escreve na própria
  unidade; `AdminEscola` não edita nutricionista; `Nutricionista` lê o próprio; `Merendeira`
  e `Responsavel` não acessam.
- RF15: no autocadastro do tipo `Nutricionista`, o CRN é validado no formato e guardado na
  solicitação de acesso (`SolicitacaoAcesso.CrnSolicitado`), **não** em `Usuario.Crn`. Somente
  a aprovação (Spec 033) copia o valor para `Usuario.Crn`, na mesma transação.
- RF16: o dono do perfil não altera o próprio CRN. A implementação atual aplica `DefinirCrn`
  também no caminho do dono em `PUT /api/usuarios/{id}`; isso deve ser corrigido — CRN é campo
  administrativo, editado apenas pelo ADM da unidade.

## Requisitos não funcionais

- RNF1: a regra de formato e normalização vive no domínio (ADR-004); controller e DTO não
  reimplementam a validação.
- RNF2: nenhuma dependência nova; sem chamada de rede para validar CRN.
- RNF3: a unicidade é garantida por índice único no banco e por verificação no serviço, com
  mensagem determinística.
- RNF4: nenhum CRN aparece em log, notificação ou resposta de outro escopo.
- RNF5: a validação é determinística e independente de cultura; `ToUpperInvariant`.

## Restrições

- A Spec 020 define o autoatendimento de perfil (nome, e-mail, telefone, foto); ela **não**
  passa a permitir edição de CRN. CRN é campo administrativo.
- O snapshot `NutricionistaResponsavel` e `Cardapio.NutricionistaCrn` já existem; esta spec
  não altera o formato do snapshot, apenas garante que ele nasça válido.
- Migration nova apenas para o índice único; nenhuma coluna nova.
- A lista de regiões é configuração, não constante de código, para não travar o sistema se a
  faixa oficial mudar.

## Dependências

- Spec 009 (papéis), Spec 020 (perfil e contrato de erro) e Spec 024 (fronteiras e serviços).
- Spec 028 (matriz de permissões e trilha de ação privilegiada).
- Spec 033 (cadastro e autorização) — dona do CRN solicitado e da aprovação que o efetiva.
- Spec 029 e Spec 032 consomem `CrnValido` e a mensagem de bloqueio.

## Casos de borda

- Criar `Nutricionista` sem CRN → 400, nada gravado.
- CRN `CRN-3` ou `CRN-3-12` → 400 por número fora do intervalo.
- CRN `CRN-12-12345` → 400 por região fora da faixa configurada.
- CRN com letras no número → 400.
- Dois usuários com o mesmo CRN normalizado → 409.
- Cadastro de nutricionista com CRN válido cria solicitação com `CrnSolicitado`; `Usuario.Crn`
  permanece nulo até a aprovação.
- Dono tentando alterar o próprio CRN via `PUT /api/usuarios/{id}` → 403 ou campo ignorado,
  nunca gravado.
- Nutricionista legado com `Crn = null` → login 200; criar cardápio 400; aparece no
  diagnóstico.
- CRN válido em usuário `Merendeira` → 400.
- Mudança de papel de `Nutricionista` para `Merendeira` → CRN limpo, trilha com valor
  anterior.
- Nutricionista inativa ou de outra unidade → não aparece na seleção de cardápio.
- CRN com sufixo (ex.: `/P`) → 400 enquanto a regra de sufixo não for definida (D3).

## Critérios de aceitação

- [ ] Criar `Nutricionista` sem CRN devolve 400 e não grava.
- [ ] CRN em formato aceito é normalizado para `CRN-<região>-<número>`.
- [ ] Formatos inválidos devolvem 400 com exemplo e nada é gravado.
- [ ] CRN duplicado devolve 409 e não grava.
- [ ] Usuário de outro papel com CRN devolve 400.
- [ ] Mudança para `Nutricionista` sem CRN devolve 400.
- [ ] Mudança para outro papel limpa o CRN e grava a trilha com valor anterior.
- [ ] `GET /api/nutricionistas` devolve somente ativos, da unidade, com CRN válido.
- [ ] Nutricionista legado sem CRN é listado no diagnóstico e bloqueado no cardápio.
- [ ] O campo só aparece no frontend para o papel `Nutricionista`.
- [ ] Nenhuma rota devolve CRN de usuário fora do escopo.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro novo.

## Validação

Criar uma nutricionista com CRN válido e outra sem CRN (deve falhar). Tentar CRN duplicado,
formato inválido, papel errado e mudança de papel. Conferir normalização, trilha, diagnóstico
de legado e a lista de seleção usada pelo cardápio. Executar os gates do projeto.
