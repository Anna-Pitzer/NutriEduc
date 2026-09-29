using Microsoft.AspNetCore.Mvc;
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
    public IActionResult PostLogin([FromBody] LoginUsuarioDTO dadosLoginDTO)
    {
        bool value = _usuarioService.VerificarLogin(dadosLoginDTO);
        
        if (value){return Ok($"Seja bem-vindo, {_usuarioService.ObterNomePeloEmail(dadosLoginDTO.Email)}!");}
        return NotFound("Email ou senha incorretos.");
    }

    [HttpPatch("reset-senha")]
    public IActionResult PatchSenha([FromBody] DadosUsuarioDTO usuarioDTO)
    {
        bool value = _usuarioService.TrocarSenha(usuarioDTO.Email, usuarioDTO.Senha);
        
        if (!value) {return NotFound("Email incorreto ou senha inválida.");}
        
        return Ok("Senha trocada com sucesso!");
    }
}