using Ntc.Domain.Entity;

namespace Ntc.Application.Seguranca;

/// <summary>
/// Matriz autoritativa de permissões (Spec 028, RF4; ADR-021). Função pura, sem IO:
/// <see cref="Nivel"/> é a fonte executável e <see cref="Documentada"/> é a transcrição
/// legível da tabela do requirements. Os dois conjuntos precisam coincidir exatamente —
/// o teste de conformidade falha em qualquer divergência (RNF4).
/// </summary>
public static class MatrizDePermissoes
{
    private const NivelDeAcesso N = NivelDeAcesso.Nenhum;
    private const NivelDeAcesso S = NivelDeAcesso.Solicitar;
    private const NivelDeAcesso R = NivelDeAcesso.Ler;
    private const NivelDeAcesso W = NivelDeAcesso.Escrita;
    private const NivelDeAcesso RC = NivelDeAcesso.LerCriar;
    private const NivelDeAcesso RW = NivelDeAcesso.LerEscrever;

    /// <summary>
    /// Tabela documentada do requirements, transcrita célula a célula. Serve de referência
    /// legível e de âncora do teste de concordância.
    /// </summary>
    public static IReadOnlyDictionary<(Recurso, PapelUsuario), NivelDeAcesso> Documentada
        { get; } = new Dictionary<(Recurso, PapelUsuario), NivelDeAcesso>
    {
        [(Recurso.Unidade, PapelUsuario.AdminSecretaria)] = RW,
        [(Recurso.Unidade, PapelUsuario.AdminEscola)] = R,
        [(Recurso.Unidade, PapelUsuario.Nutricionista)] = R,
        [(Recurso.Unidade, PapelUsuario.Merendeira)] = R,

        [(Recurso.Escola, PapelUsuario.AdminSecretaria)] = RW,
        [(Recurso.Escola, PapelUsuario.AdminEscola)] = R,
        [(Recurso.Escola, PapelUsuario.Nutricionista)] = R,
        [(Recurso.Escola, PapelUsuario.Merendeira)] = R,

        [(Recurso.ContaUsuario, PapelUsuario.AdminSecretaria)] = RW,
        [(Recurso.ContaUsuario, PapelUsuario.AdminEscola)] = R,

        [(Recurso.PapelDoUsuario, PapelUsuario.AdminSecretaria)] = W,

        [(Recurso.PerfilProprio, PapelUsuario.AdminSecretaria)] = RW,
        [(Recurso.PerfilProprio, PapelUsuario.AdminEscola)] = RW,
        [(Recurso.PerfilProprio, PapelUsuario.Nutricionista)] = RW,
        [(Recurso.PerfilProprio, PapelUsuario.Merendeira)] = RW,
        [(Recurso.PerfilProprio, PapelUsuario.Responsavel)] = RW,

        [(Recurso.Vinculo, PapelUsuario.AdminSecretaria)] = RW,
        [(Recurso.Vinculo, PapelUsuario.AdminEscola)] = R,
        [(Recurso.Vinculo, PapelUsuario.Responsavel)] = R,

        [(Recurso.Aluno, PapelUsuario.AdminSecretaria)] = RW,
        [(Recurso.Aluno, PapelUsuario.AdminEscola)] = R,
        [(Recurso.Aluno, PapelUsuario.Nutricionista)] = R,
        [(Recurso.Aluno, PapelUsuario.Merendeira)] = R,
        [(Recurso.Aluno, PapelUsuario.Responsavel)] = S,

        [(Recurso.RestricaoDoAluno, PapelUsuario.AdminSecretaria)] = RW,
        [(Recurso.RestricaoDoAluno, PapelUsuario.AdminEscola)] = R,
        [(Recurso.RestricaoDoAluno, PapelUsuario.Nutricionista)] = R,
        [(Recurso.RestricaoDoAluno, PapelUsuario.Merendeira)] = R,
        [(Recurso.RestricaoDoAluno, PapelUsuario.Responsavel)] = S,

        [(Recurso.HistoricoDoAluno, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.HistoricoDoAluno, PapelUsuario.AdminEscola)] = R,
        [(Recurso.HistoricoDoAluno, PapelUsuario.Nutricionista)] = R,
        [(Recurso.HistoricoDoAluno, PapelUsuario.Merendeira)] = R,
        [(Recurso.HistoricoDoAluno, PapelUsuario.Responsavel)] = R,

        [(Recurso.CatalogoDeAlimentos, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.CatalogoDeAlimentos, PapelUsuario.AdminEscola)] = R,
        [(Recurso.CatalogoDeAlimentos, PapelUsuario.Nutricionista)] = RW,
        [(Recurso.CatalogoDeAlimentos, PapelUsuario.Merendeira)] = R,

        [(Recurso.CatalogoDeRestricoes, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.CatalogoDeRestricoes, PapelUsuario.AdminEscola)] = R,
        [(Recurso.CatalogoDeRestricoes, PapelUsuario.Nutricionista)] = RW,
        [(Recurso.CatalogoDeRestricoes, PapelUsuario.Merendeira)] = R,
        [(Recurso.CatalogoDeRestricoes, PapelUsuario.Responsavel)] = R,

        [(Recurso.CardapioGeral, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.CardapioGeral, PapelUsuario.AdminEscola)] = R,
        [(Recurso.CardapioGeral, PapelUsuario.Nutricionista)] = RW,
        [(Recurso.CardapioGeral, PapelUsuario.Merendeira)] = R,
        [(Recurso.CardapioGeral, PapelUsuario.Responsavel)] = R,

        [(Recurso.CardapioAdaptado, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.CardapioAdaptado, PapelUsuario.AdminEscola)] = R,
        [(Recurso.CardapioAdaptado, PapelUsuario.Nutricionista)] = RW,
        [(Recurso.CardapioAdaptado, PapelUsuario.Merendeira)] = R,
        [(Recurso.CardapioAdaptado, PapelUsuario.Responsavel)] = R,

        [(Recurso.ParametroDeGeracao, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.ParametroDeGeracao, PapelUsuario.Nutricionista)] = RW,

        [(Recurso.PublicacaoDeCardapio, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.PublicacaoDeCardapio, PapelUsuario.AdminEscola)] = R,
        [(Recurso.PublicacaoDeCardapio, PapelUsuario.Nutricionista)] = RW,
        [(Recurso.PublicacaoDeCardapio, PapelUsuario.Merendeira)] = R,

        [(Recurso.Ocorrencia, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.Ocorrencia, PapelUsuario.AdminEscola)] = RW,
        [(Recurso.Ocorrencia, PapelUsuario.Nutricionista)] = RW,
        [(Recurso.Ocorrencia, PapelUsuario.Merendeira)] = RC,
        [(Recurso.Ocorrencia, PapelUsuario.Responsavel)] = RC,

        [(Recurso.Solicitacao, PapelUsuario.AdminSecretaria)] = RW,
        [(Recurso.Solicitacao, PapelUsuario.AdminEscola)] = RC,
        [(Recurso.Solicitacao, PapelUsuario.Nutricionista)] = RW,
        [(Recurso.Solicitacao, PapelUsuario.Merendeira)] = RC,
        [(Recurso.Solicitacao, PapelUsuario.Responsavel)] = RC,

        [(Recurso.Feedback, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.Feedback, PapelUsuario.AdminEscola)] = RW,
        [(Recurso.Feedback, PapelUsuario.Nutricionista)] = RW,
        [(Recurso.Feedback, PapelUsuario.Merendeira)] = RC,
        [(Recurso.Feedback, PapelUsuario.Responsavel)] = RC,

        [(Recurso.Notificacao, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.Notificacao, PapelUsuario.AdminEscola)] = R,
        [(Recurso.Notificacao, PapelUsuario.Nutricionista)] = R,
        [(Recurso.Notificacao, PapelUsuario.Merendeira)] = R,
        [(Recurso.Notificacao, PapelUsuario.Responsavel)] = R,

        [(Recurso.Relatorio, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.Relatorio, PapelUsuario.AdminEscola)] = R,
        [(Recurso.Relatorio, PapelUsuario.Nutricionista)] = R,

        [(Recurso.Painel, PapelUsuario.AdminSecretaria)] = R,
        [(Recurso.Painel, PapelUsuario.AdminEscola)] = R,
        [(Recurso.Painel, PapelUsuario.Nutricionista)] = R,
        [(Recurso.Painel, PapelUsuario.Merendeira)] = R,
        [(Recurso.Painel, PapelUsuario.Responsavel)] = R,

        [(Recurso.Diagnostico, PapelUsuario.AdminSecretaria)] = R,

        [(Recurso.SolicitacaoDeAcesso, PapelUsuario.AdminSecretaria)] = RW
    };

