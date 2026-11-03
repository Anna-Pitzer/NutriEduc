using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>Persistência dos parâmetros de geração por unidade (Spec 013, RF1 e RF2).</summary>
public class ParametroGeracaoRepository : IParametroGeracaoRepository
{
    private readonly ConnectionContext _context;

    public ParametroGeracaoRepository(ConnectionContext context)
    {
        _context = context;
    }

    public ParametroGeracao? ObterPorUnidade(int unidadeId) =>
        _context.ParametrosGeracao.FirstOrDefault(parametro => parametro.UnidadeId == unidadeId);

    public ParametroGeracao Add(ParametroGeracao parametro)
    {
        _context.ParametrosGeracao.Add(parametro);
        return parametro;
    }

    public void Update(ParametroGeracao parametro)
    {
        _context.ParametrosGeracao.Update(parametro);
    }
}
