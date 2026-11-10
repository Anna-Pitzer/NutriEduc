using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>Repositório em memória de vínculos responsável-aluno para os testes de serviço.</summary>
public class VinculoRepositoryFake : IVinculoRepository
{
    private readonly List<VinculoResponsavelAluno> _itens = new();

    public void Add(VinculoResponsavelAluno vinculo) => _itens.Add(vinculo);

    public bool Existe(int alunoId, int responsavelUsuarioId) =>
        _itens.Any(item => item.AlunoId == alunoId && item.ResponsavelUsuarioId == responsavelUsuarioId);

    public bool Remover(int alunoId, int responsavelUsuarioId)
    {
        var vinculo = _itens.FirstOrDefault(item =>
            item.AlunoId == alunoId && item.ResponsavelUsuarioId == responsavelUsuarioId);

        if (vinculo is null)
        {
            return false;
        }

        _itens.Remove(vinculo);
        return true;
    }

    public List<VinculoResponsavelAluno> ListarPorAluno(int alunoId) =>
        _itens.Where(item => item.AlunoId == alunoId).ToList();

    public bool ExistePorResponsavel(int usuarioId) =>
        _itens.Any(item => item.ResponsavelUsuarioId == usuarioId);
}
