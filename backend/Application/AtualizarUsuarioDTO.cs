using System.ComponentModel.DataAnnotations;
using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>
/// Entrada de `PUT /api/usuarios/{id}`. O dono do perfil só altera nome, e-mail,
/// telefone e foto; papel e escopo são aplicados apenas quando o solicitante é
/// administrador do escopo (RF4/RF5), regra aplicada no serviço.
/// </summary>
public class AtualizarUsuarioDTO
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    public string? Telefone { get; set; }
    public string? Foto { get; set; }

    /// <summary>Registro no conselho de nutrição; identifica a autora do cardápio.</summary>
    [StringLength(20, ErrorMessage = "O CRN deve ter no máximo 20 caracteres.")]
    public string? Crn { get; set; }

    /// <summary>Quando informado, só um administrador do escopo pode aplicar.</summary>
    public PapelUsuario? Papel { get; set; }

    public int? EscolaId { get; set; }
    public int? UnidadeId { get; set; }

    /// <summary>
    /// Alunos a vincular quando o papel passa a ser <c>Responsavel</c> (Spec 034). A conta
    /// não pode terminar o pedido sem ao menos um vínculo.
    /// </summary>
    public List<int>? AlunosIds { get; set; }
}
