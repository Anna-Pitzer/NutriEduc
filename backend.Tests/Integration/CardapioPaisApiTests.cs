using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Portal dos pais (Spec 035, revisão): o responsável vê o cardápio geral e a versão
/// adaptada do filho na mesma consulta, sem perder o alerta quando a adaptada não existe.
/// </summary>
public class CardapioPaisApiTests
{
    private static readonly DateOnly Dia = new(2026, 11, 2);

    private sealed record Cenario(
        HttpClient Nutricionista,
        HttpClient Responsavel,
        int UnidadeId,
        int EscolaId,
        int AlunoComRestricaoId,
        int AlunoSemRestricaoId,
        int GeralId,
        int AdaptadoId);

    private static int ObterRestricaoId(ApiFactory fabrica, string nome)
    {
        using var scope = fabrica.Services.CreateScope();
        var restricoes = scope.ServiceProvider.GetRequiredService<IRestricaoRepository>();
        return restricoes.Listar(null, false).First(restricao => restricao.Nome == nome).Id;
    }

    private static async Task<int> CriarAlimento(
        HttpClient client,
        string nome,
        IEnumerable<int>? restricaoIds = null)
    {
        var categorias = await client.GetFromJsonAsync<JsonElement>("/api/categorias-alimento");
        var categoriaId = categorias[0].GetProperty("id").GetInt32();

        var resposta = await client.PostAsJsonAsync("/api/alimentos", new
        {
            nome,
            categoriaId,
            valores = new
            {
                caloriasKcal = 130m,
                proteinasG = 2.7m,
                carboidratosG = 28.2m,
                gordurasG = 0.3m,
                fibrasG = 0.4m,
                sodioMg = 1m
            },
            restricaoIds = restricaoIds ?? Array.Empty<int>()
        });

        resposta.EnsureSuccessStatusCode();

        return (await resposta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();
    }

    private static int CriarAluno(
        ApiFactory fabrica,
        int unidadeId,
        int escolaId,
        string[] restricoes)
    {
        using var scope = fabrica.Services.CreateScope();
        var alunos = scope.ServiceProvider.GetRequiredService<IAlunoRepository>();
        var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

        var aluno = new Aluno(
            $"Aluno {Guid.NewGuid():N}",
            new DateOnly(2015, 5, 20),
            new Turma("Escola", "5 ano"),
            null,
            null,
            restricoes,
            false,
            new Responsavel("Responsável", "11988888888"),
            escolaId);

        aluno.DefinirUnidade(unidadeId);

        unidadeDeTrabalho.ExecutarEmTransacao(() => alunos.Add(aluno));

        return aluno.Id;
    }

    private static void Vincular(ApiFactory fabrica, int alunoId, int responsavelUsuarioId)
    {
        using var scope = fabrica.Services.CreateScope();
        var vinculos = scope.ServiceProvider.GetRequiredService<IVinculoRepository>();
        var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

        unidadeDeTrabalho.ExecutarEmTransacao(
            () => vinculos.Add(new VinculoResponsavelAluno(alunoId, responsavelUsuarioId)));
    }

    private static object Payload(int unidadeId, int nutricionistaId, int alimentoId) => new
    {
        nome = "Semana dos pais",
        unidadeId,
        nutricionistaId,
        confirmarIncompatibilidades = true,
        periodo = new { inicio = "2026-11-02", fim = "2026-11-06" },
        refeicoes = new[]
        {
            new
            {
                nome = "Almoço",
                ordem = 1,
                itens = new[] { new { alimentoId, gramas = 100m } }
            }
        }
    };

    private static async Task<JsonElement> ConsultarAsync(HttpClient cliente, int alunoId) =>
        await cliente.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/cardapio?data={Dia:yyyy-MM-dd}");

    private static async Task<Cenario> PrepararAsync(ApiFactory fabrica)
    {
        var (nutricionista, nutricionistaId, unidadeId) =
            await fabrica.CriarNutricionistaComUnidadeAsync();
        var escolaId = fabrica.CriarEscolaNaUnidade(unidadeId);
        var lactoseId = ObterRestricaoId(fabrica, "Lactose");

        var leite = await CriarAlimento(
            nutricionista,
            $"Leite {Guid.NewGuid():N}",
            new[] { lactoseId });
        await CriarAlimento(nutricionista, $"Bebida {Guid.NewGuid():N}");

        var alunoComRestricaoId = CriarAluno(fabrica, unidadeId, escolaId, new[] { "Lactose" });
        var alunoSemRestricaoId = CriarAluno(fabrica, unidadeId, escolaId, Array.Empty<string>());

        var (responsavel, responsavelId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: unidadeId);

        Vincular(fabrica, alunoComRestricaoId, responsavelId);
        Vincular(fabrica, alunoSemRestricaoId, responsavelId);

        var criacao = await nutricionista.PostAsJsonAsync(
            "/api/cardapios",
            Payload(unidadeId, nutricionistaId, leite));

        criacao.EnsureSuccessStatusCode();

        var geralId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();

        var adaptados = await nutricionista.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios/{geralId}/adaptados");

        var adaptadoId = adaptados[0].GetProperty("id").GetInt32();

        return new Cenario(
            nutricionista,
            responsavel,
            unidadeId,
            escolaId,
            alunoComRestricaoId,
            alunoSemRestricaoId,
            geralId,
            adaptadoId);
    }

    [Fact]
    public async Task Responsavel_VeGeralEAdaptadoDoFilhoNaMesmaConsulta()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica);

