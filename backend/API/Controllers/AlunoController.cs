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
        try
        {
            _alunoService.CriarAluno(dto);
            return Ok("Aluno inserido.");
        }
        catch (ArgumentException erro)
        {
            return BadRequest(new { erro = erro.Message });
        }
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
            return NotFound(new { erro = "Aluno não encontrado." });
        }
        return Ok(dadosAluno);
    }

    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] DadosAlunoDTO dto)
    {
        try
        {
            var aluno = _alunoService.AtualizarAluno(id, dto);

            if (aluno == null)
            {
                return NotFound(new { erro = "Aluno não encontrado." });
            }

            return Ok(aluno);
        }
        catch (ArgumentException erro)
        {
            return BadRequest(new { erro = erro.Message });
        }
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