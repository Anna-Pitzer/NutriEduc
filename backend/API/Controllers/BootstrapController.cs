using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Bootstrap de administrador (Spec 022 v2). As rotas de criação são anônimas de
/// propósito: existem para ser usadas antes de qualquer sessão. A barreira é o token —
/// nunca o segredo da URL, que vaza em histórico, log e <c>Referer</c>.
///
/// 403 explícito em vez de <c>Forbid()</c>: o challenge do cookie trocaria a mensagem do
/// serviço por "Acesso negado.".
/// </summary>
[ApiController]
[Route("api/bootstrap")]
public class BootstrapController : RespostaController
{
    private readonly BootstrapService _bootstrap;
    private readonly EscopoService _escopo;

    public BootstrapController(BootstrapService bootstrap, EscopoService escopo)
    {
        _bootstrap = bootstrap;
        _escopo = escopo;
    }

    /// <summary>
    /// Informa se a rota está disponível. Não revela token, unidades nem quantos
    /// administradores existem.
    /// </summary>
    [HttpGet("status")]
    public IActionResult Status()
    {
        var estado = _bootstrap.Estado();

        return Ok(new
        {
            disponivel = estado.Disponivel,
            motivo = estado.Motivo
        });
    }

    [HttpPost("admin")]
    public IActionResult CriarAdmin([FromBody] CriarAdminBootstrapDTO dto)
    {
        var resultado = _bootstrap.CriarAdmin(dto);

        return resultado.Status switch
        {
            ResultadoStatus.Ok => Created(
                $"/api/usuario/perfil/{resultado.Valor}",
                new { id = resultado.Valor }),
            ResultadoStatus.NaoEncontrado => NotFound(new { erro = resultado.Erro }),
            ResultadoStatus.Conflito => Conflict(new { erro = resultado.Erro }),
            _ => StatusCode(StatusCodes.Status403Forbidden, new { erro = resultado.Erro })
        };
    }

    /// <summary>
    /// Emite o token de 30 minutos da unidade do solicitante. O valor em claro aparece
    /// só nesta resposta; depois disso apenas o hash existe.
    /// </summary>
    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost("token")]
    public IActionResult GerarToken()
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        var resultado = _bootstrap.GerarToken(escopo);

        return resultado.Status switch
        {
            ResultadoStatus.Ok => Created("/api/bootstrap/token", new
            {
                token = resultado.Valor!.Token,
                expiraEm = resultado.Valor.ExpiraEm
            }),
            ResultadoStatus.Conflito => Conflict(new { erro = resultado.Erro }),
            _ => StatusCode(StatusCodes.Status403Forbidden, new { erro = resultado.Erro })
        };
    }

    /// <summary>Estado do token da unidade, para o painel desabilitar o botão.</summary>
    [Authorize(Policy = "AdminSecretaria")]
    [HttpGet("token")]
    public IActionResult ConsultarToken()
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Ok(_bootstrap.ConsultarToken(escopo));
    }
}
