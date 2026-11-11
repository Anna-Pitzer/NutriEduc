using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Consulta pública mínima de unidades ativas para o cadastro (Spec 033): anônima, apenas
/// id e nome, sem expor dados administrativos nem a rota autenticada.
/// </summary>
public class UnidadesAtivasApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    private static int _cpf = 1;

    public UnidadesAtivasApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListarAtivas_Anonimo_RetornaApenasAtivas_ComIdENome()
    {
        var (_, unidadeAtiva) = _factory.CriarEscolaComUnidade();
        var (_, unidadeInativa) = _factory.CriarEscolaComUnidade();

        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeInativa);
        var desativar = await admin.PostAsync($"/api/unidades/{unidadeInativa}/desativar", null);
        Assert.Equal(HttpStatusCode.OK, desativar.StatusCode);

        var client = _factory.CreateClient();
        var resposta = await client.GetAsync("/api/unidades/ativas");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var lista = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        var ids = lista.Select(item => item.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(unidadeAtiva, ids);
        Assert.DoesNotContain(unidadeInativa, ids);

        foreach (var item in lista)
        {
            var propriedades = item.EnumerateObject().Select(p => p.Name).ToList();
            Assert.Contains("id", propriedades);
            Assert.Contains("nome", propriedades);
            Assert.DoesNotContain("ativa", propriedades);
            Assert.DoesNotContain("escolaId", propriedades);
        }
    }

    [Fact]
    public async Task Cadastro_EmUnidadeInativa_Retorna400()
    {
        var (_, unidadeInativa) = _factory.CriarEscolaComUnidade();

        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeInativa);
        await admin.PostAsync($"/api/unidades/{unidadeInativa}/desativar", null);

        var client = _factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("/api/usuario/registrar", new
        {
            nome = "Solicitante Inativo",
            cpf = $"{System.Threading.Interlocked.Increment(ref _cpf):00000000000}",
            dataNascimento = "1990-01-01",
            email = $"inativo.{Guid.NewGuid():N}@nutrieduc.local",
            senha = "senha12345",
            telefone = (string?)null,
            foto = (string?)null,
            tipo = "Responsavel",
            unidadeId = unidadeInativa,
            crn = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastro_EmUnidadeInexistente_Retorna404()
    {
        var client = _factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("/api/usuario/registrar", new
        {
            nome = "Solicitante Fantasma",
            cpf = $"{System.Threading.Interlocked.Increment(ref _cpf):00000000000}",
            dataNascimento = "1990-01-01",
            email = $"fantasma.{Guid.NewGuid():N}@nutrieduc.local",
            senha = "senha12345",
            telefone = (string?)null,
            foto = (string?)null,
            tipo = "Responsavel",
            unidadeId = 999999,
            crn = (string?)null
        });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
