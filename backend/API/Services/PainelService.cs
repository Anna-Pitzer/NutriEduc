using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso do painel por papel (Spec 025). Concentra acesso, escopo, período e a
/// montagem das quatro leituras; as consultas agregadas ficam em <see cref="IPainelRepository"/>
/// e a regra de compatibilidade continua sendo a única da Spec 014 (ADR-011, D6).
/// Nenhum método grava (RNF2).
/// </summary>
public sealed class PainelService
{
    /// <summary>Janela do indicador de cardápios publicados (RF1).</summary>
    public const int DiasDeCardapioPublicado = 7;

    /// <summary>Período padrão do relatório do filho (RF6).</summary>
    public const int DiasDeRelatorioPadrao = 30;

    /// <summary>Intervalo máximo aceito no relatório do filho (RF6).</summary>
    public const int DiasDeRelatorioMaximo = 366;

    private const string SemUnidade =
        "Sua sessão não está vinculada a uma unidade; o painel não pode ser montado.";

    private const string FilhoComRestricao =
        "Aluno com restrição: o cardápio do dia é a versão servida da área Cardápios.";

    private readonly IPainelRepository _painel;
    private readonly IAlunoRepository _alunos;
    private readonly ICardapioRepository _cardapios;
    private readonly IRestricaoRepository _restricoes;
    private readonly IVinculoRepository _vinculos;
    private readonly IAlimentoRepository _alimentos;
    private readonly IEscolaRepository _escolas;
    private readonly TimeProvider _relogio;

    public PainelService(
        IPainelRepository painel,
        IAlunoRepository alunos,
        ICardapioRepository cardapios,
        IRestricaoRepository restricoes,
        IVinculoRepository vinculos,
        IAlimentoRepository alimentos,
        IEscolaRepository escolas,
        TimeProvider relogio)
    {
        _painel = painel;
        _alunos = alunos;
        _cardapios = cardapios;
        _restricoes = restricoes;
        _vinculos = vinculos;
        _alimentos = alimentos;
        _escolas = escolas;
        _relogio = relogio;
    }

    // ---------- Secretaria (RF1, RF2) ----------

    public Resultado<PainelSecretariaDTO> Secretaria(EscopoUsuario escopo)
    {
        if (escopo.Papel != PapelUsuario.AdminSecretaria)
        {
            return Resultado<PainelSecretariaDTO>.Proibido();
        }

        if (escopo.UnidadeId is not { } unidadeId)
        {
            return Resultado<PainelSecretariaDTO>.Proibido(SemUnidade);
        }

        var agora = _relogio.GetUtcNow().UtcDateTime;
        var indicadores = _painel.ContarIndicadores(
            unidadeId,
            agora.AddDays(-DiasDeCardapioPublicado));

        return Resultado<PainelSecretariaDTO>.Ok(new PainelSecretariaDTO
        {
            Unidade = NomeDaUnidade(unidadeId),
            Alunos = indicadores.Alunos,
            AlunosPorEscola = indicadores.AlunosPorEscola
                .Select(item => new AlunosPorEscolaDTO
                {
                    EscolaId = item.EscolaId,
                    Escola = item.Escola,
                    Total = item.Total
                })
                .ToList(),
            UsuariosAtivosPorPapel = indicadores.UsuariosAtivosPorPapel
                .Select(item => new UsuariosAtivosPorPapelDTO
                {
                    Papel = item.Papel,
                    Total = item.Total
                })
                .ToList(),
            CardapiosPublicadosUltimosSeteDias = indicadores.CardapiosPublicados,
            OcorrenciasAbertas = indicadores.OcorrenciasAbertas,
            SolicitacoesPendentes = indicadores.SolicitacoesPendentes
        });
    }

    // ---------- Merendeira (RF3, RF4) ----------

    public Resultado<PainelMerendeiraDTO> Merendeira(DateOnly? data, EscopoUsuario escopo)
    {
        if (escopo.Papel != PapelUsuario.Merendeira)
        {
            return Resultado<PainelMerendeiraDTO>.Proibido();
        }

        if (escopo.UnidadeId is not { } unidadeId)
        {
            return Resultado<PainelMerendeiraDTO>.Proibido(SemUnidade);
        }

        var dia = data ?? Hoje();
        var resposta = new PainelMerendeiraDTO
        {
            Data = dia,
            Unidade = NomeDaUnidade(unidadeId)
        };

        var cardapio = _cardapios.ObterGeralPublicado(unidadeId, dia);

        if (cardapio is null)
        {
            resposta.Motivo = "Não há cardápio publicado para a data.";
            return Resultado<PainelMerendeiraDTO>.Ok(resposta);
        }

        var contagens = ContarAlunosComRestricao(cardapio, unidadeId);
        resposta.Cardapio = MontarCardapio(cardapio, contagens);

        return Resultado<PainelMerendeiraDTO>.Ok(resposta);
    }

