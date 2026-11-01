using System.ComponentModel.DataAnnotations;

namespace Ntc.Application;

/// <summary>Entrada de criação/atualização de escola.</summary>
public class EscolaDTO
{
    [Required(ErrorMessage = "O nome da escola é obrigatório.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "O nome da escola deve ter entre 2 e 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;
}

/// <summary>Entrada de criação/atualização de unidade escolar.</summary>
public class UnidadeDTO
{
    [Required(ErrorMessage = "O nome da unidade é obrigatório.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "O nome da unidade deve ter entre 2 e 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;
}
