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
