---
name: perfil-crn-nutricionista
status: created
references: V20, ADR-004, ADR-021, ADR-009
---

# Design — Perfil e CRN da nutricionista

## Princípio

CRN é **identidade profissional**, não texto livre. O valor entra no domínio como objeto de
valor: normaliza uma única vez, valida o formato e só então chega à entidade. A regra de papel
(obrigatório só para `Nutricionista`) fica no agregado `Usuario`; a autorização de quem edita
fica na aplicação; a unicidade é garantida pelo banco.

## Estrutura

```
backend/
  Domain/Entities/
    Usuario.cs                    (Crn tipado e regra por papel)
    CrnDeNutricionista.cs         (novo — objeto de valor: formato, normalização, igualdade)
  Domain/Services/
    PoliticaDeCrn.cs              (novo — exige CRN válido por papel e para operar cardápio)
  Application/
    CriarUsuarioDTO.cs            (+ Crn com validação de formato)
    AtualizarUsuarioDTO.cs        (+ Crn)
    UsuarioRespostaDTO.cs         (+ Crn e CrnValido)
    NutricionistaDTO.cs           (+ CrnValido; lista de seleção já filtra)
  API/Services/UsuarioService.cs  (criação, edição e mudança de papel)
  API/Controllers/UsuarioAdminController.cs (+ diagnóstico de legado)
frontend/src/pages/
  secretaria/Usuarios.tsx         (campo condicional + máscara)
  comum/Perfil.tsx                (exibe CRN, somente leitura)
```

## Objeto de valor

```csharp
public sealed record CrnDeNutricionista
{
    public string Valor { get; }          // "CRN-3-12345"

    public static bool TryParse(string? entrada, IReadOnlySet<int> regioes,
                                out CrnDeNutricionista? crn, out string? erro);

    public static string Normalizar(string entrada) =>
        Regex.Replace(entrada.Trim().ToUpperInvariant(), @"[\s.\-]+", "-");
}
```

Gramática aceita:

```
entrada := "CRN" sep regiao sep numero
sep     := "-" | "." | " "
regiao  := 1..11          (configurável em Crn__RegioesValidas)
numero  := 3..8 dígitos
saída   := "CRN-<regiao>-<numero>"
```

`TryParse` remove espaços, converte para maiúsculas, colapsa separadores, valida faixa e
dígitos e devolve a forma canônica. A faixa de regiões é configuração para não travar o
sistema caso a lista oficial mude; a especificação não presume a lista completa (D3).

## Regras no agregado `Usuario`

| Operação | Regra |
|---|---|
| construtor com `Papel == Nutricionista` | `Crn` obrigatório e válido |
| construtor com outro papel | `Crn` deve ser nulo; valor presente lança |
| `DefinirCrn` | grava a forma canônica; `null`/vazio limpa |
| `DefinirPapel(Nutricionista, ...)` | exige `Crn` válido fornecido na mesma operação |
| `DefinirPapel(outro)` | limpa `Crn` e devolve o valor anterior para a trilha |
| `AtualizarPerfil` | não toca em `Crn` (autoatendimento não edita) |

## CRN no cadastro e na aprovação

- autocadastro do tipo `Nutricionista` valida o CRN e o guarda em
  `SolicitacaoAcesso.CrnSolicitado`; `Usuario.Crn` permanece nulo enquanto a conta não tem
  papel;
- a aprovação (Spec 033) copia o valor para `Usuario.Crn` na mesma transação; se o papel
  concedido não for `Nutricionista`, o CRN solicitado é descartado e a aprovação falha se
  houver CRN sem papel de nutrição;
- `PUT /api/usuarios/{id}` só aplica `DefinirCrn` quando o solicitante é administrador do
  escopo; o dono do perfil nunca altera o CRN. O código atual chama `DefinirCrn` antes de
  distinguir dono × administrador e deve ser corrigido.

`PoliticaDeCrn.ExigirParaCardapio(usuario)` devolve erro acionável quando o papel é
`Nutricionista` e o CRN é nulo/inválido. As Specs 029 e 032 chamam essa política; nenhum
serviço de cardápio reimplementa a mensagem.

## Persistência

