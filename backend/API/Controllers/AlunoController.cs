using Ntc.Application;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]

public class AlunoController : ControllerBase
{
    private readonly AlunoService _alunoService;

    public AlunoController(AlunoService alunoService)
    {
        _alunoService = alunoService ?? throw new ArgumentException();
    }

    [HttpPost("registrar")]
    public IActionResult PostAddAluno([FromBody] DadosAlunoDTO dto)
    {
        bool value = _alunoService.CriarAluno(dto);

        if (!value)
        {
            return BadRequest("Error ao adicionar aluno.");
        }
        return Ok("Aluno inserido.");
    }

    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(_alunoService.ListarAlunos());
    }

    [HttpGet("perfil/{id}")]
    public IActionResult GetAluno(int id)
    {
        var dadosAluno = _alunoService.ObterAlunoPorId(id);
        if (dadosAluno == null)
        {
            return NotFound("Aluno não encontrado.");
        }
        return Ok(dadosAluno);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
    {
        if (!_alunoService.ExcluirAluno(id))
        {
            return NotFound("Aluno não encontrado.");
        }

        return NoContent();
    }
}