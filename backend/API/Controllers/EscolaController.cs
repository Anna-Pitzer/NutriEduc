using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Escolas (Spec 027). Criar, editar, desativar e excluir é exclusivo de AdminSecretaria;
/// listar devolve apenas a própria unidade/escola.
/// </summary>
[ApiController]
[Route("api/escolas")]
public class EscolaController : RespostaController
{
    private readonly EscolaService _escolas;
    private readonly ExclusaoService _exclusao;
    private readonly EscopoService _escopo;

    public EscolaController(
        EscolaService escolas,
        ExclusaoService exclusao,
        EscopoService escopo)
    {
        _escolas = escolas;
        _exclusao = exclusao;
        _escopo = escopo;
    }

    [Authorize(Policy = "HierarquiaLeitura")]
    [HttpGet]
    public IActionResult Listar([FromQuery] int? unidadeId)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        if (!_escolas.FiltroNoEscopo(unidadeId, escopo))
        {
            return Forbid();
        }

        return Ok(_escolas.Listar(escopo, unidadeId));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost]
    public IActionResult Criar([FromBody] EscolaDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _escolas.Criar(dto, escopo),
            escola => Created($"/api/escolas/{escola.Id}", escola));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] EscolaDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_escolas.Atualizar(id, dto, escopo), escola => Ok(escola));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost("{id:int}/desativar")]
    public IActionResult Desativar(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_escolas.DefinirAtiva(id, false, escopo), escola => Ok(escola));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost("{id:int}/reativar")]
    public IActionResult Reativar(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_escolas.DefinirAtiva(id, true, escopo), escola => Ok(escola));
    }

    /// <summary>Exclusão definitiva: `409` quando há usuário, aluno ou unidade referenciando.</summary>
    [Authorize(Policy = "AdminSecretaria")]
    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_exclusao.ExcluirEscola(id, escopo), _ => NoContent());
    }
}
