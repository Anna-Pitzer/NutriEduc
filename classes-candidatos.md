# Classes candidatas a virar domínio — NutriEduc

Levantamento de tipos que hoje são **primitivo solto, DTO anêmico ou read model** e que
deveriam existir no domínio como **objeto de valor** (VO), **entidade** ou **agregado**.

**Escopo analisado:** `backend/Domain`, `backend/Application`, `backend/API/Services`,
`backend/Infrastructure`, `frontend/src`. Levantado em **2026-11-17**.

**Este documento não altera código.** É um inventário priorizado para virar spec.
Cada candidato informa onde está hoje, o problema, a proposta e o custo.

---

## 1. Critério usado

Um tipo entra na lista se cumprir **pelo menos um** destes:

| # | Critério | Pergunta de teste |
| --- | --- | --- |
| C1 | Invariante própria | O conceito tem regra que precisa valer em todo lugar? |
| C2 | Regra duplicada | A mesma validação está escrita em mais de um arquivo? |
| C3 | Representação dupla | O mesmo dado existe como texto e como id em pontos diferentes? |
| C4 | Vocabulário ausente | O código usa `string`/`int` onde o negócio tem nome próprio? |
| C5 | Erro representável | Um valor inválido consegue existir em memória? |

**Não é critério:** estética, simetria com outras linguagens, ou "VO para tudo".
`ValoresNutricionais`, `PeriodoVigencia` e `ConjuntoDeRestricoes` já resolvem seus
conceitos; multiplicar tipos para as seis medidas ou para cada string não traz invariante
nova e só aumenta custo de mapa EF (ver §6).

---

## 2. Já resolvido — não são candidatos

Confirmado no código; não reabrir:

| Tipo | Onde | Por que está adequado |
| --- | --- | --- |
| `PeriodoVigencia` | `Domain/Entities/PeriodoVigencia.cs` | VO com `Inicio`/`Fim`, limite de 366 dias, tipo complexo mapeado |
| `ValoresNutricionais` | `Domain/Entities/ValoresNutricionais.cs` | VO de composição por 100 g, valida as seis medidas |
| `ConjuntoDeRestricoes` | `Domain/Entities/ConjuntoDeRestricoes.cs` | VO com chave canônica, igualdade e `DeIds`/`DaChave` |
| `CrnDeNutricionista` | `Domain/Entities/CrnDeNutricionista.cs` | VO de validação de CRN (131 linhas, incluindo política) |
| `HashSenha`, `HashDeToken` | `Domain/Entities/` | VOs de hash com comparação própria |
| `EscopoUsuario`, `Filtro*` | `Domain/` | Records de consulta em vocabulário de negócio (Spec 024, F5) |
| `AlteracaoSolicitada`, `NutricionistaResponsavel` | `Domain/Entities/` | Records imutáveis com semântica de snapshot |
| `Turma` | `Domain/Entities/Turma.cs` | Já é VO, mas com campos de texto — ver **C6** |

---

## 3. Resumo priorizado

| ID | Tipo proposto | Hoje | Critério | Prioridade |
| --- | --- | --- | --- | --- |
| C5 | `Crn` (concluir conversão) | `Usuario.Crn` é `string?`; VO só valida | C1, C4 | **P1** |
| C1 | `Cpf` | `Usuario.Cpf` é `string?` + método estático | C1, C2, C5 | **P1** |
| C3 | `NomePessoal` | 3 entidades + 2 implementações da regra | C2, C5 | **P1** |
| C2 | `Telefone` | 3 entidades + helper estático | C1, C2 | **P2** |
| C4 | `EmailDeContato` | `Usuario.Email` é `string?`, regra inline | C1, C2 | **P2** |
| C8 | `RestricaoDoAluno` | `IReadOnlyList<string>` em coluna JSON | C1, C4, C5 | **P2** |
| C6 | `TurmaDoAluno` | `Turma.Escola`/`Serie` texto × `Aluno.EscolaId` | C3, C5 | **P2** |
| C7 | `ChaveDeConjuntoDeRestricoes` | `Cardapio.ConjuntoRestricoesChave` é `string?` | C4, C5 | **P3** |
| C9 | `Gramatura` | `ItemRefeicao.QuantidadeGramas` é `decimal` | C1, C4 | **P3** |
| F1–F4 | Tipos compartilhados no frontend | tipos e literais duplicados | C2, C3 | **P1 (front)** |

