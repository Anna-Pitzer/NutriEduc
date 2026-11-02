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
achados com severidade derivada da **categoria da restrição** (`CategoriaRestricao` da
Spec 010): `Bloqueante` para `Alergia`, `Alerta` para `Restricao`. O vínculo entre
alimento e restrição é só o par, sem motivo próprio (Spec 011). O domínio não consulta
banco, relógio nem serviço. Geração consome `AlimentosNaoPermitidos`; publicação consome
`Compativel`.

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

## ADR-014: Exclusão definitiva com bloqueio por referência

**Status:** aceito (2026-10-31)

**Contexto:** o ADR-008 preferiu desativação para não perder histórico. A secretaria pediu
excluir escolas, unidades e pessoas: sem exclusão, um cadastro criado por engano fica no
sistema para sempre e continua aparecendo nas listas.

**Decisão:** `DELETE` em escola, unidade e usuário, permitido **só quando nada referencia**
o registro; caso contrário `409` com a mensagem do que impede. Desativar continua
disponível para cadastros em uso. Regras: escola bloqueia com usuário ou unidade legada;
unidade bloqueia com usuário, aluno, token de cadastro ou consumo de bootstrap; usuário
bloqueia a própria conta, o último `AdminSecretaria`, vínculo de responsável e token já
emitido. A checagem vive em `ExclusaoService`, uma única fonte para HTTP e UI.

**Consequências:**
- Positivo: cadastro errado é removível; a mensagem diz exatamente o que desbloqueia.
- Negativo: a exclusão é irreversível e não há trilha de auditoria do registro removido,
  apenas o log padrão da requisição.

## ADR-013: Unidade é da rede, não da escola

**Status:** aceito (2026-10-31)

**Contexto:** a Spec 009 modelou `UnidadeEscolar` como subdivisão de uma escola, com nome
único dentro dela. O cadastro por token (Spec 022) precisa de unidades organizacionais da
secretaria — ex.: "Secretaria Municipal de Educação do Rio de Janeiro" — e o nome é
digitado por quem não tem sessão. Nome único por escola tornaria a digitação ambígua e
obrigaria o usuário a conhecer a escola.

**Decisão:** `UnidadeEscolar` é entidade da rede. `EscolaId` passa a ser **opcional**
(dado legado), o nome é **único na rede** e não há rota pública de listagem: a unidade é
resolvida pelo nome exato. `AdminSecretaria` recebe a unidade como atribuição de origem,
não como escopo.

**Consequências:**
- Positivo: nome único elimina ambiguidade; o bootstrap não depende de escola.
- Negativo: `AdminEscola` perde o vínculo aluno→escola derivado da unidade — o escopo de
  aluno desse papel fica limitado a unidades que ainda guardam `EscolaId` legado. A
  correção definitiva é `Aluno.EscolaId`, registrada como pendência.
- Negativo: os endpoints `/api/escolas/{id}/unidades` foram substituídos por
  `/api/unidades` (GET e POST).

## ADR-015: Catálogo de restrições semeado e regra de vínculo no domínio

**Status:** aceito (2026-11-01)

**Contexto:** a Spec 010 entregou catálogo editável pela interface, com tela própria de
cadastro, edição e desativação. O proprietário do produto avaliou o resultado e decidiu que
a interface deve voltar a oferecer a lista fixa de restrições e alergias que existia antes
da spec, sem qualquer tela para incluir item novo. A primeira implementação também tinha um
defeito estrutural: `AlunoService` chamava `RestricaoService` para validar a existência do
nome no catálogo — serviço chamando serviço, com a regra de negócio fora do domínio, em
desacordo com ADR-004.

**Decisão:**

1. O catálogo continua existindo no backend como fonte de verdade do vínculo, mas deixa de
   ser gerenciável pela interface. As opções exibidas ao aluno são fixas e vêm do catálogo
   semeado pela migration `SeedCatalogoRestricoes` (`INSERT OR IGNORE`), com os mesmos nomes
   que a interface exibia antes da Spec 010.
2. A tela de catálogo, a rota e o item de menu foram removidos, junto das funções de CRUD
   sem consumidor no frontend. `POST`/`PUT`/`desativar`/`reativar` continuam existindo na
   API, disponíveis para administração externa.
