using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso da adaptação manual (Spec 029, T6).
///
/// Orquestra o que a função pura não pode fazer: valida escopo e escola, calcula os
/// conjuntos existentes na escola, verifica o teto de 50 versões, reutiliza o rascunho
/// existente (RNF5) e persiste tudo na mesma transação. A regra nutricional continua em
/// <see cref="AdaptacaoDeCardapio"/>.
/// </summary>
public sealed class AdaptarCardapioService
{
    public const int MaximoDeAdaptados = 50;

    private readonly ICardapioRepository _cardapios;
    private readonly IAlimentoRepository _alimentos;
    private readonly IAlunoRepository _alunos;
    private readonly IEscolaRepository _escolas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IRestricaoRepository _restricoes;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly AutorizacaoService _autorizacao;

    public AdaptarCardapioService(
        ICardapioRepository cardapios,
        IAlimentoRepository alimentos,
        IAlunoRepository alunos,
        IEscolaRepository escolas,
        IUsuarioRepository usuarios,
        IRestricaoRepository restricoes,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        AutorizacaoService autorizacao)
    {
        _cardapios = cardapios;
        _alimentos = alimentos;
        _alunos = alunos;
        _escolas = escolas;
        _usuarios = usuarios;
        _restricoes = restricoes;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _autorizacao = autorizacao;
    }

    public Resultado<List<CardapioAdaptadoResumoDTO>> Adaptar(
        int geralId,
        AdaptarCardapioDTO dto,
        EscopoUsuario escopo)
    {
        var geral = _cardapios.ObterComRefeicoes(geralId);

        if (geral is null)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.NaoEncontrado(
                "Cardápio não encontrado.");
        }

