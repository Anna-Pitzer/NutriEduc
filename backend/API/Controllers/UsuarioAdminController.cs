using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;

namespace Ntc.Controllers;

/// <summary>
/// Gestão de usuários (RF4, RF5, RF7). Criar, listar e ativar/desativar são de
/// administrador; atualizar aceita o dono do perfil ou um administrador do escopo — a
/// decisão final é do serviço.
/// </summary>
[ApiController]
[Route("api/usuarios")]
public class UsuarioAdminController : RespostaController
{
    private readonly UsuarioService _usuarios;
    private readonly EscopoService _escopo;
    private readonly ExclusaoService _exclusao;

    public UsuarioAdminController(
        UsuarioService usuarios,
        EscopoService escopo,
        ExclusaoService exclusao)
    {
        _usuarios = usuarios;
        _escopo = escopo;
        _exclusao = exclusao;
    }

    [Authorize(Policy = "Admin")]
    [HttpGet]
    public IActionResult Listar(
        [FromQuery] PapelUsuario? papel,
        [FromQuery] int? escolaId,
        [FromQuery] int? unidadeId,
        [FromQuery] bool? ativo,
        [FromQuery] string? busca)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        var filtro = new FiltroUsuarios(papel, escolaId, unidadeId, ativo, busca);

        return Ok(_usuarios.Listar(filtro, escopo));
    }

    [Authorize(Policy = "Admin")]
    [HttpPost]
    public IActionResult Criar([FromBody] CriarUsuarioDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(
            _usuarios.CriarUsuarioAdministrado(dto, escopo),
            id => Created($"/api/usuario/perfil/{id}", new { id }));
    }

    /// <summary>
    /// O dono do perfil também chega aqui; por isso a política é apenas "sessão válida"
    /// e o serviço decide entre dono, administrador do escopo e 403.
    /// </summary>
    [Authorize]
    [HttpPut("{id:int}")]
    public IActionResult Atualizar(int id, [FromBody] AtualizarUsuarioDTO dto)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_usuarios.AtualizarUsuario(id, dto, escopo), usuario => Ok(usuario));
    }

    [Authorize(Policy = "Admin")]
    [HttpPost("{id:int}/ativar")]
    public IActionResult Ativar(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_usuarios.DefinirAtivo(id, true, escopo), usuario => Ok(usuario));
    }

    [Authorize(Policy = "Admin")]
    [HttpPost("{id:int}/desativar")]
    public IActionResult Desativar(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_usuarios.DefinirAtivo(id, false, escopo), usuario => Ok(usuario));
    }

    /// <summary>
    /// Exclusão definitiva. Bloqueia a própria conta, o último `AdminSecretaria` e contas
    /// com vínculo ou token de cadastro (`409`).
    /// </summary>
    [Authorize(Policy = "Admin")]
    [HttpDelete("{id:int}")]
    public IActionResult Excluir(int id)
    {
        var escopo = ObterEscopo(_escopo);

        if (escopo is null)
        {
            return SessaoInvalida();
        }

        return Traduzir(_exclusao.ExcluirUsuario(id, escopo), _ => NoContent());
    }
}
