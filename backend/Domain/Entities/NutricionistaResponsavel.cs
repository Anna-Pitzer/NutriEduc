namespace Ntc.Domain.Entity;

/// <summary>
/// Retrato da nutricionista responsável no momento em que o cardápio foi criado.
///
/// Snapshot de propósito, sem FK para <see cref="Usuario"/> (ADR-009): editar o cadastro
/// da profissional depois não reescreve a autoria histórica do cardápio. O agregado
/// <see cref="Cardapio"/> valida e normaliza os campos ao receber este retrato.
/// </summary>
public sealed record NutricionistaResponsavel(
    int Id,
    string? Nome,
    string? Email,
    string? Cpf,
    string? Crn);
