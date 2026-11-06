using Backend.Tests.Fakes;
using Backend.Tests.Support;
using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Casos de uso da versão adaptada (Spec 029): criação do par geral+adaptados na mesma
/// operação, bloqueio sem substituto, confirmação registrada, idempotência, invalidação
/// pelo geral, versão servida e bloqueio de publicação.
/// </summary>
public class CardapioAdaptadoServiceTests
{
    private sealed record Cenario(
        CardapioService Servico,
        AdaptarCardapioService Adaptacao,
        CardapioServidoService Servido,
        PublicacaoCardapioService Publicacao,
        CardapioRepositoryFake Cardapios,
        AlimentoRepositoryFake Alimentos,
        AlunoRepositoryFake Alunos,
        RestricaoRepositoryFake Restricoes,
        UsuarioRepositoryFake Usuarios,
        int UnidadeId,
        int EscolaA,
        int EscolaB,
        int LactoseId,
        int LeiteId,
        int BebidaId);

    private static readonly DateOnly Inicio = new(2026, 11, 2);
    private static readonly DateOnly Fim = new(2026, 11, 6);

    private static async Task<Cenario> MontarAsync()
    {
        var cardapios = new CardapioRepositoryFake();
        var alimentos = new AlimentoRepositoryFake();
        var alunos = new AlunoRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var usuarios = new UsuarioRepositoryFake();
        var restricoes = new RestricaoRepositoryFake();
        var unidadeDeTrabalho = new UnidadeDeTrabalhoFake();
        var autorizacao = new AutorizacaoService();

        cardapios.ResolverAlimento = id => alimentos.Obter(id);

        var unidadeId = escolas.AddUnidade(new UnidadeEscolar("Unidade A")).Id;
        var escolaA = escolas.Add(new Escola("Escola A", unidadeId)).Id;
        var escolaB = escolas.Add(new Escola("Escola B", unidadeId)).Id;

        var lactose = restricoes.Add(new RestricaoAlimentar("Lactose", CategoriaRestricao.Alergia));

        var leite = alimentos.Add(new Alimento(
            "Leite",
            1,
            ValoresNutricionais.Criar(130m, 2.7m, 28.2m, 0.3m, 0.4m, 1m),
            new[] { lactose.Id }));

        var bebida = alimentos.Add(new Alimento(
            "Bebida vegetal",
            1,
            ValoresNutricionais.Criar(130m, 2.7m, 28.2m, 0.3m, 0.4m, 1m),
            null));

        var nutricionista = new Usuario(
            "Nutricionista A",
            "11111111111",
            new DateOnly(1990, 1, 1),
            "nutri.a@teste.com",
            PapelUsuario.Nutricionista,
            null,
            unidadeId,
            "CRN-3 12345");
        usuarios.Add(nutricionista);

        await Task.CompletedTask;

        return new Cenario(
            new CardapioService(
                cardapios,
                alimentos,
                alunos,
                escolas,
                usuarios,
                restricoes,
                unidadeDeTrabalho,
                autorizacao),
            new AdaptarCardapioService(
                cardapios,
                alimentos,
                alunos,
                escolas,
                usuarios,
                restricoes,
                unidadeDeTrabalho,
                autorizacao),
            new CardapioServidoService(cardapios, alunos, escolas, restricoes),
            new PublicacaoCardapioService(cardapios, alimentos, alunos, usuarios, unidadeDeTrabalho),
            cardapios,
            alimentos,
            alunos,
            restricoes,
            usuarios,
            unidadeId,
            escolaA,
            escolaB,
            lactose.Id,
            leite.Id,
            bebida.Id);
    }

    private static EscopoUsuario Nutricao(int unidadeId) =>
        new(1, PapelUsuario.Nutricionista, null, unidadeId);

    private static void AdicionarAluno(
        Cenario cenario,
        string nome,
        int escolaId,
        params string[] restricoes)
    {
        var aluno = new Aluno(
            nome,
            new DateOnly(2015, 5, 20),
            new Turma("Escola", "5 ano"),
            null,
            null,
            restricoes,
            false,
            new Responsavel("Responsável", "11988888888"),
            escolaId);

        aluno.DefinirUnidade(cenario.UnidadeId);
        cenario.Alunos.Add(aluno);
    }

