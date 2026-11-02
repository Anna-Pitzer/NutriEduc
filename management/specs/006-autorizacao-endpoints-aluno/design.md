---
name: autorizacao-endpoints-aluno
status: created
references: V5, ADR-002, L6
---

# Design — Autorização dos endpoints de aluno

## Estrutura

```text
backend/API/Controllers/AlunoController.cs   ([Authorize] por endpoint desde a Spec 009)
backend/Program.cs                           (policies LeituraAluno/GestaoAluno e eventos do cookie)
frontend/src/services/AlunoApi.ts            (+ credentials em 5 chamadas)
frontend/src/services/erroApi.ts             (+ ErroRequisicao/naoAutorizado)
frontend/src/pages/nutricao/alunos/*.tsx     (logout() ao receber 401)
backend.Tests/Integration/ApiFactory.cs      (+ CreateAuthenticatedClientAsync)
backend.Tests/Integration/AlunoApiTests.cs   (autenticado + 401 anônimo e cookie inválido)
```

Nenhuma entidade, migration ou serviço novo. A mudança é de borda.

## Backend

```csharp
[ApiController]
[Route("api/[controller]")]
public class AlunoController : ControllerBase
```

Na Spec 006 era `[Authorize]` na classe, cobrindo os cinco endpoints de uma vez (RF6).
A Spec 009 o substituiu por `[Authorize(Policy = "LeituraAluno")]` nos `GET` e
`[Authorize(Policy = "GestaoAluno")]` nos verbos de escrita, porque a regra passou a
depender do papel. Não há `[AllowAnonymous]` em nenhum endpoint. O esquema de cookie
está em `Program.cs`, e `OnRedirectToLogin`/`OnRedirectToAccessDenied` devolvem 401/403
em JSON em vez de redirecionar para HTML.

Não se cria policy nem `RequireRole`: qualquer sessão válida passa. A distinção de
papel e o escopo por unidade entram na Spec 009, que substituirá o atributo por
política sem mudar as rotas.

## Frontend

`AlunoApi.ts` passa a declarar a credencial em todas as chamadas:

```ts
await fetch(`${ApiURL}/api/aluno`, { credentials: "include" });
```

O `ApiURL` é vazio e o proxy do Vite encaminha `/api` para `http://localhost:5001`;
o cookie é same-origin e `SameSite=Lax` continua válido.

Tratamento de 401: `ErroRequisicao` carrega o status e `naoAutorizado(error)` responde
por `status === 401`; as telas de aluno chamam `logout()` nesse caso, em vez de mostrar
"Erro ao buscar alunos". `logout()` limpa a sessão e `ProtectedRoutes` redireciona para
`/login`. O helper vive em `erroApi.ts`, não duplicado em cada `catch`.

## Testes de integração

Os testes atuais chamam a API sem cookie; com `[Authorize]` todos passariam a
receber 401. A fábrica ganha um helper:

```csharp
public async Task<HttpClient> CreateAuthenticatedClientAsync()
{
    var client = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
    var email = $"aluno.api.{Guid.NewGuid():N}@nutrieduc.local";

    await client.PostAsJsonAsync("/api/usuario/registrar", new
    {
        nome = "Usuario de Teste", cpf = "12345678901",
        dataNascimento = "1990-01-01", email, senha = "senha12345",
        telefone = (string?)null, foto = (string?)null
    });
    await client.PostAsJsonAsync("/api/usuario/login", new { email, senha = "senha12345" });
    return client;
}
```

`AlunoApiTests` passa a usar esse cliente. Um teste novo, com cliente anônimo,
percorre os cinco verbos e exige 401. Nenhum teste existente muda de intenção: apenas
troca o cliente anônimo pelo autenticado.

## Contrato

| Endpoint | Anônimo | Sessão válida |
|---|---|---|
| `POST /api/aluno/registrar` | 401 | 200 ou 400 |
| `GET /api/aluno` | 401 | 200 |
| `GET /api/aluno/perfil/{id}` | 401 | 200 ou 404 |
| `PUT /api/aluno/{id}` | 401 | 200 ou 400/404 |
| `DELETE /api/aluno/{id}` | 401 | 204 ou 404 |

## Sequência

1. `[Authorize]` no controller.
2. Helper de cliente autenticado na fábrica.
3. Migrar `AlunoApiTests` para o cliente autenticado.
4. Teste dos cinco verbos anônimos.
5. `credentials: "include"` e tratamento de 401 no frontend.
6. Gates.

## Riscos

- **Testes que dependiam de acesso anônimo**: a migração para cliente autenticado é
  obrigatória na mesma mudança; deixar para depois produz suíte vermelha.
- **401 sem corpo**: o frontend não pode depender de `{ erro }` antes da Spec 007;
  o tratamento por status é o caminho.
- **Usuário de teste**: o helper cria um usuário por cliente; como o banco é em
  memória por classe, não há colisão de e-mail.
- **Rotas públicas futuras**: cardápios publicados (Spec 012) terão controller
  próprio e não herdam este atributo.

> **Atualização de 2026-10-31 (auditoria):** o `[Authorize]` de classe foi substituído
> pelas policies por endpoint da Spec 009, o 401 passou a ter corpo `{ erro }` (Spec 007)
> e o teste de integração ganhou o caso de cookie adulterado. Rotas, payloads e o
> contrato observável de 401 anônimo seguem os mesmos.
