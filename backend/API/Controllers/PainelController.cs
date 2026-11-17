using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;
using Ntc.Domain;

namespace Ntc.Controllers;

/// <summary>
/// Painel de entrada por papel (Spec 025). Cada rota exige o papel do próprio painel; o
/// serviço decide escopo e período, e o controller apenas traduz em HTTP (ADR-005).
/// </summary>
[ApiController]
[Route("api/painel")]
public class PainelController : RespostaController
{
    private readonly PainelService _painel;
    private readonly EscopoService _escopo;

    public PainelController(PainelService painel, EscopoService escopo)
    {
        _painel = painel;
        _escopo = escopo;
    }

    /// <summary>Indicadores agregados da unidade do solicitante (RF1, RF2).</summary>
    [Authorize(Policy = "AdminSecretaria")]
    [HttpGet("secretaria")]
    public IActionResult Secretaria() => ComEscopo(escopo => _painel.Secretaria(escopo));

    /// <summary>Cardápio publicado do dia da unidade, com alertas por item (RF3, RF4).</summary>
    [Authorize(Policy = "Merendeira")]
    [HttpGet("merendeira")]
    public IActionResult Merendeira([FromQuery] DateOnly? data) =>
        ComEscopo(escopo => _painel.Merendeira(data, escopo));

    /// <summary>Filhos vinculados, restrições e cardápio do dia (RF5).</summary>
    [Authorize(Policy = "Responsavel")]
    [HttpGet("familia")]
    public IActionResult Familia([FromQuery] DateOnly? data) =>
        ComEscopo(escopo => _painel.Familia(data, escopo));

    /// <summary>Relatório do próprio filho no período (RF6, RF7).</summary>
    [Authorize(Policy = "Responsavel")]
    [HttpGet("familia/{alunoId:int}/relatorio")]
    public IActionResult RelatorioDoFilho(
        int alunoId,
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate) =>
        ComEscopo(escopo => _painel.RelatorioDoFilho(alunoId, de, ate, escopo));

    private IActionResult ComEscopo<T>(Func<EscopoUsuario, Resultado<T>> leitura)
    {
        var escopo = ObterEscopo(_escopo);

        return escopo is null
            ? SessaoInvalida()
            : Traduzir(leitura(escopo), valor => Ok(valor));
    }
}
