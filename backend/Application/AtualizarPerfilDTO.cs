using System.ComponentModel.DataAnnotations;

namespace Ntc.Application;

public class AtualizarPerfilDTO
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Nome { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = "";
    public string? Telefone { get; set; }
    public string? Foto { get; set; }
}