using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Catálogo de alimentos (Spec 011, RF7 a RF9). Nenhuma regra de negócio aqui: o
/// <see cref="AlimentoService"/> decide escopo, categoria, vínculos e duplicidade; este
/// controller só traduz o <see cref="Resultado{T}"/> em status HTTP (ADR-005).
///
/// Leitura usa <c>SessaoValida</c> porque a recusa por papel acontece no serviço, com o
/// <see cref="EscopoService"/> — mesmo padrão da Spec 010.
/// </summary>
[ApiController]
[Route("api/alimentos")]
public class AlimentoController : RespostaController
{
    private readonly AlimentoService _alimentoService;
    private readonly EscopoService _escopo;

    public AlimentoController(AlimentoService alimentoService, EscopoService escopo)
    {
        _alimentoService = alimentoService;
        _escopo = escopo;
    }

    /// <summary>Listagem paginada com busca, categoria e inativos (exclusivo da nutrição).</summary>
    /// <summary>
    /// Listagem paginada com busca, categoria, inativos (exclusivo da nutrição) e
    /// ordenação por nome, categoria ou mais recentes.
    /// </summary>
    [Authorize(Policy = "CatalogoDeAlimentosLeitura")]
    [HttpGet]
    public IActionResult Listar(
        [FromQuery] string? busca,
        [FromQuery] int? categoriaId,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho,
        [FromQuery] bool incluirInativos = false,
        [FromQuery] string? ordenar = null)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _alimentoService.Listar(
                busca, categoriaId, pagina, tamanho, incluirInativos, escopo, ordenar),
            Ok);
    }

    [Authorize(Policy = "CatalogoDeAlimentosLeitura")]
    [HttpGet("{id:int}")]
    public IActionResult Obter(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_alimentoService.Obter(id, escopo), Ok);
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpPost]
    public IActionResult Criar([FromBody] SalvarAlimentoDTO dto)
    {
        return Traduzir(
            _alimentoService.Criar(dto),
            criado => Created($"/api/alimentos/{criado.Id}", criado));
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] SalvarAlimentoDTO dto)
    {
        return Traduzir(_alimentoService.Atualizar(id, dto), Ok);
    }

    /// <summary>Desativar é soft delete: o alimento continua acessível por id (RF6).</summary>
    [Authorize(Policy = "Nutricionista")]
    [HttpPost("{id:int}/desativar")]
    public IActionResult Desativar(int id)
    {
        return Traduzir(_alimentoService.Desativar(id), Ok);
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpPost("{id:int}/reativar")]
    public IActionResult Reativar(int id)
    {
        return Traduzir(_alimentoService.Reativar(id), Ok);
    }
}
