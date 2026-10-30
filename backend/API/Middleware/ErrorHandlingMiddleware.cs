using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ntc.Middleware;

/// <summary>
/// Traduz exceções em um único contrato de erro: `{ "erro": "mensagem" }`.
/// Nenhum detalhe técnico vaza no corpo da resposta.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

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
                _logger.LogError(excecao, "Falha após o início da resposta em {Rota}.",
                    context.Request.Path);
                throw;
            }

            var (status, mensagem) = Mapear(excecao);

            if (status >= StatusCodes.Status500InternalServerError)
                _logger.LogError(excecao, "Erro não tratado em {Rota}.", context.Request.Path);
            else
                _logger.LogWarning("Erro previsível em {Rota}: {Mensagem}",
                    context.Request.Path, excecao.Message);

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { erro = mensagem });
        }
    }

    private static (int Status, string Mensagem) Mapear(Exception excecao) => excecao switch
    {
        ArgumentException => (StatusCodes.Status400BadRequest,
            string.IsNullOrWhiteSpace(excecao.Message) ? "Dados inválidos." : excecao.Message),
        KeyNotFoundException => (StatusCodes.Status404NotFound, excecao.Message),
        InvalidOperationException => (StatusCodes.Status409Conflict, excecao.Message),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            "O registro foi alterado por outra operação."),
        _ => (StatusCodes.Status500InternalServerError, "Erro interno.")
    };
}
