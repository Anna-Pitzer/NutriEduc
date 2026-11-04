using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Diagnóstico de legado da hierarquia (Spec 027, T9). Não corrige dados: apenas conta
/// registros que ainda não têm o vínculo exigido pela cadeia unidade → escola → aluno.
/// </summary>
[ApiController]
[Route("api/diagnostico")]
public class DiagnosticoController : ControllerBase
{
    private readonly HierarquiaService _hierarquia;

    public DiagnosticoController(HierarquiaService hierarquia)
    {
        _hierarquia = hierarquia;
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpGet("hierarquia")]
    public IActionResult Hierarquia() => Ok(_hierarquia.Diagnosticar());
}