---

## 4. Candidatos de backend

### C5 — `Crn` como tipo do domínio (concluir o que já existe)

**Onde está hoje**

- `Usuario.Crn` é `string?` — `Domain/Entities/Usuario.cs:21`
- `Usuario.CrnValido => CrnDeNutricionista.EhValido(Crn)` — `Usuario.cs:24`
- `usuario.DefinirCrn(string? crn)` — `Usuario.cs:176`
- VO pronto em `Domain/Entities/CrnDeNutricionista.cs` (validação + política)

**Problema.** O VO existe, mas só é usado como **função de validação**: o valor continua
`string` no estado da entidade. `CrnValido` pode ser `false` em um usuário já persistido —
situação real que o CRN é um conceito do domínio com regra, formato e papel exclusivo.

**Proposta.** `public CrnDeNutricionista? Crn { get; private set; }`, com
`DefinirCrn(CrnDeNutricionista?)`; `CrnValido` desaparece como propriedade derivada (o tipo
garante). `PoliticaDeCrn` e `ValidacaoDeNutricionista` passam a receber o VO.

**Impacto.** `Usuario.cs`, `PoliticaDeCrn`, `ValidacaoDeNutricionista`, `UsuarioService`,
`UsuarioAdminController`, mapeamento EF (`HasConversion` para `string`) e `UsuarioDTO`
(`crn` continua string na borda). Sem migration se usar conversão.

**Risco.** Baixo. É o candidato de maior valor e menor custo: o VO já existe e já é testado.

---

### C1 — `Cpf`

**Onde está hoje**

- `Usuario.Cpf` é `string?` — `Usuario.cs:11`
- `Usuario.NormalizarCpf(string?)` é **estático na entidade** — `Usuario.cs:221`
- Regra: apenas dígitos, exatamente 11, sem dígito verificador (Spec 033, RF3/D7)

**Problema.** Invariante do conceito mora num método estático da entidade; qualquer outro
ponto que receba CPF precisa lembrar de chamar `NormalizarCpf`. `null` e `""` são estados
representáveis para um dado que, uma vez existente, tem forma obrigatória.

**Proposta.** `public sealed record Cpf` com fábrica `Cpf.De(string?)` → apenas dígitos, 11
posições, `ToString()` devolve o valor normalizado e `Mascarado()` devolve os três últimos
dígitos. Unicidade **não** entra no VO (é regra de serviço, como hoje).

**Impacto.** `Usuario.cs`, `UsuarioService` (unicidade), `SolicitacaoDeAcesso`, DTOs de
entrada/saída, `BootstrapAdmin`, mapeamento EF com `HasConversion`.

**Risco.** Médio — CPF aparece em snapshot de autoria (`NutricionistaResponsavel`) e em
`SolicitacaoDeAcesso`; a conversão precisa cobrir todos. O `aprovacao` grava CPF validado
(Spec 033), então há caminho de escrita a revisar.

---

### C3 — `NomePessoal`

**Onde está hoje — a mesma regra em dois lugares**

- `Aluno.Nome` (`Aluno.cs:39`) e `Responsavel.Nome` (`Responsavel.cs:19`) via
  `ValidacaoDeCampo.Texto(nome, ..., NomeMin, NomeMax)` — `Responsavel.cs:51`, `NomeMax = 100`
  (`Responsavel.cs:17`)
- `Usuario.AtualizarPerfil` valida **inline**, com a mesma faixa:
  `if (nome.Length < 2 || nome.Length > 100)` — `Usuario.cs:193-203`

**Problema.** Critério C2 puro: "nome de 2 a 100 caracteres, com trim" está escrito duas
vezes, em formas diferentes (helper × `if` inline). Mudar a faixa exige tocar dois arquivos
e não há garantia de que continuem iguais — o `ValidacaoDeCampo` documenta justamente que
"mudar a regra de texto exige tocar apenas este arquivo", o que hoje é falso para `Usuario`.

**Proposta.** `public sealed record NomePessoal` com trim, mínimo 2, máximo 100 e
`ToString()`. `ValidacaoDeCampo.Texto` permanece para textos livres com limites próprios
(observação, motivo, nome de cardápio), que são conceitos **diferentes** de nome de pessoa.

