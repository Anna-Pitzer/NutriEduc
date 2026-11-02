namespace Ntc.Domain.Entity;

/// <summary>
/// Vínculo N:N entre um aluno e um usuário com papel <see cref="PapelUsuario.Responsavel"/>.
/// A chave é composta por <see cref="AlunoId"/> e <see cref="ResponsavelUsuarioId"/>; a
/// unicidade do par é garantida pelo índice do banco e pela checagem no serviço.
/// </summary>
public class VinculoResponsavelAluno
{
    public int AlunoId { get; private set; }

    public int ResponsavelUsuarioId { get; private set; }

    /// <summary>Somente para o EF Core.</summary>
    private VinculoResponsavelAluno()
    {
    }

    public VinculoResponsavelAluno(int alunoId, int responsavelUsuarioId)
    {
        if (alunoId <= 0)
        {
            throw new ArgumentException("Vínculo exige aluno válido.");
        }

        if (responsavelUsuarioId <= 0)
        {
            throw new ArgumentException("Vínculo exige responsável válido.");
        }

        AlunoId = alunoId;
        ResponsavelUsuarioId = responsavelUsuarioId;
    }
}
