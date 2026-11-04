using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso da geração automática (Spec 013). O serviço só orquestra: carrega parâmetros,
/// candidatos, alunos e publicados recentes e delega a seleção ao domínio puro. Nenhuma regra
/// de geração vive aqui (RNF5).
/// </summary>
public sealed class GeracaoCardapioService
{
    private const decimal GramasPorcao = 100m;

    private readonly IParametroGeracaoRepository _parametros;
    private readonly IAlimentoRepository _alimentos;
    private readonly IAlunoRepository _alunos;
    private readonly ICardapioRepository _cardapios;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public GeracaoCardapioService(
        IParametroGeracaoRepository parametros,
        IAlimentoRepository alimentos,
        IAlunoRepository alunos,
        ICardapioRepository cardapios,
        IUsuarioRepository usuarios,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _parametros = parametros;
        _alimentos = alimentos;
        _alunos = alunos;
        _cardapios = cardapios;
        _usuarios = usuarios;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    public Resultado<ParametroGeracaoDTO> ObterParametros(int unidadeId)
    {
        var parametro = _parametros.ObterPorUnidade(unidadeId);

        return parametro is null
            ? Resultado<ParametroGeracaoDTO>.NaoEncontrado(
                "Parâmetros de geração não encontrados para a unidade.")
            : Resultado<ParametroGeracaoDTO>.Ok(ParaDTO(parametro));
    }

    public Resultado<ParametroGeracaoDTO> SalvarParametros(int unidadeId, SalvarParametroGeracaoDTO dto)
    {
        var existente = _parametros.ObterPorUnidade(unidadeId);

        ParametroGeracao parametro;

        if (existente is null)
        {
            parametro = new ParametroGeracao(
                unidadeId,
                dto.CaloriasMinimasRefeicao,
                dto.CaloriasMaximasRefeicao,
                dto.OpcoesPorRefeicao,
                dto.JanelaRepeticaoDias,
                dto.ExcluirAlimentosIncompativeis,
                dto.CategoriasObrigatorias,
                dto.RefeicoesPadrao);

            _unidadeDeTrabalho.ExecutarEmTransacao(() => _parametros.Add(parametro));
        }
        else
        {
            existente.Atualizar(
                dto.CaloriasMinimasRefeicao,
                dto.CaloriasMaximasRefeicao,
                dto.OpcoesPorRefeicao,
                dto.JanelaRepeticaoDias,
                dto.ExcluirAlimentosIncompativeis,
                dto.CategoriasObrigatorias,
                dto.RefeicoesPadrao);

            _unidadeDeTrabalho.ExecutarEmTransacao(() => _parametros.Update(existente));

            parametro = existente;
        }

        return Resultado<ParametroGeracaoDTO>.Ok(ParaDTO(parametro));
    }

    public Resultado<GeracaoCardapioDTO> Gerar(GerarCardapioDTO dto)
    {
        if (dto.UnidadeId <= 0)
            return Resultado<GeracaoCardapioDTO>.Invalido("Informe a unidade para gerar o cardápio.");

        if (dto.Periodo is null)
            return Resultado<GeracaoCardapioDTO>.Invalido("O período do cardápio é obrigatório.");

        var parametros = _parametros.ObterPorUnidade(dto.UnidadeId);

        if (parametros is null)
            return Resultado<GeracaoCardapioDTO>.Invalido(
                "Parâmetros de geração não configurados para a unidade.");

        if (dto.NutricionistaId <= 0)
            return Resultado<GeracaoCardapioDTO>.Invalido(
                "Selecione a nutricionista responsável pelo cardápio.");

        var nutricionista = _usuarios.GetById(dto.NutricionistaId);

        if (nutricionista is null || nutricionista.Papel != PapelUsuario.Nutricionista)
            return Resultado<GeracaoCardapioDTO>.Invalido(
                "Selecione uma nutricionista válida para o cardápio.");

        if (!nutricionista.Ativo)
            return Resultado<GeracaoCardapioDTO>.Invalido(
                "A nutricionista selecionada está inativa.");

        // Determinismo: os candidatos são carregados já ordenados por id, antes do embaralhamento.
        var alimentos = _alimentos.ListarAtivosComVinculos();
        var candidatos = alimentos
            .Select(alimento => new CandidatoAlimento(alimento.Id, alimento.CategoriaId, alimento.Valores))
            .ToList();

        var refeicoes = parametros.RefeicoesPadrao
            .Select((nome, indice) => new RefeicaoPadrao(nome, indice + 1))
            .ToList();

        var bloqueados = new HashSet<int>();

        if (parametros.ExcluirAlimentosIncompativeis)
            bloqueados.UnionWith(CalcularAlimentosBloqueados(alimentos, dto.UnidadeId));

        if (parametros.JanelaRepeticaoDias > 0)
        {
            var dataLimite = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-parametros.JanelaRepeticaoDias);
            bloqueados.UnionWith(_cardapios.ObterAlimentosUsadosDesde(dto.UnidadeId, dataLimite));
        }

        var seed = dto.Seed ?? Random.Shared.Next(int.MinValue, int.MaxValue);

        var resultado = GeradorCardapioAutomatico.Gerar(new EntradaGeracao(
            refeicoes,
            candidatos,
            parametros,
            bloqueados,
            seed));

        var cardapio = MontarCardapio(dto, resultado, nutricionista);

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _cardapios.Add(cardapio));

