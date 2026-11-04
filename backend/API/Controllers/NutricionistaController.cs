using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

/// <summary>
/// Leitura das nutricionistas que podem assinar um cardápio (Spec 027). Existe separada da
/// administração de usuários porque a tela de geração é da nutrição, e `api/usuarios` é
/// restrita a administradores.
/// </summary>
[ApiController]
[Route("api/nutricionistas")]
public class NutricionistaController : RespostaController
{
    private readonly UsuarioService _usuarios;
    private readonly EscopoService _escopo;

    public NutricionistaController(UsuarioService usuarios, EscopoService escopo)
    {
        _usuarios = usuarios;
        _escopo = escopo;
    }

    [Authorize(Policy = "Nutricionista")]
    [HttpGet]
    public IActionResult Listar()
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Ok(_usuarios.ListarNutricionistas(escopo));
    }
}
