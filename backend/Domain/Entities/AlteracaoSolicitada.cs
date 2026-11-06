namespace Ntc.Domain.Entity;

/// <summary>
/// Alteração pedida para um campo cadastral (Spec 030, RF2). O valor no pedido é o snapshot
/// lido pelo servidor na criação; o cliente nunca o informa. É imutável.
/// </summary>
public sealed record AlteracaoSolicitada(
    CampoDeDadoDoAluno Campo,
    string ValorSolicitado,
    string ValorNoPedido);
