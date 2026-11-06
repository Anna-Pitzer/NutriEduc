using Backend.Tests.Fakes;
using Backend.Tests.Support;
using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Regras do caso de uso do cardápio (Spec 012): escopo por papel, unidade e alimento
/// válidos, RF4 (alimento desativado só se já estava), duplicação e bloqueio de exclusão
/// fora de rascunho. Todos os testes usam fakes — nenhum toca banco.
/// </summary>
public class CardapioServiceTests
{
    private sealed record Cenario(
        CardapioService Servico,
        CardapioRepositoryFake Cardapios,
        AlimentoRepositoryFake Alimentos,
        AlunoRepositoryFake Alunos,
        RestricaoRepositoryFake Restricoes,
        UnidadeDeTrabalhoFake UnidadeDeTrabalho,
        int UnidadeA,
        int UnidadeB,
        int EscolaA);

    // A unidade A é sempre a primeira criada no fake e a nutricionista responsável tem id 1.
    private static readonly EscopoUsuario Nutricao =
        new(1, PapelUsuario.Nutricionista, null, 1);

    private static readonly EscopoUsuario NutricaoDaUnidadeB =
        new(2, PapelUsuario.Nutricionista, null, 2);

    private static EscopoUsuario Merendeira(int unidadeId, int escolaId) =>
        new(2, PapelUsuario.Merendeira, escolaId, unidadeId);

    private static SalvarCardapioDTO Entrada(
        int unidadeId,
        string nome = "Semana 12",
        string inicio = "2026-11-02",
        string fim = "2026-11-06",
        int nutricionistaId = 1,
        params (string Nome, int Ordem, int AlimentoId, decimal Gramas)[] itensDaRefeicao) => new()
        {
            Nome = nome,
            UnidadeId = unidadeId,
            NutricionistaId = nutricionistaId,
            Periodo = new PeriodoCardapioDTO
            {
                Inicio = DateOnly.Parse(inicio),
                Fim = DateOnly.Parse(fim)
            },
            Refeicoes = new List<SalvarRefeicaoDTO>
            {
                new()
                {
                    Nome = itensDaRefeicao.Length > 0 ? itensDaRefeicao[0].Nome : "Almoço",
                    Ordem = itensDaRefeicao.Length > 0 ? itensDaRefeicao[0].Ordem : 1,
                    Itens = itensDaRefeicao
                        .Select(item => new SalvarItemCardapioDTO
                        {
                            AlimentoId = item.AlimentoId,
                            Gramas = item.Gramas
                        })
                        .ToList()
                }
            }
        };

    private static Cenario Montar()
    {
        var cardapios = new CardapioRepositoryFake();
        var alimentos = new AlimentoRepositoryFake();
        var alunos = new AlunoRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var usuarios = new UsuarioRepositoryFake();
        var restricoes = new RestricaoRepositoryFake();
        var unidadeDeTrabalho = new UnidadeDeTrabalhoFake();

        // O repositório real carrega o alimento de cada item (Include); o fake faz igual.
        cardapios.ResolverAlimento = id => alimentos.Obter(id);

        var unidadeA = escolas.AddUnidade(new UnidadeEscolar("Unidade A")).Id;
        var unidadeB = escolas.AddUnidade(new UnidadeEscolar("Unidade B")).Id;
        var escolaA = escolas.Add(new Escola("Escola de teste", unidadeA)).Id;

        var nutricionistaA = new Usuario(
            "Nutricionista A",
            "11111111111",
            new DateOnly(1990, 1, 1),
            "nutri.a@teste.com",
            PapelUsuario.Nutricionista,
            null,
            unidadeA,
            "CRN-3 12345");
        usuarios.Add(nutricionistaA);

        var nutricionistaB = new Usuario(
            "Nutricionista B",
            "22222222222",
            new DateOnly(1990, 1, 1),
            "nutri.b@teste.com",
            PapelUsuario.Nutricionista,
            null,
            unidadeB,
            "CRN-3 54321");
        usuarios.Add(nutricionistaB);

        var servico = new CardapioService(
            cardapios,
            alimentos,
            alunos,
            escolas,
            usuarios,
            restricoes,
            unidadeDeTrabalho,
            new Ntc.Application.Seguranca.AutorizacaoService());

        return new Cenario(
            servico,
            cardapios,
            alimentos,
            alunos,
            restricoes,
            unidadeDeTrabalho,
            unidadeA,
            unidadeB,
            escolaA);
    }

