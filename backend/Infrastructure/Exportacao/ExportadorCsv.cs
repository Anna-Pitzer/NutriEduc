using System.Text;
using Ntc.Domain.Interface;
using Ntc.Domain.Relatorios;

namespace Ntc.Exportacao;

/// <summary>
/// Exportador CSV (RFC 4180) — segunda implementação de <see cref="IExportadorDeRelatorio"/>
/// (ADR-012), escolhida em `RelatorioService` pelo parâmetro `formato`. Emite a tabela pura:
/// separador `;`, UTF-8 com BOM (para o Excel abrir acentos) e `\r\n` entre linhas. Título e
/// rodapé são metadados do documento e ficam fora do arquivo, senão quebrariam o cabeçalho.
/// Determinístico: a mesma tabela produz os mesmos bytes.
/// </summary>
public sealed class ExportadorCsv : IExportadorDeRelatorio
{
    private const char Separador = ';';
    private const string FimDeLinha = "\r\n";
    private static readonly char[] PrecisamDeAspas = { Separador, '"', '\r', '\n' };

    public string Extensao => "csv";

    public string TipoDeConteudo => "text/csv; charset=utf-8";

    public byte[] Gerar(TabelaDeRelatorio tabela)
    {
        var colunas = tabela.Colunas.Count == 0 ? new[] { "Valor" } : tabela.Colunas.ToArray();
        var texto = new StringBuilder();

        EscreverLinha(texto, colunas);

        foreach (var linha in tabela.Linhas)
        {
            EscreverLinha(texto, linha);
        }

        var corpo = Encoding.UTF8.GetBytes(texto.ToString());
        var bom = Encoding.UTF8.GetPreamble();
        var arquivo = new byte[bom.Length + corpo.Length];

        bom.CopyTo(arquivo, 0);
        corpo.CopyTo(arquivo, bom.Length);

        return arquivo;
    }

    private static void EscreverLinha(StringBuilder texto, IReadOnlyList<string> campos)
    {
        for (var i = 0; i < campos.Count; i++)
        {
            if (i > 0)
            {
                texto.Append(Separador);
            }

            texto.Append(Escapar(campos[i]));
        }

        texto.Append(FimDeLinha);
    }

    private static string Escapar(string valor) =>
        valor.IndexOfAny(PrecisamDeAspas) < 0
            ? valor
            : $"\"{valor.Replace("\"", "\"\"")}\"";
}
