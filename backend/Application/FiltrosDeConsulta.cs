using Ntc.Domain;
using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>
/// Tradução de escopo de sessão para filtros de negócio (Spec 024, F5).
///
/// É o único ponto que conhece a regra papel → recorte de dados. Os portos de repositório
/// recebem <see cref="FiltroAluno"/> e <see cref="FiltroUsuarios"/>, que não têm nenhuma
/// palavra de autorização — se um papel novo aparecer, muda-se só este arquivo.
/// </summary>
public static class FiltrosDeConsulta
{
    /// <summary>
    /// Recorte de alunos permitido ao solicitante, sempre aplicado no banco. Os filtros
    /// opcionais da requisição apenas estreitam o escopo — nunca ampliam (Spec 027, RF13).
    /// </summary>
    public static FiltroAluno ParaAluno(
        EscopoUsuario escopo,
        int? escolaId = null,
        int? unidadeId = null,
        string? busca = null,
        string? serie = null)
    {
        var filtro = escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista =>
                escopo.UnidadeId is { } unidadeDoEscopo
                    ? new FiltroAluno(UnidadeId: unidadeDoEscopo)
                    : FiltroAluno.Nenhum,
            PapelUsuario.AdminEscola or PapelUsuario.Merendeira =>
                escopo.EscolaId is { } escolaDoEscopo
                    ? new FiltroAluno(EscolaId: escolaDoEscopo)
                    : FiltroAluno.Nenhum,
            PapelUsuario.Responsavel => new FiltroAluno(ResponsavelUsuarioId: escopo.UsuarioId),
            _ => FiltroAluno.Nenhum
        };

        return Restringir(filtro, escolaId, unidadeId) with
        {
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(),
            Serie = string.IsNullOrWhiteSpace(serie) ? null : serie.Trim()
        };
    }

    /// <summary>
    /// Aplica os filtros da requisição como restrições adicionais. Pedido que contradiz o
    /// escopo devolve <see cref="FiltroAluno.Nenhum"/> em vez de ampliar o alcance.
    /// </summary>
    private static FiltroAluno Restringir(
        FiltroAluno filtro,
        int? escolaId,
        int? unidadeId)
    {
        if (unidadeId is > 0)
        {
            if (filtro.UnidadeId is { } unidadeFixa && unidadeFixa != unidadeId)
            {
                return FiltroAluno.Nenhum;
            }

            filtro = filtro with { UnidadeId = unidadeId };
        }

        if (escolaId is > 0)
        {
            if (filtro.EscolaId is { } escolaFixa && escolaFixa != escolaId)
            {
                return FiltroAluno.Nenhum;
            }

            filtro = filtro with { EscolaId = escolaId };
        }

        return filtro;
    }

    /// <summary>
    /// Filtro de usuários com o recorte do solicitante já incorporado (Spec 027/028):
    /// unidade para ADM/nutrição, escola para AdminEscola/Merendeira.
    /// </summary>
    public static FiltroUsuarios ParaUsuario(FiltroUsuarios filtro, EscopoUsuario escopo)
    {
        if (escopo.Papel is PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista)
        {
            return escopo.UnidadeId is { } unidadeId
                ? filtro with { UnidadeId = unidadeId }
                : filtro with { UnidadeId = -1 };
        }

        if (escopo.Papel is PapelUsuario.AdminEscola or PapelUsuario.Merendeira)
        {
            return filtro with { EscolaId = escopo.EscolaId ?? -1 };
        }

        return filtro with { UnidadeId = -1 };
    }
}