3. A regra de existência saiu do serviço e virou objeto de domínio
   (`CatalogoDeRestricoes`): recebe as entradas ativas e devolve o nome canônico, lançando
   `ArgumentException` para nome desconhecido ou inativo. `AlunoService` passou a depender
   apenas de portas de repositório.

**Consequências:**
- Positivo: uma regra em um dono só, no domínio, testável sem infraestrutura; nenhum
  serviço chama outro serviço.
- Positivo: a interface volta ao comportamento que o proprietário considera correto, sem
  perder a validação do backend (nenhuma gravação aceita nome fora do catálogo).
- Negativo: incluir uma restrição nova passa a exigir migration nova ou chamada direta à
  API — decisão consciente, registrada também em `specs/010-restricoes-e-alergias/`.
- Negativo: as categorias do vínculo só podem evoluir fora da interface; a tela não é
  lugar de configurar catálogo.

## ADR-016: Trilha de alterações única para todos os dados do aluno

**Status:** aceito (2026-11-01) — escopo detalhado na Spec 023, ainda não implementada

**Contexto:** a Spec 010 criou `HistoricoRestricaoAluno`, presa a um único atributo. Toda
outra mudança de cadastro do aluno — nome, nascimento, turma, telefone, observação,
anafilaxia, responsável, unidade — não deixa rastro: não se sabe o que mudou, quando, nem
por quem. Repetir o padrão "uma tabela por atributo" reproduziria o mesmo erro que a
própria Spec 010 corrigiu no catálogo.

**Decisão:** uma trilha append-only genérica por aluno, `HistoricoAluno`, filha do agregado,
com `Tipo` (`Criado`, `Alterado`, `RestricaoAdicionada`, `RestricaoRemovida`), `Campo`
opcional, `ValorAnterior`/`ValorNovo` como snapshots textuais, `AutorNome` e `CriadoEmUtc`.
O diff é calculado no domínio (`Aluno.AtualizarDados` devolve as alterações) e persistido na
mesma `SaveChanges` do aluno, mantendo ADR-009 e a atomicidade da Spec 010.

A tabela `HistoricosRestricoesAlunos` é migrada para a nova trilha e removida; a rota
`GET /api/aluno/{id}/restricoes/historico` continua respondendo, agora filtrada da trilha
única, para não quebrar consumidores.

**Consequências:**
- Positivo: um lugar para "quem mudou o quê e quando" em todo o cadastro, com o mesmo
  formato de leitura e de escopo por papel já usado no histórico de restrições.
- Positivo: novo atributo não exige tabela nova.
- Negativo: uma edição ampla gera vários eventos (um por campo); a mitigação é comparar
  valores normalizados e não gravar campo inalterado.
- Negativo: a trilha cresce mais rápido que a de restrições; toda leitura é paginada e
  indexada por `(AlunoId, CriadoEmUtc)`.

## ADR-017: Colunas de ação centralizadas na listagem de alunos

**Status:** aceito (2026-11-01)

**Contexto:** na tela `/aluno/vizualizar`, os cabeçalhos "Histórico", "Editar" e "Excluir"
usavam `text-left`, enquanto os botões dentro das células já estavam centralizados. O
texto do cabeçalho ficava deslocado em relação ao ícone que ele rotula.

**Decisão:** os três cabeçalhos passaram a usar `text-center`, alinhando rótulo e ícone. As
colunas de conteúdo (Nome, Escola, Série, Restrições) continuam à esquerda.

**Consequências:**
- Positivo: cabeçalho e ação na mesma coluna visual; nenhuma mudança de comportamento,
  contrato ou dado.
- Regra derivada: coluna que contém apenas ação centralizada tem cabeçalho centralizado.
- Negativo: nenhum; é mudança de apresentação, reversível em uma linha.

## ADR-018: Fronteiras de camada explícitas

**Status:** aceito (2026-11-01)

**Contexto:** a análise da Spec 024 encontrou sete fronteiras borradas. As entidades
estavam protegidas (sem setters públicos, sem EF, invariantes no construtor), mas a
periferia vazava: `DadosAlunoDTO` servia entrada e saída; `BootstrapService` calculava
SHA-256; `CriarUsuario` sinalizava e-mail duplicado com `null` e o controller traduzia em
mensagem; `Aluno.DefinirUnidade` e `Usuario.DefinirPapel` aceitavam referência inexistente
ou desativada; `EscopoUsuario` — vocabulário de autorização — era parâmetro de portos de
domínio; `ExclusaoService` decidia política e escopo no mesmo método; `Usuario.Senha`
expunha o material de autenticação.

