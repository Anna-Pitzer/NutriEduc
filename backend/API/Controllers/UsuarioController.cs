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
        var id = _usuarioService.CriarUsuario(usuarioDTO);

        if (id == null)
        {
            return BadRequest(new { erro = "E-mail já cadastrado." });
        }

        return Created(
            $"/api/usuario/perfil/{id.Value}",
            new { id = id.Value, nome = usuarioDTO.Nome, email = usuarioDTO.Email });
    }

    [Authorize]
    [HttpGet("perfil/{id:int}")]
    public IActionResult GetUser(int id)
    {
        var emailDaSessao = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(emailDaSessao))
        {
            return Unauthorized(new { erro = "Sessão inválida ou ausente." });
        }

        var idDaSessao = _usuarioService.ObterIdPeloEmail(emailDaSessao);

        if (idDaSessao == null)
        {
            return Unauthorized(new { erro = "Sessão inválida ou ausente." });
        }

        if (idDaSessao.Value != id)
        {
            return Forbid();
        }

        var dadosUsuario = _usuarioService.ObterUsuarioPorId(id);

        if (dadosUsuario == null)
        {
            return NotFound(new { erro = "Usuário não encontrado." });
        }

        return Ok(new
        {
            id,
            nome = dadosUsuario.Nome,
            email = dadosUsuario.Email,
            telefone = dadosUsuario.Telefone,
            foto = dadosUsuario.Foto
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> PostLogin(
        [FromBody] LoginUsuarioDTO dadosLoginDTO)
    {   
        bool valido = _usuarioService.VerificarLogin(dadosLoginDTO);

        if (!valido)
        {
            return Unauthorized(new { erro = "Email ou senha incorretos." });
        }

        var nome = _usuarioService.ObterNomePeloEmail(dadosLoginDTO.Email);

        var id = _usuarioService.ObterIdPeloEmail(dadosLoginDTO.Email);

        if (id == null)
        {
            return Unauthorized(new { erro = "Sessão inválida ou ausente." });
        }

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
            id = id.Value,
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
            return Unauthorized(new { erro = "Sessão inválida ou ausente." });
        }

        var nome = _usuarioService.ObterNomePeloEmail(email);

        var id = _usuarioService.ObterIdPeloEmail(email);

        if (nome == null || id == null)
        {
            return Unauthorized(new { erro = "Sessão inválida ou ausente." });
        }

        return Ok(new
        {
            id = id.Value,
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

    [Authorize]
    [HttpPut("perfil/{id:int}")]
    public async Task<IActionResult> AtualizarPerfil(
        int id,
        [FromBody] AtualizarPerfilDTO dto)
    {
        var emailDaSessao = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(emailDaSessao))
        {
            return Unauthorized(new { erro = "Sessão inválida ou ausente." });
        }

        var idDaSessao = _usuarioService.ObterIdPeloEmail(emailDaSessao);

        if (idDaSessao == null)
        {
            return Unauthorized(new { erro = "Sessão inválida ou ausente." });
        }

        if (idDaSessao.Value != id)
        {
            return Forbid();
        }

        var perfil = _usuarioService.AtualizarPerfil(id, dto);

        if (perfil == null)
        {
            return NotFound(new { erro = "Usuário não encontrado." });
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, perfil.Email!),
            new Claim(ClaimTypes.Name, perfil.Nome ?? "")
        };

        var identidade = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var propriedades = new AuthenticationProperties
        {
            IsPersistent = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidade),
            propriedades
        );

        return Ok(new
        {
            id,
            nome = perfil.Nome,
            email = perfil.Email,
            telefone = perfil.Telefone,
            foto = perfil.Foto
        });
    }
}