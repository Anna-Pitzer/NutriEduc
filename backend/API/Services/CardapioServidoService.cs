using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Resolve as versões visíveis para cada aluno (Spec 029, RF15; revisão do portal dos pais).
///
/// Aluno sem restrição recebe o geral publicado da unidade. Aluno com restrições recebe a
/// versão adaptada publicada de <c>(origem, escola, conjunto exato)</c>. Sem essa versão o
/// resultado é <c>Indisponivel</c> com alerta — o geral nunca é usado como versão servida e
/// item bloqueante nunca é servido. O geral publicado continua exposto em
/// <see cref="CardapioServidoDTO.CardapioGeral"/> para consulta do responsável.
/// </summary>
public sealed class CardapioServidoService
{
    private readonly ICardapioRepository _cardapios;
    private readonly IAlunoRepository _alunos;
    private readonly IEscolaRepository _escolas;
    private readonly IRestricaoRepository _restricoes;

    public CardapioServidoService(
        ICardapioRepository cardapios,
        IAlunoRepository alunos,
        IEscolaRepository escolas,
        IRestricaoRepository restricoes)
    {
        _cardapios = cardapios;
        _alunos = alunos;
        _escolas = escolas;
        _restricoes = restricoes;
    }

    public Resultado<CardapioServidoDTO> Obter(
        int alunoId,
        DateOnly? data,
        EscopoUsuario escopo)
    {
        var filtro = FiltroDoEscopo(escopo);

        if (filtro is null)
        {
            return Resultado<CardapioServidoDTO>.Proibido();
        }

        var aluno = _alunos.Obter(alunoId, filtro);

        if (aluno is null)
        {
            return _alunos.ObterPorId(alunoId) is null
                ? Resultado<CardapioServidoDTO>.NaoEncontrado("Aluno não encontrado.")
                : Resultado<CardapioServidoDTO>.Proibido();
        }

        var dia = data ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var porNome = RestricoesDoAluno.PorNomeNormalizado(
            _restricoes.Listar(categoria: null, incluirInativas: true));

        var resposta = new CardapioServidoDTO
        {
            AlunoId = aluno.Id,
            AlunoNome = aluno.Nome,
            EscolaId = aluno.EscolaId
        };

        if (aluno.EscolaId is not { } escolaId)
        {
            resposta.Motivo = "Aluno sem escola vinculada; a versão servida é indeterminada.";
            resposta.Alerta = true;
            return Resultado<CardapioServidoDTO>.Ok(resposta);
        }

        var escola = _escolas.GetEscola(escolaId);

        if (escola?.UnidadeId is not { } unidadeId)
        {
            resposta.Motivo = "Escola do aluno sem unidade vinculada.";
            resposta.Alerta = true;
            return Resultado<CardapioServidoDTO>.Ok(resposta);
        }

        if (RestricoesDoAluno.TemRestricaoSemCatalogo(aluno, porNome))
        {
            resposta.Motivo =
                "O aluno possui restrição sem cadastro no catálogo; a versão servida não pode ser determinada com segurança.";
            resposta.Alerta = true;
            return Resultado<CardapioServidoDTO>.Ok(resposta);
        }

        var conjunto = RestricoesDoAluno.Conjunto(aluno, porNome);
        resposta.ConjuntoRestricoesChave = conjunto.Chave;

        var geral = _cardapios.ObterGeralPublicado(unidadeId, dia);

        if (geral is null)
        {
            resposta.Motivo = "Não há cardápio geral publicado para a data.";
            resposta.Alerta = true;
            return Resultado<CardapioServidoDTO>.Ok(resposta);
        }

        resposta.RequerAdaptacao = !conjunto.EstaVazio;

        var detalheGeral = CardapioMapper.ParaDetalhe(geral);
        resposta.CardapioGeral = detalheGeral;

        if (conjunto.EstaVazio)
        {
            resposta.Disponivel = true;
            resposta.Cardapio = detalheGeral;
            return Resultado<CardapioServidoDTO>.Ok(resposta);
        }

        var adaptado = _cardapios.ObterAdaptadoPublicado(
            geral.Id,
            escolaId,
            conjunto.Chave,
            dia);

        if (adaptado is null)
        {
            resposta.Motivo =
                $"Não há versão adaptada publicada para a escola {escola!.Nome} e o conjunto {conjunto.Chave}.";
            resposta.Alerta = true;
            return Resultado<CardapioServidoDTO>.Ok(resposta);
        }

        var detalheAdaptado = CardapioMapper.ParaDetalhe(adaptado);
        detalheAdaptado.Restricoes = RestricoesDoAluno.DoAluno(aluno, porNome)
            .Select(restricao => new RestricaoDaVersaoDTO
            {
                Id = restricao.Id,
                Nome = restricao.Nome,
                Categoria = restricao.Categoria
            })
            .ToList();

        resposta.CardapioAdaptado = detalheAdaptado;
        resposta.Disponivel = true;
        resposta.Cardapio = detalheAdaptado;

        return Resultado<CardapioServidoDTO>.Ok(resposta);
    }

    /// <summary>
    /// Traduz o papel para o recorte de aluno. Responsável só alcança os próprios filhos;
    /// escola só a própria escola; unidade só a própria unidade. Qualquer escopo incompleto
    /// devolve <c>null</c> e o resultado é 403.
    /// </summary>
    private static FiltroAluno? FiltroDoEscopo(EscopoUsuario escopo) =>
        escopo.Papel switch
        {
            PapelUsuario.Responsavel => new FiltroAluno(ResponsavelUsuarioId: escopo.UsuarioId),
            PapelUsuario.AdminEscola or PapelUsuario.Merendeira =>
                escopo.EscolaId is { } escolaId ? new FiltroAluno(EscolaId: escolaId) : null,
            PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista =>
                escopo.UnidadeId is { } unidadeId ? new FiltroAluno(UnidadeId: unidadeId) : null,
            _ => null
        };
}
