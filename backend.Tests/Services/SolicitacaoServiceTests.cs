using Backend.Tests.Fakes;
using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Casos de uso da solicitação (Spec 017): criação por papel e vínculo, decisão separada
/// por tipo, efeito da restrição no aluno e ausência de efeito no cardápio.
/// </summary>
public class SolicitacaoServiceTests
{
    private sealed record Cenario(
        SolicitacaoService Servico,
        SolicitacaoRepositoryFake Solicitacoes,
        NotificacaoRepositoryFake Notificacoes,
        AlunoRepositoryFake Alunos,
        CardapioRepositoryFake Cardapios,
        int UnidadeId,
        int EscolaId,
        int AlunoId,
        int CardapioId);

    private static readonly DateTime Inicio = new(2026, 11, 2);
    private static readonly DateTime Fim = new(2026, 11, 6);

    private static EscopoUsuario Admin(int unidadeId) =>
        new(1, PapelUsuario.AdminSecretaria, null, unidadeId);

    private static EscopoUsuario Nutricionista(int unidadeId) =>
        new(2, PapelUsuario.Nutricionista, null, unidadeId);

    private static EscopoUsuario Responsavel(int unidadeId) =>
        new(3, PapelUsuario.Responsavel, null, unidadeId);

    private static EscopoUsuario Merendeira(int unidadeId, int escolaId) =>
        new(4, PapelUsuario.Merendeira, escolaId, unidadeId);

    private static EscopoUsuario AdminEscola(int unidadeId, int escolaId) =>
        new(5, PapelUsuario.AdminEscola, escolaId, unidadeId);

    private static Cenario Montar()
    {
        var solicitacoes = new SolicitacaoRepositoryFake();
        var notificacoes = new NotificacaoRepositoryFake();
        var alunos = new AlunoRepositoryFake();
        var cardapios = new CardapioRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var usuarios = new UsuarioRepositoryFake();
        var restricoes = new RestricaoRepositoryFake();
        var unidadeDeTrabalho = new UnidadeDeTrabalhoFake();
        var autorizacao = new AutorizacaoService();
        var trilha = new TrilhaAcaoRepositoryFake();
        var vinculos = new VinculoRepositoryFake();
        var notificacaoService = new NotificacaoService(notificacoes, unidadeDeTrabalho);

        var unidadeId = escolas.AddUnidade(new UnidadeEscolar("Unidade A")).Id;
        var escolaId = escolas.Add(new Escola("Escola A", unidadeId)).Id;

        usuarios.Add(new Usuario(
            "Admin", "11111111111", new DateOnly(1990, 1, 1), "admin@teste.com",
            PapelUsuario.AdminSecretaria, null, unidadeId));

        usuarios.Add(new Usuario(
            "Nutricionista", "22222222222", new DateOnly(1990, 1, 1), "nutri@teste.com",
            PapelUsuario.Nutricionista, null, unidadeId, "CRN-3 12345"));

        usuarios.Add(new Usuario(
            "Responsável", "33333333333", new DateOnly(1990, 1, 1), "resp@teste.com",
            PapelUsuario.Responsavel, null, unidadeId));

        usuarios.Add(new Usuario(
            "Merendeira", "44444444444", new DateOnly(1990, 1, 1), "merenda@teste.com",
            PapelUsuario.Merendeira, escolaId, unidadeId));

        usuarios.Add(new Usuario(
            "Admin Escola", "55555555555", new DateOnly(1990, 1, 1), "escola@teste.com",
            PapelUsuario.AdminEscola, escolaId, unidadeId));

        var aluno = new Aluno(
            "Ana",
            new DateOnly(2015, 5, 20),
            new Turma("Escola A", "5 ano"),
            null,
            null,
            null,
            false,
            new Responsavel("Responsável", "11988888888"),
            escolaId);

        aluno.DefinirUnidade(unidadeId);
        alunos.Add(aluno);
        alunos.Vinculos.Add((3, aluno.Id));
        solicitacoes.Vinculos.Add((3, aluno.Id));

        restricoes.Add(new RestricaoAlimentar("Lactose", CategoriaRestricao.Alergia));
        restricoes.Add(new RestricaoAlimentar("Gluten", CategoriaRestricao.Restricao));

        var cardapio = new Cardapio(
            "Semana 12",
            unidadeId,
            new PeriodoVigencia(DateOnly.FromDateTime(Inicio), DateOnly.FromDateTime(Fim)),
            Array.Empty<Refeicao>());

        cardapios.Add(cardapio);

        var hierarquia = new HierarquiaService(escolas, alunos, usuarios);

        var alunoService = new AlunoService(
            alunos,
            restricoes,
            usuarios,
            hierarquia,
            autorizacao,
            trilha,
            unidadeDeTrabalho,
            vinculos,
            notificacaoService);

        var servico = new SolicitacaoService(
            solicitacoes,
            alunos,
            cardapios,
            escolas,
            usuarios,
            unidadeDeTrabalho,
            alunoService,
            notificacaoService);

        return new Cenario(
            servico,
            solicitacoes,
            notificacoes,
            alunos,
            cardapios,
            unidadeId,
            escolaId,
            aluno.Id,
            cardapio.Id);
    }

