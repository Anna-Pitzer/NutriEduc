using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Referências institucionais verificadas antes de gravar (Spec 024, F4). A entidade valida
/// o formato do id; existência e situação são regra de aplicação, e a falha acontece antes
/// de qualquer persistência.
/// </summary>
public class ReferenciasInstitucionaisApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ReferenciasInstitucionaisApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object Payload(string nome, int? escolaId) => new
    {
        nome,
        nascimento = "2015-05-10",
        escola = "Escola Teste",
        serie = "3o ano",
        telefone = "11999999999",
        observacao = "",
        restricoes = Array.Empty<string>(),
        anafilaxia = false,
        nomeResponsavel = "Maria Teste",
        telefoneResponsavel = "11988888888",
        escolaId
    };

    [Fact]
    public async Task CriarAluno_ComEscolaInexistente_Retorna400ENaoGrava()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);

        var resposta = await client.PostAsJsonAsync(
            "/api/aluno/registrar", Payload("Aluno Escola Fantasma", 999999));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(
            "Escola inexistente.",
            corpo.RootElement.GetProperty("erro").GetString());

        var alunos = await client.GetFromJsonAsync<List<JsonElement>>("/api/aluno");
        Assert.DoesNotContain(alunos!, aluno =>
            aluno.GetProperty("nome").GetString() == "Aluno Escola Fantasma");
    }

    [Fact]
    public async Task CriarAluno_ComUnidadeDesativada_Retorna400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();

        using (var scope = _factory.Services.CreateScope())
        {
            var escolas = scope.ServiceProvider.GetRequiredService<IEscolaRepository>();
            var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

            var unidade = escolas.GetUnidade(unidadeId)!;
            unidade.Desativar();
            unidadeDeTrabalho.ExecutarEmTransacao(() => escolas.UpdateUnidade(unidade));
        }

        var resposta = await client.PostAsJsonAsync(
            "/api/aluno/registrar", Payload("Aluno Unidade Inativa", escolaId));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Contains(
            "desativada",
            corpo.RootElement.GetProperty("erro").GetString());
    }
}