    // ---------- Família (RF5) ----------

    public Resultado<PainelFamiliaDTO> Familia(DateOnly? data, EscopoUsuario escopo)
    {
        if (escopo.Papel != PapelUsuario.Responsavel)
        {
            return Resultado<PainelFamiliaDTO>.Proibido();
        }

        var dia = data ?? Hoje();
        var catalogo = CatalogoDeRestricoes();

        var filhos = _alunos
            .Listar(new FiltroAluno(ResponsavelUsuarioId: escopo.UsuarioId))
            .OrderBy(aluno => aluno.Nome, StringComparer.Ordinal)
            .Select(aluno => MontarFilho(aluno, dia, catalogo))
            .ToList();

        return Resultado<PainelFamiliaDTO>.Ok(new PainelFamiliaDTO
        {
            Data = dia,
            Filhos = filhos
        });
    }

    private FilhoDoPainelDTO MontarFilho(
        Aluno aluno,
        DateOnly dia,
        IReadOnlyDictionary<string, RestricaoAlimentar> catalogo)
    {
        var filho = new FilhoDoPainelDTO
        {
            Id = aluno.Id,
            Nome = aluno.Nome,
            Escola = NomeDaEscola(aluno.EscolaId),
            Unidade = aluno.UnidadeId is { } unidadeId ? NomeDaUnidade(unidadeId) : null,
            Restricoes = RestricoesDoAluno.DoAluno(aluno, catalogo)
                .Select(restricao => new RestricaoDoFilhoDTO
                {
                    Nome = restricao.Nome,
                    Categoria = restricao.Categoria
                })
                .ToList(),
            RequerAdaptacao = aluno.Restricoes.Count > 0
        };

        if (filho.RequerAdaptacao)
        {
            filho.Motivo = FilhoComRestricao;
            return filho;
        }

        if (aluno.UnidadeId is not { } unidadeDoAluno)
        {
            filho.Motivo = "Aluno sem unidade vinculada; não há cardápio associado.";
            return filho;
        }

        var cardapio = _cardapios.ObterGeralPublicado(unidadeDoAluno, dia);

        if (cardapio is null)
        {
            filho.Motivo = "Não há cardápio publicado para a data.";
            return filho;
        }

        filho.Cardapio = MontarCardapio(cardapio, contagens: null);
        return filho;
    }

    // ---------- Relatório do filho (RF6, RF7) ----------

    public Resultado<RelatorioDoFilhoDTO> RelatorioDoFilho(
        int alunoId,
        DateOnly? de,
        DateOnly? ate,
        EscopoUsuario escopo)
    {
        if (escopo.Papel != PapelUsuario.Responsavel)
        {
            return Resultado<RelatorioDoFilhoDTO>.Proibido();
        }

        // Negativa uniforme (D5): aluno de outro responsável e id inexistente respondem
        // igual, sem revelar existência.
        if (!_vinculos.Existe(alunoId, escopo.UsuarioId))
        {
            return Resultado<RelatorioDoFilhoDTO>.Proibido();
        }

        var aluno = _alunos.ObterPorId(alunoId);

        if (aluno is null)
        {
            return Resultado<RelatorioDoFilhoDTO>.Proibido();
        }

        var hoje = Hoje();
        var inicio = de ?? hoje.AddDays(-(DiasDeRelatorioPadrao - 1));
        var fim = ate ?? hoje;

        if (inicio > fim)
        {
            return Resultado<RelatorioDoFilhoDTO>.Invalido(
                "A data inicial não pode ser posterior à data final.");
        }

        if (fim.DayNumber - inicio.DayNumber + 1 > DiasDeRelatorioMaximo)
        {
            return Resultado<RelatorioDoFilhoDTO>.Invalido(
                $"O período máximo do relatório é de {DiasDeRelatorioMaximo} dias.");
        }

        var inicioUtc = inicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = fim.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var catalogo = CatalogoDeRestricoes();

        return Resultado<RelatorioDoFilhoDTO>.Ok(new RelatorioDoFilhoDTO
        {
            AlunoId = aluno.Id,
            AlunoNome = aluno.Nome,
            De = inicio,
            Ate = fim,
            Restricoes = RestricoesDoAluno.DoAluno(aluno, catalogo)
                .Select(restricao => new RestricaoDoFilhoDTO
                {
                    Nome = restricao.Nome,
                    Categoria = restricao.Categoria
                })
                .ToList(),
            Alteracoes = _painel
                .AlteracoesDeRestricao(alunoId, inicioUtc, fimUtc)
                .Select(evento => new AlteracaoDeRestricaoDTO
                {
                    Tipo = evento.Tipo,
                    Restricao = evento.Tipo == TipoEventoAluno.RestricaoRemovida
                        ? evento.ValorAnterior ?? string.Empty
                        : evento.ValorNovo ?? string.Empty,
                    Autor = evento.AutorNome,
                    Origem = evento.Origem,
                    CriadoEm = evento.CriadoEmUtc
                })
                .ToList(),
            Ocorrencias = _painel
                .OcorrenciasDoAluno(alunoId, inicioUtc, fimUtc)
                .Select(ocorrencia => new OcorrenciaDoFilhoDTO
                {
                    Id = ocorrencia.Id,
                    Tipo = ocorrencia.Tipo,
                    Status = ocorrencia.Status,
                    Descricao = ocorrencia.Descricao,
                    CriadaEm = ocorrencia.CriadoEmUtc
                })
                .ToList()
        });
    }