- Coluna `Usuarios.Crn` já existe (texto 20, anulável). Nenhuma coluna nova.
- Migration `CrnUnico`: índice único filtrado em `Crn` para `Crn IS NOT NULL`.
- Antes do índice, a migration verifica duplicidade. Se existir, **falha com relatório** de
  ids/nomes e não altera dados; a correção é manual pelo ADM.
- Nenhum backfill inventa CRN. Registros legados permanecem nulos e entram no diagnóstico.
- O snapshot `Cardapios.NutricionistaCrn` continua texto anulável; passa a ser preenchido
  apenas com CRN válido.

## Contrato de API

| Rota | Campo/regra | Papel |
|---|---|---|
| `POST /api/usuarios` | `crn` obrigatório se `papel = Nutricionista`; proibido nos demais | AdminSecretaria |
| `PUT /api/usuarios/{id}` | mesma regra; mudança de papel para nutrição exige `crn` | AdminSecretaria |
| `GET /api/usuarios` | devolve `crn` e `crnValido` | AdminSecretaria |
| `GET /api/usuario/me` | devolve o próprio `crn` | autenticado |
| `GET /api/nutricionistas` | somente ativos, da unidade, com CRN válido | Nutricionista, ADM |
| `GET /api/usuarios/diagnostico-crn` | lista nutricionistas sem CRN ou inválido | AdminSecretaria |

Erros seguem `{ erro }` (ADR-005):

| Situação | Status | Mensagem |
|---|---|---|
| Nutricionista sem CRN | 400 | "Informe o CRN da nutricionista no formato CRN-<região>-<número>." |
| Formato inválido | 400 | "CRN inválido. Exemplo aceito: CRN-3-12345." |
| CRN duplicado | 409 | "Este CRN já está vinculado a outro usuário." |
| CRN em papel não nutricionista | 400 | "Somente nutricionistas possuem CRN." |
| Operação de cardápio sem CRN válido | 400 | "A nutricionista responsável está sem CRN válido. Corrija o cadastro." |

## Frontend

- `Usuarios.tsx`: o campo CRN aparece somente quando o papel é `Nutricionista`; máscara
  guia a digitação, mas a mensagem de erro exibida é a do servidor.
- `Perfil.tsx`: exibe `CRN` em modo leitura para a própria nutricionista; sem campo editável.
- Listagem administrativa mostra `crn` ou "—" e marca `crnValido = false`.
- Seleção de nutricionista no cardápio mostra nome + CRN e já recebe a lista filtrada.

## Testes obrigatórios

| Teste | Prova |
|---|---|
| `Crn_TryParse_AceitaVariacoes` | RF2 (espaço, ponto, hífen, caixa) |
| `Crn_TryParse_RejeitaRegiaoNumeroETamanho` | RF2 |
| `Usuario_NutricionistaSemCrn_Lanca` | RF3 |
| `Usuario_OutroPapelComCrn_Lanca` | RF6 |
| `DefinirPapel_ParaNutricionistaSemCrn_Lanca` | RF4 |
| `DefinirPapel_ParaOutroLimpaCrn` | RF4 |
| `Criar_Nutricionista_CrnDuplicado_409` | RF5 |
| `Nutricionistas_ListaSoComCrnValido` | RF8 |
| `Cardapio_SemCrn_400` | RF9 (consumido por 029/032) |
| `Diagnostico_ListaLegadoSemCrn` | RF10 |
| `Trilha_CrnAnteriorENovo` | RF11 |
| `Me_NaoExpoeCrnDeOutroUsuario` | RF7, RNF4 |

## Riscos

- **Faixa de regiões errada:** a lista oficial pode mudar ou haver sufixos. Mitigação:
  configuração `Crn__RegioesValidas` e validação de formato apenas; a especificação registra
  a dúvida (D3) em vez de fixar a lista.
- **Legado sem CRN:** bloqueia operação de cardápio por decisão do dono. Mitigação:
  diagnóstico e mensagem acionável; login preservado.
- **Duplicidade pré-existente:** a migration falha com relatório em vez de apagar ou escolher
  um registro.
- **CRN não é prova de autenticidade:** o campo pode ser preenchido com número de terceiro.
  O sistema não promete verificação; a responsabilidade é declarada no cadastro.
- **Vazamento de PII:** CRN é dado profissional, mas só aparece no escopo da unidade; a
  resposta de outro papel é 404/403 conforme a matriz da Spec 028.
