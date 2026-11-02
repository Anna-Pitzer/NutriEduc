using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Unidades da rede (secretaria). A unidade **nasce no bootstrap do primeiro
/// administrador**; aqui só se lista, renomeia, ativa/desativa e exclui.
/// </summary>
[ApiController]
[Route("api/unidades")]
public class UnidadeController : RespostaController
{
    private readonly EscolaService _escolas;
    private readonly ExclusaoService _exclusao;

    public UnidadeController(EscolaService escolas, ExclusaoService exclusao)
    {
        _escolas = escolas;
        _exclusao = exclusao;
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(_escolas.ListarUnidades());
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] UnidadeDTO dto)
    {
        return Traduzir(_escolas.AtualizarUnidade(id, dto), unidade => Ok(unidade));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost("{id:int}/desativar")]
    public IActionResult Desativar(int id)
    {
        return Traduzir(_escolas.DefinirAtivaUnidade(id, false), unidade => Ok(unidade));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost("{id:int}/reativar")]
    public IActionResult Reativar(int id)
    {
        return Traduzir(_escolas.DefinirAtivaUnidade(id, true), unidade => Ok(unidade));
    }

    /// <summary>Exclusão definitiva: `409` quando há usuário, aluno ou token referenciando.</summary>
    [Authorize(Policy = "AdminSecretaria")]
    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
    {
        return Traduzir(_exclusao.ExcluirUnidade(id), _ => NoContent());
    }
}
