namespace Ntc.Domain.Entity;

/// <summary>
/// Tipo da solicitação (Spec 017, RF1). O valor <c>DadosCadastrais</c> existe desde já
/// porque o agregado é dono do fluxo; o payload e a aplicação desse tipo pertencem à
/// Spec 030.
/// </summary>
public enum TipoSolicitacao
{
    MudancaCardapio,
    Ingrediente,
    FaltaIngrediente,
    InclusaoAlergia,
    AlteracaoRestricao,
    DadosCadastrais
}