    // ---------- Montagem ----------

    /// <summary>
    /// Contagem de alunos distintos da unidade afetados por algum vínculo alimento↔restrição
    /// (RF4). Usa a função pura da Spec 014: a regra não é reimplementada aqui.
    /// </summary>
    private Dictionary<(int RefeicaoOrdem, int AlimentoId), int> ContarAlunosComRestricao(
        Cardapio cardapio,
        int unidadeId)
    {
        var itens = cardapio.Refeicoes
            .OrderBy(refeicao => refeicao.Ordem)
            .SelectMany(refeicao => refeicao.Itens.Select(item => new ItemAnalisado(
                refeicao.Ordem,
                refeicao.Nome,
                item.AlimentoId,
                item.Alimento?.Nome ?? string.Empty)))
            .ToList();

        if (itens.Count == 0)
        {
            return new Dictionary<(int, int), int>();
        }

        var ids = itens.Select(item => item.AlimentoId).Distinct().ToList();
        var vinculos = new Dictionary<int, IReadOnlyList<VinculoAnalisado>>();

        foreach (var alimento in _alimentos.ObterComVinculosPorIds(ids))
        {
            vinculos[alimento.Id] = alimento.Vinculos
                .Where(vinculo => vinculo.Restricao is not null)
                .Select(vinculo => new VinculoAnalisado(
                    vinculo.RestricaoId,
                    vinculo.Restricao!.Nome,
                    vinculo.Restricao.Categoria))
                .ToList();
        }

        var alunos = _alunos
            .Listar(new FiltroAluno(UnidadeId: unidadeId))
            .Select(aluno => new AlunoAnalisado(aluno.Id, aluno.Nome, aluno.Restricoes))
            .ToList();

        var analise = AnaliseCompatibilidade.Analisar(itens, vinculos, alunos);

        return analise.Bloqueantes
            .Concat(analise.Alertas)
            .GroupBy(achado => (achado.RefeicaoOrdem, achado.AlimentoId))
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo
                    .SelectMany(achado => achado.AlunosAfetados)
                    .Select(aluno => aluno.Id)
                    .Distinct()
                    .Count());
    }

    private static CardapioDoDiaDTO MontarCardapio(
        Cardapio cardapio,
        IReadOnlyDictionary<(int RefeicaoOrdem, int AlimentoId), int>? contagens) =>
        new()
        {
            Nome = cardapio.Nome,
            PublicadoEm = cardapio.PublicadoEmUtc,
            Refeicoes = cardapio.Refeicoes
                .OrderBy(refeicao => refeicao.Ordem)
                .Select(refeicao => new RefeicaoDoCardapioDoDiaDTO
                {
                    Nome = refeicao.Nome,
                    Itens = refeicao.Itens
                        .OrderBy(item => item.Alimento?.Nome ?? string.Empty, StringComparer.Ordinal)
                        .Select(item => new ItemDoCardapioDoDiaDTO
                        {
                            Alimento = item.Alimento?.Nome ?? $"Alimento {item.AlimentoId}",
                            Gramas = item.QuantidadeGramas,
                            AlunosComRestricao = contagens is not null &&
                                contagens.TryGetValue(
                                    (refeicao.Ordem, item.AlimentoId),
                                    out var total)
                                    ? total
                                    : 0
                        })
                        .ToList()
                })
                .ToList()
        };

    private IReadOnlyDictionary<string, RestricaoAlimentar> CatalogoDeRestricoes() =>
        RestricoesDoAluno.PorNomeNormalizado(
            _restricoes.Listar(categoria: null, incluirInativas: true));

    private string NomeDaUnidade(int unidadeId) =>
        _escolas.GetUnidade(unidadeId)?.Nome ?? $"Unidade {unidadeId}";

    private string? NomeDaEscola(int? escolaId) =>
        escolaId is { } id ? _escolas.GetEscola(id)?.Nome ?? $"Escola {id}" : null;

    private DateOnly Hoje() =>
        DateOnly.FromDateTime(_relogio.GetUtcNow().UtcDateTime);
}
