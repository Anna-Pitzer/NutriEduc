using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

public interface IVinculoRepository
{
    void Add(VinculoResponsavelAluno vinculo);
    bool Existe(int alunoId, int responsavelUsuarioId);
    bool Remover(int alunoId, int responsavelUsuarioId);
    List<VinculoResponsavelAluno> ListarPorAluno(int alunoId);

    /// <summary>True quando o responsável ainda está ligado a algum aluno.</summary>
    bool ExistePorResponsavel(int usuarioId);
}