**Impacto.** `Aluno`, `Usuario`, `Responsavel`, `ValidacaoDeCampo`, mapeamentos EF, DTOs de
entrada/saída e o snapshot `NutricionistaResponsavel.Nome`.

**Risco.** Baixo-médio: são três entidades e dois snapshots, mas a mudança é mecânica e
coberta por testes de invariante existentes.

---

### C2 — `Telefone`

**Onde está hoje**

- `Aluno.Telefone` (`Aluno.cs:50`), `Responsavel.Telefone` (`Responsavel.cs:21`),
  `Usuario.Telefone` (`Usuario.cs:14`)
- Regra em `ValidacaoDeCampo.Telefone` (`ValidacaoDeCampo.cs:39-50`): opcional, 10 ou 11
  dígitos, sempre sem máscara

**Problema.** A regra já está centralizada (bom), mas o tipo não: três entidades guardam
`string.Empty` como "sem telefone", então ausência e valor vazio são indistinguíveis e toda
comparação precisa do helper. Critério C5: um telefone com 9 dígitos nunca deveria existir.

**Proposta.** `public sealed record Telefone` com `Telefone.Nenhum` (ou uso de `null`) e
fábrica que aceita vazio → ausência explícita. Não confundir com telefone de responsável
(snapshot) — mesma regra, tipo único.

**Impacto.** Três entidades, `AlunoEntrada`, `DadosAlunoDTO` de saída, mapeamentos EF.

**Risco.** Baixo. Ganho principal é eliminar `""` como telefone.

---

### C4 — `EmailDeContato`

**Onde está hoje**

- `Usuario.Email` é `string?` — `Usuario.cs:13`
- Regra **inline** em `AtualizarPerfil` (`Usuario.cs:193-212`): trim, `ToLowerInvariant`,
  máximo 150 e `MailAddress.TryCreate` com `endereco.Address != email`

**Problema.** Normalização (minúsculas) e validação no mesmo método que atualiza **nome** —
dois conceitos, um método. Critério C2/C5: e-mail em maiúsculas ou com caixa mista é
representável e a unicidade depende do serviço lembrar de normalizar.

**Proposta.** `public sealed record EmailDeContato` com normalização para minúsculas na
fábrica e validação de formato/limite. `AtualizarPerfil` passa a receber `(NomePessoal,
EmailDeContato)` — ou os setters viram `DefinirNome`/`DefinirEmail`.

**Impacto.** `Usuario.cs`, `UsuarioService` (unicidade e login), `AutenticacaoService`,
DTOs de entrada/saída, `Perfil`.

**Risco.** Médio: o login compara e-mail — qualquer mudança de normalização afeta sessão.
Exige teste de "login com e-mail em maiúsculas" antes e depois.

---

### C8 — `RestricaoDoAluno`

**Onde está hoje**

- `Aluno.Restricoes` é `IReadOnlyList<string>` sobre o campo `_restricoes` — `Aluno.cs:54`
- Persistida como JSON, com mapeamento que depende do nome do campo privado —
  `ConnectionContext.cs:219-226`
- `AlunoEntrada.Restricoes` é `List<string>` — `Application/AlunoEntrada.cs`
- Regras no agregado: máximo 20 (`Aluno.cs:28`), sem duplicata case-insensitive,
  `NormalizarRestricao`/`ContemRestricao`/`RemoverRestricao` (`Aluno.cs:222-250`)

**Problema.** O catálogo de restrições é **normalizado** (`RestricaoAlimentar` com
`NomeNormalizado`), mas o vínculo do aluno é **texto em JSON**. Isso já está registrado
como dívida em `arch.md` §9 ("`Aluno.Restricoes` em JSON … reavaliar na migração"). O
agregado `Aluno` passa a conhecer regra de catálogo que não é dele, e renomear uma restrição
no catálogo deixa o aluno com o nome antigo, em silêncio.

**Proposta.** `public sealed record RestricaoDoAluno(int RestricaoId, string NomeNormalizado)`
ou entidade de junção `AlunoRestricao(AlunoId, RestricaoId)`. O nome de exibição passa a vir
do catálogo, não de cópia. `RemoverRestricao(string)` vira `RemoverRestricao(RestricaoId)`.

**Impacto.** Alta: `Aluno.cs`, `AlunoEntrada`, `ConnectionContext` (trocar o JSON por tabela),
**migration nova**, `CardapioService`/`RestricoesDoAluno`/`AdaptacaoDeCardapio` (que hoje
casam por nome), telas de aluno e portal da família.

