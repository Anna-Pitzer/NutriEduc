namespace Ntc.Application;

/// <summary>
/// Nutricionista disponível para ser escolhida como responsável na geração de cardápio.
/// Expõe apenas o que identifica a profissional — nome, e-mail, CPF e CRN — e nunca dado
/// de autenticação.
/// </summary>
public sealed record NutricionistaDTO(
    int Id,
    string? Nome,
    string? Email,
    string? Cpf,
    string? Crn,
    bool CrnValido);
