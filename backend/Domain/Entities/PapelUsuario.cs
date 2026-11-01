namespace Ntc.Domain.Entity;

/// <summary>
/// Papéis previstos pela Spec 009. A ordem numérica não é usada como fallback:
/// <see cref="Usuario.Papel"/> é anulável justamente para que "sem papel" não vire
/// <see cref="Nutricionista"/> (valor 0) por acidente de desserialização.
/// </summary>
public enum PapelUsuario
{
    Nutricionista = 0,
    Merendeira = 1,
    Responsavel = 2,
    AdminSecretaria = 3,
    AdminEscola = 4
}