    private static Alimento CriarAlimento(AlimentoRepositoryFake alimentos, string nome) =>
        alimentos.Add(new Alimento(
            nome,
            1,
            ValoresNutricionais.Criar(130, 2.7m, 28.2m, 0.3m, 0.4m, 1),
            null));

    /// <summary>
    /// Alimento com vínculo a uma restrição. O fake não resolve o `Include` do vínculo
    /// (a navegação <c>Restricao</c>), então ela é definida manualmente para a análise
    /// enxergar nome e categoria — o mesmo que o repositório real carrega.
    /// </summary>
    private static Alimento CriarAlimentoComRestricao(
        AlimentoRepositoryFake alimentos,
        string nome,
        string restricao,
        CategoriaRestricao categoria)
    {
        var catalogo = new RestricaoAlimentar(restricao, categoria);
        var alimento = alimentos.Add(new Alimento(
            nome,
            1,
            ValoresNutricionais.Criar(130, 2.7m, 28.2m, 0.3m, 0.4m, 1),
            new[] { 1 }));

        foreach (var vinculo in alimento.Vinculos)
        {
            PropriedadesPrivadas.Definir(vinculo, nameof(AlimentoRestricao.Restricao), catalogo);
        }

        return alimento;
    }

    [Fact]
    public void Criar_ComUnidadeInexistente_DevolveInvalido()
    {
        var cenario = Montar();
        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");

        var resultado = cenario.Servico.Criar(Entrada(
            unidadeId: 999,
            itensDaRefeicao: ("Almoço", 1, arroz.Id, 100m)), Nutricao);

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Contains("Unidade", resultado.Erro!);
    }

    [Fact]
    public void Criar_ComAlimentoInexistente_DevolveInvalido()
    {
        var cenario = Montar();

        var resultado = cenario.Servico.Criar(Entrada(
            cenario.UnidadeA,
            itensDaRefeicao: ("Almoço", 1, 999, 100m)), Nutricao);

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Contains("não encontrado", resultado.Erro!);
    }

    [Fact]
    public void Criar_ComAlimentoDesativado_DevolveInvalido()
    {
        var cenario = Montar();
        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");
        arroz.Desativar();
        cenario.Alimentos.Update(arroz);

        var resultado = cenario.Servico.Criar(Entrada(
            cenario.UnidadeA,
            itensDaRefeicao: ("Almoço", 1, arroz.Id, 100m)), Nutricao);

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Contains("desativado", resultado.Erro!);
    }

    [Fact]
    public void Criar_Valido_DevolveTotaisDoDominio()
    {
        var cenario = Montar();
        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");

        var resultado = cenario.Servico.Criar(Entrada(
            cenario.UnidadeA,
            itensDaRefeicao: ("Almoço", 1, arroz.Id, 100m)), Nutricao);

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.Equal(SituacaoCardapio.Rascunho, resultado.Valor!.Situacao);
        Assert.Equal(130m, resultado.Valor.Total.CaloriasKcal);
        Assert.Equal(130m, resultado.Valor.Refeicoes[0].Total.CaloriasKcal);
    }

    [Fact]
    public void Listar_ComoMerendeiraSemUnidade_DevolveProibido()
    {
        var cenario = Montar();

        var resultado = cenario.Servico.Listar(
            null, null, null, null, null,
            new EscopoUsuario(4, PapelUsuario.Merendeira, 1, null));

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
    }

    [Fact]
    public void Listar_ComoMerendeiraDeOutraUnidade_DevolveProibido()
    {
        var cenario = Montar();

        var resultado = cenario.Servico.Listar(
            cenario.UnidadeA, null, null, null, null, Merendeira(cenario.UnidadeB, cenario.EscolaA));

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
    }

