---
name: cadastro-e-autorizacao-de-acesso
status: created
references: V22, V17, ADR-021, ADR-004, ADR-019, ADR-010
---

# Design — Cadastro e autorização de acesso

## Princípio

A conta é a **pessoa**; a solicitação é a **intenção**; a aprovação é o **ato administrativo**.
Separar os três evita o estado atual, em que "sem papel" significa ao mesmo tempo "não pediu
nada", "pediu e ninguém viu" e "foi recusado". A solicitação guarda o tipo pedido e a unidade de
destino; a aprovação, em uma transação, transforma intenção em papel, escopo e vínculos.

## Estrutura

```
backend/
  Domain/Entities/
    SolicitacaoDeAcesso.cs        (novo — estado, tipo, unidade, decisão)
    StatusSolicitacaoAcesso.cs    (novo — Pendente | Aprovada | Recusada)
    TipoCadastroSolicitado.cs     (novo — Nutricionista | Merendeira | Responsavel)
    Usuario.cs                    (papel por tipo, CPF e CRN)
  Application/
    CadastroEntrada.cs            (novo — DTO público com Tipo e UnidadeId)
    SolicitacaoAcessoDTOs.cs      (novo — fila, detalhe, aprovação, recusa)
  Domain/Services/
    PoliticaDeAprovacao.cs        (novo — tipo → papel/escopo, vínculo obrigatório)
  Application/Cadastro/
    RegistrarAcessoService.cs     (novo — usuário + solicitação)
    AprovarAcessoService.cs       (novo — decisão transacional)
  API/Controllers/
    CadastroController.cs         (novo — POST público)
    SolicitacaoAcessoController.cs(novo — fila e decisão)
frontend/src/pages/
  comum/login.tsx                 (etapa "tipo de cadastro" + campos por tipo)
  secretaria/SolicitacoesAcesso.tsx (novo — fila, detalhe, aprovar/recusar)
  secretaria/Usuarios.tsx         (remove papel sugerido para conta sem papel)
```

## Domínio

`SolicitacaoDeAcesso`:

| Campo | Regra |
|---|---|
| `UsuarioId` | obrigatório; FK `Restrict` |
| `TipoSolicitado` | `Nutricionista` \| `Merendeira` \| `Responsavel`; imutável |
| `UnidadeId` | obrigatório; unidade escolhida no cadastro, existente e ativa |
| `Status` | `Pendente` \| `Aprovada` \| `Recusada`; transições só a partir de `Pendente` |
| `CrnSolicitado` | obrigatório se `Nutricionista`; proibido nos demais; normalizado pela Spec 031 |
| `SolicitadoEmUtc` | data do cadastro |
| `DecididoEmUtc` | data da decisão |
| `DecididoPorId` / `DecididoPorNome` | snapshot do ADM (ADR-009) |
| `PapelConcedido` | papel efetivamente atribuído (pode diferir do solicitado só com justificativa) |
| `EscolaConcedidaId` | obrigatório para `Merendeira`; proibido nos demais |
| `Motivo` | obrigatório na recusa; pode registrar a correção de papel |
| `JustificativaCorrecaoPapel` | obrigatória quando `PapelConcedido != TipoSolicitado` |

Invariantes:

- só existe **uma** solicitação `Pendente` por usuário; o banco garante com índice único
  filtrado;
- `TipoSolicitado` não muda; corrigir intenção exige nova solicitação;
- `Aprovada` exige `PapelConcedido` e `DecididoPor*`; `Recusada` exige `Motivo`;
- `PapelConcedido` nunca é `AdminSecretaria` ou `AdminEscola`;
- `Responsavel` aprovado exige ao menos um vínculo; `Merendeira` exige escola; `Nutricionista`
  exige CRN válido;
- a transição é monotônica: `Pendente → Aprovada` ou `Pendente → Recusada`, uma única vez.

`PoliticaDeAprovacao` (função pura) recebe tipo, dados da solicitação e escopo e devolve o
`PlanoDeAprovacao` (papel, escola, unidade, CRN) ou a lista de erros. Não consulta banco.

## Persistência

Migration `SolicitacoesDeAcesso`:

