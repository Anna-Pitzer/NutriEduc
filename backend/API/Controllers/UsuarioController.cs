using Microsoft.AspNetCore.Mvc;
using Ntc.Application;
using Ntc.Domain.Entity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
namespace Ntc.Controllers;


[ApiController]
[Route("api/[controller]")]
public class UsuarioController : RespostaController
{
    private readonly UsuarioService _usuarioService;
    private readonly AutenticacaoService _autenticacao;
    private readonly RegistrarAcessoService _registroAcesso;

    public UsuarioController(
        UsuarioService usuarioService,
        AutenticacaoService autenticacao,
        RegistrarAcessoService registroAcesso)
    {
        _usuarioService = usuarioService;
        _autenticacao = autenticacao;
        _registroAcesso = registroAcesso;
    }

    /// <summary>
    /// Autocadastro público (Spec 033). Cria a conta sem papel e a solicitação de acesso
    /// pendente, registrando o tipo pedido e a unidade de destino.
    /// </summary>
    [HttpPost("registrar")]
    public IActionResult PostAdd([FromBody]CadastroEntrada entrada)
    {
        return Traduzir(
            _registroAcesso.Registrar(entrada),
            registro => Created($"/api/solicitacoes-acesso/{registro.SolicitacaoId}", registro));
    }

    [Authorize]
    [HttpGet("perfil/{id:int}")]
    public IActionResult GetUser(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idDaSessao))
        {
            return SessaoInvalida();
        }

        if (idDaSessao != id)
        {
            return Forbid();
        }

        var perfil = _usuarioService.ObterUsuarioPorId(id);

        if (perfil == null)
        {
            return NotFound(new { erro = "Usuário não encontrado." });
        }

        return Ok(perfil);
    }

    [HttpPost("login")]
    public async Task<IActionResult> PostLogin(
        [FromBody] LoginUsuarioDTO dadosLoginDTO)
    {
        var resultado = _autenticacao.Autenticar(dadosLoginDTO, out var usuario);

        if (resultado != ResultadoLogin.Ok)
        {
            // RF5: conta sem papel (pendente/recusada) responde 401 genérico, sem confirmar
            // a existência do e-mail.
            var mensagem = resultado switch
            {
                ResultadoLogin.Inativo => "Conta desativada.",
                _ => "Email ou senha incorretos."
            };

            return Unauthorized(new { erro = mensagem });
        }

        var resposta = _usuarioService.ObterUsuarioPorId(usuario!.Id)!;

        await EmitirSessao(resposta);

        return Ok(resposta);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult ObterSessao()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return SessaoInvalida();
        }

        var usuario = _usuarioService.ObterUsuarioPorId(usuarioId);

        if (usuario?.Papel is null)
        {
            return SessaoInvalida();
        }

        return Ok(usuario);
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
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idDaSessao))
        {
            return SessaoInvalida();
        }

        if (idDaSessao != id)
        {
            return Forbid();
        }

        var perfil = _usuarioService.AtualizarPerfil(id, dto);

        if (perfil == null)
        {
            return NotFound(new { erro = "Usuário não encontrado." });
        }

        var usuario = _usuarioService.ObterUsuarioPorId(id);

        if (usuario is not null)
        {
            await EmitirSessao(usuario);
        }

        return Ok(perfil);
    }

    /// <summary>
    /// Emite o cookie com as claims de identidade e de autorização. Estas cinco claims
    /// são a entrada do <see cref="EscopoService"/>; acrescentar uma exige atualizar o
    /// serviço e a matriz de acesso da Spec 009.
    /// </summary>
    private async Task EmitirSessao(UsuarioRespostaDTO usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email ?? string.Empty),
            new(ClaimTypes.Name, usuario.Nome ?? string.Empty),
            new(ClaimTypes.Role, usuario.Papel!.Value.ToString()),
            new("escola", usuario.EscolaId?.ToString() ?? string.Empty),
            new("unidade", usuario.UnidadeId?.ToString() ?? string.Empty)
        };

        var identidade = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidade),
            new AuthenticationProperties
            {
                IsPersistent = true
            }
        );
    }
}
