using Microsoft.AspNetCore.Mvc;
using Ntc.Application;

namespace Ntc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlunoController : ControllerBase
{
    private readonly AlunoService _alunoService;

    public AlunoController(AlunoService alunoService)
    {
        _alunoService = alunoService;
    }

    [HttpPost]
    public IActionResult Cadastrar([FromBody] DadosAlunoDTO dto)
    {
        _alunoService.CriarAluno(dto);
        return Ok("Aluno cadastrado com sucesso.");
    }

    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(_alunoService.ListarAlunos());
    }

    [HttpGet("{id}")]
    public IActionResult ObterPorId(int id)
    {
        var aluno = _alunoService.ObterAlunoPorId(id);
        if (aluno == null) return NotFound("Aluno não encontrado.");
        return Ok(aluno);
    }

    [HttpDelete("{id}")]
    public IActionResult Excluir(int id)
    {
        bool excluido = _alunoService.ExcluirAluno(id);
        if (!excluido) return NotFound("Aluno não encontrado.");
        return NoContent();
    }
}
