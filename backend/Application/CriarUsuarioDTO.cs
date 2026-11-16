using System.ComponentModel.DataAnnotations;
using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>
/// Entrada de `POST /api/usuarios` (Spec 009, RF4). O papel é obrigatório aqui: é o
/// administrador quem define o que a conta pode fazer. `escolaId` é obrigatório para
/// papéis que exigem escola e proibido para os globais — a regra final é do domínio.
/// </summary>
public class CriarUsuarioDTO
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CPF é obrigatório.")]
    public string Cpf { get; set; } = string.Empty;

    public DateOnly DataNascimento { get; set; }

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 100 caracteres.")]
    public string Senha { get; set; } = string.Empty;

    public string? Telefone { get; set; }
    public string? Foto { get; set; }

    /// <summary>Registro no conselho de nutrição; identifica a autora do cardápio.</summary>
    [StringLength(20, ErrorMessage = "O CRN deve ter no máximo 20 caracteres.")]
    public string? Crn { get; set; }

    [Required(ErrorMessage = "O papel é obrigatório.")]
    public PapelUsuario? Papel { get; set; }

    public int? EscolaId { get; set; }
    public int? UnidadeId { get; set; }

    /// <summary>
    /// Alunos vinculados (Spec 034). Obrigatório quando o papel é <c>Responsavel</c>: a
    /// conta só existe com vínculo, criado no mesmo ato pelo administrador.
    /// </summary>
    public List<int>? AlunosIds { get; set; }
}
