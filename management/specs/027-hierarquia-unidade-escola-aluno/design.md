---
name: hierarquia-unidade-escola-aluno
status: implemented
references: V16, ADR-020, ADR-021, ADR-004
---

# Design — Hierarquia rede → unidade → escola → aluno

## Princípio

Uma única cadeia, um único escritor. `Escola.UnidadeId` é a fonte de verdade do vínculo
escola→unidade; `Aluno.EscolaId` é a fonte de verdade do vínculo aluno→escola;
`Aluno.UnidadeId` é projeção mantida pelo domínio. Nenhum DTO de entrada pode contradizer a
cadeia.

## Estrutura

```
backend/
  Domain/Entities/
    UnidadeEscolar.cs        (existente — sem UnidadeId; segue entidade da rede)
    Escola.cs                (+ UnidadeId obrigatório, unicidade por unidade)
    Aluno.cs                 (+ EscolaId, UnidadeId derivado no domínio)
    Usuario.cs               (vínculo institucional obrigatório por papel)
    VinculoResponsavelAluno.cs (existente — N:N único por par)
  Domain/Interface/
    IEscolaRepository.cs     (existente — filtros por unidade e escola)
    IAlunoRepository.cs      (existente — filtro por escola)
  Application/
    HierarquiaService.cs     (novo — deriva e valida a cadeia)
  Infrastructure/Repositories/
    EscolaRepository.cs      (filtro por unidade no SQL)
    AlunoRepository.cs       (filtro por escola, substitui Unidade.EscolaId)
  API/Services/
    BootstrapService.cs      (cria unidade inicial quando ausente)
    EscolaService.cs         (escopo da própria unidade)
  API/Controllers/
    EscolaController.cs      (+ unidadeId obrigatório)
    AlunoController.cs       (+ escolaId obrigatório)
frontend/src/
  pages/secretaria/Escolas.tsx (select de unidade)
  pages/nutricao/alunos/*      (select unidade → escola)
  services/EscolaApi.ts, AlunoApi.ts
```

## Domínio

- `Escola`: construtor exige `unidadeId` não vazio; `Nome` normalizado 2–120;
  `Desativar`/`Reativar`; `DefinirUnidade(novaUnidadeId, possuiAlunos)` lança
  `ArgumentException` quando `possuiAlunos` e a unidade muda.
- `Aluno`: `EscolaId` obrigatório no construtor e em `AtualizarDados`; `UnidadeId` recebe o
  valor de `Escola.UnidadeId` na mesma operação, nunca de DTO. `TransferirEscola(novaEscola,
  mesmaUnidade)` exige mesma unidade e atualiza `UnidadeId` no mesmo passo.
- `Usuario`: `DefinirPapel(papel, unidadeId?, escolaId?)` valida a matriz de RF6. Papel que
  exige escola deriva a unidade da escola; papel que exige unidade recusa `escolaId`; papel
  `Responsavel` recusa ambos.
- `VinculoResponsavelAluno`: inalterado; o serviço consulta o vínculo antes de qualquer
  leitura de aluno pelo responsável.

## Persistência

Migration `HierarquiaEscolaAluno`:

1. `Escolas.UnidadeId` nullable + FK `Restrict`; índice único `(UnidadeId, NomeNormalizado)`
   com filtro para `UnidadeId` não nulo.
2. `Alunos.EscolaId` nullable + FK `Restrict`; índice `(EscolaId)`.
3. `Usuarios.UnidadeId`/`EscolaId` continuam anuláveis no banco; a obrigatoriedade por papel
   é do domínio (mesmo padrão de PEND-038 para legado).
4. Backfill em duas etapas, idempotente:
   a. `Escolas.UnidadeId := Unidades.EscolaId` quando o legado existir;
   b. `Alunos.EscolaId :=` escola da unidade legada quando houver exatamente uma escola;
      caso contrário, permanece nulo e o registro é listado no diagnóstico.
5. Nenhuma coluna é removida nesta migration.

O diagnóstico (`GET /api/diagnostico/hierarquia`, papel `AdminSecretaria`) devolve contagens
de: escolas sem unidade, alunos sem escola, usuários sem vínculo obrigatório. Não corrige
dados.

## Contrato de API

| Rota | Mudança | Papel |
|---|---|---|
| `POST /api/escolas` | `unidadeId` obrigatório; unidade = a do solicitante | AdminSecretaria |
| `PUT /api/escolas/{id}` | mudança de unidade bloqueada com alunos (409) | AdminSecretaria |
| `GET /api/escolas` | filtro `unidadeId` sempre restrito à sessão | AdminSecretaria, AdminEscola |
| `POST /api/aluno` | `escolaId` obrigatório; `unidadeId` recusado | AdminSecretaria |
| `PUT /api/aluno/{id}` | `escolaId` aceito apenas na mesma unidade | AdminSecretaria |
| `GET /api/aluno` | filtro `escolaId`; escopo por papel | todos os papéis |
| `POST /api/aluno/{id}/transferir-escola` | nova rota; mesma unidade | AdminSecretaria |
| `GET /api/usuario/me` | devolve `papel`, `unidadeId`, `escolaId` derivados | autenticado |
| `POST /api/bootstrap` | cria a unidade inicial quando ausente | anônimo com token |

Erros seguem a ADR-005 (`{ erro }`). Fronteira de unidade responde 404; fronteira de escola
responde 403.

## Escopo por papel

| Papel | Vínculo obrigatório | Alcance de aluno |
|---|---|---|
| AdminSecretaria | `UnidadeId` | toda a unidade |
| AdminEscola | `EscolaId` | a própria escola |
| Merendeira | `EscolaId` | a própria escola |
| Nutricionista | `UnidadeId` | toda a unidade (leitura) |
| Responsavel | vínculo N:N | apenas alunos vinculados |

## Testes obrigatórios

| Teste | Prova |
|---|---|
| `Escola_SemUnidade_Lanca` | RF2 no domínio |
| `Escola_ComAlunos_NaoMudaDeUnidade` | RF5 |
| `Aluno_SemEscola_Lanca` / `Aluno_UnidadeDerivadaDaEscola` | RF3, RF4 |
| `Aluno_DtoComUnidadeDivergente_NaoSobrescreve` | RF4 (single writer) |
| `Bootstrap_CriaUnidadeQuandoAusente` | RF8 |
| `Bootstrap_SegundaVezMesmaUnidade_Conflito` | RF8 |
| `HierarquiaApiTests` (matriz 200/403/404/401) | RF9, RF10, RF14 |
| `Diagnostico_Hierarquia_ListaLegado` | RNF3 |

## Riscos

- **Backfill ambíguo:** aluno legado cuja unidade tem mais de uma escola não tem escola
  determinável. Mitigação: permanece nulo, leitura só pelo ADM, diagnóstico explícito; nunca
  inventar escola.
- **Divergência da coluna denormalizada:** mitigada por escritor único e teste de integridade;
  a alternativa (remover `Aluno.UnidadeId`) quebra filtros e relatórios existentes.
- **Escopo global herdado:** enquanto houver `AdminSecretaria` sem unidade, ele autentica mas
  não escreve; o diagnóstico lista esses usuários.
- **Superfície de mudança:** `AlunoRepository.Filtrar` passa a usar `EscolaId`; qualquer
  consulta que ainda use `Unidade.EscolaId` deve ser eliminada na mesma entrega.
