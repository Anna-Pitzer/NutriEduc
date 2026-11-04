using Ntc.Domain;
using Ntc.Domain.Entity;

namespace Ntc.Application.Seguranca;

/// <summary>Resultado de uma decisão de acesso (Spec 028, T3).</summary>
public sealed record DecisaoDeAcesso(bool Permitido, ResultadoStatus Status)
{
    public static DecisaoDeAcesso Permitida { get; } = new(true, ResultadoStatus.Ok);

    public static DecisaoDeAcesso Negada(ResultadoStatus status) => new(false, status);
}

/// <summary>
/// Decide acesso a um recurso combinando a matriz (Spec 028) com a fronteira de escopo
/// (unidade → escola → vínculo). Função pura: recebe a sessão e o alvo já carregado.
///
/// Regra de borda (RF8): sem permissão, alvo de outra unidade é 404; alvo da mesma unidade
/// fora do escopo mais estreito é 403. Nenhum caminho revela existência entre unidades.
/// </summary>
public sealed class AutorizacaoService
{
    /// <summary>
    /// Avalia a permissão. O alvo é opcional; sem ele a decisão é apenas de matriz.
    /// </summary>
    public DecisaoDeAcesso Avaliar(
        Recurso recurso,
        EscopoUsuario escopo,
        OperacaoDeAcesso operacao,
        AlvoDeEscopo? alvo = null)
    {
        var nivel = MatrizDePermissoes.Nivel(recurso, escopo.Papel);
        var alvoAvaliado = alvo ?? AlvoDeEscopo.SemFronteira;

        if (!nivel.Permite(operacao))
        {
            return DecisaoDeAcesso.Negada(StatusDaNegacao(escopo, alvoAvaliado));
        }

        if (escopo.UnidadeId is { } unidadeDoEscopo &&
            alvoAvaliado.UnidadeId is { } unidadeDoAlvo &&
            unidadeDoEscopo != unidadeDoAlvo)
        {
            return DecisaoDeAcesso.Negada(ResultadoStatus.NaoEncontrado);
        }

        if (escopo.EscolaId is { } escolaDoEscopo &&
            alvoAvaliado.EscolaId is { } escolaDoAlvo &&
            escolaDoEscopo != escolaDoAlvo)
        {
            return DecisaoDeAcesso.Negada(ResultadoStatus.Proibido);
        }

        if (escopo.Papel == PapelUsuario.Responsavel &&
            !alvoAvaliado.VinculadoAoResponsavel)
        {
            return DecisaoDeAcesso.Negada(ResultadoStatus.Proibido);
        }

        return DecisaoDeAcesso.Permitida;
    }

    /// <summary>
    /// Atalho para chamadores que só precisam saber a negação: <c>null</c> significa
    /// permitido; qualquer outro valor é o status HTTP-específico a traduzir.
    /// </summary>
    public ResultadoStatus? Verificar(
        Recurso recurso,
        EscopoUsuario escopo,
        OperacaoDeAcesso operacao,
        AlvoDeEscopo? alvo = null)
    {
        var decisao = Avaliar(recurso, escopo, operacao, alvo);
        return decisao.Permitido ? null : decisao.Status;
    }

    private static ResultadoStatus StatusDaNegacao(
        EscopoUsuario escopo,
        AlvoDeEscopo alvo)
    {
        if (escopo.UnidadeId is { } unidadeDoEscopo &&
            alvo.UnidadeId is { } unidadeDoAlvo &&
            unidadeDoEscopo != unidadeDoAlvo)
        {
            return ResultadoStatus.NaoEncontrado;
        }

        return ResultadoStatus.Proibido;
    }
}