    [Fact]
    public void Listar_ComoMerendeiraDaUnidade_DevolvePropriosCardapios()
    {
        var cenario = Montar();
        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");

        cenario.Servico.Criar(Entrada(cenario.UnidadeA, itensDaRefeicao: ("Almoço", 1, arroz.Id, 100m)), Nutricao);
        cenario.Servico.Criar(
            Entrada(cenario.UnidadeB, nutricionistaId: 2, itensDaRefeicao: ("Jantar", 1, arroz.Id, 100m)),
            NutricaoDaUnidadeB);

        var resultado = cenario.Servico.Listar(
            null, null, null, null, null, Merendeira(cenario.UnidadeA, cenario.EscolaA));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.Equal(1, resultado.Valor!.Total);
        Assert.Equal(cenario.UnidadeA, resultado.Valor.Itens[0].UnidadeId);
    }

    [Fact]
    public void Listar_ContaBloqueantesEAlertasDoCardapio()
    {
        var cenario = Montar();
        var iogurte = CriarAlimentoComRestricao(
            cenario.Alimentos, "Iogurte", "Lactose", CategoriaRestricao.Alergia);

        var aluno = new Aluno(
            "Ana",
            new DateOnly(2015, 5, 20),
            new Turma("Escola Municipal", "5 ano"),
            null,
            null,
            new[] { "Lactose" },
            false,
            new Responsavel("Ana Silva", "11988888888"),
            cenario.EscolaA);
        aluno.DefinirUnidade(cenario.UnidadeA);
        cenario.Alunos.Add(aluno);

        cenario.Servico.Criar(
            Entrada(cenario.UnidadeA, itensDaRefeicao: ("Almoço", 1, iogurte.Id, 100m)),
            Nutricao);

        var resultado = cenario.Servico.Listar(null, null, null, null, null, Nutricao);

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);

