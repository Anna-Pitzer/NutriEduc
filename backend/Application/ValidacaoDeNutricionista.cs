using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>
/// Regra única de autoria do cardápio (Spec 029, RF9 e RF17; Spec 031).
///
/// A nutricionista precisa estar ativa, exercer o papel, pertencer à unidade do cardápio e
/// ter CRN válido. Criar, adaptar e publicar usam a mesma validação; o login não é afetado
/// pela ausência de CRN.
/// </summary>
public static class ValidacaoDeNutricionista
{
    public static string? Motivo(Usuario? usuario, int unidadeId)
    {
        if (usuario is null)
        {
            return "Nutricionista responsável não encontrada.";
        }

        if (!usuario.Ativo)
        {
            return "A nutricionista responsável está inativa.";
        }

        if (usuario.Papel != PapelUsuario.Nutricionista)
        {
            return "O responsável informado não exerce o papel de nutricionista.";
        }

        if (usuario.UnidadeId != unidadeId)
        {
            return "A nutricionista responsável pertence a outra unidade.";
        }

        if (string.IsNullOrWhiteSpace(usuario.Crn))
        {
            return "A nutricionista responsável não possui CRN cadastrado. Atualize o cadastro antes de criar, adaptar ou publicar cardápio.";
        }

        return null;
    }

    public static NutricionistaResponsavel Retrato(Usuario usuario) =>
        new(usuario.Id, usuario.Nome, usuario.Email, usuario.Cpf, usuario.Crn);
}
