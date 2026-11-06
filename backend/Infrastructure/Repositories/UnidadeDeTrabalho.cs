using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>
/// Unidade de trabalho sobre o <see cref="ConnectionContext"/> Scoped (Spec 024, F6).
/// Abre uma transação por operação de caso de uso, confirma com <c>SaveChanges</c> e
/// desfaz em qualquer exceção — sem que nenhum repositório decida transação.
/// </summary>
public sealed class UnidadeDeTrabalho : IUnidadeDeTrabalho
{
    private readonly ConnectionContext _context;

    public UnidadeDeTrabalho(ConnectionContext context)
    {
        _context = context;
    }

    public T ExecutarEmTransacao<T>(Func<T> operacao)
    {
        using var transacao = _context.Database.BeginTransaction();

        try
        {
            var resultado = operacao();

            _context.SaveChanges();
            transacao.Commit();

            return resultado;
        }
        catch
        {
            transacao.Rollback();
            throw;
        }
    }

    public void ExecutarEmTransacao(Action operacao) =>
        ExecutarEmTransacao(() =>
        {
            operacao();
            return true;
        });

    public void Salvar() => _context.SaveChanges();
}
