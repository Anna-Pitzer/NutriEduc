using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Categorias do catálogo de alimentos (Spec 011, RF1). Somente a nutrição escreve;
/// leitura segue a mesma política do catálogo de alimentos (RF9).
/// </summary>
[ApiController]
[Route("api/categorias-alimento")]
public class CategoriaAlimentoController : RespostaController
{
    private readonly AlimentoService _alimentoService;
    private readonly EscopoService _escopo;

    public CategoriaAlimentoController(AlimentoService alimentoService, EscopoService escopo)
    {
        _alimentoService = alimentoService;
        _escopo = escopo;
    }

    [Authorize(Policy = "CatalogoDeAlimentosLeitura")]
    [HttpGet]
    public IActionResult Listar([FromQuery] bool incluirInativas = false)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_alimentoService.ListarCategorias(incluirInativas, escopo), Ok);
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpPost]
    public IActionResult Criar([FromBody] SalvarCategoriaAlimentoDTO dto)
    {
        return Traduzir(
            _alimentoService.CriarCategoria(dto),
            criada => Created($"/api/categorias-alimento/{criada.Id}", criada));
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] SalvarCategoriaAlimentoDTO dto)
    {
        return Traduzir(_alimentoService.AtualizarCategoria(id, dto), Ok);
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpPost("{id:int}/desativar")]
    public IActionResult Desativar(int id)
    {
        return Traduzir(_alimentoService.DesativarCategoria(id), Ok);
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpPost("{id:int}/reativar")]
    public IActionResult Reativar(int id)
    {
        return Traduzir(_alimentoService.ReativarCategoria(id), Ok);
    }
}