        return Resultado<GeracaoCardapioDTO>.Ok(ParaDTO(cardapio.Id, resultado));
    }

    private HashSet<int> CalcularAlimentosBloqueados(List<Alimento> alimentos, int unidadeId)
    {
        var itens = alimentos
            .Select(alimento => new ItemAnalisado(0, "Candidatos", alimento.Id, alimento.Nome))
            .ToList();

        var vinculosPorAlimento = alimentos.ToDictionary(
            alimento => alimento.Id,
            alimento => (IReadOnlyList<VinculoAnalisado>)alimento.Vinculos
                .Where(vinculo => vinculo.Restricao is not null)
                .Select(vinculo => new VinculoAnalisado(
                    vinculo.RestricaoId,
                    vinculo.Restricao!.Nome,
                    vinculo.Restricao.Categoria))
                .ToList());

        var alunos = _alunos.Listar(new FiltroAluno(UnidadeId: unidadeId))
            .Select(aluno => new AlunoAnalisado(aluno.Id, aluno.Nome, aluno.Restricoes))
            .ToList();

        var resultado = AnaliseCompatibilidade.Analisar(itens, vinculosPorAlimento, alunos);

        return resultado.Bloqueantes
            .Select(achado => achado.AlimentoId)
            .ToHashSet();
    }

    private static Cardapio MontarCardapio(
        GerarCardapioDTO dto,
        ResultadoGeracao resultado,
        Usuario nutricionista)
    {
        var refeicoes = resultado.Refeicoes
            .Select(refeicao => new Refeicao(
                refeicao.Refeicao.Nome,
                refeicao.Refeicao.Ordem,
                refeicao.Itens.Select(item => new ItemRefeicao(item.AlimentoId, GramasPorcao))))
            .ToList();

        var responsavel = new NutricionistaResponsavel(
            nutricionista.Id,
            nutricionista.Nome,
            nutricionista.Email,
            nutricionista.Cpf,
            nutricionista.Crn);

        return new Cardapio(
            dto.Nome,
            dto.UnidadeId,
            new PeriodoVigencia(dto.Periodo!.Inicio, dto.Periodo.Fim),
            refeicoes,
            nutricionista: responsavel);
    }

    private static ParametroGeracaoDTO ParaDTO(ParametroGeracao parametro) => new()
    {
        Id = parametro.Id,
        UnidadeId = parametro.UnidadeId,
        CaloriasMinimasRefeicao = parametro.CaloriasMinimasRefeicao,
        CaloriasMaximasRefeicao = parametro.CaloriasMaximasRefeicao,
        OpcoesPorRefeicao = parametro.OpcoesPorRefeicao,
        JanelaRepeticaoDias = parametro.JanelaRepeticaoDias,
        ExcluirAlimentosIncompativeis = parametro.ExcluirAlimentosIncompativeis,
        CategoriasObrigatorias = parametro.CategoriasObrigatorias.ToList(),
        RefeicoesPadrao = parametro.RefeicoesPadrao.ToList()
    };

    private static GeracaoCardapioDTO ParaDTO(int cardapioId, ResultadoGeracao resultado) => new()
    {
        CardapioId = cardapioId,
        Seed = resultado.Seed,
        Totais = resultado.Refeicoes
            .Select(refeicao => new TotalRefeicaoGeradaDTO
            {
                Refeicao = refeicao.Refeicao.Nome,
                CaloriasKcal = refeicao.Itens.Sum(item => item.Valores.CaloriasKcal)
            })
            .ToList(),
        Avisos = resultado.Avisos.ToList()
    };
}