        // Só o geral publicado: a adaptada ainda não existe e o alerta permanece.
        var publicarGeral = await cenario.Nutricionista.PostAsync(
            $"/api/cardapios/{cenario.GeralId}/publicar",
            null);
        publicarGeral.EnsureSuccessStatusCode();

        var antes = await ConsultarAsync(cenario.Responsavel, cenario.AlunoComRestricaoId);

        Assert.Equal(cenario.GeralId, antes.GetProperty("cardapioGeral").GetProperty("id").GetInt32());
        Assert.Equal(JsonValueKind.Null, antes.GetProperty("cardapioAdaptado").ValueKind);
        Assert.False(antes.GetProperty("disponivel").GetBoolean());
        Assert.True(antes.GetProperty("requerAdaptacao").GetBoolean());
        Assert.True(antes.GetProperty("alerta").GetBoolean());

        var publicarAdaptado = await cenario.Nutricionista.PostAsync(
            $"/api/cardapios/{cenario.AdaptadoId}/publicar",
            null);
        publicarAdaptado.EnsureSuccessStatusCode();

        var comRestricao = await ConsultarAsync(cenario.Responsavel, cenario.AlunoComRestricaoId);

        Assert.True(comRestricao.GetProperty("disponivel").GetBoolean());
        Assert.True(comRestricao.GetProperty("requerAdaptacao").GetBoolean());
        Assert.Equal(
            cenario.GeralId,
            comRestricao.GetProperty("cardapioGeral").GetProperty("id").GetInt32());
        Assert.Equal(
            cenario.AdaptadoId,
            comRestricao.GetProperty("cardapioAdaptado").GetProperty("id").GetInt32());
        Assert.Equal(
            "Adaptado",
            comRestricao.GetProperty("cardapioAdaptado").GetProperty("tipo").GetString());
        Assert.Equal(
            cenario.AdaptadoId,
            comRestricao.GetProperty("cardapio").GetProperty("id").GetInt32());

        // Filho sem restrição: só a versão geral, marcada como disponível.
        var semRestricao = await ConsultarAsync(cenario.Responsavel, cenario.AlunoSemRestricaoId);

        Assert.True(semRestricao.GetProperty("disponivel").GetBoolean());
        Assert.False(semRestricao.GetProperty("requerAdaptacao").GetBoolean());
        Assert.Equal(
            cenario.GeralId,
            semRestricao.GetProperty("cardapioGeral").GetProperty("id").GetInt32());
        Assert.Equal(JsonValueKind.Null, semRestricao.GetProperty("cardapioAdaptado").ValueKind);
    }

    [Fact]
    public async Task Responsavel_DeOutroAluno_Recebe403()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica);

        var (outroResponsavel, _) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: cenario.UnidadeId);

        var resposta = await outroResponsavel.GetAsync(
            $"/api/aluno/{cenario.AlunoComRestricaoId}/cardapio?data={Dia:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}
