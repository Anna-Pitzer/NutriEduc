using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Trilha append-only de ações privilegiadas (Spec 028, T7/RF11). Verifica autor,
/// data UTC e valores anterior/novo na mesma transação da ação.
/// </summary>
public class AuditoriaAcaoTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuditoriaAcaoTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    private static object PayloadAluno(string nome, int escolaId) => new
    {
        nome,
        nascimento = "2015-05-10",
        escola = "Rótulo",
        serie = "3o ano",
        telefone = "11999999999",
        observacao = "",
        restricoes = Array.Empty<string>(),
        anafilaxia = false,
        nomeResponsavel = "Maria Teste",
        telefoneResponsavel = "11988888888",
        escolaId
    };

    private static async Task<int> CriarEscola(HttpClient admin, int unidadeId)
    {
        var resposta = await admin.PostAsJsonAsync("/api/escolas", new
        {
            nome = $"Escola {Guid.NewGuid():N}",
            unidadeId
        });
        resposta.EnsureSuccessStatusCode();

        return (await Ler(resposta)).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task AcaoPrivilegiada_GravaTrilhaComAutorEDataUtc()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaA = await CriarEscola(admin, _factory.UnidadeIdPadrao);
        var escolaB = await CriarEscola(admin, _factory.UnidadeIdPadrao);

        var criacaoAluno = await admin.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno($"Aluno Auditado {Guid.NewGuid():N}", escolaA));
        criacaoAluno.EnsureSuccessStatusCode();
        var alunoId = (await Ler(criacaoAluno)).GetProperty("id").GetInt32();

        var email = $"auditoria.{Guid.NewGuid():N}@nutrieduc.local";
        var criacaoUsuario = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Responsavel Auditado",
            cpf = "12345678901",
            dataNascimento = "1990-01-01",
            email,
            senha = "senha12345",
            papel = nameof(PapelUsuario.Responsavel),
            unidadeId = _factory.UnidadeIdPadrao
        });
        criacaoUsuario.EnsureSuccessStatusCode();
        var usuarioId = (await Ler(criacaoUsuario)).GetProperty("id").GetInt32();

        var vinculo = await admin.PostAsync(
            $"/api/aluno/{alunoId}/responsaveis/{usuarioId}", null);
        Assert.Equal(HttpStatusCode.NoContent, vinculo.StatusCode);

        var papel = await admin.PutAsJsonAsync($"/api/usuarios/{usuarioId}", new
        {
            nome = "Responsavel Auditado",
            email,
            papel = nameof(PapelUsuario.Merendeira),
            escolaId = escolaA,
            unidadeId = _factory.UnidadeIdPadrao
        });
        Assert.Equal(HttpStatusCode.OK, papel.StatusCode);

        var desativar = await admin.PostAsync($"/api/usuarios/{usuarioId}/desativar", null);
        Assert.Equal(HttpStatusCode.OK, desativar.StatusCode);

        var transferir = await admin.PostAsJsonAsync(
            $"/api/aluno/{alunoId}/transferir-escola",
            new { escolaId = escolaB });
        Assert.Equal(HttpStatusCode.OK, transferir.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var trilha = scope.ServiceProvider.GetRequiredService<ITrilhaAcaoRepository>().Listar();

        Assert.Contains(trilha, acao => acao.Tipo == "VinculoCriado");
        Assert.Contains(trilha, acao => acao.Tipo == "PapelAlterado");
        Assert.Contains(trilha, acao => acao.Tipo == "UsuarioDesativado");
        Assert.Contains(trilha, acao => acao.Tipo == "AlunoTransferido");

        Assert.All(trilha, acao =>
        {
            Assert.True(acao.AutorId > 0);
            Assert.False(string.IsNullOrWhiteSpace(acao.AutorNome));
            Assert.Equal(DateTimeKind.Utc, acao.EmUtc.Kind);
            Assert.False(string.IsNullOrWhiteSpace(acao.AlvoTipo));
        });

        var papelAlterado = Assert.Single(
            trilha, acao => acao.Tipo == "PapelAlterado");
        Assert.Equal(nameof(PapelUsuario.Responsavel), papelAlterado.ValorAnterior);
        Assert.Equal(nameof(PapelUsuario.Merendeira), papelAlterado.ValorNovo);

        var transferencia = Assert.Single(
            trilha, acao => acao.Tipo == "AlunoTransferido");
        Assert.Equal(escolaA.ToString(), transferencia.ValorAnterior);
        Assert.Equal(escolaB.ToString(), transferencia.ValorNovo);
    }
}