        if (geral.Tipo != TipoCardapio.Geral)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Invalido(
                "Somente um cardápio geral pode ser adaptado.");
        }

        if (geral.Situacao == SituacaoCardapio.Cancelado)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Invalido(
                "Cardápio cancelado não pode ser adaptado.");
        }

        var negacao = _autorizacao.Verificar(
            Recurso.CardapioAdaptado,
            escopo,
            OperacaoDeAcesso.Atualizar,
            new AlvoDeEscopo(geral.UnidadeId, dto.EscolaId));

        if (negacao == ResultadoStatus.NaoEncontrado)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.NaoEncontrado(
                "Cardápio não encontrado.");
        }

        if (negacao is not null)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Proibido();
        }

        if (dto.EscolaId <= 0)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Invalido(
                "Informe a escola da versão adaptada.");
        }

        var escola = _escolas.GetEscola(dto.EscolaId);

        if (escola is null || escola.UnidadeId != geral.UnidadeId)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Invalido(
                "A escola da versão adaptada precisa pertencer à unidade do cardápio.");
        }

        var usuario = _usuarios.GetById(escopo.UsuarioId);
        var motivoDaNutricionista = ValidacaoDeNutricionista.Motivo(usuario, geral.UnidadeId);

        if (motivoDaNutricionista is not null)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Invalido(motivoDaNutricionista);
        }

        var catalogoDeRestricoes = _restricoes.Listar(categoria: null, incluirInativas: true);
        var porId = catalogoDeRestricoes.ToDictionary(restricao => restricao.Id);
        var porNome = RestricoesDoAluno.PorNomeNormalizado(catalogoDeRestricoes);

        var alunosDaEscola = _alunos.Listar(new FiltroAluno(EscolaId: dto.EscolaId));

        if (alunosDaEscola.Count == 0)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Ok(
                new List<CardapioAdaptadoResumoDTO>());
        }

        var conjuntos = ResolverConjuntos(dto, alunosDaEscola, porId, porNome);

        if (conjuntos is null)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Invalido(
                "O conjunto informado contém restrição inexistente no catálogo.");
        }

        if (conjuntos.Count == 0)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Ok(
                new List<CardapioAdaptadoResumoDTO>());
        }

        var catalogoDeAlimentos = _alimentos.ListarAtivosComVinculos();
        var restricoes = catalogoDeRestricoes
            .Select(restricao => new RestricaoDoCatalogo(
                restricao.Id,
                restricao.Nome,
                restricao.Categoria))
            .ToList();

        var autor = usuario?.Nome ?? "sistema";
        var agora = DateTime.UtcNow;
        var novos = new List<Cardapio>();
        var atualizados = new List<Cardapio>();
        var resumos = new List<CardapioAdaptadoResumoDTO>();

        foreach (var conjunto in conjuntos)
        {
            var existente = _cardapios.ObterAdaptado(geral.Id, dto.EscolaId, conjunto.Chave);

            if (existente is not null && existente.Situacao != SituacaoCardapio.Rascunho)
            {
                return Resultado<List<CardapioAdaptadoResumoDTO>>.Conflito(
                    $"Já existe uma versão adaptada {existente.Situacao} para esta escola e conjunto. " +
                    "A substituição de versão publicada pertence ao fluxo de revisão da Spec 015.");
            }

            var resultado = AdaptacaoDeCardapio.Adaptar(
                geral,
                dto.EscolaId,
                conjunto,
                catalogoDeAlimentos,
                restricoes);

            if (existente is null)
            {
                var adaptado = Cardapio.CriarAdaptado(
                    geral,
                    dto.EscolaId,
                    conjunto,
                    resultado.Refeicoes,
                    resultado.Substituicoes,
                    resultado.Pendencias,
                    resultado.Status);

                adaptado.RegistrarCriacao(autor);
                novos.Add(adaptado);
                resumos.Add(ParaResumo(adaptado, escola.Nome));
            }
            else
            {
                existente.AplicarAdaptacao(
                    resultado.Refeicoes,
                    resultado.Substituicoes,
                    resultado.Pendencias,
                    resultado.Status,
                    autor,
                    agora);

                atualizados.Add(existente);
                resumos.Add(ParaResumo(existente, escola.Nome));
            }
        }

        var total = _cardapios.ContarAdaptados(geral.Id) + novos.Count;

        if (total > MaximoDeAdaptados)
        {
            throw new LimiteDeAdaptacoesException(MaximoDeAdaptados, total);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            foreach (var novo in novos)
            {
                _cardapios.Add(novo);
            }

            foreach (var atualizado in atualizados)
            {
                _cardapios.Update(atualizado);
            }
        });

        return Resultado<List<CardapioAdaptadoResumoDTO>>.Ok(resumos);
    }

    /// <summary>
    /// Conjuntos a adaptar: o conjunto explícito do pedido ou todos os conjuntos distintos
    /// existentes entre os alunos da escola (RF2, RF10 e RF21).
    /// </summary>
    private static List<ConjuntoDeRestricoes>? ResolverConjuntos(
        AdaptarCardapioDTO dto,
        IReadOnlyList<Aluno> alunosDaEscola,
        IReadOnlyDictionary<int, RestricaoAlimentar> porId,
        IReadOnlyDictionary<string, RestricaoAlimentar> porNome)
    {
        if (dto.RestricaoIds is { Count: > 0 } informados)
        {
            if (informados.Distinct().Any(id => !porId.ContainsKey(id)))
            {
                return null;
            }

            return new List<ConjuntoDeRestricoes> { ConjuntoDeRestricoes.DeIds(informados) };
        }

        return alunosDaEscola
            .Select(aluno => RestricoesDoAluno.Conjunto(aluno, porNome))
            .Where(conjunto => !conjunto.EstaVazio)
            .Distinct()
            .OrderBy(conjunto => conjunto.Chave, StringComparer.Ordinal)
            .ToList();
    }

    private static CardapioAdaptadoResumoDTO ParaResumo(Cardapio adaptado, string escolaNome) => new()
    {
        Id = adaptado.Id,
        Nome = adaptado.Nome,
        OrigemId = adaptado.OrigemId ?? 0,
        EscolaId = adaptado.EscolaId ?? 0,
        EscolaNome = escolaNome,
        ConjuntoRestricoesChave = adaptado.ConjuntoRestricoesChave ?? string.Empty,
        StatusAdaptacao = adaptado.StatusAdaptacao,
        Situacao = adaptado.Situacao,
        TotalSubstituicoes = adaptado.Substituicoes.Count,
        TotalPendencias = adaptado.Pendencias.Count,
        SemRestricoes = false
    };
}