1. tabela `SolicitacoesAcesso` com FKs `UsuarioId` (`Restrict`), `UnidadeId` (`Restrict`) e
   `EscolaConcedidaId` (`Restrict`, nulo permitido);
2. índice único filtrado `(UsuarioId)` para `Status = 'Pendente'`;
3. índice `(UnidadeId, Status, SolicitadoEmUtc)` para a fila;
4. coluna `Usuarios.Cpf` continua como está; a unicidade de CPF é aplicada por índice único
   quando a decisão D7 for confirmada (ver pendências). Enquanto não confirmada, o serviço
   verifica duplicidade e devolve 409;
5. nenhum backfill inventa tipo. Contas antigas com `Papel = null` e sem solicitação entram no
   diagnóstico de legado (RF15); o ADM pode criar uma solicitação manual com o tipo correto ou
   atribuir o papel pela via administrativa.

## Registro público

```text
POST /api/usuario/registrar
{
  "nome": "...", "cpf": "12345678901", "dataNascimento": "2010-05-02",
  "email": "...", "senha": "...", "telefone": null,
  "tipo": "Nutricionista", "unidadeId": 3, "crn": "CRN-3-12345"
}
```

Passos transacionais:

1. validar campos comuns (nome, CPF 11 dígitos, data passada, e-mail único, senha 8–100);
2. validar unidade existente e ativa;
3. validar tipo e campos do tipo (CRN válido só para nutricionista, proibido nos demais);
4. criar `Usuario` com `Papel = null`, sem CRN, `Ativo = true`;
5. criar `SolicitacaoDeAcesso` `Pendente` com unidade e CRN solicitado;
6. responder `201` com `usuarioId`, `solicitacaoId` e `status = "Pendente"`.

Nada é enviado por e-mail nesta spec; a confirmação é a mensagem na tela.

## Aprovação

```text
POST /api/solicitacoes-acesso/{id}/aprovar
{
  "escolaId": 7,                       // obrigatório para Merendeira
  "alunosIds": [12, 15],               // obrigatório para Responsavel
  "papelConcedido": "Responsavel",     // opcional; padrão = TipoSolicitado
  "justificativaCorrecaoPapel": "..."  // obrigatória se papelConcedido != TipoSolicitado
}
```

Ordem de validação (tudo antes de abrir a transação, exceto existência dos alunos):

1. solicitação existe e é da unidade do escopo; senão 404;
2. status `Pendente`; senão 409;
3. papel concedido permitido (nunca administrativo); senão 403;
4. tipo × papel coerente ou justificativa presente; senão 400;
5. `Merendeira`: escola obrigatória, existente, ativa e da unidade; senão 400/404;
6. `Nutricionista`: CRN solicitado válido (Spec 031); senão 400;
7. `Responsavel`: `alunosIds` não vazio, cada aluno existe, está ativo e pertence à unidade
   (via escola); senão 400/404;
8. abrir transação e aplicar:
   `usuario.DefinirPapel(papel, escola, unidade)` + `DefinirCrn` quando aplicável;
   criar os vínculos únicos;
   `solicitacao.Aprovar(...)`;
   gravar trilha (papel solicitado × concedido, unidade, escola, alunos);
   emitir `SolicitacaoStatus`;
   confirmar.

Falha em qualquer passo desfaz usuário, vínculos, estado e trilha.

## Recusa e reconsideração

- `POST /api/solicitacoes-acesso/{id}/recusar` exige `motivo` 5–500, grava autor, data e trilha;
  o usuário continua sem papel e sem acesso.
- Reconsiderar é criar uma nova solicitação para o mesmo usuário:
  `POST /api/solicitacoes-acesso` com `{ usuarioId, tipo, unidadeId, crn? }`, exclusivo do ADM da
  unidade e bloqueado se já existir `Pendente`.

## Contrato de API

