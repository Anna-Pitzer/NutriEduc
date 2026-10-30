using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Ntc.Middleware;

/// <summary>
/// Completa o contrato de erro do <see cref="ErrorHandlingMiddleware"/> com os casos
/// que não passam por exceção: modelo inválido, JSON malformado, verbo não permitido,
/// content-type não suportado e rota inexistente. Todo erro sob `/api/` responde
/// `{ "erro": "mensagem" }`.
/// </summary>
public static class ContratoDeErro
{
    /// <summary>
    /// Substitui o <c>ProblemDetails</c> padrão do <c>[ApiController]</c> por
    /// <c>{ erro }</c> quando a validação de modelo falha ou o JSON não pode ser lido.
    /// </summary>
    public static IServiceCollection AddContratoDeErro(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = contexto =>
            {
                var parametros = new HashSet<string>(
                    contexto.ActionDescriptor.Parameters.Select(p => p.Name ?? string.Empty),
                    StringComparer.Ordinal);

                var mensagem = contexto.ModelState
                    .Where(campo => campo.Value?.Errors.Count > 0)
                    .Select(campo => Texto(campo.Key, campo.Value!.Errors[0], parametros))
                    .FirstOrDefault() ?? "Dados inválidos.";

                return new BadRequestObjectResult(new { erro = mensagem });
            };
        });

        // 415 e demais erros de cliente que o MVC converte em ProblemDetails.
        services.Configure<MvcOptions>(options =>
            options.Filters.Add(new ContratoDeErroFilter()));

        return services;
    }

    /// <summary>
    /// Escreve <c>{ erro }</c> quando a resposta terminou com status de erro e ainda
    /// não tem corpo. Respostas que já escreveram corpo (controllers, cookie 401/403
    /// e o middleware de exceções) passam intactas.
    /// </summary>
    public static IApplicationBuilder UseContratoDeErro(this IApplicationBuilder app) =>
        app.UseStatusCodePages(async contexto =>
        {
            var resposta = contexto.HttpContext.Response;

            resposta.ContentType = "application/json";
            await resposta.WriteAsJsonAsync(new { erro = Mensagem(resposta.StatusCode) });
        });

    private static string Mensagem(int status) => status switch
    {
        404 => "Recurso não encontrado.",
        405 => "Método não permitido.",
        415 => "Formato de conteúdo não suportado.",
        _ => "Requisição inválida."
    };

    private static string Texto(
        string chave,
        ModelError erro,
        HashSet<string> parametros) =>
        parametros.Contains(chave) || chave is "" or "$" || erro.Exception is JsonException
            ? "Requisição inválida."
            : string.IsNullOrWhiteSpace(erro.ErrorMessage)
                ? "Dados inválidos."
                : erro.ErrorMessage;
}

/// <summary>
/// Converte em <c>{ erro }</c> os <c>ProblemDetails</c> que o <c>[ApiController]</c>
/// gera para erros de cliente, como o <c>415</c> de content-type não suportado.
/// </summary>
public sealed class ContratoDeErroFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext contexto)
    {
        if (contexto.Result is not ObjectResult resultado ||
            resultado.Value is not ProblemDetails detalhes)
        {
            return;
        }

        var status = detalhes.Status ?? resultado.StatusCode ?? StatusCodes.Status400BadRequest;

        var erro = status switch
        {
            404 => "Recurso não encontrado.",
            405 => "Método não permitido.",
            415 => "Formato de conteúdo não suportado.",
            _ => "Requisição inválida."
        };

        contexto.Result = new ObjectResult(new { erro }) { StatusCode = status };
    }

    public void OnResultExecuted(ResultExecutedContext contexto)
    {
    }
}