    /// <summary>
    /// Fonte executável. Negar é o padrão: qualquer par não declarado devolve
    /// <see cref="NivelDeAcesso.Nenhum"/> (RNF1).
    /// </summary>
    public static NivelDeAcesso Nivel(Recurso recurso, PapelUsuario papel) =>
        (recurso, papel) switch
        {
            (Recurso.Unidade, PapelUsuario.AdminSecretaria) => RW,
            (Recurso.Unidade, PapelUsuario.AdminEscola) => R,
            (Recurso.Unidade, PapelUsuario.Nutricionista) => R,
            (Recurso.Unidade, PapelUsuario.Merendeira) => R,

            (Recurso.Escola, PapelUsuario.AdminSecretaria) => RW,
            (Recurso.Escola, PapelUsuario.AdminEscola) => R,
            (Recurso.Escola, PapelUsuario.Nutricionista) => R,
            (Recurso.Escola, PapelUsuario.Merendeira) => R,

            (Recurso.ContaUsuario, PapelUsuario.AdminSecretaria) => RW,
            (Recurso.ContaUsuario, PapelUsuario.AdminEscola) => R,

            (Recurso.PapelDoUsuario, PapelUsuario.AdminSecretaria) => W,

            (Recurso.PerfilProprio, PapelUsuario.AdminSecretaria) => RW,
            (Recurso.PerfilProprio, PapelUsuario.AdminEscola) => RW,
            (Recurso.PerfilProprio, PapelUsuario.Nutricionista) => RW,
            (Recurso.PerfilProprio, PapelUsuario.Merendeira) => RW,
            (Recurso.PerfilProprio, PapelUsuario.Responsavel) => RW,

            (Recurso.Vinculo, PapelUsuario.AdminSecretaria) => RW,
            (Recurso.Vinculo, PapelUsuario.AdminEscola) => R,
            (Recurso.Vinculo, PapelUsuario.Responsavel) => R,

            (Recurso.Aluno, PapelUsuario.AdminSecretaria) => RW,
            (Recurso.Aluno, PapelUsuario.AdminEscola) => R,
            (Recurso.Aluno, PapelUsuario.Nutricionista) => R,
            (Recurso.Aluno, PapelUsuario.Merendeira) => R,
            (Recurso.Aluno, PapelUsuario.Responsavel) => S,

            (Recurso.RestricaoDoAluno, PapelUsuario.AdminSecretaria) => RW,
            (Recurso.RestricaoDoAluno, PapelUsuario.AdminEscola) => R,
            (Recurso.RestricaoDoAluno, PapelUsuario.Nutricionista) => R,
            (Recurso.RestricaoDoAluno, PapelUsuario.Merendeira) => R,
            (Recurso.RestricaoDoAluno, PapelUsuario.Responsavel) => S,

            (Recurso.HistoricoDoAluno, PapelUsuario.AdminSecretaria) => R,
            (Recurso.HistoricoDoAluno, PapelUsuario.AdminEscola) => R,
            (Recurso.HistoricoDoAluno, PapelUsuario.Nutricionista) => R,
            (Recurso.HistoricoDoAluno, PapelUsuario.Merendeira) => R,
            (Recurso.HistoricoDoAluno, PapelUsuario.Responsavel) => R,

            (Recurso.CatalogoDeAlimentos, PapelUsuario.AdminSecretaria) => R,
            (Recurso.CatalogoDeAlimentos, PapelUsuario.AdminEscola) => R,
            (Recurso.CatalogoDeAlimentos, PapelUsuario.Nutricionista) => RW,
            (Recurso.CatalogoDeAlimentos, PapelUsuario.Merendeira) => R,

            (Recurso.CatalogoDeRestricoes, PapelUsuario.AdminSecretaria) => R,
            (Recurso.CatalogoDeRestricoes, PapelUsuario.AdminEscola) => R,
            (Recurso.CatalogoDeRestricoes, PapelUsuario.Nutricionista) => RW,
            (Recurso.CatalogoDeRestricoes, PapelUsuario.Merendeira) => R,
            (Recurso.CatalogoDeRestricoes, PapelUsuario.Responsavel) => R,

            (Recurso.CardapioGeral, PapelUsuario.AdminSecretaria) => R,
            (Recurso.CardapioGeral, PapelUsuario.AdminEscola) => R,
            (Recurso.CardapioGeral, PapelUsuario.Nutricionista) => RW,
            (Recurso.CardapioGeral, PapelUsuario.Merendeira) => R,
            (Recurso.CardapioGeral, PapelUsuario.Responsavel) => R,

            (Recurso.CardapioAdaptado, PapelUsuario.AdminSecretaria) => R,
            (Recurso.CardapioAdaptado, PapelUsuario.AdminEscola) => R,
            (Recurso.CardapioAdaptado, PapelUsuario.Nutricionista) => RW,
            (Recurso.CardapioAdaptado, PapelUsuario.Merendeira) => R,
            (Recurso.CardapioAdaptado, PapelUsuario.Responsavel) => R,

            (Recurso.ParametroDeGeracao, PapelUsuario.AdminSecretaria) => R,
            (Recurso.ParametroDeGeracao, PapelUsuario.Nutricionista) => RW,

            (Recurso.PublicacaoDeCardapio, PapelUsuario.AdminSecretaria) => R,
            (Recurso.PublicacaoDeCardapio, PapelUsuario.AdminEscola) => R,
            (Recurso.PublicacaoDeCardapio, PapelUsuario.Nutricionista) => RW,
            (Recurso.PublicacaoDeCardapio, PapelUsuario.Merendeira) => R,

            (Recurso.Ocorrencia, PapelUsuario.AdminSecretaria) => R,
            (Recurso.Ocorrencia, PapelUsuario.AdminEscola) => RW,
            (Recurso.Ocorrencia, PapelUsuario.Nutricionista) => RW,
            (Recurso.Ocorrencia, PapelUsuario.Merendeira) => RC,
            (Recurso.Ocorrencia, PapelUsuario.Responsavel) => RC,

            (Recurso.Solicitacao, PapelUsuario.AdminSecretaria) => RW,
            (Recurso.Solicitacao, PapelUsuario.AdminEscola) => RC,
            (Recurso.Solicitacao, PapelUsuario.Nutricionista) => RW,
            (Recurso.Solicitacao, PapelUsuario.Merendeira) => RC,
            (Recurso.Solicitacao, PapelUsuario.Responsavel) => RC,

            (Recurso.Feedback, PapelUsuario.AdminSecretaria) => R,
            (Recurso.Feedback, PapelUsuario.AdminEscola) => RW,
            (Recurso.Feedback, PapelUsuario.Nutricionista) => RW,
            (Recurso.Feedback, PapelUsuario.Merendeira) => RC,
            (Recurso.Feedback, PapelUsuario.Responsavel) => RC,

            (Recurso.Notificacao, PapelUsuario.AdminSecretaria) => R,
            (Recurso.Notificacao, PapelUsuario.AdminEscola) => R,
            (Recurso.Notificacao, PapelUsuario.Nutricionista) => R,
            (Recurso.Notificacao, PapelUsuario.Merendeira) => R,
            (Recurso.Notificacao, PapelUsuario.Responsavel) => R,

            (Recurso.Relatorio, PapelUsuario.AdminSecretaria) => R,
            (Recurso.Relatorio, PapelUsuario.AdminEscola) => R,
            (Recurso.Relatorio, PapelUsuario.Nutricionista) => R,

            (Recurso.Painel, PapelUsuario.AdminSecretaria) => R,
            (Recurso.Painel, PapelUsuario.AdminEscola) => R,
            (Recurso.Painel, PapelUsuario.Nutricionista) => R,
            (Recurso.Painel, PapelUsuario.Merendeira) => R,
            (Recurso.Painel, PapelUsuario.Responsavel) => R,

            (Recurso.Diagnostico, PapelUsuario.AdminSecretaria) => R,

            (Recurso.SolicitacaoDeAcesso, PapelUsuario.AdminSecretaria) => RW,

            _ => N
        };
}
