namespace Ntc.Domain.Entity;

/// <summary>
/// Campos cadastrais editáveis por solicitação (Spec 030, RF3). Enum fechado: CPF, escola,
/// conta e dados de restrição/alergia ficam fora de propósito.
/// </summary>
public enum CampoDeDadoDoAluno
{
    Nome,
    DataNascimento,
    NomeResponsavel,
    TelefoneResponsavel,
    Observacao
}
