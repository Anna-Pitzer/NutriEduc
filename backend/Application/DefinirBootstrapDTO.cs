using System.ComponentModel.DataAnnotations;

namespace Ntc.Application;

/// <summary>
/// Liga/desliga a criação de administrador pelo painel. `Permitir` é anulável de
/// propósito: com `bool` puro, um corpo `{}` desligaria a rota em silêncio.
/// </summary>
public class DefinirBootstrapDTO
{
    [Required(ErrorMessage = "Informe permitir: true ou false.")]
    public bool? Permitir { get; set; }
}
