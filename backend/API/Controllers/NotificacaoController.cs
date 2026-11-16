using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ntc.Controllers;

/// <summary>
/// Caixa de notificações do destinatário autenticado (Spec 018, RF10–RF12). Não existe
/// endpoint administrativo: toda leitura é restrita ao dono da sessão.
/// </summary>
[ApiController]
[Route("api/notificacoes")]
public class NotificacaoController : RespostaController
{
    private readonly NotificacaoService _notificacoes;
    private readonly EscopoService _escopo;

    public NotificacaoController(NotificacaoService notificacoes, EscopoService escopo)
    {
        _notificacoes = notificacoes;
        _escopo = escopo;
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpGet]
    public IActionResult Listar(
        [FromQuery] bool? naoLidas,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_notificacoes.Listar(naoLidas, pagina, tamanho, escopo), Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost("{id:int}/ler")]
    public IActionResult MarcarLida(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_notificacoes.MarcarLida(id, escopo), Ok);
    }

    [Authorize(Policy = "SessaoValida")]
    [HttpPost("ler-todas")]
    public IActionResult MarcarTodas()
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_notificacoes.MarcarTodas(escopo), _ => Ok());
    }
}