    private static CriarSolicitacaoDTO Restricao(int alunoId, string nome = "Lactose") => new()
    {
        Tipo = TipoSolicitacao.InclusaoAlergia,
        AlunoId = alunoId,
        RestricaoNome = nome,
        Descricao = "Ana apresentou reação leve após o lanche."
    };

    [Fact]
    public void Criar_Restricao_ComoResponsavelVinculado_CriaAberta()
    {
        var cenario = Montar();

        var resultado = cenario.Servico.Criar(
            Restricao(cenario.AlunoId),
            Responsavel(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.Equal(StatusSolicitacao.Aberta, resultado.Valor!.Status);
        Assert.Equal(cenario.UnidadeId, resultado.Valor.UnidadeId);
        Assert.Equal(cenario.EscolaId, resultado.Valor.EscolaId);
        Assert.Equal("Ana", resultado.Valor.Aluno);
    }

    [Fact]
    public void Criar_Restricao_ResponsavelSemVinculo_Proibido()
    {
        var cenario = Montar();
        cenario.Alunos.Vinculos.Clear();
        cenario.Solicitacoes.Vinculos.Clear();

        var resultado = cenario.Servico.Criar(
            Restricao(cenario.AlunoId),
            Responsavel(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
    }

    [Fact]
    public void Criar_DadosCadastrais_AdminEscola_Proibido()
    {
        var cenario = Montar();

        var resultado = cenario.Servico.Criar(
            new CriarSolicitacaoDTO
            {
                Tipo = TipoSolicitacao.DadosCadastrais,
                AlunoId = cenario.AlunoId,
                Descricao = "Corrigir o nome do aluno."
            },
            AdminEscola(cenario.UnidadeId, cenario.EscolaId));

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
    }

    [Fact]
    public void Criar_Ingrediente_SemIngrediente_Invalido()
    {
        var cenario = Montar();

        var resultado = cenario.Servico.Criar(
            new CriarSolicitacaoDTO
            {
                Tipo = TipoSolicitacao.Ingrediente,
                Descricao = "Falta o ingrediente do almoço."
            },
            Merendeira(cenario.UnidadeId, cenario.EscolaId));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Empty(cenario.Solicitacoes.GetItens());
    }

    [Fact]
    public void Aprovar_Restricao_AplicaNoAlunoEGravaTrilha()
    {
        var cenario = Montar();
        var criada = cenario.Servico
            .Criar(Restricao(cenario.AlunoId), Responsavel(cenario.UnidadeId))
            .Valor!;

        Assert.Equal(ResultadoStatus.Ok,
            cenario.Servico.Analisar(criada.Id, Admin(cenario.UnidadeId)).Status);

        var aprovada = cenario.Servico.Aprovar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Confirmado com a família." },
            Admin(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, aprovada.Status);
        Assert.Equal(StatusSolicitacao.Concluida, aprovada.Valor!.Status);

        var aluno = cenario.Alunos.GetAluno(cenario.AlunoId)!;
        Assert.Contains("Lactose", aluno.Restricoes);

        var evento = Assert.Single(aluno.Historico);
        Assert.Equal(TipoEventoAluno.RestricaoAdicionada, evento.Tipo);
        Assert.Equal("Lactose", evento.ValorNovo);
        Assert.Equal("Admin", evento.AutorNome);

        Assert.Equal(
            new[]
            {
                StatusSolicitacao.Aberta,
                StatusSolicitacao.EmAnalise,
                StatusSolicitacao.Aprovada,
                StatusSolicitacao.Concluida
            },
            aprovada.Valor.Historico.Select(item => item.Status));
    }

    [Fact]
    public void Aprovar_RestricaoForaDoCatalogo_MantemEmAnalise()
    {
        var cenario = Montar();
        var criada = cenario.Servico
            .Criar(Restricao(cenario.AlunoId, "Amendoim"), Responsavel(cenario.UnidadeId))
            .Valor!;

        cenario.Servico.Analisar(criada.Id, Admin(cenario.UnidadeId));

        var resultado = cenario.Servico.Aprovar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Confirmado." },
            Admin(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Equal(StatusSolicitacao.EmAnalise, cenario.Solicitacoes.GetItens().Single().Status);
        Assert.Empty(cenario.Alunos.GetAluno(cenario.AlunoId)!.Restricoes);
    }

    [Fact]
    public void Aprovar_Restricao_ComoNutricionista_Proibido()
    {
        var cenario = Montar();
        var criada = cenario.Servico
            .Criar(Restricao(cenario.AlunoId), Responsavel(cenario.UnidadeId))
            .Valor!;

        cenario.Servico.Analisar(criada.Id, Admin(cenario.UnidadeId));

        var resultado = cenario.Servico.Aprovar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Confirmado." },
            Nutricionista(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
        Assert.Equal(StatusSolicitacao.EmAnalise, cenario.Solicitacoes.GetItens().Single().Status);
    }

    [Fact]
    public void Aprovar_DuasVezes_DevolveConflito()
    {
        var cenario = Montar();
        var criada = cenario.Servico
            .Criar(Restricao(cenario.AlunoId), Responsavel(cenario.UnidadeId))
            .Valor!;

        cenario.Servico.Analisar(criada.Id, Admin(cenario.UnidadeId));
        cenario.Servico.Aprovar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Confirmado." },
            Admin(cenario.UnidadeId));

        var segunda = cenario.Servico.Aprovar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Confirmado de novo." },
            Admin(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Conflito, segunda.Status);
        Assert.Single(cenario.Alunos.GetAluno(cenario.AlunoId)!.Historico);
    }

    [Fact]
    public void Recusar_SemResposta_Invalido()
    {
        var cenario = Montar();
        var criada = cenario.Servico
            .Criar(Restricao(cenario.AlunoId), Responsavel(cenario.UnidadeId))
            .Valor!;

        cenario.Servico.Analisar(criada.Id, Admin(cenario.UnidadeId));

        var resultado = cenario.Servico.Recusar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = string.Empty },
            Admin(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Equal(StatusSolicitacao.EmAnalise, cenario.Solicitacoes.GetItens().Single().Status);
    }

    [Fact]
    public void Cancelar_PorOutroUsuario_Proibido()
    {
        var cenario = Montar();
        var criada = cenario.Servico
            .Criar(Restricao(cenario.AlunoId), Responsavel(cenario.UnidadeId))
            .Valor!;

        var resultado = cenario.Servico.Cancelar(criada.Id, Admin(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
        Assert.Equal(StatusSolicitacao.Aberta, cenario.Solicitacoes.GetItens().Single().Status);
    }

    [Fact]
    public void Aprovar_Cardapio_NaoAlteraCardapio_EAvisa()
    {
        var cenario = Montar();
        var antes = cenario.Cardapios.ObterComRefeicoes(cenario.CardapioId)!;

        var criada = cenario.Servico.Criar(
            new CriarSolicitacaoDTO
            {
                Tipo = TipoSolicitacao.MudancaCardapio,
                CardapioId = cenario.CardapioId,
                Descricao = "Trocar o arroz por macarrão na sexta."
            },
            Merendeira(cenario.UnidadeId, cenario.EscolaId)).Valor!;

        cenario.Servico.Analisar(criada.Id, Nutricionista(cenario.UnidadeId));
        var aprovada = cenario.Servico.Aprovar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Aplicar na próxima edição." },
            Nutricionista(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, aprovada.Status);
        Assert.Equal(StatusSolicitacao.Concluida, aprovada.Valor!.Status);
        Assert.Contains("Spec 015", aprovada.Valor.Aviso);

        var depois = cenario.Cardapios.ObterComRefeicoes(cenario.CardapioId)!;
        Assert.Equal(antes.Nome, depois.Nome);
        Assert.Equal(antes.Situacao, depois.Situacao);
        Assert.Equal(antes.Refeicoes.Count, depois.Refeicoes.Count);
    }

    [Fact]
    public void Listar_ResponsavelVePropriosEDosFilhos()
    {
        var cenario = Montar();

        cenario.Servico.Criar(
            Restricao(cenario.AlunoId),
            Responsavel(cenario.UnidadeId));

        cenario.Servico.Criar(
            new CriarSolicitacaoDTO
            {
                Tipo = TipoSolicitacao.MudancaCardapio,
                CardapioId = cenario.CardapioId,
                Descricao = "Trocar a sobremesa da quarta."
            },
            Nutricionista(cenario.UnidadeId));

        var pagina = cenario.Servico.Listar(
            null, null, null, null, null, null, null, null, null, null,
            Responsavel(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, pagina.Status);
        Assert.Equal(1, pagina.Valor!.Total);
        Assert.Equal(TipoSolicitacao.InclusaoAlergia, pagina.Valor.Itens[0].Tipo);
    }

    [Fact]
    public void Listar_NutricionistaNaoVeDadosCadastrais()
    {
        var cenario = Montar();

        var pagina = cenario.Servico.Listar(
            null, null, null, null, null, null, null, null, null, null,
            Nutricionista(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, pagina.Status);
        Assert.Empty(pagina.Valor!.Itens);
    }

    [Fact]
    public void Listar_MerendeiraNaoVeDadosCadastrais()
    {
        var cenario = Montar();

        cenario.Servico.Criar(
            Dados(cenario.AlunoId, ("nome", "Ana Maria")),
            Responsavel(cenario.UnidadeId));

        cenario.Servico.Criar(
            Restricao(cenario.AlunoId),
            Responsavel(cenario.UnidadeId));

        var pagina = cenario.Servico.Listar(
            null, null, null, null, null, null, null, null, null, null,
            Merendeira(cenario.UnidadeId, cenario.EscolaId));

        Assert.Equal(ResultadoStatus.Ok, pagina.Status);
        Assert.Single(pagina.Valor!.Itens);
        Assert.Equal(TipoSolicitacao.InclusaoAlergia, pagina.Valor.Itens[0].Tipo);
    }

    private static CriarSolicitacaoDTO Dados(
        int alunoId,
        params (string Campo, string Valor)[] alteracoes) => new()
    {
        Tipo = TipoSolicitacao.DadosCadastrais,
        AlunoId = alunoId,
        Justificativa = "Correção solicitada pela família.",
        Alteracoes = alteracoes
            .Select(alteracao => new AlteracaoSolicitadaDTO
            {
                Campo = alteracao.Campo,
                ValorSolicitado = alteracao.Valor
            })
            .ToList()
    };

    [Fact]
    public void Criar_DadosCadastrais_GravaSnapshotDoValorAtual()
    {
        var cenario = Montar();

        var criada = cenario.Servico.Criar(
            Dados(
                cenario.AlunoId,
                ("nome", "Ana Maria"),
                ("telefoneResponsavel", "11977776666")),
            Responsavel(cenario.UnidadeId)).Valor!;

        Assert.Equal(StatusSolicitacao.Aberta, criada.Status);
        Assert.Equal(2, criada.Alteracoes.Count);
        Assert.Equal(
            "Ana",
            criada.Alteracoes
                .Single(alteracao => alteracao.Campo == CampoDeDadoDoAluno.Nome)
                .ValorNoPedido);
        Assert.Equal(
            "Ana Maria",
            criada.Alteracoes
                .Single(alteracao => alteracao.Campo == CampoDeDadoDoAluno.Nome)
                .ValorSolicitado);
    }

    [Fact]
    public void Criar_DadosCadastrais_CampoForaDaLista_Invalido()
    {
        var cenario = Montar();

        var resultado = cenario.Servico.Criar(
            Dados(cenario.AlunoId, ("cpf", "12345678901")),
            Responsavel(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Contains("Aceitos", resultado.Erro!);
        Assert.Empty(cenario.Solicitacoes.GetItens());
    }

    [Fact]
    public void Criar_DadosCadastrais_SegundoPedidoMesmoCampo_Conflito()
    {
        var cenario = Montar();
        var primeiro = cenario.Servico.Criar(
            Dados(cenario.AlunoId, ("nome", "Ana Maria")),
            Responsavel(cenario.UnidadeId)).Valor!;

        var segundo = cenario.Servico.Criar(
            Dados(cenario.AlunoId, ("nome", "Ana Paula")),
            Responsavel(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Conflito, segundo.Status);
        Assert.Contains($"#{primeiro.Id}", segundo.Erro!);
        Assert.Single(cenario.Solicitacoes.GetItens());
    }

    [Fact]
    public void Criar_DadosCadastrais_SemVinculo_Proibido()
    {
        var cenario = Montar();
        cenario.Alunos.Vinculos.Clear();

        var resultado = cenario.Servico.Criar(
            Dados(cenario.AlunoId, ("nome", "Ana Maria")),
            Responsavel(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
        Assert.Empty(cenario.Solicitacoes.GetItens());
    }

    [Fact]
    public void Aprovar_DadosCadastrais_AplicaTodosOsCamposEGravaOrigem()
    {
        var cenario = Montar();
        var criada = cenario.Servico.Criar(
            Dados(
                cenario.AlunoId,
                ("nome", "Ana Maria"),
                ("telefoneResponsavel", "11977776666")),
            Responsavel(cenario.UnidadeId)).Valor!;

        cenario.Servico.Analisar(criada.Id, Admin(cenario.UnidadeId));

        var aprovada = cenario.Servico.Aprovar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Confirmado com a família." },
            Admin(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Ok, aprovada.Status);
        Assert.Equal(StatusSolicitacao.Concluida, aprovada.Valor!.Status);

        var aluno = cenario.Alunos.GetAluno(cenario.AlunoId)!;
        Assert.Equal("Ana Maria", aluno.Nome);
        Assert.Equal("11977776666", aluno.Responsavel.Telefone);
        Assert.NotEmpty(aluno.Historico);
        Assert.All(aluno.Historico, evento =>
            Assert.Equal($"Solicitacao:{criada.Id}", evento.Origem));
    }

    [Fact]
    public void Aprovar_DadosCadastrais_ValorInvalido_NaoAplicaNada()
    {
        var cenario = Montar();
        var criada = cenario.Servico.Criar(
            Dados(cenario.AlunoId, ("dataNascimento", "2099-01-01")),
            Responsavel(cenario.UnidadeId)).Valor!;

        cenario.Servico.Analisar(criada.Id, Admin(cenario.UnidadeId));

        var resultado = cenario.Servico.Aprovar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Confirmado." },
            Admin(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Equal(StatusSolicitacao.EmAnalise, cenario.Solicitacoes.GetItens().Single().Status);
        Assert.Equal("Ana", cenario.Alunos.GetAluno(cenario.AlunoId)!.Nome);
    }

    [Fact]
    public void Aprovar_DadosCadastrais_DivergenciaDoSnapshot_Conflito()
    {
        var cenario = Montar();
        var criada = cenario.Servico.Criar(
            Dados(cenario.AlunoId, ("nome", "Ana Maria")),
            Responsavel(cenario.UnidadeId)).Valor!;

        var aluno = cenario.Alunos.GetAluno(cenario.AlunoId)!;
        aluno.AtualizarDados(
            "Ana Alterada",
            aluno.Nascimento,
            aluno.Turma,
            aluno.Telefone,
            aluno.Observacao,
            aluno.Restricoes,
            aluno.Anafilaxia,
            aluno.Responsavel,
            aluno.UnidadeId,
            aluno.EscolaId!.Value);
        cenario.Alunos.UpdateAluno(aluno);

        cenario.Servico.Analisar(criada.Id, Admin(cenario.UnidadeId));

        var resultado = cenario.Servico.Aprovar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Confirmado." },
            Admin(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Conflito, resultado.Status);
        Assert.Contains("Nome", resultado.Erro!);
        Assert.Equal(StatusSolicitacao.EmAnalise, cenario.Solicitacoes.GetItens().Single().Status);
        Assert.Equal("Ana Alterada", cenario.Alunos.GetAluno(cenario.AlunoId)!.Nome);
    }

    [Fact]
    public void Recusar_DadosCadastrais_MotivoCurto_Invalido()
    {
        var cenario = Montar();
        var criada = cenario.Servico.Criar(
            Dados(cenario.AlunoId, ("nome", "Ana Maria")),
            Responsavel(cenario.UnidadeId)).Valor!;

        cenario.Servico.Analisar(criada.Id, Admin(cenario.UnidadeId));

        var resultado = cenario.Servico.Recusar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Não." },
            Admin(cenario.UnidadeId));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Equal(StatusSolicitacao.EmAnalise, cenario.Solicitacoes.GetItens().Single().Status);
        Assert.Equal("Ana", cenario.Alunos.GetAluno(cenario.AlunoId)!.Nome);
    }

    [Fact]
    public void Aprovar_DadosCadastrais_NotificaSemValorSolicitado()
    {
        var cenario = Montar();
        var criada = cenario.Servico.Criar(
            Dados(cenario.AlunoId, ("nome", "Ana Maria")),
            Responsavel(cenario.UnidadeId)).Valor!;

        cenario.Servico.Analisar(criada.Id, Admin(cenario.UnidadeId));
        cenario.Servico.Aprovar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Confirmado." },
            Admin(cenario.UnidadeId));

        var notificacoes = cenario.Notificacoes.GetItens();

        Assert.NotEmpty(notificacoes);
        Assert.All(notificacoes, notificacao =>
        {
            Assert.Equal(TipoNotificacao.SolicitacaoStatus, notificacao.Tipo);
            Assert.DoesNotContain("Ana Maria", notificacao.Mensagem);
            Assert.DoesNotContain("11988888888", notificacao.Mensagem);
        });
    }

    [Fact]
    public void Aprovar_Restricao_EmiteNotificacaoSemDadoClinico()
    {
        var cenario = Montar();
        var criada = cenario.Servico
            .Criar(Restricao(cenario.AlunoId), Responsavel(cenario.UnidadeId))
            .Valor!;

        cenario.Servico.Analisar(criada.Id, Admin(cenario.UnidadeId));
        cenario.Servico.Aprovar(
            criada.Id,
            new DecidirSolicitacaoDTO { Resposta = "Confirmado." },
            Admin(cenario.UnidadeId));

        var notificacoes = cenario.Notificacoes.GetItens();

        Assert.Equal(4, notificacoes.Count);
        Assert.All(notificacoes, notificacao =>
        {
            Assert.Equal(TipoNotificacao.SolicitacaoStatus, notificacao.Tipo);
            Assert.Equal("Solicitacao", notificacao.ReferenciaTipo);
            Assert.Equal(criada.Id, notificacao.ReferenciaId);
            Assert.DoesNotContain("Lactose", notificacao.Mensagem);
        });
        Assert.Contains(notificacoes, notificacao => notificacao.Mensagem.Contains("EmAnalise"));
        Assert.Contains(notificacoes, notificacao => notificacao.Mensagem.Contains("Concluida"));
    }
}
