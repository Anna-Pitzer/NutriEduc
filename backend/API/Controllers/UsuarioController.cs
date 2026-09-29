using Microsoft.AspNetCore.Mvc;
using Ntc.Domain.Entity;
using Ntc.Application;
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
        _usuarioService.CriarUsuario(usuarioDTO);
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
    public IActionResult PostLogin([FromBody] LoginUsuarioDTO dadosLoginDTO)
    {
        
        bool value = _usuarioService.VerificarLogin(dadosLoginDTO);
        
        if (value){return Ok($"Seja bem-vindo, {_usuarioService.ObterNomePeloEmail(dadosLoginDTO.Email)}");}
        return NotFound("Email ou senha incorretos");
    }

    [HttpPatch("reset-senha")]
    public IActionResult PatchSenha([FromBody] DadosUsuarioDTO usuarioDTO)
    {
        if (string.IsNullOrEmpty(usuarioDTO.Senha) || usuarioDTO.Senha.Length < 8)
        {
            return BadRequest("Senha com menos de 8 caracteres.");
        }
        
        if (!_usuarioService.TrocarSenha(usuarioDTO.Email, usuarioDTO.Senha))
        {
            return NotFound("Email inserido não existe.");
        }
        return Ok("Senha trocada com sucesso!");
    }
}