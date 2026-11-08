using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso da geração automática (Spec 013; Spec 032). O serviço orquestra o pipeline de
/// 13 passos: valida parâmetros e nutricionista, carrega catálogo/alunos/escolas uma vez,
/// gera no domínio, valida o cardápio inteiro e persiste geral + adaptados + trilha na mesma
/// transação. Nenhuma regra de seleção vive aqui (RNF1).
/// </summary>
public sealed class GeracaoCardapioService
{
    private const decimal GramasPorcao = 100m;

    private readonly IParametroGeracaoRepository _parametros;
    private readonly IAlimentoRepository _alimentos;
    private readonly IAlunoRepository _alunos;
    private readonly ICardapioRepository _cardapios;
    private readonly IUsuarioRepository _usuarios;
    private readonly ICategoriaAlimentoRepository _categorias;
    private readonly IEscolaRepository _escolas;
    private readonly IRestricaoRepository _restricoes;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public GeracaoCardapioService(
        IParametroGeracaoRepository parametros,
        IAlimentoRepository alimentos,
        IAlunoRepository alunos,
        ICardapioRepository cardapios,
        IUsuarioRepository usuarios,
        ICategoriaAlimentoRepository categorias,
        IEscolaRepository escolas,
        IRestricaoRepository restricoes,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _parametros = parametros;
        _alimentos = alimentos;
        _alunos = alunos;
        _cardapios = cardapios;
        _usuarios = usuarios;
        _categorias = categorias;
        _escolas = escolas;
        _restricoes = restricoes;
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
        // RF1: categorias obrigatórias existentes e ativas falham no salvamento (400), não na geração.
        foreach (var categoriaId in dto.CategoriasObrigatorias)
        {
            var categoria = _categorias.Obter(categoriaId);

            if (categoria is null || !categoria.Ativa)
            {
                return Resultado<ParametroGeracaoDTO>.Invalido(
                    $"A categoria obrigatória {categoriaId} não existe ou está inativa.");
            }
        }

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
        // 1. Parâmetros básicos.
        if (dto.UnidadeId <= 0)
            return Resultado<GeracaoCardapioDTO>.Invalido("Informe a unidade para gerar o cardápio.");

        if (dto.Periodo is null)
            return Resultado<GeracaoCardapioDTO>.Invalido("O período do cardápio é obrigatório.");

        var parametros = _parametros.ObterPorUnidade(dto.UnidadeId);

        if (parametros is null)
            return Resultado<GeracaoCardapioDTO>.Invalido(
                "Parâmetros de geração não configurados para a unidade.");

        // 2. Nutricionista: papel, ativo, mesma unidade e CRN válido (RF2), antes de carregar catálogo.
        if (dto.NutricionistaId <= 0)
            return Resultado<GeracaoCardapioDTO>.Invalido(
                "Selecione a nutricionista responsável pelo cardápio.");

        var nutricionista = _usuarios.GetById(dto.NutricionistaId);
        var motivoDaNutricionista = ValidacaoDeNutricionista.Motivo(nutricionista, dto.UnidadeId);

        if (motivoDaNutricionista is not null)
            return Resultado<GeracaoCardapioDTO>.Invalido(motivoDaNutricionista);

        // 3. Catálogo ativo ordenado por id (RF4).
        var alimentos = _alimentos.ListarAtivosComVinculos();
        var candidatos = alimentos
            .Select(alimento => new CandidatoAlimento(alimento.Id, alimento.CategoriaId, alimento.Valores))
            .ToList();

        // 4. Escolas, alunos e restrições da unidade (RF14).
        var escolas = _escolas.ListarPorUnidade(dto.UnidadeId)
            .Where(escola => escola.Ativa)
            .OrderBy(escola => escola.Nome, StringComparer.Ordinal)
            .ToList();

        var catalogoDeRestricoes = _restricoes.Listar(categoria: null, incluirInativas: true);
        var porNome = RestricoesDoAluno.PorNomeNormalizado(catalogoDeRestricoes);
        var restricoesParaAdaptar = catalogoDeRestricoes
            .Select(restricao => new RestricaoDoCatalogo(restricao.Id, restricao.Nome, restricao.Categoria))
            .ToList();

        var alunosDaUnidade = _alunos.Listar(new FiltroAluno(UnidadeId: dto.UnidadeId));

        var refeicoes = parametros.RefeicoesPadrao
            .Select((nome, indice) => new RefeicaoPadrao(nome, indice + 1))
            .ToList();

        // Bloqueados só pela janela de repetição; restrições individuais ficam na adaptação (RF3/RF4).
        var bloqueados = new HashSet<int>();

        if (parametros.JanelaRepeticaoDias > 0)
        {
            var dataLimite = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-parametros.JanelaRepeticaoDias);
            bloqueados.UnionWith(_cardapios.ObterAlimentosUsadosDesde(dto.UnidadeId, dataLimite));
        }

