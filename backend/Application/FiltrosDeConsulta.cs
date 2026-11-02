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
    /// <summary>Recorte de alunos permitido ao solicitante, sempre aplicado no banco.</summary>
    public static FiltroAluno ParaAluno(EscopoUsuario escopo)
    {
        return escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista => FiltroAluno.Todos,
            PapelUsuario.AdminEscola => escopo.EscolaId is { } escolaId
                ? new FiltroAluno(EscolaId: escolaId)
                : FiltroAluno.Nenhum,
            PapelUsuario.Merendeira => escopo.UnidadeId is { } unidadeId
                ? new FiltroAluno(UnidadeId: unidadeId)
                : FiltroAluno.Nenhum,
            PapelUsuario.Responsavel => new FiltroAluno(ResponsavelUsuarioId: escopo.UsuarioId),
            _ => FiltroAluno.Nenhum
        };
    }

    /// <summary>
    /// Filtro de usuários com o recorte do solicitante já incorporado: escola para
    /// <c>AdminEscola</c>, sem restrição para os papéis globais.
    /// </summary>
    public static FiltroUsuarios ParaUsuario(FiltroUsuarios filtro, EscopoUsuario escopo)
    {
        if (escopo.Global)
        {
            return filtro;
        }

        // Não global só administra a própria escola; o recorte vence um filtro divergente.
        // Sem escola definida, nenhum usuário é visível (-1 não casa com id válido).
        return filtro with { EscolaId = escopo.EscolaId ?? -1 };
    }
}
