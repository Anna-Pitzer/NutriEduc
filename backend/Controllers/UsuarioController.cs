using Microsoft.AspNetCore.Mvc;
using Ntc.Model;
using Ntc.ViewModel;
namespace Ntc.Controllers;


[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuarioController(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException();
    }

    [HttpPost("registrar")]
    public IActionResult PostAdd([FromBody]UsuarioViewModel usuarioView)
    {
        var usuario = new Usuario(usuarioView.Nome, usuarioView.Cpf, usuarioView.DataNascimento, usuarioView.Email, usuarioView.Senha);

        _usuarioRepository.RegisterUser(usuario);

        return Ok($"Usuario {usuario.Nome} foi cadastrado com sucesso.");
    }

    [HttpGet]
    public IActionResult GetUsers()
    {
        var listaUsuarios = _usuarioRepository.ListUsers();

        return Ok(listaUsuarios);
    }

    [HttpPost("login")]
    public IActionResult PostLogin([FromBody]UsuarioViewModel usuarioView)
    {
        var usuario = new Usuario(usuarioView.Email, usuarioView.Senha);
        
        int value = _usuarioRepository.LoginUser(usuario);
        
        if (value == 1){return Ok($"Seja bem-vindo, {_usuarioRepository.GetName(usuarioView.Email)}");}
        return NotFound("Email ou senha incorretos");
    }
}