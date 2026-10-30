---
name: padronizacao-contrato-rotas
status: created
references: V9, V8, ADR-005, L6, L7
---

# Design — Padrão de contrato das rotas HTTP

## Estrutura

```text
backend/API/Middleware/ContratoDeErro.cs          (novo — 400 de modelo, 404/405/415)
backend/Program.cs                                (registra as duas extensões)
backend/API/Controllers/UsuarioController.cs      (201, perfil autorizado, {id:int})
backend/API/Controllers/AlunoController.cs        (201, {id:int})
backend/API/Services/UsuarioService.cs            (CriarUsuario -> int?, - TrocarSenha)
backend/API/Services/AlunoService.cs              (CriarAluno -> DadosAlunoDTO)
backend/Application/AtualizarPerfilDTO.cs         (mensagens em português)
backend/Application/AlterarSenhaDTO.cs            (apagado)
backend.Tests/Integration/ContratoRotasTests.cs   (novo)
backend.Tests/Integration/{Aluno,Usuario,Erro}ApiTests.cs  (status atualizado)
backend.Tests/Services/UsuarioServiceTests.cs     (- 2 testes de TrocarSenha)
README.md                                         (201, 405, 415, regra de dono)
```

Nenhuma entidade, nenhuma migration, nenhum campo novo no banco.

## Autoridade única do formato de erro

`ErrorHandlingMiddleware` já traduz exceção. O que falta são os erros que **não** são
exceção: modelo inválido, verbo errado, content-type errado e rota inexistente. Os
dois pontos novos vivem no mesmo arquivo, para que o formato de erro continue com um
dono só:

```csharp
namespace Ntc.Middleware;

public static class ContratoDeErro
{
    public static IServiceCollection AddContratoDeErro(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = contexto =>
            {
                var mensagem = contexto.ModelState
                    .Where(campo => campo.Value?.Errors.Count > 0)
                    .Select(campo => Texto(campo.Key, campo.Value!.Errors[0]))
                    .FirstOrDefault() ?? "Dados inválidos.";

                return new BadRequestObjectResult(new { erro = mensagem });
            };
        });

        return services;
    }

    // Chave "" ou "$" é erro de leitura do JSON, não de campo.
    private static string Texto(string chave, ModelError erro) =>
        chave is "" or "$"
            ? "Requisição inválida."
            : string.IsNullOrWhiteSpace(erro.ErrorMessage)
                ? "Dados inválidos."
                : erro.ErrorMessage;

    public static IApplicationBuilder UseContratoDeErro(this IApplicationBuilder app) =>
        app.UseStatusCodePages(async contexto =>
        {
            var resposta = contexto.HttpContext.Response;

            var erro = resposta.StatusCode switch
            {
                404 => "Recurso não encontrado.",
                405 => "Método não permitido.",
                415 => "Formato de conteúdo não suportado.",
                _ => "Requisição inválida."
            };

            resposta.ContentType = "application/json";
            await resposta.WriteAsJsonAsync(new { erro });
        });
}
```

Registro em `Program.cs`, antes da autenticação, ao lado do middleware:

```csharp
builder.Services.AddContratoDeErro();
...
app.UseContratoDeErro();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
```

Por que não basta o middleware: `InvalidModelStateResponseFactory` roda **antes** do
controller e não lança exceção; `UseStatusCodePages` só age quando a resposta ainda não
tem corpo nem `Content-Type`, então `401`/`403` do cookie `{ erro }` (Spec 007) e os
`404 { erro }` dos controllers continuam intactos.

Mensagens do `AtualizarPerfilDTO` ganham `ErrorMessage` em português, porque
`DataAnnotations` sem mensagem responde em inglês:

```csharp
[Required(ErrorMessage = "O nome é obrigatório.")]
[StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
public string Nome { get; set; } = "";

[Required(ErrorMessage = "O e-mail é obrigatório.")]
[EmailAddress(ErrorMessage = "E-mail inválido.")]
[StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
public string Email { get; set; } = "";
```

## Criação devolve 201 com o recurso

`Add` dos repositórios já chama `SaveChanges()`, então `aluno.Id`/`usuario.Id` estão
preenchidos quando o método retorna. Nenhuma consulta extra é necessária.

### UsuarioService

`CriarUsuario` muda de `bool` para `int?`: devolve o id criado ou `null` quando o
e-mail já existe. O contrato fica explícito no XML doc do método.

```csharp
/// <summary>
/// Cria o usuário e devolve o id gerado. Devolve <c>null</c> quando o e-mail já
/// está cadastrado — o controller responde 400 nesse caso.
/// </summary>
public int? CriarUsuario(DadosUsuarioDTO dto) { ... return usuario.Id; }
```

### AlunoService

`CriarAluno` muda de `void` para `DadosAlunoDTO`, reutilizando `ParaDTO` (que já
preenche `Id`).

### Controllers

```csharp
[HttpPost("registrar")]
public IActionResult PostAddAluno([FromBody] DadosAlunoDTO dto)
{
    var criado = _alunoService.CriarAluno(dto);
    return Created($"/api/aluno/perfil/{criado.Id}", criado);
}
```

```csharp
[HttpPost("registrar")]
public IActionResult PostAdd([FromBody] DadosUsuarioDTO usuarioDTO)
{
    var id = _usuarioService.CriarUsuario(usuarioDTO);

    if (id == null)
        return BadRequest(new { erro = "E-mail já cadastrado." });

    return Created(
        $"/api/usuario/perfil/{id.Value}",
        new { id = id.Value, nome = usuarioDTO.Nome, email = usuarioDTO.Email });
}
```

