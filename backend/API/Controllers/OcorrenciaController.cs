using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Fluxo de ocorrências (Spec 016). A política de autenticação apenas exige sessão; quem
/// cria, vê e muda o status é decidido no serviço, que também aplica o escopo da Spec 028.
/// </summary>
[ApiController]
[Route("api/ocorrencias")]
public class OcorrenciaController : RespostaController
{
    private readonly OcorrenciaService _ocorrencias;
    private readonly EscopoService _escopo;

    public OcorrenciaController(OcorrenciaService ocorrencias, EscopoService escopo)
    {
        _ocorrencias = ocorrencias;
        _escopo = escopo;
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpGet]
    public IActionResult Listar(
        [FromQuery] string? tipo,
        [FromQuery] string? status,
        [FromQuery] int? alunoId,
        [FromQuery] int? unidadeId,
        [FromQuery] int? escolaId,
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _ocorrencias.Listar(
                tipo, status, alunoId, unidadeId, escolaId, de, ate, pagina, tamanho, escopo),
            Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpGet("{id:int}")]
    public IActionResult Obter(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_ocorrencias.Obter(id, escopo), Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost]
    public IActionResult Criar([FromBody] CriarOcorrenciaDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _ocorrencias.Criar(dto, escopo),
            criado => Created($"/api/ocorrencias/{criado.Id}", criado));
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost("{id:int}/status")]
    public IActionResult MudarStatus(int id, [FromBody] MudarStatusOcorrenciaDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_ocorrencias.MudarStatus(id, dto, escopo), Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost("{id:int}/cancelar")]
    public IActionResult Cancelar(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_ocorrencias.Cancelar(id, escopo), Ok);
    }
}
