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
    public IActionResult Listar(
        [FromQuery] int? escolaId,
        [FromQuery] int? unidadeId)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        if (!_alunoService.FiltroNoEscopo(escolaId, unidadeId, escopo))
        {
            return Forbid();
        }

        return Ok(_alunoService.ListarAlunos(escopo, escolaId, unidadeId));
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

        return Traduzir(_alunoService.ObterAlunoPorId(id, escopo), Ok);
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

        return escopo.EscopoDeUnidade
            ? NotFound(new { erro = "Aluno não encontrado." })
            : Forbid();
    }

    /// <summary>
    /// Transfere o aluno entre escolas da mesma unidade (Spec 027, RF11), registrando a
    /// mudança na trilha do aluno.
    /// </summary>
    [Authorize(Policy = "AdminSecretaria")]
    [HttpPost("{id:int}/transferir-escola")]
    public IActionResult TransferirEscola(int id, [FromBody] TransferirEscolaDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        var aluno = _alunoService.TransferirEscola(id, dto.EscolaId, escopo);

        if (aluno != null)
        {
            return Ok(aluno);
        }

        return escopo.EscopoDeUnidade
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

        return escopo.EscopoDeUnidade
            ? NotFound(new { erro = "Aluno não encontrado." })
            : Forbid();
    }
}
