using Ntc.Domain.Entity;

namespace Ntc.Domain.Services;

/// <summary>
/// Predicado único de CRN para as operações de cardápio (Spec 031, RF9).
///
/// Existe para que as Specs 029 e 032 consumam a mesma mensagem e a mesma regra de
/// validade, sem reimplementá-las em cada serviço. O login não é afetado: uma nutricionista
/// legada sem CRN continua autenticando, apenas fica bloqueada de criar/gerar/adaptar/publicar.
/// </summary>
public static class PoliticaDeCrn
{
    public const string MensagemCardapioSemCrn =
        "A nutricionista responsável está sem CRN válido. Corrija o cadastro.";

    /// <summary>
    /// Devolve a mensagem de bloqueio quando o usuário exerce o papel de nutricionista e o
    /// CRN é nulo ou inválido; devolve <c>null</c> quando não há bloqueio.
    /// </summary>
    public static string? ExigirParaCardapio(Usuario usuario)
    {
        if (usuario.Papel != PapelUsuario.Nutricionista)
        {
            return null;
        }

        return CrnDeNutricionista.EhValido(usuario.Crn) ? null : MensagemCardapioSemCrn;
    }
}
