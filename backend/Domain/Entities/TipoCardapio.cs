namespace Ntc.Domain.Entity;

/// <summary>
/// Tipo do cardápio (Spec 029, RF1). O <c>Geral</c> é a versão editorial da unidade e a base
/// de todas as adaptações; o <c>Adaptado</c> é a projeção de um geral para o conjunto de
/// restrições de uma escola (ADR-022). Nunca existe adaptado de adaptado.
/// </summary>
public enum TipoCardapio
{
    Geral,
    Adaptado
}
