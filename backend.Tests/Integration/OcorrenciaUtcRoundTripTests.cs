using Microsoft.Extensions.DependencyInjection;
using Ntc.Database;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Regressão determinística da Spec 016 (RF9): o cancelamento compara o dia civil em
/// America/Sao_Paulo. A ocorrência precisa sobreviver ao round-trip do SQLite preservando o
/// Kind Utc — a falha histórica (DateTimeKind.Unspecified) deslocava a data na fronteira
/// 00:00–03:00 UTC e quebrava o "mesmo dia".
/// </summary>
public class OcorrenciaUtcRoundTripTests
{
    [Fact]
    public void Cancelamento_NoLimiteDoFuso_UtcRoundTrip_NaoDeslocaDia()
    {
        using var fabrica = new ApiFactory();
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();

        // 01:00 UTC = 22:00 do dia anterior em America/Sao_Paulo (UTC-3).
        var criadoEmUtc = new DateTime(2026, 11, 11, 1, 0, 0, DateTimeKind.Utc);

        int ocorrenciaId;
        using (var gravacao = fabrica.Services.CreateScope())
        {
            var contexto = gravacao.ServiceProvider.GetRequiredService<ConnectionContext>();
            var uow = gravacao.ServiceProvider.GetRequiredService<Ntc.Domain.Interface.IUnidadeDeTrabalho>();

            var ocorrencia = new Ocorrencia(
                TipoOcorrencia.Outro,
                unidadeId,
                escolaId,
                1,
                "Autor Teste",
                "Descrição válida.",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                criadoEmUtc);

            uow.ExecutarEmTransacao(() => contexto.Ocorrencias.Add(ocorrencia));
            ocorrenciaId = ocorrencia.Id;
        }

        // Releitura em OUTRO contexto (simula a requisição seguinte): o Kind precisa continuar Utc.
        using (var releitura = fabrica.Services.CreateScope())
        {
            var contexto = releitura.ServiceProvider.GetRequiredService<ConnectionContext>();
            var ocorrencia = contexto.Ocorrencias.First(o => o.Id == ocorrenciaId);

            // 02:00 UTC = 23:00 do dia anterior em São Paulo → mesmo dia civil da criação.
            var cancelamentoUtc = new DateTime(2026, 11, 11, 2, 0, 0, DateTimeKind.Utc);

            ocorrencia.Cancelar("Autor Teste", cancelamentoUtc);

            Assert.Equal(StatusOcorrencia.Cancelada, ocorrencia.Status);
        }
    }
}
