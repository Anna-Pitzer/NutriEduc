using Ntc.Domain.Relatorios;

namespace Ntc.Domain.Interface;

/// <summary>
/// Leitura dos relatórios (Spec 019, RNF2). Somente leitura, já filtrada pelo escopo
/// resolvido em <see cref="FiltroDeRelatorio"/> e devolvida como tabela pronta.
/// </summary>
public interface IRelatorioRepository
{
    TabelaDeRelatorio Obter(TipoDeRelatorio tipo, FiltroDeRelatorio filtro);
}