O status do e-mail duplicado **continua 400**: é validação de entrada, não conflito de
estado concorrente. `409` fica reservado ao que o Spec 007 já mapeia
(`InvalidOperationException`).

## Perfil: sessão e dono

```csharp
[Authorize]
[HttpGet("perfil/{id:int}")]
public IActionResult GetUser(int id)
{
    var emailDaSessao = User.FindFirstValue(ClaimTypes.Email);
    if (string.IsNullOrWhiteSpace(emailDaSessao))
        return Unauthorized(new { erro = "Sessão inválida ou ausente." });

    var idDaSessao = _usuarioService.ObterIdPeloEmail(emailDaSessao);
    if (idDaSessao == null)
        return Unauthorized(new { erro = "Sessão inválida ou ausente." });

    if (idDaSessao.Value != id)
        return Forbid();

    var perfil = _usuarioService.ObterUsuarioPorId(id);
    if (perfil == null)
        return NotFound(new { erro = "Usuário não encontrado." });

    return Ok(new
    {
        id,
        nome = perfil.Nome,
        email = perfil.Email,
        telefone = perfil.Telefone,
        foto = perfil.Foto
    });
}
```

- A checagem de dono é a mesma de `AtualizarPerfil` (`:139-156`), sem inventar regra nova.
- `Forbid()` dispara `OnRedirectToAccessDenied`, que a Spec 007 já configurou para
  `403 { erro }`.
- O corpo é um objeto anônimo, igual ao que `AtualizarPerfil` devolve — sem DTO novo, sem
  `cpf`, sem `senha`. `Perfil.tsx` consome exatamente `nome`, `email`, `telefone` e
  `foto`, então nenhuma tela muda.

## Constraints `{id:int}`

| Rota | Antes | Depois |
|---|---|---|
| `GET /api/aluno/perfil/{id}` | `{id}` | `{id:int}` |
| `GET /api/usuario/perfil/{id}` | `{id}` | `{id:int}` |
| `PUT /api/usuario/perfil/{id}` | `{id}` | `{id:int}` |
| `PUT /api/aluno/{id:int}` | já | — |
| `DELETE /api/aluno/{id:int}` | já | — |

`DocumentationTests.Combinar` já normaliza constraints, então o `README.md` continua com
`{id}` e o teste não muda por causa disso.

## Código morto

- Apagar `backend/Application/AlterarSenhaDTO.cs`.
- Remover `UsuarioService.TrocarSenha` e os dois testes
  `TrocarSenha_ComSenhaCurta_RetornaFalse` / `TrocarSenha_ComSenhaNova_Atualiza`.
- `DefinirSenha` continua coberto pelos testes de cadastro e login.

## Testes

`Integration/ContratoRotasTests.cs` (novo):

| Caso | Esperado |
|---|---|
| `POST /api/aluno/registrar` válido | `201`, `Location` = `/api/aluno/perfil/{id}`, corpo com `id` |
| `POST /api/usuario/registrar` válido | `201`, `Location` = `/api/usuario/perfil/{id}`, corpo sem `senha` |
| `GET /api/aluno/perfil` sem cookie | `401 { erro }` |
| `GET /api/usuario/perfil/{id}` do próprio usuário | `200`, sem `cpf`, sem `senha` |
| `GET /api/usuario/perfil/{id}` de outro usuário | `403 { erro }` |
| `PUT /api/usuario/perfil/{id}` com `nome` vazio | `400 { erro }` em português |
| Login com JSON truncado | `400 { erro = "Requisição inválida." }` |
| `PATCH /api/aluno` | `405 { erro }` |
| `POST /api/aluno/registrar` com `text/plain` | `415 { erro }` |
| `GET /api/nao-existe` | `404 { erro }` |

Testes existentes que mudam:

| Arquivo | Mudança |
|---|---|
| `AlunoApiTests` | 3 asserções `OK` → `Created`; helper `CriarAluno` inclui `Location` |
| `UsuarioApiTests` | `Post_ComDadosValidos_Retorna200` → `201` |
| `ErroApiTests` | `Usuario_Inexistente_Retorna404ComErro` usa cliente autenticado; novo teste anônimo com `401` |
| `UsuarioServiceTests` | remove os 2 testes de `TrocarSenha` |

## Sequência

1. `ContratoDeErro` + registro no `Program.cs` + mensagens pt-BR do DTO.
2. `CriarAluno`/`CriarUsuario` devolvendo o criado; controllers com `201` + `Location`.
3. `GET` de perfil autorizado e sem `cpf`/`senha`.
4. Constraints `{id:int}`.
5. Remoção do código morto.
6. Testes novos e ajuste dos existentes.
7. `README.md` na mesma mudança.
8. Gates.

## Riscos

- **`UseStatusCodePages` cobre demais.** Ele escreve em qualquer resposta vazia com
  status ≥ 400; é o comportamento desejado e não afeta respostas que já têm corpo ou
  `Content-Type`.
- **`ProblemDetails` some do contrato.** Consumidores que dependessem de `errors`/
  `title` quebram — nenhum consumidor no repositório lê esses campos.
- **`201` em vez de `200` em duas rotas.** `fetch` trata `ok` para qualquer 2xx, então os
  quatro consumidores do frontend continuam funcionando; os testes de integração
  precisam ser atualizados.
- **Perfil deixa de ser público.** `Perfil.tsx` só consulta o próprio id; `buscarPerfil`
  continua válido. Qualquer uso futuro para consultar outro usuário será `403` — é o
  objetivo do PEND-001.
- **Remover `TrocarSenha`** reduz dois testes; se um endpoint de troca de senha for
  planejado, ele volta como spec própria, não como método órfão.
