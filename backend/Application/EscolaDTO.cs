using System.ComponentModel.DataAnnotations;

namespace Ntc.Application;

/// <summary>Entrada de criação/atualização de escola.</summary>
public class EscolaDTO
{
    [Required(ErrorMessage = "O nome da escola é obrigatório.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "O nome da escola deve ter entre 2 e 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Unidade da escola (Spec 027). No `POST` é obrigatória e precisa ser a unidade da
    /// sessão; no `PUT` só é aplicada quando o administrador move a escola.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Informe a unidade da escola.")]
    public int? UnidadeId { get; set; }
}

/// <summary>Entrada de criação/atualização de unidade escolar.</summary>
public class UnidadeDTO
{
    [Required(ErrorMessage = "O nome da unidade é obrigatório.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "O nome da unidade deve ter entre 2 e 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;
}

/// <summary>Entrada da transferência de aluno entre escolas da mesma unidade.</summary>
public class TransferirEscolaDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Informe a escola de destino.")]
    public int EscolaId { get; set; }
}

/// <summary>Contagens de legado da hierarquia (Spec 027, T9).</summary>
public sealed record DiagnosticoHierarquiaDTO(
    int EscolasSemUnidade,
    int AlunosSemEscola,
    int UsuariosSemVinculoObrigatorio);
