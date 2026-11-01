using System.ComponentModel.DataAnnotations;

namespace Ntc.Application;

/// <summary>
/// Dados do bootstrap de administrador (Spec 022). O papel não é informado: a rota
/// sempre cria <c>AdminSecretaria</c>, porque é justamente o papel que ela existe para
/// semear.
/// </summary>
public class CriarAdminBootstrapDTO
{
    [Required(ErrorMessage = "Informe o token de configuração.")]
    public string Token { get; set; } = "";

    /// <summary>
    /// Nome exato da unidade de destino. Obrigatório para o token de ambiente; ignorado
    /// quando o token é o temporário do painel, que já carrega a unidade de quem o gerou.
    /// </summary>
    public string? Unidade { get; set; }

    [Required(ErrorMessage = "O campo Nome é obrigatório.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 120 caracteres.")]
    public string Nome { get; set; } = "";

    [Required(ErrorMessage = "O campo CPF é obrigatório.")]
    public string Cpf { get; set; } = "";

    [Required(ErrorMessage = "O campo Nascimento é obrigatório.")]
    public DateOnly DataNascimento { get; set; }

    [Required(ErrorMessage = "O campo E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "O campo Senha é obrigatório.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
    public string Senha { get; set; } = "";
}
