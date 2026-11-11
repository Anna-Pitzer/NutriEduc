namespace Ntc.Domain.Entity;

/// <summary>
/// Tipo de cadastro solicitado no autocadastro público (Spec 033, RF1). Fechado em três
/// valores; papéis administrativos nunca são autossolicitáveis. O tipo registra a
/// intenção, não concede papel nem permissão.
/// </summary>
public enum TipoCadastroSolicitado
{
    Nutricionista = 0,
    Merendeira = 1,
    Responsavel = 2
}
