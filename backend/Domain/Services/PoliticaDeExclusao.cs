namespace Ntc.Domain.Services;

/// <summary>
/// Política de exclusão definitiva de cadastros (ADR-014, Spec 024 F7).
///
/// Funções puras: recebem apenas fatos já apurados e devolvem o impedimento, ou
/// <c>null</c> quando a exclusão é permitida. Não conhecem repositório, HTTP nem sessão —
/// quem pergunta "pode excluir?" e quem pergunta "quem pode excluir?" são camadas
/// diferentes, e esta é só a primeira.
/// </summary>
public static class PoliticaDeExclusao
{
    /// <summary>Escola é bloqueada por usuário ou por unidade legada que ainda a cita.</summary>
    public static string? ParaEscola(bool temUsuarios, bool temUnidadesLegadas)
    {
        if (temUsuarios)
        {
            return "Há usuários vinculados a esta escola. Exclua ou mova essas contas antes.";
        }

        if (temUnidadesLegadas)
        {
            return "Há unidades com esta escola como origem. Exclua essas unidades antes.";
        }

        return null;
    }

    /// <summary>
    /// Unidade é bloqueada por usuário, aluno, cardápio, token emitido ou consumo de
    /// bootstrap. O cardápio entrou na lista com a Spec 012: a FK é `Restrict`, então
    /// sem esta checagem a exclusão estouraria no banco como 500.
    /// </summary>
    public static string? ParaUnidade(
        bool temUsuarios,
        bool temAlunos,
        bool temTokenOuConsumo,
        bool temCardapios = false)
    {
        if (temUsuarios)
        {
            return "Há usuários vinculados a esta unidade. Mova ou exclua essas contas antes.";
        }

        if (temAlunos)
        {
            return "Há alunos vinculados a esta unidade. Mova ou exclua esses alunos antes.";
        }

        if (temCardapios)
        {
            return "Há cardápios vinculados a esta unidade. Exclua ou cancele esses cardápios antes.";
        }

        if (temTokenOuConsumo)
        {
            return "Esta unidade já participou do cadastro por token e não pode ser excluída.";
        }

        return null;
    }

    /// <summary>
    /// Usuário é bloqueado por exclusão da própria conta, por ser o último administrador da
    /// secretaria, por vínculo de responsável ou por token já emitido.
    /// </summary>
    public static string? ParaUsuario(
        int alvoId,
        bool alvoEhAdminSecretaria,
        int solicitanteId,
        int totalAdminSecretaria,
        bool temVinculos,
        bool temTokens)
    {
        if (alvoId == solicitanteId)
        {
            return "Não é possível excluir a própria conta.";
        }

        if (alvoEhAdminSecretaria && totalAdminSecretaria <= 1)
        {
            return "Não é possível excluir o último administrador da secretaria.";
        }

        if (temVinculos)
        {
            return "Este responsável ainda está vinculado a alunos. Remova os vínculos antes.";
        }

        if (temTokens)
        {
            return "Este usuário já emitiu token de cadastro e não pode ser excluído.";
        }

        return null;
    }
}
