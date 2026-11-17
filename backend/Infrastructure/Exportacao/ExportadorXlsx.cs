using System.IO.Compression;
using System.Security;
using System.Text;
using Ntc.Domain.Interface;
using Ntc.Domain.Relatorios;

namespace Ntc.Exportacao;

/// <summary>
/// Exportador XLSX sem dependência externa (Spec 019). Gera um pacote OOXML mínimo —
/// planilha única com título, cabeçalho, linhas e rodapé — usando apenas
/// <see cref="ZipArchive"/> e XML escrito à mão. Determinístico: a mesma tabela produz o
/// mesmo arquivo.
/// </summary>
public sealed class ExportadorXlsx : IExportadorDeRelatorio
{
    public string Extensao => "xlsx";

    public string TipoDeConteudo =>
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public byte[] Gerar(TabelaDeRelatorio tabela)
    {
        using var memoria = new MemoryStream();

        using (var pacote = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            Escrever(pacote, "[Content_Types].xml", ContentTypes);
            Escrever(pacote, "_rels/.rels", RelacoesRaiz);
            Escrever(pacote, "xl/workbook.xml", Workbook);
            Escrever(pacote, "xl/_rels/workbook.xml.rels", RelacoesWorkbook);
            Escrever(pacote, "xl/styles.xml", Estilos);
            Escrever(pacote, "xl/worksheets/sheet1.xml", Planilha(tabela));
        }

        return memoria.ToArray();
    }

    private static string Planilha(TabelaDeRelatorio tabela)
    {
        var colunas = tabela.Colunas.Count == 0 ? new[] { "Valor" } : tabela.Colunas.ToArray();
        var ultimaColuna = Letra(colunas.Length - 1);
        var xml = new StringBuilder();

        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        xml.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

        xml.Append("<cols>");
        for (var i = 0; i < colunas.Length; i++)
        {
            var largura = i == 0 ? 30 : 18;
            xml.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{largura}\" customWidth=\"1\"/>");
        }
        xml.Append("</cols>");

        xml.Append("<sheetData>");

        var linha = 1;
        xml.Append(Linha(linha, 1, new[] { tabela.Titulo }));

        linha += 2;
        xml.Append(Linha(linha, 2, colunas));

        foreach (var valores in tabela.Linhas)
        {
            linha++;
            xml.Append(Linha(linha, 0, valores));
        }

        if (!string.IsNullOrWhiteSpace(tabela.Rodape))
        {
            linha += 2;
            xml.Append(Linha(linha, 0, new[] { tabela.Rodape }));
        }

        xml.Append("</sheetData>");

        if (colunas.Length > 1)
        {
            xml.Append("<mergeCells count=\"2\">");
            xml.Append($"<mergeCell ref=\"A1:{ultimaColuna}1\"/>");

            if (!string.IsNullOrWhiteSpace(tabela.Rodape))
            {
                xml.Append($"<mergeCell ref=\"A{linha}:{ultimaColuna}{linha}\"/>");
            }

            xml.Append("</mergeCells>");
        }

        xml.Append("</worksheet>");
        return xml.ToString();
    }

    private static string Linha(int numero, int estilo, IReadOnlyList<string> valores)
    {
        var xml = new StringBuilder();
        xml.Append($"<row r=\"{numero}\">");

        for (var i = 0; i < valores.Count; i++)
        {
            var referencia = $"{Letra(i)}{numero}";
            var texto = Escapar(valores[i]);

            xml.Append($"<c r=\"{referencia}\" s=\"{estilo}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{texto}</t></is></c>");
        }

        xml.Append("</row>");
        return xml.ToString();
    }

    private static string Letra(int indice) => ((char)('A' + indice)).ToString();

    private static string Escapar(string? valor) =>
        SecurityElement.Escape(valor ?? string.Empty) ?? string.Empty;

    private static void Escrever(ZipArchive pacote, string caminho, string conteudo)
    {
        var entrada = pacote.CreateEntry(caminho, CompressionLevel.Optimal);

        using var fluxo = entrada.Open();
        using var escritor = new StreamWriter(fluxo, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        escritor.Write(conteudo);
    }

    private const string ContentTypes =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
        "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
        "</Types>";

    private const string RelacoesRaiz =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>";

    private const string Workbook =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
        "<sheets><sheet name=\"Relatório\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
        "</workbook>";

    private const string RelacoesWorkbook =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
        "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
        "</Relationships>";

    private const string Estilos =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<fonts count=\"3\">" +
        "<font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
        "<font><b/><sz val=\"14\"/><name val=\"Calibri\"/></font>" +
        "<font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
        "</fonts>" +
        "<fills count=\"3\">" +
        "<fill><patternFill patternType=\"none\"/></fill>" +
        "<fill><patternFill patternType=\"gray125\"/></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFD9D9D9\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
        "</fills>" +
        "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"3\">" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
        "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>" +
        "</cellXfs>" +
        "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
        "</styleSheet>";
}
