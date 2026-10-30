# ADR — Architecture Decision Records

Formato: `ADR-XXX: título` seguido de Status, Contexto, Decisão e Consequências.

---

## ADR-001: Manter ASP.NET Core 10 com EF Core e SQLite

**Status:** aceito

**Contexto:** o projeto é acadêmico, executado localmente, com um único desenvolvedor
por vez e entrega em prazo curto. O banco precisa ser criado sem servidor.

**Decisão:** manter ASP.NET Core 10 + EF Core 10 + SQLite, aplicando migrations no
startup.

**Consequências:**
- Positivo: execução local sem infraestrutura, migrations versionadas, `dotnet run`
  suficiente para subir a API.
- Negativo: `Database.Migrate()` no startup não escala para múltiplas instâncias;
  SQLite tem escrita serializada.
- Aceitável no escopo atual; revisar apenas se houver deploy concorrente.

---

## ADR-002: Autenticação por cookie HTTP-only em vez de token no cliente

**Status:** aceito

**Contexto:** a aplicação é uma SPA servida na mesma origem da API em desenvolvimento,
e o frontend já implementa recuperação de sessão por cookie.

**Decisão:** manter cookie `NutriEduc.Auth` HTTP-only, `SameSite=Lax`, 8h com sliding
expiration, e exigir `credentials: "include"` em toda chamada autenticada.

**Consequências:**
- Positivo: nenhum token acessível por JavaScript; `HttpOnly` reduz superfície de XSS.
- Negativo: exige proxy same-origin ou CORS com credenciais; `SecurePolicy.SameAsRequest`
  precisa se tornar `Always` em produção.
- Regra derivada: ausência de `credentials` é tratada como defeito (Spec 008).

---

## ADR-003: Hash de senha com PBKDF2 nativo, sem dependência nova

**Status:** aceito

**Contexto:** senhas estão em texto claro. Adicionar `Microsoft.AspNetCore.Identity`
introduziria dependência apenas para hashing, num projeto que tem somente pacotes de
EF Core.

**Decisão:** usar PBKDF2-SHA256 via `System.Security.Cryptography.Rfc2898DeriveBytes`,
salt aleatório por usuário, ≥100.000 iterações, formato persistido
`pbkdf2-sha256$<iteracoes>$<salt>$<hash>` em base64, tudo encapsulado no domínio de
`Usuario`.

**Consequências:**
- Positivo: zero dependência nova; determinístico e testável sem infraestrutura.
- Negativo: implementação própria exige cuidado com comparação em tempo constante;
  usar `CryptographicOperations.FixedTimeEquals`.
- Migração: valores sem o prefixo são tratados como legado, comparados em claro uma
  única vez e re-hasheados no login bem-sucedido.

---

## ADR-004: Validação de invariantes exclusivamente no domínio

**Status:** aceito

**Contexto:** hoje o frontend valida nome, e-mail e telefone; o backend não valida
nada em `Aluno` e valida parcialmente em `Usuario`. Regras duplicadas divergem.

**Decisão:** a validação que garante integridade pertence à entidade. DTOs validam
apenas forma e presença. O frontend valida apenas para experiência de uso e nunca é
fonte de verdade.

**Consequências:**
- Positivo: uma única fonte de verdade por regra; teste unitário de domínio cobre a
  regra sem subir HTTP nem banco.
- Negativo: mensagens de erro passam a vir do domínio, exigindo contrato de erro
  padronizado (ADR-005).
- Sequência obrigatória: validar tudo → atribuir no fim (espelha `AtualizarPerfil`).

---

## ADR-005: Contrato de erro único com middleware global

**Status:** aceito

**Contexto:** cada controller inventa um texto de erro e usa `BadRequest` genérico,
inclusive com erro de digitação na mensagem. O frontend exibe texto cru.

**Decisão:** handler global converte `ArgumentException` em 400 e exceções não
tratadas em 500, sempre no formato `{ "erro": "mensagem" }`, sem stack trace.

**Consequências:**
- Positivo: frontend consome um único formato; mensagem de domínio chega ao usuário
  sem tradução manual por endpoint.
- Negativo: exige mapear exceções esperadas; exceção não mapeada vira 500 e precisa
  de log.
- Nenhum `catch` vazio é permitido; falha silenciosa é defeito.

---

## ADR-006: `Aluno` recebe coleção protegida em vez de `List<string>` pública

**Status:** aceito

**Contexto:** `List<string> Restricoes` com `private set` continua aceitando
`aluno.Restricoes.Add(...)` de fora, quebrando a invariante de unicidade e permitindo
estado inválido sem passar por nenhuma regra.

**Decisão:** expor `IReadOnlyList<string>` sobre um campo privado, com
`AdicionarRestricao`/`RemoverRestricao` contendo as invariantes (não vazio após trim,
sem duplicata case-insensitive, máximo 20 itens). A persistência continua na coluna
`Restricoes` como JSON, via conversor.

**Consequências:**
- Positivo: encapsulamento real sem migration de schema.
- Negativo: consultas por restrição continuam impossíveis em SQL; normalização fica
  dependente da entidade `Restricao` (Spec 010).

---

## ADR-007: Escola e unidade são escopo de autorização, não texto

**Status:** aceito (2026-10-30)

