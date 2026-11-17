using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Endpoints de relatórios (Spec 019). Leitura por pré-visualização e exportação XLSX;
/// o serviço decide acesso e escopo, o controller só traduz em HTTP (ADR-005).
/// </summary>
[ApiController]
[Route("api/relatorios")]
public class RelatorioController : RespostaController
{
    private readonly RelatorioService _relatorios;
    private readonly EscopoService _escopo;

    public RelatorioController(RelatorioService relatorios, EscopoService escopo)
    {
        _relatorios = relatorios;
        _escopo = escopo;
    }

    /// <summary>Pré-visualização com no máximo 200 linhas (RF4).</summary>
    [Authorize(Policy = "SessaoValida")]
    [HttpGet("{tipo}/preview")]
    public IActionResult Preview(
        string tipo,
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        [FromQuery] int? unidadeId,
        [FromQuery] int? escolaId)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _relatorios.Previsualizar(tipo, de, ate, unidadeId, escolaId, escopo),
            Ok);
    }

    /// <summary>Exportação XLSX do conjunto completo do filtro (RF5).</summary>
    [Authorize(Policy = "SessaoValida")]
    [HttpGet("{tipo}/exportar")]
    public IActionResult Exportar(
        string tipo,
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        [FromQuery] int? unidadeId,
        [FromQuery] int? escolaId)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _relatorios.Exportar(tipo, de, ate, unidadeId, escolaId, escopo),
            arquivo => File(arquivo.Conteudo, arquivo.TipoDeConteudo, arquivo.Nome));
    }
}
