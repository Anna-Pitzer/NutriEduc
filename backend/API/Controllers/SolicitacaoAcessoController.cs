using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Fila e decisão de solicitações de acesso (Spec 033). A aprovação é exclusiva do ADM da
/// unidade (matriz 028: <see cref="Ntc.Application.Seguranca.Recurso.SolicitacaoDeAcesso"/>).
/// </summary>
[ApiController]
[Route("api/solicitacoes-acesso")]
public class SolicitacaoAcessoController : RespostaController
{
    private readonly AprovarAcessoService _aprovacoes;
    private readonly EscopoService _escopo;

    public SolicitacaoAcessoController(AprovarAcessoService aprovacoes, EscopoService escopo)
    {
        _aprovacoes = aprovacoes;
        _escopo = escopo;
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpGet]
    public IActionResult Listar(
        [FromQuery] string? status,
        [FromQuery] string? tipo,
        [FromQuery] string? busca,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _aprovacoes.Listar(status, tipo, busca, pagina, tamanho, escopo),
            resultado => Ok(resultado));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpGet("{id:int}")]
    public IActionResult Obter(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_aprovacoes.Obter(id, escopo), resultado => Ok(resultado));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost("{id:int}/aprovar")]
    public IActionResult Aprovar(int id, [FromBody] AprovarAcessoDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_aprovacoes.Aprovar(id, dto, escopo), resultado => Ok(resultado));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost("{id:int}/recusar")]
    public IActionResult Recusar(int id, [FromBody] RecusarAcessoDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_aprovacoes.Recusar(id, dto, escopo), resultado => Ok(resultado));
    }

    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost]
    public IActionResult Criar([FromBody] CriarSolicitacaoAcessoDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_aprovacoes.Criar(dto, escopo), resultado => Ok(resultado));
    }
}
