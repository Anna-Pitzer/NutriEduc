using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ntc.Domain.Services;

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
                _logger.LogError(excecao,
                    "Falha após o início da resposta em {Rota} (correlação {Correlacao}).",
                    context.Request.Path, context.TraceIdentifier);
                throw;
            }

            var (status, mensagem, detalhes) = Mapear(excecao);

            // O identificador de correlação liga a resposta ao registro de log sem expor
            // detalhe técnico ao cliente (RF2/RNF3).
            if (status >= StatusCodes.Status500InternalServerError)
                _logger.LogError(excecao,
                    "Erro não tratado em {Rota} (correlação {Correlacao}).",
                    context.Request.Path, context.TraceIdentifier);
            else
                _logger.LogWarning(
                    "Erro previsível em {Rota} (correlação {Correlacao}): {Mensagem}",
                    context.Request.Path, context.TraceIdentifier, excecao.Message);

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            if (detalhes is null)
                await context.Response.WriteAsJsonAsync(new { erro = mensagem });
            else
                await context.Response.WriteAsJsonAsync(new { erro = mensagem, detalhes });
        }
    }

    private static (int Status, string Mensagem, object? Detalhes) Mapear(Exception excecao) => excecao switch
    {
        AcessoNegadoException negado => (negado.NaoEncontrado
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status403Forbidden,
            negado.Message, null),
        GeracaoImpossivelException geracao => (StatusCodes.Status422UnprocessableEntity,
            geracao.Message, geracao.Diagnostico),
        ValidacaoDeGeracaoException validacao => (StatusCodes.Status422UnprocessableEntity,
            validacao.Message, validacao.Erros),
        PublicacaoBloqueadaException publicacao => (StatusCodes.Status422UnprocessableEntity,
            publicacao.Message, publicacao.Detalhes),
        PublicacaoSobrepostaException sobreposta => (StatusCodes.Status409Conflict,
            sobreposta.Message, new { cardapioId = sobreposta.CardapioId }),
        LimiteDeAdaptacoesException limite => (StatusCodes.Status422UnprocessableEntity,
            limite.Message, new { limite = limite.Limite, solicitadas = limite.Solicitadas }),
        ArgumentException => (StatusCodes.Status400BadRequest,
            string.IsNullOrWhiteSpace(excecao.Message) ? "Dados inválidos." : excecao.Message, null),
        KeyNotFoundException => (StatusCodes.Status404NotFound, excecao.Message, null),
        InvalidOperationException => (StatusCodes.Status409Conflict, excecao.Message, null),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            "O registro foi alterado por outra operação.", null),
        _ => (StatusCodes.Status500InternalServerError, "Erro interno.", null)
    };
}