**Contexto:** `Aluno.Turma.Escola` é uma string e `Usuario` não tem papel nem vínculo
institucional. Sem chave, filtrar "alunos da minha unidade" exigiria comparar texto,
e o mesmo nome de escola em unidades diferentes seria indistinguível.

**Decisão:** `Escola` e `UnidadeEscolar` viram entidades. `Usuario` recebe `Papel`,
`EscolaId`, `UnidadeId` e `Ativo`; `Aluno` recebe `UnidadeId` anulável. `Turma.Escola`
permanece apenas como rótulo de exibição. O escopo é resolvido no backend
(`EscopoService`) e aplicado na consulta SQL.

**Consequências:**
- Positivo: autorização por chave, não por texto; uma única fonte de escopo.
- Negativo: dados legados ficam sem `UnidadeId` e só são visíveis para nutrição e
  secretaria até serem vinculados.
- Negativo: `AdminEscola` passa a depender de migração de dados para enxergar alunos
  antigos; a alternativa (inferir escola por texto) foi rejeitada por não ser chave.

---

## ADR-008: Desativação em vez de exclusão física para cadastros referenciados

**Status:** aceito (2026-10-30)

**Contexto:** escola, unidade, usuário, alimento, restrição e cardápio são
referenciados por histórico, ocorrências e relatórios. Excluir a linha quebraria
referências ou apagaria auditoria.

**Decisão:** cadastros referenciados têm `Ativo` (ou situação equivalente) e operação
`Desativar`/`Reativar`. Exclusão física só é permitida onde não há referência
(cardápio em `Rascunho`, por exemplo, com cascata no próprio histórico).

**Consequências:**
- Positivo: histórico e relatórios continuam íntegros; reativação é trivial.
- Negativo: toda consulta de seleção precisa filtrar `Ativo` explicitamente; a
  ausência do filtro é um defeito de teste, não uma escolha.

---

## ADR-009: Histórico append-only com snapshots de autor e alvo

**Status:** aceito (2026-10-30)

**Contexto:** restrições do aluno, status de cardápio, ocorrências e solicitações
precisam de rastro de "quem mudou o quê e quando". Se o histórico guardar apenas FKs,
renomear ou desativar o autor reescreve o passado; excluir o aluno apaga o motivo do
registro.

**Decisão:** cada agregado com ciclo de vida tem uma coleção de eventos somente
inserção, com `AutorNome`, `CriadoEmUtc` e cópia dos nomes relevantes do alvo
(`RestricaoNome`, `AlunoId` + nome no DTO da ocorrência). Não há endpoint de edição
ou exclusão de evento.

**Consequências:**
- Positivo: auditoria imutável e legível sem joins.
- Negativo: duplicação controlada de nomes; renomear um aluno não corrige o histórico
  antigo — isso é intencional.
- Negativo: coleções crescem sem limite; toda leitura é paginada e indexada.

---

## ADR-010: Concorrência otimista com `ConcurrencyCheck` no SQLite

**Status:** aceito (2026-10-30)

**Contexto:** publicar cardápio e decidir solicitação são operações com pré-condição
de estado. Duas requisições simultâneas poderiam publicar duas vezes ou aprovar duas
vezes. SQLite não oferece `rowversion` como o SQL Server.

**Decisão:** as propriedades de estado (`Situacao`, `Status`) e `Versao` recebem
`[ConcurrencyCheck]`. O `UPDATE` inclui os valores originais; conflito gera
`DbUpdateConcurrencyException`, traduzida para 409 pelo middleware.

**Consequências:**
- Positivo: sem lock explícito e sem dependência de banco específico.
- Negativo: colunas de estado passam a fazer parte do `WHERE`; atualização em massa
  fora do agregado precisa respeitar isso.

---

## ADR-011: Compatibilidade é função pura com severidade única

**Status:** aceito (2026-10-30)

**Contexto:** três fluxos precisam da mesma resposta — geração (013), consulta (014) e
publicação (015). Implementar o cruzamento em cada um garantiria divergência na
primeira correção.

**Decisão:** `AnaliseCompatibilidade.Analisar` recebe records já carregados e devolve
achados com severidade (`Bloqueante` para vínculo `Alergenico`, `Alerta` para
`Restricao`). O domínio não consulta banco, relógio nem serviço. Geração consome
`AlimentosNaoPermitidos`; publicação consome `Compativel`.

**Consequências:**
- Positivo: uma regra, três consumidores, testável sem infraestrutura.
- Negativo: o serviço de aplicação precisa carregar itens, vínculos e alunos antes de
  chamar; a função não resolve dados por conta própria.

---

## ADR-012: Relatório é tabela; exportador é plugável

**Status:** aceito (2026-10-30)

**Contexto:** PDF, Excel e Word compartilham os mesmos dados e filtros. Se cada
exportador montasse sua consulta, os totais divergiriam entre formatos.

**Decisão:** todo relatório produz `RelatorioGerado` (colunas tipadas, linhas, totais,
ordenação, filtros aplicados). Os exportadores implementam `IRelatorioExportador` e
recebem esse objeto pronto, sem acesso a banco. A pré-visualização usa o mesmo
objeto, limitado a 200 linhas, com total agregado separado.

**Consequências:**
- Positivo: preview e arquivo nunca divergem; novo formato não altera relatório.
- Negativo: materializar 50.000 linhas em memória é o limite aceito; acima disso,
  413 — não há streaming nesta fase.
