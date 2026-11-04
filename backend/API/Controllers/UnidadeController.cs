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
    private readonly EscopoService _escopo;

    public UnidadeController(
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
    public IActionResult Listar()
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Ok(_escolas.ListarUnidades(escopo));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] UnidadeDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _escolas.AtualizarUnidade(id, dto, escopo),
            unidade => Ok(unidade));
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

        return Traduzir(
            _escolas.DefinirAtivaUnidade(id, false, escopo),
            unidade => Ok(unidade));
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

        return Traduzir(
            _escolas.DefinirAtivaUnidade(id, true, escopo),
            unidade => Ok(unidade));
    }

    /// <summary>Exclusão definitiva: `409` quando há usuário, aluno ou token referenciando.</summary>
    [Authorize(Policy = "AdminSecretaria")]
    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_exclusao.ExcluirUnidade(id, escopo), _ => NoContent());
    }
}