        var resumo = Assert.Single(resultado.Valor!.Itens);
        Assert.Equal(1, resumo.TotalBloqueantes);
        Assert.Equal(0, resumo.TotalAlertas);
    }

    [Fact]
    public void Listar_ComSituacaoInvalida_DevolveInvalido()
    {
        var cenario = Montar();

        var resultado = cenario.Servico.Listar(null, null, "Encerrado", null, null, Nutricao);

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Contains("Situação inválida", resultado.Erro!);
    }

    [Fact]
    public void Obter_ComoMerendeiraDeOutraUnidade_DevolveProibido()
    {
        var cenario = Montar();
        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");
        var criado = cenario.Servico
            .Criar(Entrada(cenario.UnidadeA, itensDaRefeicao: ("Almoço", 1, arroz.Id, 100m)), Nutricao)
            .Valor!;

        var resultado = cenario.Servico.Obter(criado.Id, Merendeira(cenario.UnidadeB, cenario.EscolaA));

        Assert.Equal(ResultadoStatus.NaoEncontrado, resultado.Status);
    }

    [Fact]
    public void Obter_ComoMerendeiraDaUnidade_DevolveDetalheComAlimento()
    {
        var cenario = Montar();
        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");
        var criado = cenario.Servico
            .Criar(Entrada(cenario.UnidadeA, itensDaRefeicao: ("Almoço", 1, arroz.Id, 100m)), Nutricao)
            .Valor!;

        var resultado = cenario.Servico.Obter(criado.Id, Merendeira(cenario.UnidadeA, cenario.EscolaA));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.Equal("Arroz", resultado.Valor!.Refeicoes[0].Itens[0].Alimento);
    }

    [Fact]
    public void Atualizar_ComAlimentoDesativadoQueJaEstava_Aceita()
    {
        var cenario = Montar();
        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");
        var criado = cenario.Servico
            .Criar(Entrada(cenario.UnidadeA, itensDaRefeicao: ("Almoço", 1, arroz.Id, 100m)), Nutricao)
            .Valor!;

        arroz.Desativar();
        cenario.Alimentos.Update(arroz);

        var resultado = cenario.Servico.Atualizar(
            criado.Id,
            Entrada(cenario.UnidadeA, nome: "Semana 13", itensDaRefeicao: ("Almoço", 1, arroz.Id, 120m)), Nutricao);

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.Equal("Semana 13", resultado.Valor!.Nome);
        Assert.Equal(156m, resultado.Valor.Total.CaloriasKcal);
    }

    [Fact]
    public void Atualizar_ComAlimentoDesativadoNovo_DevolveInvalido()
    {
        var cenario = Montar();
        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");
        var feijao = CriarAlimento(cenario.Alimentos, "Feijão");
        var criado = cenario.Servico
            .Criar(Entrada(cenario.UnidadeA, itensDaRefeicao: ("Almoço", 1, arroz.Id, 100m)), Nutricao)
            .Valor!;

        feijao.Desativar();
        cenario.Alimentos.Update(feijao);

        var resultado = cenario.Servico.Atualizar(
            criado.Id,
            Entrada(cenario.UnidadeA, itensDaRefeicao: ("Almoço", 1, feijao.Id, 100m)), Nutricao);

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Contains("desativado", resultado.Erro!);
    }

    [Fact]
    public void Excluir_CardapioPublicado_DevolveConflito()
    {
        var cenario = Montar();
        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");
        var criado = cenario.Servico
            .Criar(Entrada(cenario.UnidadeA, itensDaRefeicao: ("Almoço", 1, arroz.Id, 100m)), Nutricao)
            .Valor!;

        var armazenado = cenario.Cardapios.ObterComRefeicoes(criado.Id)!;
        PropriedadesPrivadas.Definir(
            armazenado, nameof(Cardapio.Situacao), SituacaoCardapio.Publicado);

        var resultado = cenario.Servico.Excluir(criado.Id, Nutricao);

        Assert.Equal(ResultadoStatus.Conflito, resultado.Status);
        Assert.Single(cenario.Cardapios.GetCardapios());
    }

    [Fact]
    public void Excluir_Rascunho_RemoveDoRepositorio()
    {
        var cenario = Montar();
        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");
        var criado = cenario.Servico
            .Criar(Entrada(cenario.UnidadeA, itensDaRefeicao: ("Almoço", 1, arroz.Id, 100m)), Nutricao)
            .Valor!;

        var resultado = cenario.Servico.Excluir(criado.Id, Nutricao);

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.Empty(cenario.Cardapios.GetCardapios());
    }

    [Fact]
    public void Duplicar_CopiaComposicaoComNovoPeriodoEZeraSituacao()
    {
        var cenario = Montar();
        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");
        var origem = cenario.Servico
            .Criar(Entrada(cenario.UnidadeA, itensDaRefeicao: ("Almoço", 1, arroz.Id, 100m)), Nutricao)
            .Valor!;

        var resultado = cenario.Servico.Duplicar(origem.Id, new DuplicarCardapioDTO
        {
            Nome = "Semana 13",
            Periodo = new PeriodoCardapioDTO
            {
                Inicio = new DateOnly(2026, 11, 9),
                Fim = new DateOnly(2026, 11, 13)
            }
        }, Nutricao);

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);

        var copia = resultado.Valor!;

        Assert.NotEqual(origem.Id, copia.Id);
        Assert.Equal("Semana 13", copia.Nome);
        Assert.Equal(SituacaoCardapio.Rascunho, copia.Situacao);
        Assert.Equal(origem.Id, copia.CardapioDuplicadoDeId);
        Assert.Equal(new DateOnly(2026, 11, 9), copia.Periodo.Inicio);
        Assert.Equal(130m, copia.Total.CaloriasKcal);
        Assert.Equal("Arroz", copia.Refeicoes[0].Itens[0].Alimento);
    }

    [Fact]
    public void Duplicar_CardapioInexistente_DevolveNaoEncontrado()
    {
        var cenario = Montar();

        var resultado = cenario.Servico.Duplicar(999, new DuplicarCardapioDTO
        {
            Periodo = new PeriodoCardapioDTO
            {
                Inicio = new DateOnly(2026, 11, 9),
                Fim = new DateOnly(2026, 11, 13)
            }
        }, Nutricao);

        Assert.Equal(ResultadoStatus.NaoEncontrado, resultado.Status);
    }
}
