using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Endpoints da geração automática de cardápios (Spec 013). Toda a superfície é exclusiva
/// de `Nutricionista` (RF11); `AdminEscola` não acessa por não ter escopo por unidade.
/// </summary>
[ApiController]
[Route("api")]
public class GeracaoController : RespostaController
{
    private readonly GeracaoCardapioService _geracao;

    public GeracaoController(GeracaoCardapioService geracao)
    {
        _geracao = geracao;
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpGet("unidades/{unidadeId:int}/parametros-geracao")]
    public IActionResult Obter(int unidadeId)
        => Traduzir(_geracao.ObterParametros(unidadeId), Ok);

    [Authorize(Policy = "Nutricionista")]
    [HttpPut("unidades/{unidadeId:int}/parametros-geracao")]
    public IActionResult Salvar(int unidadeId, [FromBody] SalvarParametroGeracaoDTO dto)
        => Traduzir(_geracao.SalvarParametros(unidadeId, dto), Ok);

    [Authorize(Policy = "Nutricionista")]
    [HttpPost("cardapios/gerar")]
    public IActionResult Gerar([FromBody] GerarCardapioDTO dto)
        => Traduzir(_geracao.Gerar(dto), Ok);
}
