namespace Ntc.Application;

/// <summary>Pré-visualização de um relatório (Spec 019, RF3/RF4).</summary>
public class RelatorioPreviewDTO
{
    public string Tipo { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public List<string> Colunas { get; set; } = new();

    public List<List<string>> Linhas { get; set; } = new();

    /// <summary>Total de linhas do filtro, mesmo quando a prévia é truncada.</summary>
    public int TotalLinhas { get; set; }

    public string? Rodape { get; set; }
}

/// <summary>Arquivo pronto para download (Spec 019, RF5).</summary>
public sealed record ArquivoDeRelatorio(byte[] Conteudo, string Nome, string TipoDeConteudo);
