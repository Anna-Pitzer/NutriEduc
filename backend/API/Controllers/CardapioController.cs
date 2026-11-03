using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Endpoints do cardápio (Spec 012 e Spec 015). Escrita usa a política `Nutricionista`;
/// leitura interna usa `SessaoValida` e recusa por papel no serviço; a rota pública é a única
/// sem `[Authorize]` e por isso exige `unidadeId` explicitamente (RF9).
/// </summary>
[ApiController]
[Route("api/cardapios")]
public class CardapioController : RespostaController
{
    private readonly CardapioService _cardapios;
    private readonly CompatibilidadeService _compatibilidade;
    private readonly PublicacaoCardapioService _publicacao;
    private readonly EscopoService _escopo;

    public CardapioController(
        CardapioService cardapios,
        CompatibilidadeService compatibilidade,
        PublicacaoCardapioService publicacao,
        EscopoService escopo)
    {
        _cardapios = cardapios;
        _compatibilidade = compatibilidade;
        _publicacao = publicacao;
        _escopo = escopo;
    }

    /// <summary>Listagem interna, escopada por papel, com filtro por unidade, data e situação.</summary>
    [Authorize(Policy = "SessaoValida")]
    [HttpGet]
    public IActionResult Listar(
        [FromQuery] int? unidadeId,
        [FromQuery] DateOnly? data,
        [FromQuery] string? situacao,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _cardapios.Listar(unidadeId, data, situacao, pagina, tamanho, escopo),
            Ok);
    }

    /// <summary>
    /// Rota pública (RF9): devolve apenas cardápios publicados cujo período inclui a data.
    /// Sem `[Authorize]` de propósito — é a leitura de pais e merendeiras.
    /// </summary>
    [HttpGet("publicados")]
    public IActionResult ListarPublicados(
        [FromQuery] int? unidadeId,
        [FromQuery] DateOnly? data,
        [FromQuery] string? refeicao,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho)
        => Traduzir(_cardapios.ListarPublicados(unidadeId, data, refeicao, pagina, tamanho), Ok);

    [Authorize(Policy = "SessaoValida")]
    [HttpGet("{id:int}")]
    public IActionResult Obter(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_cardapios.Obter(id, escopo), Ok);
    }

    /// <summary>Análise de compatibilidade (Spec 014): nutrição vê toda a rede, merendeira só a própria unidade.</summary>
    [Authorize(Policy = "SessaoValida")]
    [HttpGet("{id:int}/compatibilidade")]
    public IActionResult Compatibilidade(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_compatibilidade.Analisar(id, escopo), Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpGet("{id:int}/historico")]
    public IActionResult Historico(
        int id,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_publicacao.ListarHistorico(id, pagina, tamanho, escopo), Ok);
    }

    /// <summary>Feed de alterações no escopo do usuário (Spec 015, RF11).</summary>
    [Authorize(Policy = "SessaoValida")]
    [HttpGet("alteracoes")]
    public IActionResult Alteracoes(
        [FromQuery] DateTime desde,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_publicacao.ListarAlteracoes(desde, pagina, tamanho, escopo), Ok);
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpPost]
    public IActionResult Criar([FromBody] SalvarCardapioDTO dto)
        => Traduzir(
            _cardapios.Criar(dto, ObterEscopo(_escopo)!),
            criado => Created($"/api/cardapios/{criado.Id}", criado));

    [Authorize(Policy = "Nutricionista")]
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] SalvarCardapioDTO dto)
        => Traduzir(_cardapios.Atualizar(id, dto, ObterEscopo(_escopo)!), Ok);

    [Authorize(Policy = "Nutricionista")]
    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
        => Traduzir(_cardapios.Excluir(id), _ => NoContent());

    [Authorize(Policy = "Nutricionista")]
    [HttpPost("{id:int}/duplicar")]
    public IActionResult Duplicar(int id, [FromBody] DuplicarCardapioDTO dto)
        => Traduzir(
            _cardapios.Duplicar(id, dto, ObterEscopo(_escopo)!),
            criado => Created($"/api/cardapios/{criado.Id}", criado));

    [Authorize(Policy = "Nutricionista")]
    [HttpPost("{id:int}/publicar")]
    public IActionResult Publicar(int id)
        => Traduzir(_publicacao.Publicar(id, ObterEscopo(_escopo)!), Ok);

    [Authorize(Policy = "Nutricionista")]
    [HttpPost("{id:int}/cancelar")]
    public IActionResult Cancelar(int id, [FromBody] CancelarCardapioDTO dto)
        => Traduzir(_publicacao.Cancelar(id, dto, ObterEscopo(_escopo)!), Ok);

    [Authorize(Policy = "Nutricionista")]
    [HttpPost("{id:int}/revisao")]
    public IActionResult Revisao(int id)
        => Traduzir(
            _publicacao.CriarRevisao(id, ObterEscopo(_escopo)!),
            criado => Created($"/api/cardapios/{criado.Id}", criado));
}
