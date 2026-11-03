using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso da publicação, versionamento e histórico (Spec 015). O serviço orquestra o
/// fluxo de oito passos e delega pré-condições e transições ao agregado <see cref="Cardapio"/>
/// (RNF1). Nenhuma regra de transição vive aqui.
/// </summary>
public sealed class PublicacaoCardapioService
{
    private readonly ICardapioRepository _cardapios;
    private readonly IAlimentoRepository _alimentos;
    private readonly IAlunoRepository _alunos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public PublicacaoCardapioService(
        ICardapioRepository cardapios,
        IAlimentoRepository alimentos,
        IAlunoRepository alunos,
        IUsuarioRepository usuarios,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _cardapios = cardapios;
        _alimentos = alimentos;
        _alunos = alunos;
        _usuarios = usuarios;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    public Resultado<CardapioDTO> Publicar(int id, EscopoUsuario escopo)
    {
        var cardapio = _cardapios.ObterComRefeicoes(id);

        if (cardapio is null)
            return Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.");

        // Pré-condição de situação antes de qualquer escrita (Spec 015, fluxo passo 3).
        if (cardapio.Situacao != SituacaoCardapio.Rascunho)
            return Resultado<CardapioDTO>.Conflito("Somente rascunho pode ser publicado.");

        // Compatibilidade (passo 4): bloqueante impede, alerta não (Spec 014/ADR-011).
        var bloqueantes = CalcularBloqueantes(cardapio);

        if (bloqueantes.Count > 0)
            throw new PublicacaoBloqueadaException(bloqueantes);

        // Sobreposição de publicado (passo 5); a revisão ignora o original que será substituído.
        var conflitante = _cardapios.ExistePublicadoNoPeriodo(
            cardapio.UnidadeId, cardapio.Periodo, cardapio.RevisaoDeId);

        if (conflitante is { } idConflitante)
            throw new PublicacaoSobrepostaException(idConflitante);

        var autor = NomeDoAutor(escopo);
        var utc = DateTime.UtcNow;

        Cardapio? original = null;

        if (cardapio.RevisaoDeId is { } revisaoDeId)
        {
            original = _cardapios.ObterComRefeicoes(revisaoDeId);

            if (original is null)
                return Resultado<CardapioDTO>.NaoEncontrado("Cardápio original da revisão não encontrado.");
        }

        // Passos 6 a 8: substituir original (se revisão), publicar e persistir na mesma transação.
        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            original?.MarcarSubstituido(cardapio.Id, autor, utc);
            cardapio.Publicar(autor, utc);
            _cardapios.Update(cardapio);
        });