**Risco.** Alto — é a mudança de maior superfície da lista. Sugerido tratar como spec
própria, não como refatoração incidental. É a que mais paga, porque hoje o casamento por
nome é o ponto frágil de todo o fluxo de adaptação de cardápio.

---

### C6 — `TurmaDoAluno` (representação dupla de escola)

**Onde está hoje**

- `Turma.Escola` (`string`) e `Turma.Serie` (`string`) — `Turma.cs:20,22`
- `Aluno.EscolaId` (`int?`) — `Aluno.cs:79`
- `AlunoEntrada` tem **os dois**: `Escola` (string) e `EscolaId` (`int?`) —
  `Application/AlunoEntrada.cs`

**Problema.** Critério C3: a escola do aluno existe como **texto** (em `Turma`) e como
**referência** (em `Aluno.EscolaId`). Dois dados para o mesmo fato significam duas verdades:
transferir de escola pode atualizar um e não o outro, e o texto não tem integridade
referencial. O `escolaId` foi introduzido pela Spec 027 sem remover o texto da Spec 001.

**Proposta.** `public sealed record TurmaDoAluno(int EscolaId, string Serie)` (ou manter
`Turma` como entidade mas com `EscolaId` em vez de `Escola` texto), e **remover** `Escola` de
`AlunoEntrada`, aceitando apenas `EscolaId`. Exibição resolve o nome pela FK.

**Impacto.** `Turma`, `Aluno` (construtor, `AtualizarDados`, `TransferirEscola`), DTOs,
`AlunoRepository` (filtro por `Serie` hoje compara texto), telas de cadastro e visualização,
relatório de alunos.

**Risco.** Médio-alto: exige backfill de `Turma.Escola` → id (é justamente o tipo de decisão
que precisa de migration com decisão humana quando há texto sem correspondência).

---

### C7 — `ChaveDeConjuntoDeRestricoes`

**Onde está hoje**

- `Cardapio.ConjuntoRestricoesChave` é `string?` — `Cardapio.cs:50`
- Produzida/consumida por `ConjuntoDeRestricoes.DaChave(string?)` — VO existente
- Usada no índice único filtrado `(OrigemId, EscolaId, ConjuntoRestricoesChave)`

**Problema.** Critério C4: o VO `ConjuntoDeRestricoes` já tem a chave canônica, mas o
`Cardapio` guarda a **string crua** e a converte de volta em vários pontos. Uma chave mal
formada no banco não é detectada por tipo, e `Cardapio` precisa conhecer o formato textual.

**Proposta.** `public sealed record ChaveDeConjuntoDeRestricoes` com `De(ConjuntoDeRestricoes)`
e `ParaConjunto()`, persistida por `HasConversion` para string (mantém o índice).

**Impacto.** `Cardapio.cs`, `AdaptarCardapioService`, `CardapioService`,
`PublicacaoCardapioService`, `ContextoDeRestricoesService`, `ConnectionContext`.

**Risco.** Baixo-médio: sem migration (mesma coluna), mas com teste de round-trip obrigatório,
inclusive para o índice único.

---

### C9 — `Gramatura`

**Onde está hoje**

- `ItemRefeicao.QuantidadeGramas` é `decimal` — `ItemRefeicao.cs:21`
- Limite `GramasMaximas = 5000m` — `ItemRefeicao.cs:15`
- Validação via `ValidacaoDeCampo.Medida` — `ValidacaoDeCampo.cs:58`

**Problema.** O limite de 5000 g pertence a **gramatura de item de refeição**, não a
"medida nutricional por 100 g" (que é o conceito de `ValidacaoDeCampo.Medida`). Um decimal
fora da faixa é representável até a validação do construtor; `CalculoNutricional` recebe
`decimal` solto e não sabe se é grama, kcal ou mg.

**Proposta.** `public sealed record Gramatura` com máximo 5000, duas casas e
`MultiplicarPor(ValoresNutricionais)` para concentrar a regra de proporção.

**Impacto.** `ItemRefeicao`, `CalculoNutricional`, `AdaptacaoDeCardapio`, DTOs de cardápio,
`ConnectionContext` (tipo complexo ou conversão).

**Risco.** Baixo, porém ganho moderado — a regra de proporção já está isolada em
`CalculoNutricional`. Só vale se houver intenção de unificar cálculo.

