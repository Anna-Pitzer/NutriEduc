using Ntc.Domain.Relatorios;

namespace Ntc.Domain.Interface;

/// <summary>
/// Exportador plugável (ADR-012). Recebe a tabela pronta e devolve o arquivo; não lê
/// banco, não decide permissão e não altera a tabela.
/// </summary>
public interface IExportadorDeRelatorio
{
    string Extensao { get; }

    string TipoDeConteudo { get; }

    byte[] Gerar(TabelaDeRelatorio tabela);
}
