using Ntc.Application;
using Ntc.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Ntc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlunoController : RespostaController
{
    private readonly AlunoService _alunoService;
    private readonly HistoricoAlunoService _historicoService;
    private readonly EscopoService _escopo;

    public AlunoController(
        AlunoService alunoService,
        HistoricoAlunoService historicoService,
        EscopoService escopo)
    {
        _alunoService = alunoService;
        _historicoService = historicoService;
        _escopo = escopo;
    }

    [Authorize(Policy = "GestaoAluno")]
    [HttpPost("registrar")]
    public IActionResult PostAddAluno([FromBody] AlunoEntrada dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        var criado = _alunoService.CriarAluno(dto, escopo);
        return Created($"/api/aluno/perfil/{criado.Id}", criado);
    }

    [Authorize(Policy = "LeituraAluno")]
    [HttpGet]
    public IActionResult Listar()
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Ok(_alunoService.ListarAlunos(escopo));
    }

    [Authorize(Policy = "LeituraAluno")]
    [HttpGet("perfil/{id:int}")]
    public IActionResult GetAluno(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        var dadosAluno = _alunoService.ObterAlunoPorId(id, escopo);

        if (dadosAluno != null)
        {
            return Ok(dadosAluno);
        }

        // RF9: fora do escopo responde 403 sem revelar existência. Apenas o escopo
        // global distingue "não existe" (404) de "não é seu" (403).
        return escopo.Global
            ? NotFound(new { erro = "Aluno não encontrado." })
            : Forbid();
    }

    /// <summary>
    /// Trilha completa de alterações do aluno (Spec 023, RF6): criação, campos alterados e
    /// restrições, em ordem decrescente de data, limitada ao escopo do solicitante.
    /// </summary>
    [Authorize(Policy = "LeituraAluno")]
    [HttpGet("{id:int}/historico")]
    public IActionResult Historico(
        int id,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _historicoService.Listar(id, pagina, tamanho, escopo),
            Ok);
    }

    /// <summary>
    /// Rota publicada pela Spec 010, mantida por compatibilidade (Spec 023, RF7): devolve o
    /// recorte de restrições da trilha única com o mesmo JSON de antes.
    /// </summary>
    [Authorize(Policy = "LeituraAluno")]
    [HttpGet("{id:int}/restricoes/historico")]
    public IActionResult HistoricoRestricoes(
        int id,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanho)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _historicoService.ListarRestricoes(id, pagina, tamanho, escopo),
            Ok);
    }

    [Authorize(Policy = "GestaoAluno")]
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] AlunoEntrada dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        var aluno = _alunoService.AtualizarAluno(id, dto, escopo);

        if (aluno != null)
        {
            return Ok(aluno);
        }

        return escopo.Global
            ? NotFound(new { erro = "Aluno não encontrado." })
            : Forbid();
    }

    [Authorize(Policy = "GestaoAluno")]
    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        if (_alunoService.ExcluirAluno(id, escopo))
        {
            return NoContent();
        }

        return escopo.Global
            ? NotFound(new { erro = "Aluno não encontrado." })
            : Forbid();
    }
}
