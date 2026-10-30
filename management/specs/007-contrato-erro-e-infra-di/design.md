---
name: contrato-erro-e-infra-di
status: created
references: V8, ADR-005, L4
---

# Design — Contrato de erro único e infraestrutura de DI

## Estrutura

```text
backend/API/Middleware/ErrorHandlingMiddleware.cs   (novo)
backend/Program.cs                                  (registro, DI, connection string)
backend/API/Controllers/AlunoController.cs          (- try/catch)
backend/API/Controllers/UsuarioController.cs        (- try/catch, respostas { erro })
backend.Tests/Integration/ErroApiTests.cs           (novo)
```

Nenhuma entidade nem migration.

## Middleware

```csharp
public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception excecao)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(excecao, "Falha após o início da resposta.");
                throw;
            }

            var (status, mensagem) = Mapear(excecao);
            if (status >= 500)
                _logger.LogError(excecao, "Erro não tratado em {Rota}.", context.Request.Path);
            else
                _logger.LogWarning("Erro previsível em {Rota}: {Mensagem}", context.Request.Path, excecao.Message);

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { erro = mensagem });
        }
    }

    private static (int Status, string Mensagem) Mapear(Exception excecao) => excecao switch
    {
        ArgumentException => (400, excecao.Message),
        KeyNotFoundException => (404, excecao.Message),
        InvalidOperationException => (409, excecao.Message),
        DbUpdateConcurrencyException => (409, "O registro foi alterado por outra operação."),
        _ => (500, "Erro interno.")
    };
}
```

`TransicaoInvalidaException` (Spec 015) herda de `InvalidOperationException`, então
esta mesma regra a traduz em 409 sem que o middleware conheça o tipo da spec.

Registro em `Program.cs`, antes da autenticação:

```csharp
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
```

`Response.Clear()` só é chamado porque `HasStarted` foi verificado; sem essa guarda o
middleware quebraria respostas em streaming.

## 401 e 403 no contrato

Os eventos de cookie passam a escrever JSON:

```csharp
options.Events.OnRedirectToLogin = context =>
{
    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new { erro = "Sessão inválida ou ausente." });
};

options.Events.OnRedirectToAccessDenied = context =>
{
    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new { erro = "Acesso negado." });
};
```

## Controllers

- Remover `try/catch (ArgumentException)` de `AlunoController` e `UsuarioController`.
- Trocar `NotFound("...")` por `NotFound(new { erro = "..." })` e `BadRequest("...")`
  por `BadRequest(new { erro = "..." })`.
- `DELETE /api/aluno/{id}` continua 204 ou 404 no formato novo.
- Nenhuma regra nova é adicionada ao controller.

## Infraestrutura de DI

```csharp
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source = database.db";

builder.Services.AddDbContext<ConnectionContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<IAlunoRepository, AlunoRepository>();
builder.Services.AddScoped<AlunoService>();
```

`ConnectionContext.OnConfiguring` mantém `if (options.IsConfigured) return;`, então a
configuração injetada prevalece e o fallback só vale quando não há options (testes que
constroem o contexto direto continuam funcionando).

`appsettings.json` recebe:

```json
{ "ConnectionStrings": { "Default": "Data Source = database.db" } }
```

## Testes

`ErroApiTests` cobre:

| Caso | Esperado |
|---|---|
| `POST /api/aluno/registrar` inválido | 400, `erro` string não vazia |
| `GET /api/aluno/perfil/999999` | 404, `erro` |
| `GET /api/usuario/perfil/999999` | 404, `erro` (regressão do formato string) |
| `POST /api/usuario/login` inválido | 401, `erro` |
| Rota que força exceção não tratada (apenas no ambiente de teste) | 500, `erro = "Erro interno."`, sem `stack` |

A rota de 500 usa um endpoint de diagnóstico habilitado somente em `Testing`, ou um
`IExceptionHandler` testado por unidade; a decisão é por endpoint de teste para não
expor superfície em produção.

## Sequência

1. Middleware + mapeamento.
2. Registro no pipeline e eventos de cookie.
3. Limpeza dos controllers.
4. Connection string e lifetimes.
5. Testes de contrato.
6. Gates.

## Riscos

- **Ordem do middleware**: se registrado depois de `UseAuthentication`, exceções de
  autenticação não são traduzidas. Fica antes.
- **Testes que comparam string crua**: `UsuarioApiTests` verifica propriedade `erro`,
  então continua válido; qualquer teste que compare o corpo inteiro precisa mudar.
- **Connection string**: mudar o fallback quebraria o app em desenvolvimento; o
  fallback é idêntico ao valor atual.
- **500 em teste**: expor rota de diagnóstico só em `Testing` evita superfície em
  produção; se o pipeline não permitir, testar o mapeamento por unidade.
