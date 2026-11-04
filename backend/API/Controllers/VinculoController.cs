using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Vínculo responsável↔aluno (RF6). A rota fica sob `/api/aluno` porque a superfície de
/// aluno do projeto é singular; renomear quebraria frontend, README e testes.
/// </summary>
[ApiController]
[Route("api/aluno/{alunoId:int}/responsaveis")]
public class VinculoController : RespostaController
{
    private readonly VinculoService _vinculos;
    private readonly EscopoService _escopo;

    public VinculoController(VinculoService vinculos, EscopoService escopo)
    {
        _vinculos = vinculos;
        _escopo = escopo;
    }

    [Authorize(Policy = "VinculoLeitura")]
    [HttpGet]
    public IActionResult Listar(int alunoId)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_vinculos.Listar(alunoId, escopo), lista => Ok(lista));
    }

    [Authorize(Policy = "Admin")]
    [HttpPost("{usuarioId:int}")]
    public IActionResult Vincular(int alunoId, int usuarioId)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_vinculos.Vincular(alunoId, usuarioId, escopo), _ => NoContent());
    }

    [Authorize(Policy = "Admin")]
    [HttpDelete("{usuarioId:int}")]
    public IActionResult Desvincular(int alunoId, int usuarioId)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_vinculos.Desvincular(alunoId, usuarioId, escopo), _ => NoContent());
    }
}
