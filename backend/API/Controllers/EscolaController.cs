using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Escolas (RF3). Criar, editar, desativar e excluir é exclusivo de AdminSecretaria;
/// listar exige sessão válida.
/// </summary>
[ApiController]
[Route("api/escolas")]
public class EscolaController : RespostaController
{
    private readonly EscolaService _escolas;
    private readonly ExclusaoService _exclusao;

    public EscolaController(EscolaService escolas, ExclusaoService exclusao)
    {
        _escolas = escolas;
        _exclusao = exclusao;
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(_escolas.Listar());
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost]
    public IActionResult Criar([FromBody] EscolaDTO dto)
    {
        return Traduzir(
            _escolas.Criar(dto),
            escola => Created($"/api/escolas/{escola.Id}", escola));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] EscolaDTO dto)
    {
        return Traduzir(_escolas.Atualizar(id, dto), escola => Ok(escola));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost("{id:int}/desativar")]
    public IActionResult Desativar(int id)
    {
        return Traduzir(_escolas.DefinirAtiva(id, false), escola => Ok(escola));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost("{id:int}/reativar")]
    public IActionResult Reativar(int id)
    {
        return Traduzir(_escolas.DefinirAtiva(id, true), escola => Ok(escola));
    }

    /// <summary>Exclusão definitiva: `409` quando há usuário ou unidade referenciando.</summary>
    [Authorize(Policy = "AdminSecretaria")]
    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
    {
        return Traduzir(_exclusao.ExcluirEscola(id), _ => NoContent());
    }
}