**Decisão:**

1. **Contratos por operação.** `AlunoEntrada`/`AlunoResposta` e `CriarUsuarioEntrada`/
   `UsuarioRespostaDTO`. Nenhum tipo de `Application` é entrada e saída na mesma rota;
   resposta anônima de controller deixa de existir.
2. **Hash de token no domínio.** `HashDeToken` é objeto de valor (criar, conferir em tempo
   constante, ler legado); `TokenDeCadastro` recebe o VO. A geração do segredo fica na
   aplicação porque depende de RNG da plataforma.
3. **Regra sinalizada por resultado.** `CriarUsuario` devolve `Resultado<int>`; novo
   `ResultadoStatus.Invalido` traduz para 400 no `RespostaController`. Nenhuma mensagem de
   regra de negócio vive em controller.
4. **Referências verificadas antes de mutar.** Existência e situação de unidade/escola são
   regra de aplicação, checadas antes de construir ou atualizar; a entidade valida o
   formato.
5. **Filtros de negócio no lugar de escopo de sessão.** `FiltroAluno` e `FiltroUsuarios`
   são o vocabulário dos portos; `FiltrosDeConsulta` é o único ponto que conhece a regra
   papel → recorte. `EscopoUsuario` deixa de atravessar o domínio.
6. **Política de exclusão pura.** `Domain/Services/PoliticaDeExclusao` recebe fatos e
   devolve o impedimento; quem pode excluir continua sendo decisão da aplicação.
7. **Senha encapsulada.** `Usuario.Senha` passa a `internal`, sem getter público; o
   exterior só pergunta `SenhaConfere`. A coluna `Senha` e o formato do hash não mudam.
8. **Serviços por intenção.** `AutenticacaoService` sai de `UsuarioService`;
   `BootstrapTokenService` e `ConfiguracaoDeBootstrap` saem de `BootstrapService`
   (399 → 262 linhas).

**Consequências:**
- Positivo: cada regra tem um dono; o domínio não conhece sessão, HTTP, EF nem DTO;
  controller não conhece mensagem de negócio.
- Positivo: `HashDeToken` e `PoliticaDeExclusao` são testáveis sem infraestrutura, e os
  testes de fronteira (`FronteirasDddTests`, `HashDeTokenTests`) provam isso.
- Negativo: `PostAdd` de usuário passou a devolver `UsuarioRespostaDTO` em vez de um
  objeto anônimo — campos aditivos, mesmos nomes de antes.
- Negativo: `UsuarioService` ainda reúne cadastro, perfil e administração (363 linhas);
  a divisão restante está em PEND-027.
- Regra derivada: nenhum tipo de `Domain` conhece sessão, HTTP, EF ou DTO; nenhum
  controller contém mensagem de regra; nenhum DTO de entrada é devolvido por rota.

## ADR-019: Unidade de trabalho no caso de uso, não no repositório

**Status:** aceito (2026-11-01)

**Contexto:** havia 19 chamadas de `SaveChanges` espalhadas pelos repositórios. Cada
método de escrita decidia sozinho quando confirmar, o que tornava impossível uma operação
atômica entre agregados: `BootstrapService.Criar` gravava usuário, consumo de unidade e
token em chamadas separadas, e uma falha no meio deixava estado parcial. A própria
`arch.md` já registrava a dívida.

**Decisão:** `IUnidadeDeTrabalho.ExecutarEmTransacao` abre transação, executa a operação,
confirma com `SaveChanges` e desfaz em qualquer exceção. Repositórios apenas preparam o
estado (`Add`/`Update`/`Delete` sem salvar). Todo caso de uso de escrita envolve sua
operação na unidade de trabalho, inclusive as de agregado único.

**Consequências:**
- Positivo: bootstrap e vínculo passam a ser atômicos; nenhuma escrita parcial silenciosa.
- Positivo: a decisão de transação fica visível no caso de uso, no mesmo lugar em que a
  regra é decidida.
- Negativo: um `SaveChanges` esquecido faz a escrita não persistir. A mitigação é teste de
  integração por fluxo e a rede dos 329 testes existentes.
- Negativo: código que chama repositório fora de um serviço (setup de teste) precisa usar a
  unidade de trabalho explicitamente — foi o caso de `ApiFactory`.

