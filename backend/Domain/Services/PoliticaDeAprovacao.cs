using Ntc.Domain.Entity;

namespace Ntc.Domain.Services;

/// <summary>
/// Plano de aprovação calculado pela <see cref="PoliticaDeAprovacao"/>: o papel efetivo,
/// a escola (apenas merendeira), o CRN normalizado (apenas nutricionista) e a exigência
/// de vínculo (apenas responsável). A unidade é sempre a da solicitação — a merendeira
/// deriva a unidade da escola no serviço, que conhece a referência institucional.
/// </summary>
public sealed record PlanoDeAprovacao(
    PapelUsuario Papel,
    int? EscolaId,
    string? Crn,
    bool ExigeAlunos);

/// <summary>Desfecho da política pura: plano ou lista de erros (Spec 033, RF8/RF13/RF14).</summary>
public sealed record ResultadoDePolitica(
    PlanoDeAprovacao? Plano,
    IReadOnlyList<string> Erros)
{
    public static ResultadoDePolitica Ok(PlanoDeAprovacao plano) =>
        new(plano, Array.Empty<string>());

    public static ResultadoDePolitica Falha(params string[] erros) =>
        new(null, erros);
}

/// <summary>
/// Função pura tipo → papel/escopo/CRN/vínculo (Spec 033, RF8; RNF1). Não consulta banco:
/// a existência e a atividade de escola, unidade e aluno são validadas pelo serviço; aqui
/// só se decide a forma válida do plano e se rejeita o que é proibido por tipo.
/// </summary>
public static class PoliticaDeAprovacao
{
    public const string MensagemPapelAdministrativo =
        "Nenhuma aprovação concede papel administrativo.";

    /// <summary>
    /// Valida o CRN pelo tipo solicitado, independente da aprovação (RF3). Devolve a
    /// mensagem de erro ou <c>null</c> quando o CRN é válido/ausente conforme o tipo.
    /// </summary>
    public static string? ErroDeCrn(TipoCadastroSolicitado tipo, string? crn)
    {
        if (tipo == TipoCadastroSolicitado.Nutricionista)
        {
            return CrnDeNutricionista.TryParse(crn, null, out _, out var erro)
                ? null
                : erro ?? CrnDeNutricionista.MensagemFormatoInvalido;
        }

        return string.IsNullOrWhiteSpace(crn)
            ? null
            : CrnDeNutricionista.MensagemSomenteNutricionista;
    }

    /// <summary>Papel correspondente ao tipo solicitado, sem correção (RF2/RF7).</summary>
    public static PapelUsuario PapelPadrao(TipoCadastroSolicitado tipo) => tipo switch
    {
        TipoCadastroSolicitado.Nutricionista => PapelUsuario.Nutricionista,
        TipoCadastroSolicitado.Merendeira => PapelUsuario.Merendeira,
        TipoCadastroSolicitado.Responsavel => PapelUsuario.Responsavel,
        _ => throw new ArgumentException("Tipo de cadastro inválido.")
    };

    public static ResultadoDePolitica Planejar(
        TipoCadastroSolicitado tipoSolicitado,
        PapelUsuario? papelConcedido,
        int? escolaId,
        string? crnSolicitado,
        IReadOnlyList<int>? alunosIds)
    {
        var erros = new List<string>();
        var papel = papelConcedido ?? PapelPadrao(tipoSolicitado);

        if (papel is PapelUsuario.AdminSecretaria or PapelUsuario.AdminEscola)
        {
            erros.Add(MensagemPapelAdministrativo);
        }

        if (papel is not (PapelUsuario.Nutricionista
            or PapelUsuario.Merendeira
            or PapelUsuario.Responsavel))
        {
            erros.Add("Papel concedido inválido.");
        }

        // CRN: obrigatório e válido para nutricionista; proibido nos demais (RF3/RF8).
        string? crn = null;

        if (papel == PapelUsuario.Nutricionista)
        {
            if (!CrnDeNutricionista.TryParse(crnSolicitado, null, out var valor, out var erroCrn))
            {
                erros.Add(erroCrn ?? CrnDeNutricionista.MensagemFormatoInvalido);
            }
            else
            {
                crn = valor!.Valor;
            }
        }
        else if (!string.IsNullOrWhiteSpace(crnSolicitado))
        {
            erros.Add(CrnDeNutricionista.MensagemSomenteNutricionista);
        }

        // Escola: obrigatória apenas para merendeira (RF8/RF17).
        if (papel == PapelUsuario.Merendeira)
        {
            if (escolaId is not > 0)
            {
                erros.Add("A aprovação de merendeira exige a escola.");
            }
        }
        else if (escolaId is not null)
        {
            erros.Add("Apenas merendeira recebe escola na aprovação.");
        }

        // Vínculo: responsável exige ao menos um aluno (RF9).
        var exigeAlunos = papel == PapelUsuario.Responsavel;

        if (exigeAlunos && (alunosIds is null || alunosIds.Count == 0))
        {
            erros.Add("A aprovação de responsável exige ao menos um aluno.");
        }

        return erros.Count > 0
            ? ResultadoDePolitica.Falha(erros.ToArray())
            : ResultadoDePolitica.Ok(new PlanoDeAprovacao(papel, escolaId, crn, exigeAlunos));
    }
}
