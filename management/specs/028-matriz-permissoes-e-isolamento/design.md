---
name: matriz-permissoes-e-isolamento
status: created
references: V17, ADR-021, ADR-005, L5
---

# Design — Matriz de permissões e isolamento de dados

## Princípio

Permissão é **dado**, não `if` espalhado. A matriz é uma função pura `(recurso, papel) →
nível`; o escopo é uma função pura `(sessão, cadeia) → alcance`. Controller só declara o
recurso e o alvo; serviço decide; repositório filtra no SQL. Negar é o padrão.

## Estrutura

```
backend/
  Domain/Entities/
    PapelUsuario.cs             (existente)
  Application/Seguranca/
    EscopoUsuario.cs            (existente — + escolaId e conjunto de vínculos)
    Recurso.cs                  (novo — enum fechado de recursos da matriz)
    NivelDeAcesso.cs            (novo — Nenhum, Solicitar, Ler, Escrever, LerEscrever)
    MatrizDePermissoes.cs       (novo — tabela em código, função pura)
    AutorizacaoService.cs       (novo — decide e valida alvo antes da escrita)
  API/Middleware/
    TratamentoDeErro.cs         (existente — mapeia 401/403/404/409 para { erro })
  Infrastructure/Repositories/
    FiltroDeEscopo.cs           (novo — expressão reutilizável por repositório)
  Infrastructure/Auditoria/
    TrilhaAcaoRepository.cs     (novo — append-only)
backend.Tests/
  Domain/MatrizDePermissoesTests.cs      (função pura, sem IO)
  Integration/MatrizPermissoesTests.cs   (conformidade HTTP por célula)
```

## Matriz como código

```csharp
public enum Recurso { Unidade, Escola, ContaUsuario, PapelDoUsuario, PerfilProprio,
    Vinculo, Aluno, RestricaoDoAluno, HistoricoDoAluno, CatalogoDeAlimentos,
    CatalogoDeRestricoes, CardapioGeral, CardapioAdaptado, ParametroDeGeracao,
    PublicacaoDeCardapio, Ocorrencia, Solicitacao, Feedback, Notificacao,
    Relatorio, Painel, Diagnostico }

public enum NivelDeAcesso { Nenhum, Solicitar, Ler, Escrita, LerEscrever }
```

`MatrizDePermissoes.Nivel(recurso, papel)` é `static`, sem IO, e é a única fonte executável.
A tabela do `requirements.md` e esta função devem declarar o **mesmo** conjunto de células;
`MatrizDePermissoesTests` compara os dois conjuntos e falha em qualquer divergência.

## Resolução de escopo

`EscopoUsuario` passa a carregar:

| Campo | Origem | Uso |
|---|---|---|
| `Papel` | claim `Role` | seleciona a coluna da matriz |
| `UsuarioId` | claim `NameIdentifier` | autoria e "próprio" |
| `UnidadeId` | claim `unidade` | fronteira externa (I1) |
| `EscolaId` | claim `escola` | fronteira interna (I2) |
| `Vinculos` | consulta ao vínculo | fronteira do responsável (I3) |
| `Ativo` | consulta ao usuário | revalidação (RF14) |

Regra de decisão:

1. sem sessão → `401`;
2. nível `Nenhum` → `404` se o alvo é de outra unidade, `403` caso contrário;
3. alvo inexistente → `404`;
4. nível insuficiente para a operação → `403`;
5. conflito de unicidade/estado → `409`;
6. caso contrário, executa.

`AutorizacaoService.Exigir(recurso, alvo, operacao)` recebe `alvo` com os identificadores
institucionais já resolvidos por consulta e devolve a decisão; nenhum controller compara ids
manualmente (RNF2).

## Filtro no SQL

`FiltroDeEscopo.Aplicar<T>(IQueryable<T>, EscopoUsuario, Recurso)` devolve a expressão:

- `AdminSecretaria`/`Nutricionista` → `UnidadeId == escopo.UnidadeId`;
- `AdminEscola`/`Merendeira` → `EscolaId == escopo.EscolaId`;
- `Responsavel` → `AlunoId ∈ escopo.Vinculos`;
- dado de referência (RF9) → sem filtro de escopo.

Cada repositório usa o filtro na consulta, nunca depois do `ToList` (I4). Consulta nova sem
filtro é defeito de revisão e deve falhar no teste de conformidade.

## Auditoria de ação privilegiada

Tabela append-only `TrilhaAcao` (ADR-009): `Id`, `Tipo`, `AutorId`, `AutorNome` (snapshot),
`AlvoTipo`, `AlvoId`, `ValorAnterior`, `ValorNovo`, `EmUtc`. Escrita na mesma unidade de
trabalho da ação (ADR-019). A trilha do aluno (Spec 023) continua dona do histórico do aluno;
esta trilha cobre papel, vínculo, escola e desativação.

## Contrato observável

| Situação | Status | Corpo |
|---|---|---|
| Sem sessão | 401 | `{ erro }` |
| Recurso de outra unidade | 404 | `{ erro }` |
| Recurso da mesma unidade fora do escopo | 403 | `{ erro }` |
| Nível insuficiente | 403 | `{ erro }` |
| Unicidade/estado | 409 | `{ erro }` |
| Sucesso de leitura | 200 | recurso |
| Sucesso de criação | 201 | recurso + `Location` |

Nenhum corpo de erro inclui nome de aluno, CPF, e-mail, telefone ou o motivo detalhado que
permita inferir dado de outra unidade (RF13).

## Testes obrigatórios

| Teste | Prova |
|---|---|
| `Matriz_TabelaEDocumento_Concordam` | RF4, RNF4 |
| `Matriz_FuncaoPura_NegaPorPadrao` | RNF1 |
| `MatrizPermissoesTests` (MemberData por célula) | RNF3, todos os RF de acesso |
| `Isolamento_OutraUnidade_Sempre404` | I1, RF8 |
| `Isolamento_OutraEscolaMesmaUnidade_403` | I2 |
| `Isolamento_ResponsavelSemVinculo_403` | I3 |
| `Escrita_AlvoForaDoEscopo_NaoPersiste` | I5 |
| `Relatorio_FiltroDoClienteNaoAmplia` | I6, I7, I8 |
| `PapelAlterado_SessaoSeguinteUsaNovoEscopo` | RF14 |
| `AcaoPrivilegiada_GravaTrilha` | RF11 |

## Riscos

- **Matriz como documento que envelhece:** mitigado por `MatrizDePermissoesTests`, que compara
  a tabela legível (transcrita em `MatrizDePermissoes.Documentada`) com a executável.
- **Custo da suíte:** matriz de 22 recursos × 5 papéis. Mitigação: `MemberData` gera os casos
  a partir da própria matriz; célula vazia não gera teste.
- **Sobre-bloqueio:** a fronteira 404 entre unidades pode esconder erro de configuração.
  Mitigação: diagnóstico de escopo (Spec 027) e log de auditoria interno sem PII.
- **Legado global:** `AdminSecretaria` sem unidade não escreve; o diagnóstico lista e o ADM
  da unidade corrige. Nenhuma migração inventa escopo.
- **Duas trilhas:** aluno (Spec 023) e ação administrativa (esta spec). A separação é
  intencional; a chave é o tipo de evento, não uma tabela única.
