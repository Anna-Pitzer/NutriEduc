using Microsoft.AspNetCore.Mvc;
using Ntc.Application;
using Ntc.Domain;

namespace Ntc.Controllers;

/// <summary>
/// Tradução única de <see cref="Resultado{T}"/> em HTTP: o serviço decide o acesso, o
/// controller só escolhe o status. Sem isso, cada endpoint repetiria o mesmo switch e
/// poderia divergir do contrato `{ erro }` (ADR-005).
/// </summary>
public abstract class RespostaController : ControllerBase
{
    protected IActionResult Traduzir<T>(
        Resultado<T> resultado,
        Func<T, IActionResult> sucesso)
    {
        return resultado.Status switch
        {
            ResultadoStatus.Ok => sucesso(resultado.Valor!),
            ResultadoStatus.Proibido => Forbid(),
            ResultadoStatus.NaoEncontrado => NotFound(new { erro = resultado.Erro }),
            _ => Conflict(new { erro = resultado.Erro })
        };
    }

    /// <summary>
    /// Escopo derivado das claims. Devolve <c>null</c> quando a sessão não tem papel,
    /// e o chamador responde 401.
    /// </summary>
    protected EscopoUsuario? ObterEscopo(EscopoService escopo) => escopo.Obter(User);

    protected IActionResult SessaoInvalida() =>
        Unauthorized(new { erro = "Sessão inválida ou ausente." });
}
