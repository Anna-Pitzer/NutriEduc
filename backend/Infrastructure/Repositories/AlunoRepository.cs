using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;
using Ntc.Domain;
using Ntc.Domain.Interface;

namespace Ntc.Database;
public class AlunoRepository : IAlunoRepository
{
    private readonly ConnectionContext _context;

    public AlunoRepository(ConnectionContext context)
{
    _context = context;
}
    public void Add(Aluno aluno)
    {
        _context.Alunos.Add(aluno);
        _context.SaveChanges();
    }

    public List<Aluno> Listar(EscopoUsuario escopo)
    {
        return Filtrar(escopo).ToList();
    }

    public Aluno? Obter(int id, EscopoUsuario escopo)
    {
        return Filtrar(escopo).FirstOrDefault(aluno => aluno.Id == id);
    }

    public void UpdateAluno(Aluno aluno)
    {
        _context.Alunos.Update(aluno);
        _context.SaveChanges();
    }

    public void DeleteAluno(Aluno aluno)
    {
        _context.Alunos.Remove(aluno);
        _context.SaveChanges();
    }

    /// <summary>
    /// Filtro por papel, sempre em SQL (RNF4). Aluno sem unidade só é visível para
    /// Nutricionista e AdminSecretaria — escola não o enxerga até o vínculo existir.
    /// </summary>
    private IQueryable<Aluno> Filtrar(EscopoUsuario escopo)
    {
        var alunos = _context.Alunos.AsQueryable();

        switch (escopo.Papel)
        {
            case PapelUsuario.AdminSecretaria:
            case PapelUsuario.Nutricionista:
                return alunos;

            case PapelUsuario.AdminEscola:
                var escolaId = escopo.EscolaId;
                var unidadesDaEscola = _context.Unidades
                    .Where(unidade => unidade.EscolaId == escolaId);
                return alunos.Where(aluno =>
                    aluno.UnidadeId != null &&
                    unidadesDaEscola.Any(unidade => unidade.Id == aluno.UnidadeId));

            case PapelUsuario.Merendeira:
                if (escopo.UnidadeId is null)
                {
                    return alunos.Where(_ => false);
                }

                var unidadeId = escopo.UnidadeId.Value;
                return alunos.Where(aluno => aluno.UnidadeId == unidadeId);

            case PapelUsuario.Responsavel:
                var usuarioId = escopo.UsuarioId;
                var vinculos = _context.Vinculos
                    .Where(vinculo => vinculo.ResponsavelUsuarioId == usuarioId);
                return alunos.Where(aluno =>
                    vinculos.Any(vinculo => vinculo.AlunoId == aluno.Id));

            default:
                return alunos.Where(_ => false);
        }
    }

    public bool ExistePorUnidade(int unidadeId) =>
        _context.Alunos.Any(aluno => aluno.UnidadeId == unidadeId);
}
