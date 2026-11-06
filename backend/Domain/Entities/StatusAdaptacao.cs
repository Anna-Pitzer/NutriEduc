namespace Ntc.Domain.Entity;

/// <summary>
/// Estado de uma versão adaptada (Spec 029, RF3, RF7 e RF20).
///
/// <c>Atualizado</c> prova que a versão foi gerada a partir do geral vigente, sem item
/// bloqueante e dentro da tolerância nutricional. <c>Desatualizado</c> indica que o geral
/// mudou depois da adaptação. <c>PendenteDeRevisao</c> indica que a própria adaptação não
/// encontrou substituto seguro ou estourou a tolerância; nesse estado a versão não pode ser
/// publicada nem servida.
/// </summary>
public enum StatusAdaptacao
{
    Atualizado,
    Desatualizado,
    PendenteDeRevisao
}
