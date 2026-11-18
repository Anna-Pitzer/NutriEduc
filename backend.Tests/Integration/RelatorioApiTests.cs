using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Contrato HTTP dos relatórios (Spec 019): pré-visualização, exportação XLSX e CSV,
/// catálogo fechado, validação de período e autorização pela matriz.
/// </summary>
public class RelatorioApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RelatorioApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Preview_DeRelatorioConhecido_RetornaColunasETotal()
    {
        var (client, _, _) = await _factory.CriarNutricionistaComUnidadeAsync();

        var resposta = await client.GetAsync("/api/relatorios/Cardapios/preview");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(corpo.GetProperty("colunas").GetArrayLength() > 0);
        Assert.True(corpo.GetProperty("totalLinhas").GetInt32() >= 0);
        Assert.Equal("Cardapios", corpo.GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task Preview_DeTipoDesconhecido_Retorna400()
    {
        var (client, _, _) = await _factory.CriarNutricionistaComUnidadeAsync();

        var resposta = await client.GetAsync("/api/relatorios/Inexistente/preview");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Preview_ComPeriodoInvertido_Retorna400()
    {
        var (client, _, _) = await _factory.CriarNutricionistaComUnidadeAsync();

        var resposta = await client.GetAsync(
            "/api/relatorios/Ocorrencias/preview?de=2026-02-27&ate=2026-02-20");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Exportar_RetornaArquivoXlsx()
    {
        var (client, _, _) = await _factory.CriarNutricionistaComUnidadeAsync();

        var resposta = await client.GetAsync("/api/relatorios/AlunosRestricoes/exportar");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            resposta.Content.Headers.ContentType?.MediaType);

        var bytes = await resposta.Content.ReadAsByteArrayAsync();

        Assert.True(bytes.Length > 4);
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public async Task Exportar_ComFormatoCsv_RetornaTextoCsv()
    {
        var (client, _, _) = await _factory.CriarNutricionistaComUnidadeAsync();

        var resposta = await client.GetAsync(
            "/api/relatorios/AlunosRestricoes/exportar?formato=csv");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("text/csv", resposta.Content.Headers.ContentType?.MediaType);
        Assert.Contains(".csv", resposta.Content.Headers.ContentDisposition?.ToString() ?? "");

        var bytes = await resposta.Content.ReadAsByteArrayAsync();

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);

        var texto = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        Assert.Contains(";", texto);
        Assert.DoesNotContain("<worksheet", texto);
    }

    [Fact]
    public async Task Exportar_ComFormatoDesconhecido_Retorna400()
    {
        var (client, _, _) = await _factory.CriarNutricionistaComUnidadeAsync();

        var resposta = await client.GetAsync(
            "/api/relatorios/AlunosRestricoes/exportar?formato=pdf");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Contains("xlsx", corpo.GetProperty("erro").GetString());
    }

    [Fact]
    public async Task Exportar_PorMerendeiraComFormatoInvalido_Retorna403()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.Merendeira);

        var resposta = await client.GetAsync("/api/relatorios/Ocorrencias/exportar?formato=pdf");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Preview_PorMerendeira_Retorna403()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.Merendeira);

        var resposta = await client.GetAsync("/api/relatorios/Ocorrencias/preview");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Preview_PorResponsavel_Retorna403()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.Responsavel);

        var resposta = await client.GetAsync("/api/relatorios/Ocorrencias/preview");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}
