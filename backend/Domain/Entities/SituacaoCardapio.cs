namespace Ntc.Domain.Entity;

/// <summary>
/// Situação do cardápio (Spec 012, RF1). A Spec 015 acrescenta `Substituido` ao final —
/// alteração aditiva, sem migração de dados, porque o EF persiste enum como inteiro.
/// </summary>
public enum SituacaoCardapio
{
    Rascunho,
    Publicado,
    Cancelado,
    Substituido
}
