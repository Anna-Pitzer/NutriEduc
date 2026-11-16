using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Fluxo de solicitações (Spec 017). A política de autenticação apenas exige sessão; quem
/// cria, vê e decide cada tipo é decidido no serviço, que também aplica o escopo da
/// Spec 028 e o efeito da aprovação.
/// </summary>
[ApiController]
[Route("api/solicitacoes")]
public class SolicitacaoController : RespostaController
{
    private readonly SolicitacaoService _solicitacoes;
    private readonly EscopoService _escopo;

    public SolicitacaoController(SolicitacaoService solicitacoes, EscopoService escopo)
    {
        _solicitacoes = solicitacoes;
        _escopo = escopo;
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpGet]
    public IActionResult Listar(
        [FromQuery] string? tipo,
        [FromQuery] string? status,
        [FromQuery] int? unidadeId,
        [FromQuery] int? escolaId,
        [FromQuery] int? alunoId,
        [FromQuery] int? cardapioId,
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
            _solicitacoes.Listar(
                tipo,
                status,
                unidadeId,
                escolaId,
                alunoId,
                cardapioId,
                de,
                ate,
                pagina,
                tamanho,
                escopo),
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

        return Traduzir(_solicitacoes.Obter(id, escopo), Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost]
    public IActionResult Criar([FromBody] CriarSolicitacaoDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _solicitacoes.Criar(dto, escopo),
            criado => Created($"/api/solicitacoes/{criado.Id}", criado));
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost("{id:int}/analise")]
    public IActionResult Analisar(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_solicitacoes.Analisar(id, escopo), Ok);
    }

    /// <summary>
    /// Marca a anafilaxia do aluno na análise de uma solicitação de inclusão de alergia
    /// (Spec 017/023). A decisão da solicitação continua separada.
    /// </summary>
    [Authorize(Policy = "SessaoValida")]
    [HttpPost("{id:int}/anafilaxia")]
    public IActionResult MarcarAnafilaxia(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_solicitacoes.MarcarAnafilaxia(id, escopo), Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost("{id:int}/aprovar")]
    public IActionResult Aprovar(int id, [FromBody] DecidirSolicitacaoDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_solicitacoes.Aprovar(id, dto, escopo), Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost("{id:int}/recusar")]
    public IActionResult Recusar(int id, [FromBody] DecidirSolicitacaoDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_solicitacoes.Recusar(id, dto, escopo), Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost("{id:int}/concluir")]
    public IActionResult Concluir(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_solicitacoes.Concluir(id, escopo), Ok);
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

        return Traduzir(_solicitacoes.Cancelar(id, escopo), Ok);
    }
}
