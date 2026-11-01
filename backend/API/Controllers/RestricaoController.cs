using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;
using Ntc.Domain.Entity;

namespace Ntc.Controllers;

/// <summary>
/// Catálogo de restrições e alergias (Spec 010, RF2 e RF7). Nenhuma regra de negócio aqui:
/// o <see cref="RestricaoService"/> decide o escopo e o domínio valida o nome; este
/// controller só traduz o <see cref="Resultado{T}"/> em status HTTP.
/// </summary>
[ApiController]
[Route("api/restricoes")]
public class RestricaoController : RespostaController
{
    private readonly RestricaoService _restricaoService;
    private readonly EscopoService _escopo;

    public RestricaoController(RestricaoService restricaoService, EscopoService escopo)
    {
        _restricaoService = restricaoService;
        _escopo = escopo;
    }

    /// <summary>Catálogo por categoria. `incluirInativas=true` é exclusivo do Nutricionista.</summary>
    [Authorize(Policy = "SessaoValida")]
    [HttpGet]
    public IActionResult Listar(
        [FromQuery] CategoriaRestricao? categoria,
        [FromQuery] bool incluirInativas = false)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _restricaoService.Listar(categoria, incluirInativas, escopo),
            Ok);
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpPost]
    public IActionResult Criar([FromBody] SalvarRestricaoDTO dto)
    {
        var criado = _restricaoService.Criar(dto);
        return Created($"/api/restricoes/{criado.Id}", criado);
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] SalvarRestricaoDTO dto)
    {
        return Traduzir(_restricaoService.Atualizar(id, dto), Ok);
    }

    /// <summary>Desativar preserva os vínculos existentes e impede novos (RF3, ADR-008).</summary>
    [Authorize(Policy = "Nutricionista")]
    [HttpPost("{id:int}/desativar")]
    public IActionResult Desativar(int id)
    {
        return Traduzir(_restricaoService.Desativar(id), Ok);
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpPost("{id:int}/reativar")]
    public IActionResult Reativar(int id)
    {
        return Traduzir(_restricaoService.Reativar(id), Ok);
    }
}
