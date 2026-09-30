using Microsoft.AspNetCore.Mvc;
using Ntc.Application;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
namespace Ntc.Controllers;


[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly UsuarioService _usuarioService;

    public UsuarioController(UsuarioService usuarioService)
    {
        _usuarioService = usuarioService ?? throw new ArgumentNullException();
    }

    [HttpPost("registrar")]
    public IActionResult PostAdd([FromBody]DadosUsuarioDTO usuarioDTO)
    {   
        bool value = _usuarioService.CriarUsuario(usuarioDTO);

        if (!value)
        {
            return BadRequest("E-mail já cadastrado ou senha inválida.");
        }
        return Ok($"Usuario {usuarioDTO.Nome} foi cadastrado com sucesso.");
    }

    [HttpGet("perfil/{id}")]
    public IActionResult GetUser(int id)
    {
        var dadosUsuario = _usuarioService.ObterUsuarioPorId(id);
        if (dadosUsuario == null)
        {
            return NotFound("Usuário não encontrado.");
        }
        return Ok(dadosUsuario);
    }

    [HttpPost("login")]
    public async Task<IActionResult> PostLogin(
        [FromBody] LoginUsuarioDTO dadosLoginDTO)
    {   
        bool valido = _usuarioService.VerificarLogin(dadosLoginDTO);

        if (!valido)
        {
            return Unauthorized("Email ou senha incorretos.");
        }

        var nome = _usuarioService.ObterNomePeloEmail(dadosLoginDTO.Email);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, dadosLoginDTO.Email!),
            new Claim(ClaimTypes.Name, nome ?? "")
        };

        var identidade = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var usuario = new ClaimsPrincipal(identidade);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            usuario,
            new AuthenticationProperties
            {
                IsPersistent = true
            }
        );

        return Ok(new
        {
            email = dadosLoginDTO.Email,
            nome
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult ObterSessao()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        var nome = _usuarioService.ObterNomePeloEmail(email);

        if (nome == null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            email,
            nome
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        return NoContent();
    }
}