        // 5-6. Geração determinística (RF5/RF6). Seed nulo devolve o valor sorteado (RF13).
        var seed = dto.Seed ?? Random.Shared.Next(int.MinValue, int.MaxValue);

        var resultado = GeradorCardapioAutomatico.Gerar(new EntradaGeracao(
            refeicoes,
            candidatos,
            parametros,
            bloqueados,
            seed));

        var geral = MontarCardapio(dto, resultado, nutricionista);

        // 7. Revalida o cardápio inteiro antes de persistir (RF7).
        var erros = ValidadorDeCardapioGerado.Validar(geral, parametros, alimentos);

        if (erros.Count > 0)
            throw new ValidacaoDeGeracaoException(erros);

        // 8-12. Transação: geral + adaptados por (escola, conjunto) + trilha (RF8/RF10).
        var adaptados = new List<VersaoAdaptadaGeradaDTO>();

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            geral.RegistrarCriacao(nutricionista.Nome ?? "sistema");
            _cardapios.Add(geral);

            foreach (var escola in escolas)
            {
                var conjuntos = alunosDaUnidade
                    .Where(aluno => aluno.EscolaId == escola.Id)
                    .Select(aluno => RestricoesDoAluno.Conjunto(aluno, porNome))
                    .Where(conjunto => !conjunto.EstaVazio)
                    .Distinct()
                    .OrderBy(conjunto => conjunto.Chave, StringComparer.Ordinal)
                    .ToList();

                foreach (var conjunto in conjuntos)
                {
                    var adaptacao = AdaptacaoDeCardapio.Adaptar(
                        geral, escola.Id, conjunto, alimentos, restricoesParaAdaptar);

                    var adaptado = Cardapio.CriarAdaptado(
                        geral,
                        escola.Id,
                        conjunto,
                        adaptacao.Refeicoes,
                        adaptacao.Substituicoes,
                        adaptacao.Pendencias,
                        adaptacao.Status);

                    adaptado.RegistrarCriacao(nutricionista.Nome ?? "sistema");
                    _cardapios.Add(adaptado);

                    adaptados.Add(ParaResumoAdaptado(adaptado, escola.Id, conjunto, adaptacao));
                }
            }
        });

        // 13. Resposta com ids, seed, tentativas, totais e diagnóstico vazio (RF12).
        return Resultado<GeracaoCardapioDTO>.Ok(ParaDTO(geral.Id, resultado, adaptados));
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

    private static VersaoAdaptadaGeradaDTO ParaResumoAdaptado(
        Cardapio adaptado,
        int escolaId,
        ConjuntoDeRestricoes conjunto,
        ResultadoAdaptacao adaptacao) => new()
    {
        Id = adaptado.Id,
        EscolaId = escolaId,
        Conjunto = conjunto.Chave,
        Status = adaptado.StatusAdaptacao.ToString(),
        Motivo = adaptacao.Pendencias.Count == 0
            ? null
            : string.Join(" ", adaptacao.Pendencias.Select(pendencia => pendencia.Motivo))
    };

    private static GeracaoCardapioDTO ParaDTO(
        int cardapioGeralId,
        ResultadoGeracao resultado,
        List<VersaoAdaptadaGeradaDTO> adaptados) => new()
    {
        CardapioGeralId = cardapioGeralId,
        Seed = resultado.Seed,
        TentativasPorRefeicao = resultado.TentativasPorRefeicao
            .Select(tentativa => new TentativaRefeicaoGeradaDTO
            {
                Refeicao = tentativa.Refeicao,
                Tentativas = tentativa.Tentativas
            })
            .ToList(),
        TotaisPorRefeicao = resultado.Refeicoes
            .Select(refeicao => new TotalRefeicaoGeradaDTO
            {
                Refeicao = refeicao.Refeicao.Nome,
                CaloriasKcal = refeicao.Itens.Sum(item => item.Valores.CaloriasKcal)
            })
            .ToList(),
        VersaoAdaptadaIds = adaptados,
        Avisos = resultado.Avisos.ToList(),
        Diagnostico = new List<DiagnosticoRefeicaoDTO>()
    };
}