---

## 5. Candidatos de frontend

Contexto medido: **zero** ocorrências de `any`/`as any` em `frontend/src` — o problema aqui
não é tipo frouxo, é **tipo duplicado**.

### F1 — `CategoriaRestricao` em três arquivos

Declarado separadamente em `services/AlimentoApi.ts`, `services/PainelApi.ts` e
`services/RestricaoApi.ts`. Mudar a união de categorias exige editar três arquivos e não há
garantia de que fiquem iguais (mesmo critério C2 do backend).

**Proposta.** módulo único `frontend/src/tipos/dominio.ts` com as uniões de vocabulário;
serviços importam dele.

### F2 — `StatusOcorrencia` e `TipoOcorrencia` em quatro arquivos

Declarados em `services/OcorrenciaApi.ts`, `services/PainelApi.ts`,
`pages/ocorrencias/OcorrenciasPage.tsx` e `pages/pais/OcorrenciasPais.tsx`.

### F3 — `TipoEventoAluno` em três arquivos

Declarado em `services/HistoricoAlunoApi.ts`, `services/PainelApi.ts` e
`pages/pais/RelatorioPais.tsx`.

### F3b — `Pagina` (envelope de paginação) em dois arquivos

Declarado em `services/CardapioApi.ts` e `services/SolicitacaoAcessoApi.ts`; é o mesmo
envelope `{ itens, total, pagina, tamanho }` que o backend devolve em todas as listagens
paginadas.

### F4 — Literais de status repetidos fora de tipo

Contagem de arquivos que citam o literal: `EmAnalise` 6, `PendenteDeRevisao` 4,
`Desatualizado` 4, `Aprovada` 4, `Recusada` 4, `Encerrado` 2, `Respondido` 2. Sem uma
constante ou união única, renomear um status no backend quebra a tela em silêncio (sem erro
de compilação).

**Proposta.** um arquivo de vocabulário (`tipos/dominio.ts`) com as uniões **e** os mapas de
rótulo/cor usados nas telas; páginas passam a referenciar as uniões em vez de literais.

**Risco.** Baixo — `npm run build` e `npm run lint` pegam cada divergência. Não há teste de
frontend (PEND-049), então a verificação é o compilador.

---

## 6. Ordem sugerida e restrições técnicas

**Ordem por valor ÷ risco:** C5 → C1 → C3 → C2 → C4 → C7 → F1–F4 → C9 → C6 → C8.

Motivo: os cinco primeiros não mudam schema de forma destrutiva (conversão de coluna),
têm invariante já testada e reduzem duplicação imediata. C6 e C8 mudam dados existentes e
devem ser specs próprias.

**Restrições que qualquer um destes candidatos enfrenta:**

1. **EF + SQLite.** VO mapeado como tipo complexo exige construtor sem parâmetros e setters
   privados — `record` só de leitura não funciona. Evidência: comentário de
   `PeriodoVigencia.cs` e o padrão de `ValoresNutricionais.cs`. Alternativa: `HasConversion`
   para coluna única (usada em `ConnectionContext.cs:611`).
2. **Migration obrigatória** para C8 e C6 (`arch.md` §8: "toda mudança de schema exige
   migration nova, nunca alteração do snapshot manual").
3. **Gate de documentação.** Se a mudança tocar contrato de API, a tabela de endpoints do
   `README.md` entra na mesma alteração (`DocumentationTests`).
4. **Teste antes.** `lessons.md`/`arch.md` §8.5: toda correção acompanha teste que falha
   antes e passa depois — vale para cada conversão.
5. **Não misturar.** Um candidato por alteração; converter VO e mudar schema no mesmo commit
   impede atribuir regressão.

---

## 7. Fora de escopo deste levantamento

- `Filtro*` e `IndicadoresDaUnidade` são read models de consulta, não conceitos com
  invariante — mantidos como estão (Spec 024).
- Enums de status (`StatusOcorrencia`, `StatusSolicitacao`, `TipoNotificacao` etc.) já são
  tipos de domínio; o problema é só a duplicação do lado do frontend (F1–F4).
- `UsuarioService` (757 linhas) e a extração de `SolicitarAlteracaoDeDadosService` são
  problemas de **coesão de serviço**, não de tipo de domínio: já registrados em PEND-027 e
  PEND-043.