| Rota | Regra | Papel |
|---|---|---|
| `POST /api/usuario/registrar` | público; cria usuário sem papel + solicitação | anônimo |
| `GET /api/solicitacoes-acesso?status=&tipo=&busca=&pagina=&tamanho=` | fila da própria unidade | ADM |
| `GET /api/solicitacoes-acesso/{id}` | detalhe com dados do solicitante | ADM |
| `POST /api/solicitacoes-acesso/{id}/aprovar` | decisão transacional | ADM |
| `POST /api/solicitacoes-acesso/{id}/recusar` | motivo obrigatório | ADM |
| `POST /api/solicitacoes-acesso` | reconsideração para usuário existente | ADM |
| `GET /api/solicitacoes-acesso/{id}/alunos?escolaId=&busca=` | candidatos da unidade para vínculo | ADM |
| `GET /api/usuarios/diagnostico-sem-solicitacao` | contas legadas sem papel e sem solicitação | ADM |

| Situação | Status |
|---|---|
| e-mail ou CPF duplicado | 409 |
| tipo/CRN inválido | 400 |
| fila de outra unidade | 404 |
| papel administrativo na aprovação | 403 |
| aluno de outra unidade | 404 |
| decisão concorrente ou repetida | 409 |
| aprovação parcial | desfeita; 500 sem id |

## Frontend

1. `login.tsx` — etapa 1 com três cartões de tipo; etapa 2 com os campos comuns + unidade +
   CRN condicional; após envio, tela de confirmação "aguardando autorização da unidade X".
2. `SolicitacoesAcesso.tsx` — abas por tipo, busca, paginação; detalhe com dados do solicitante;
   para merendeira, select de escola da unidade; para responsável, multi-select de alunos com
   escola e nome; aprovar/recusar com motivo; nunca sugerir papel administrativo.
3. `Usuarios.tsx` — remove `papelInicial` para contas sem papel; pendentes aparecem só na fila.
4. Login de conta pendente exibe a mensagem genérica de 401, sem revelar existência do e-mail.

## Testes obrigatórios

| Teste | Prova |
|---|---|
| `Registro_SemTipo_400` | RF1, RF20 |
| `Registro_NutricionistaSemCrn_400` | RF3 |
| `Registro_MerendeiraComCrn_400` | RF3 |
| `Registro_CriaUsuarioSemPapelESolicitacaoPendente` | RF4 |
| `Registro_EmailDuplicado_409` / `CpfDuplicado_409` | RF4 |
| `Login_Pendente_401` | RF5 |
| `Fila_OutraUnidade_404` | RF6 |
| `Aprovar_Nutricionista_GravaUnidadeECrn` | RF7, RF8 |
| `Aprovar_Merendeira_EscolaDeOutraUnidade_400` | RF8, RF17 |
| `Aprovar_Responsavel_SemAluno_400` | RF9 |
| `Aprovar_Responsavel_CriaVinculosNoMesmoAto` | RF9 |
| `Aprovar_Responsavel_AlunoDeOutraUnidade_404` | RF9 |
| `Aprovar_PapelAdministrativo_403` | RF14 |
| `Aprovar_PapelCorrigidoSemJustificativa_400` | RF13 |
| `DecisaoConcorrente_200e409` | RF12 |
| `Recusar_SemMotivo_400` | RF10 |
| `Recusar_MantemSemAcesso` | RF10 |
| `SolicitacaoPendenteDuplicada_409` | RF11 |
| `Aprovacao_FalhaNoVinculo_NaoGravaNada` | RF7 |
| `CardapioPublicado_Anonimo` | RF19 |

## Riscos

- **Enumeração de contas:** mensagem de login pendente não pode confirmar e-mail. Mitigação:
  401 genérico; log interno sem PII.
- **Concorrência:** dois ADMs decidindo. Mitigação: `ConcurrencyCheck` no estado + índice único
  de pendente.
- **Aprovação parcial:** usuário com papel e sem vínculo. Mitigação: transação única e teste de
  falha no vínculo.
- **Legado sem solicitação:** contas antigas com `papel = null` não aparecem na fila. Mitigação:
  diagnóstico e criação manual de solicitação.
- **CPF duplicado:** a regra é nova; enquanto não confirmada, o serviço verifica e o índice
  fica para a decisão D7.
- **AdminEscola sem acesso à fila:** decisão do dono (aprovação é do ADM). Se a escola precisar
  aprovar merendeira, a matriz da Spec 028 muda e o vínculo de escola também.
