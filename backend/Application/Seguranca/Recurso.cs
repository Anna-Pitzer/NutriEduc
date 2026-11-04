namespace Ntc.Application.Seguranca;

/// <summary>
/// Recursos da matriz de permissões (Spec 028, RF4). Enum fechado: recurso novo nasce sem
/// permissão até ser declarado aqui e em <see cref="MatrizDePermissoes"/> (RNF1).
/// </summary>
public enum Recurso
{
    Unidade,
    Escola,
    ContaUsuario,
    PapelDoUsuario,
    PerfilProprio,
    Vinculo,
    Aluno,
    RestricaoDoAluno,
    HistoricoDoAluno,
    CatalogoDeAlimentos,
    CatalogoDeRestricoes,
    CardapioGeral,
    CardapioAdaptado,
    ParametroDeGeracao,
    PublicacaoDeCardapio,
    Ocorrencia,
    Solicitacao,
    Feedback,
    Notificacao,
    Relatorio,
    Painel,
    Diagnostico,
    SolicitacaoDeAcesso
}