    private static SalvarCardapioDTO Entrada(
        Cenario cenario,
        bool confirmar = true,
        int? nutricionistaId = 1,
        int? alimentoId = null) => new()
    {
        Nome = "Semana 12",
        UnidadeId = cenario.UnidadeId,
        NutricionistaId = nutricionistaId,
        ConfirmarIncompatibilidades = confirmar,
        Periodo = new PeriodoCardapioDTO { Inicio = Inicio, Fim = Fim },
        Refeicoes = new List<SalvarRefeicaoDTO>
        {
            new()
            {
                Nome = "Almoço",
                Ordem = 1,
                Itens = new List<SalvarItemCardapioDTO>
                {
                    new() { AlimentoId = alimentoId ?? cenario.LeiteId, Gramas = 100m }
                }
            }
        }
    };

    [Fact]
    public async Task Criacao_ProduzGeralECadaEscolaEConjunto()
    {
        var cenario = await MontarAsync();
        AdicionarAluno(cenario, "Ana", cenario.EscolaA, "Lactose");
        AdicionarAluno(cenario, "Bia", cenario.EscolaB, "Lactose");

        var resultado = cenario.Servico.Criar(Entrada(cenario), Nutricao(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.False(resultado.Valor!.SemRestricoes);

        var todos = cenario.Cardapios.GetCardapios();
        var geral = Assert.Single(todos, cardapio => cardapio.Tipo == TipoCardapio.Geral);
        var adaptados = todos.Where(cardapio => cardapio.Tipo == TipoCardapio.Adaptado).ToList();

        Assert.Equal(2, adaptados.Count);
        Assert.Contains(adaptados, adaptado => adaptado.EscolaId == cenario.EscolaA);
        Assert.Contains(adaptados, adaptado => adaptado.EscolaId == cenario.EscolaB);
        Assert.All(adaptados, adaptado =>
        {
            Assert.Equal(geral.Id, adaptado.OrigemId);
            Assert.Equal("1", adaptado.ConjuntoRestricoesChave);
            Assert.Equal(StatusAdaptacao.Atualizado, adaptado.StatusAdaptacao);
            Assert.Single(adaptado.Substituicoes);
        });
    }

    [Fact]
    public async Task Criacao_SemConfirmacao_DevolveInvalido()
    {
        var cenario = await MontarAsync();
        AdicionarAluno(cenario, "Ana", cenario.EscolaA, "Lactose");

        var resultado = cenario.Servico.Criar(
            Entrada(cenario, confirmar: false),
            Nutricao(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Contains("Confirme", resultado.Erro!);
        Assert.Empty(cenario.Cardapios.GetCardapios());
    }

    [Fact]
    public async Task Criacao_ComConfirmacao_RegistraTrilha()
    {
        var cenario = await MontarAsync();
        AdicionarAluno(cenario, "Ana", cenario.EscolaA, "Lactose");

        var resultado = cenario.Servico.Criar(Entrada(cenario), Nutricao(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);

        var geral = cenario.Cardapios.GetCardapios().Single(cardapio => cardapio.Tipo == TipoCardapio.Geral);

        var confirmacao = Assert.Single(geral.Confirmacoes);
        Assert.Equal(cenario.LeiteId, confirmacao.AlimentoId);
        Assert.Contains("Escola A", confirmacao.Escolas);
        Assert.Equal("1", confirmacao.Conjuntos);
    }

    [Fact]
    public async Task Criacao_SemRestricoes_NaoCriaAdaptado()
    {
        var cenario = await MontarAsync();

        var resultado = cenario.Servico.Criar(Entrada(cenario), Nutricao(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.True(resultado.Valor!.SemRestricoes);
        Assert.Single(cenario.Cardapios.GetCardapios());
    }

    [Fact]
    public async Task Montagem_BloqueiaAlimentoSemSubstituto()
    {
        var cenario = await MontarAsync();
        AdicionarAluno(cenario, "Ana", cenario.EscolaA, "Lactose");
        cenario.Alimentos.Update(Desativar(cenario.Alimentos.Obter(cenario.BebidaId)!));

        var resultado = cenario.Servico.Criar(Entrada(cenario), Nutricao(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Contains("substituto seguro", resultado.Erro!);
    }

    [Fact]
    public async Task NutricionistaSemCrn_NaoCria()
    {
        var cenario = await MontarAsync();

        var semCrn = new Usuario(
            "Nutricionista sem CRN",
            "22222222222",
            new DateOnly(1990, 1, 1),
            "sem.crn@teste.com",
            PapelUsuario.Nutricionista,
            null,
            cenario.UnidadeId,
            null);
        cenario.Usuarios.Add(semCrn);

        var resultado = cenario.Servico.Criar(
            Entrada(cenario, nutricionistaId: semCrn.Id),
            Nutricao(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Contains("CRN", resultado.Erro!);
    }

    [Fact]
    public async Task EditarGeral_MarcaAdaptadosDesatualizados()
    {
        var cenario = await MontarAsync();
        AdicionarAluno(cenario, "Ana", cenario.EscolaA, "Lactose");

        var criado = cenario.Servico
            .Criar(Entrada(cenario), Nutricao(cenario.UnidadeId))
            .Valor!;

        var atualizado = cenario.Servico.Atualizar(
            criado.Id,
            Entrada(cenario),
            Nutricao(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, atualizado.Status);

        var adaptado = cenario.Cardapios
            .GetCardapios()
            .Single(cardapio => cardapio.Tipo == TipoCardapio.Adaptado);

        Assert.Equal(StatusAdaptacao.Desatualizado, adaptado.StatusAdaptacao);
    }

    [Fact]
    public async Task AdaptarDuasVezes_NaoDuplica()
    {
        var cenario = await MontarAsync();
        AdicionarAluno(cenario, "Ana", cenario.EscolaA, "Lactose");

        var geral = cenario.Servico
            .Criar(Entrada(cenario), Nutricao(cenario.UnidadeId))
            .Valor!;

        var primeira = cenario.Adaptacao.Adaptar(
            geral.Id,
            new AdaptarCardapioDTO
            {
                EscolaId = cenario.EscolaA,
                RestricaoIds = new List<int> { cenario.LactoseId }
            },
            Nutricao(cenario.UnidadeId));

        var segunda = cenario.Adaptacao.Adaptar(
            geral.Id,
            new AdaptarCardapioDTO
            {
                EscolaId = cenario.EscolaA,
                RestricaoIds = new List<int> { cenario.LactoseId }
            },
            Nutricao(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, primeira.Status);
        Assert.Equal(ResultadoStatus.Ok, segunda.Status);
        Assert.Single(
            cenario.Cardapios.GetCardapios(),
            cardapio => cardapio.Tipo == TipoCardapio.Adaptado);
    }

    [Fact]
    public async Task AlunoSemVersaoPublicada_FicaIndisponivel()
    {
        var cenario = await MontarAsync();
        AdicionarAluno(cenario, "Ana", cenario.EscolaA, "Lactose");

        var criado = cenario.Servico
            .Criar(Entrada(cenario), Nutricao(cenario.UnidadeId))
            .Valor!;

        var geral = cenario.Cardapios
            .GetCardapios()
            .Single(cardapio => cardapio.Tipo == TipoCardapio.Geral);

        geral.Publicar("teste", DateTime.UtcNow);
        cenario.Cardapios.Update(geral);

        var servido = cenario.Servido.Obter(
            cenario.Alunos.GetAlunos().Single().Id,
            Inicio,
            Nutricao(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, servido.Status);
        Assert.False(servido.Valor!.Disponivel);
        Assert.Contains("adaptada publicada", servido.Valor.Motivo!);
        Assert.Null(servido.Valor.Cardapio);
    }

    [Fact]
    public async Task PublicarGeralComAdaptadoPendente_DevolveConflito()
    {
        var cenario = await MontarAsync();
        AdicionarAluno(cenario, "Ana", cenario.EscolaA, "Lactose");

        var criado = cenario.Servico
            .Criar(Entrada(cenario), Nutricao(cenario.UnidadeId))
            .Valor!;

        var adaptado = cenario.Cardapios
            .GetCardapios()
            .Single(cardapio => cardapio.Tipo == TipoCardapio.Adaptado);

        PropriedadesPrivadas.Definir(
            adaptado,
            nameof(Cardapio.StatusAdaptacao),
            StatusAdaptacao.PendenteDeRevisao);

        cenario.Cardapios.Update(adaptado);

        var publicacao = cenario.Publicacao.Publicar(criado.Id, Nutricao(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Conflito, publicacao.Status);
        Assert.Contains("adaptada", publicacao.Erro!);
    }

    private static Alimento Desativar(Alimento alimento)
    {
        alimento.Desativar();
        return alimento;
    }
}