        return Carregar(cardapio.Id);
    }

    public Resultado<CardapioDTO> Cancelar(int id, CancelarCardapioDTO dto, EscopoUsuario escopo)
    {
        var cardapio = _cardapios.ObterComRefeicoes(id);

        if (cardapio is null)
            return Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.");

        cardapio.Cancelar(dto.Motivo, NomeDoAutor(escopo), DateTime.UtcNow);

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _cardapios.Update(cardapio));

        return Carregar(cardapio.Id);
    }

    public Resultado<CardapioDTO> CriarRevisao(int id, EscopoUsuario escopo)
    {
        var cardapio = _cardapios.ObterComRefeicoes(id);

        if (cardapio is null)
            return Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.");

        var revisao = cardapio.CriarRevisao(NomeDoAutor(escopo), DateTime.UtcNow);

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _cardapios.Add(revisao));

        return Carregar(revisao.Id);
    }

    public Resultado<PaginaDTO<HistoricoCardapioDTO>> ListarHistorico(
        int id,
        int? pagina,
        int? tamanho,
        EscopoUsuario escopo)
    {
        if (!PodeLer(escopo))
            return Resultado<PaginaDTO<HistoricoCardapioDTO>>.Proibido();

        var cardapio = _cardapios.ObterComRefeicoes(id);

        if (cardapio is null)
            return Resultado<PaginaDTO<HistoricoCardapioDTO>>.NaoEncontrado("Cardápio não encontrado.");

        if (escopo.Papel == PapelUsuario.Merendeira && escopo.UnidadeId != cardapio.UnidadeId)
            return Resultado<PaginaDTO<HistoricoCardapioDTO>>.Proibido();

        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);
        var (itens, total) = _cardapios.ListarHistorico(id, paginaEfetiva, tamanhoEfetivo);

        return Resultado<PaginaDTO<HistoricoCardapioDTO>>.Ok(new PaginaDTO<HistoricoCardapioDTO>
        {
            Pagina = paginaEfetiva,
            Tamanho = tamanhoEfetivo,
            Total = total,
            Itens = itens.Select(ParaHistorico).ToList()
        });
    }

    public Resultado<PaginaDTO<AlteracaoCardapioDTO>> ListarAlteracoes(
        DateTime desde,
        int? pagina,
        int? tamanho,
        EscopoUsuario escopo)
    {
        if (!PodeLer(escopo))
            return Resultado<PaginaDTO<AlteracaoCardapioDTO>>.Proibido();

        int? unidadeId = null;

        if (escopo.Papel == PapelUsuario.Merendeira)
        {
            if (escopo.UnidadeId is null)
                return Resultado<PaginaDTO<AlteracaoCardapioDTO>>.Proibido();

            unidadeId = escopo.UnidadeId;
        }

        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);
        var (itens, total) = _cardapios.ListarAlteracoes(unidadeId, desde, paginaEfetiva, tamanhoEfetivo);

        return Resultado<PaginaDTO<AlteracaoCardapioDTO>>.Ok(new PaginaDTO<AlteracaoCardapioDTO>
        {
            Pagina = paginaEfetiva,
            Tamanho = tamanhoEfetivo,
            Total = total,
            Itens = itens.Select(ParaAlteracao).ToList()
        });
    }

    private List<IncompatibilidadeDetalhe> CalcularBloqueantes(Cardapio cardapio)
    {
        var itens = cardapio.Refeicoes
            .OrderBy(refeicao => refeicao.Ordem)
            .SelectMany(refeicao => refeicao.Itens.Select(item => new ItemAnalisado(
                refeicao.Ordem,
                refeicao.Nome,
                item.AlimentoId,
                item.Alimento?.Nome ?? string.Empty)))
            .ToList();

        var ids = itens.Select(item => item.AlimentoId).Distinct().ToList();

        var vinculosPorAlimento = _alimentos.ObterComVinculosPorIds(ids).ToDictionary(
            alimento => alimento.Id,
            alimento => (IReadOnlyList<VinculoAnalisado>)alimento.Vinculos
                .Where(vinculo => vinculo.Restricao is not null)
                .Select(vinculo => new VinculoAnalisado(
                    vinculo.RestricaoId,
                    vinculo.Restricao!.Nome,
                    vinculo.Restricao.Categoria))
                .ToList());

        var alunos = _alunos.Listar(new FiltroAluno(UnidadeId: cardapio.UnidadeId))
            .Select(aluno => new AlunoAnalisado(aluno.Id, aluno.Nome, aluno.Restricoes))
            .ToList();

        var resultado = AnaliseCompatibilidade.Analisar(itens, vinculosPorAlimento, alunos);

        return resultado.Bloqueantes
            .Select(achado => new IncompatibilidadeDetalhe(
                achado.AlimentoNome,
                achado.RestricaoNome,
                achado.RefeicaoNome))
            .ToList();
    }

    private string NomeDoAutor(EscopoUsuario escopo) =>
        _usuarios.GetById(escopo.UsuarioId)?.Nome ?? "sistema";

    private static bool PodeLer(EscopoUsuario escopo) =>
        escopo.Papel is PapelUsuario.Nutricionista or PapelUsuario.Merendeira;

    private Resultado<CardapioDTO> Carregar(int id)
    {
        var cardapio = _cardapios.ObterComRefeicoes(id);

        return cardapio is null
            ? Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.")
            : Resultado<CardapioDTO>.Ok(CardapioMapper.ParaDetalhe(cardapio));
    }

    private static HistoricoCardapioDTO ParaHistorico(HistoricoCardapio historico) => new()
    {
        Evento = historico.Evento,
        Versao = historico.Versao,
        Autor = historico.AutorNome,
        Motivo = historico.Motivo,
        CriadoEm = historico.CriadoEmUtc
    };

    private static AlteracaoCardapioDTO ParaAlteracao(AlteracaoCardapioItem item) => new()
    {
        Id = item.Id,
        CardapioId = item.CardapioId,
        CardapioNome = item.CardapioNome,
        UnidadeNome = item.UnidadeNome,
        Evento = item.Evento,
        Versao = item.Versao,
        Autor = item.AutorNome,
        Motivo = item.Motivo,
        CriadoEm = item.CriadoEmUtc
    };
}
