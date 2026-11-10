using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Fluxo de feedback (Spec 018). A política exige apenas sessão; quem cria, responde e
/// encerra é decidido no serviço, que também aplica o escopo da Spec 028.
/// </summary>
[ApiController]
[Route("api/feedbacks")]
public class FeedbackController : RespostaController
{
    private readonly FeedbackService _feedbacks;
    private readonly EscopoService _escopo;

    public FeedbackController(FeedbackService feedbacks, EscopoService escopo)
    {
        _feedbacks = feedbacks;
        _escopo = escopo;
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpGet]
    public IActionResult Listar(
        [FromQuery] string? status,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_feedbacks.Listar(status, pagina, tamanho, escopo), Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost]
    public IActionResult Criar([FromBody] CriarFeedbackDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _feedbacks.Criar(dto, escopo),
            criado => Created($"/api/feedbacks/{criado.Id}", criado));
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost("{id:int}/responder")]
    public IActionResult Responder(int id, [FromBody] ResponderFeedbackDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_feedbacks.Responder(id, dto, escopo), Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost("{id:int}/encerrar")]
    public IActionResult Encerrar(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_feedbacks.Encerrar(id, escopo), Ok);
    }
}
