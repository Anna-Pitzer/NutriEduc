using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

public class VinculoRepository : IVinculoRepository
{
    private readonly ConnectionContext _context;

    public VinculoRepository(ConnectionContext context)
    {
        _context = context;
    }

    public void Add(VinculoResponsavelAluno vinculo)
    {
        _context.Vinculos.Add(vinculo);
    }

    public bool Existe(int alunoId, int responsavelUsuarioId)
    {
        return _context.Vinculos.Any(vinculo =>
            vinculo.AlunoId == alunoId &&
            vinculo.ResponsavelUsuarioId == responsavelUsuarioId);
    }

    public bool Remover(int alunoId, int responsavelUsuarioId)
    {
        var vinculo = _context.Vinculos.FirstOrDefault(item =>
            item.AlunoId == alunoId &&
            item.ResponsavelUsuarioId == responsavelUsuarioId);

        if (vinculo is null)
        {
            return false;
        }

        _context.Vinculos.Remove(vinculo);
        return true;
    }

    public List<VinculoResponsavelAluno> ListarPorAluno(int alunoId)
    {
        return _context.Vinculos
            .Where(vinculo => vinculo.AlunoId == alunoId)
            .ToList();
    }

    public bool ExistePorResponsavel(int usuarioId) =>
        _context.Vinculos.Any(vinculo => vinculo.ResponsavelUsuarioId == usuarioId);
}
