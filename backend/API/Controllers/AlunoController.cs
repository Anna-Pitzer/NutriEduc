using Ntc.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Authorize]
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
        var criado = _alunoService.CriarAluno(dto);
        return Created($"/api/aluno/perfil/{criado.Id}", criado);
    }

    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(_alunoService.ListarAlunos());
    }

    [HttpGet("perfil/{id:int}")]
    public IActionResult GetAluno(int id)
    {
        var dadosAluno = _alunoService.ObterAlunoPorId(id);
        if (dadosAluno == null)
        {
            return NotFound(new { erro = "Aluno não encontrado." });
        }
        return Ok(dadosAluno);
    }

    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] DadosAlunoDTO dto)
    {
        var aluno = _alunoService.AtualizarAluno(id, dto);

        if (aluno == null)
        {
            return NotFound(new { erro = "Aluno não encontrado." });
        }

        return Ok(aluno);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
    {
        if (!_alunoService.ExcluirAluno(id))
        {
            return NotFound(new { erro = "Aluno não encontrado." });
        }

        return NoContent();
    }
}