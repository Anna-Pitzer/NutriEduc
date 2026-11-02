using Backend.Tests.Fakes;
using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Regras do caso de uso do catálogo (Spec 011): categoria ativa, restrições existentes e
/// ativas, nome único, política de inativos e leitura restrita a nutrição e merenda.
/// Todos os testes usam fakes — nenhum toca banco.
/// </summary>
public class AlimentoServiceTests
{
    private static readonly EscopoUsuario Nutricao =
        new(1, PapelUsuario.Nutricionista, null, null);

    private static readonly EscopoUsuario Merenda =
        new(2, PapelUsuario.Merendeira, null, null);

    private static readonly EscopoUsuario Secretaria =
        new(3, PapelUsuario.AdminSecretaria, null, null);

    private static SalvarAlimentoDTO Entrada(
        string nome,
        int categoriaId = 1,
        params int[] restricaoIds) => new()
        {
            Nome = nome,
            CategoriaId = categoriaId,
            Valores = new ValoresNutricionaisDTO
            {
                CaloriasKcal = 130,
                ProteinasG = 2.7m,
                CarboidratosG = 28.2m,
                GordurasG = 0.3m,
                FibrasG = 0.4m,
                SodioMg = 1
            },
            RestricaoIds = restricaoIds.ToList()
        };

    private static (AlimentoService Servico,
                    AlimentoRepositoryFake Alimentos,
                    CategoriaAlimentoRepositoryFake Categorias,
                    RestricaoRepositoryFake Restricoes) Montar()
    {
        var alimentos = new AlimentoRepositoryFake();
        var categorias = new CategoriaAlimentoRepositoryFake("Cereais", "Laticínios");
        var restricoes = new RestricaoRepositoryFake("Lactose", "Amendoim");

        var servico = new AlimentoService(
            alimentos,
            categorias,
            restricoes,
            new UnidadeDeTrabalhoFake());

        return (servico, alimentos, categorias, restricoes);
    }

    [Fact]
    public void Criar_ComDadosValidos_DevolveOkEResolveVinculos()
    {
        var (servico, alimentos, _, _) = Montar();

        var resultado = servico.Criar(Entrada("Arroz branco", restricaoIds: 1));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.NotNull(resultado.Valor);
        Assert.True(resultado.Valor!.Id > 0);
        Assert.Equal("Cereais", resultado.Valor.Categoria.Nome);
        Assert.Equal(130m, resultado.Valor.Valores.CaloriasKcal);

        var vinculo = Assert.Single(resultado.Valor.Vinculos);
        Assert.Equal("Lactose", vinculo.Restricao);
        Assert.Equal(CategoriaRestricao.Restricao, vinculo.Categoria);
        Assert.Single(alimentos.GetCatalogo());
    }

    [Fact]
    public void Criar_ComCategoriaInativa_DevolveInvalidoESemPersistencia()
    {
        var (servico, alimentos, categorias, _) = Montar();
        var categoria = categorias.Desativar("Laticínios");

        var resultado = servico.Criar(Entrada("Iogurte", categoria.Id));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Empty(alimentos.GetCatalogo());
    }

    [Fact]
    public void Criar_ComCategoriaInexistente_DevolveInvalido()
    {
        var (servico, _, _, _) = Montar();

        var resultado = servico.Criar(Entrada("Arroz", categoriaId: 999));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
    }

    [Fact]
    public void Criar_ComRestricaoInexistente_DevolveInvalido()
    {
        var (servico, alimentos, _, _) = Montar();

        var resultado = servico.Criar(Entrada("Arroz", restricaoIds: 999));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Empty(alimentos.GetCatalogo());
    }

    [Fact]
    public void Criar_ComRestricaoInativa_DevolveInvalido()
    {
        var (servico, _, _, restricoes) = Montar();
        var inativa = restricoes.Desativar("Lactose");

        var resultado = servico.Criar(Entrada("Arroz", restricaoIds: inativa.Id));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
    }

    [Fact]
    public void Criar_ComVinculoRepetido_DevolveInvalido()
    {
        var (servico, alimentos, _, _) = Montar();

        var resultado = servico.Criar(Entrada("Arroz", restricaoIds: new[] { 1, 1 }));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Empty(alimentos.GetCatalogo());
    }

    [Fact]
    public void Criar_ComNomeDuplicadoSobOutraCaixa_DevolveConflito()
    {
        var (servico, alimentos, _, _) = Montar();
        servico.Criar(Entrada("Arroz branco"));

        var resultado = servico.Criar(Entrada("ARROZ BRANCO"));

        Assert.Equal(ResultadoStatus.Conflito, resultado.Status);
        Assert.Single(alimentos.GetCatalogo());
    }

    [Fact]
    public void Criar_ComValorNegativo_Lanca()
    {
        var (servico, alimentos, _, _) = Montar();
        var entrada = Entrada("Arroz");
        entrada.Valores!.ProteinasG = -2;

        Assert.Throws<ArgumentException>(() => servico.Criar(entrada));
        Assert.Empty(alimentos.GetCatalogo());
    }

    [Fact]
    public void Criar_SemValores_Lanca()
    {
        var (servico, _, _, _) = Montar();
        var entrada = Entrada("Arroz");
        entrada.Valores = null;

        Assert.Throws<ArgumentException>(() => servico.Criar(entrada));
    }

    [Fact]
    public void Listar_IncluirInativos_PorMerendeira_DevolveProibido()
    {
        var (servico, _, _, _) = Montar();

        var resultado = servico.Listar(null, null, null, null, incluirInativos: true, Merenda);

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
    }

    [Fact]
    public void Listar_PorSecretaria_DevolveProibido()
    {
        var (servico, _, _, _) = Montar();

        var resultado = servico.Listar(null, null, null, null, incluirInativos: false, Secretaria);

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
    }

    [Fact]
    public void Listar_ComTamanhoForaDaFaixa_Lanca()
    {
        var (servico, _, _, _) = Montar();

        Assert.Throws<ArgumentException>(
            () => servico.Listar(null, null, null, 0, incluirInativos: false, Nutricao));
    }

    [Fact]
    public void Listar_SemInativos_NaoDevolveAlimentoDesativado()
    {
        var (servico, _, _, _) = Montar();
        var criado = servico.Criar(Entrada("Arroz branco")).Valor!;
        servico.Criar(Entrada("Feijão carioca"));

        servico.Desativar(criado.Id);

        var pagina = servico.Listar(null, null, null, null, incluirInativos: false, Nutricao);
        var comInativos = servico.Listar(null, null, null, null, incluirInativos: true, Nutricao);

        Assert.Equal(1, pagina.Valor!.Total);
        Assert.Equal(2, comInativos.Valor!.Total);
    }

    [Fact]
    public void Listar_ComBuscaPorPrefixo_Encontra()
    {
        var (servico, _, _, _) = Montar();
        servico.Criar(Entrada("Arroz branco"));
        servico.Criar(Entrada("Feijão carioca"));

        var pagina = servico.Listar("arr", null, null, null, incluirInativos: false, Nutricao);

        Assert.Equal(1, pagina.Valor!.Total);
        Assert.Equal("Arroz branco", pagina.Valor.Itens[0].Nome);
    }

    [Fact]
    public void Listar_ComBuscaQueNaoCasaPrefixo_UsaContem()
    {
        var (servico, _, _, _) = Montar();
        servico.Criar(Entrada("Leite de coco"));

        var pagina = servico.Listar("leite", null, null, null, incluirInativos: false, Nutricao);

        Assert.Equal(1, pagina.Valor!.Total);
    }

    [Fact]
    public void Listar_ComOrdenacaoNomeDescendente_DevolveDeZParaA()
    {
        var (servico, _, _, _) = Montar();
        servico.Criar(Entrada("Arroz branco"));
        servico.Criar(Entrada("Feijão carioca"));

        var pagina = servico.Listar(
            null, null, null, null, incluirInativos: false, Nutricao, ordenar: "nome-desc");

        Assert.Equal(ResultadoStatus.Ok, pagina.Status);
        Assert.Equal(
            new[] { "Feijão carioca", "Arroz branco" },
            pagina.Valor!.Itens.Select(alimento => alimento.Nome));
    }

    [Fact]
    public void Listar_ComOrdenacaoDesconhecida_DevolveInvalido()
    {
        var (servico, _, _, _) = Montar();
        servico.Criar(Entrada("Arroz branco"));

        var pagina = servico.Listar(
            null, null, null, null, incluirInativos: false, Nutricao, ordenar: "calorias");

        Assert.Equal(ResultadoStatus.Invalido, pagina.Status);
        Assert.Contains("nome-desc", pagina.Erro!);
    }

    [Fact]
    public void Obter_Inexistente_DevolveNaoEncontrado()
    {
        var (servico, _, _, _) = Montar();

        var resultado = servico.Obter(42, Nutricao);

        Assert.Equal(ResultadoStatus.NaoEncontrado, resultado.Status);
    }

    [Fact]
    public void Atualizar_ComNomeDeOutroAlimento_DevolveConflitoENaoAltera()
    {
        var (servico, _, _, _) = Montar();
        var primeiro = servico.Criar(Entrada("Arroz branco")).Valor!;
        servico.Criar(Entrada("Feijão carioca"));

        var resultado = servico.Atualizar(primeiro.Id, Entrada("Feijão carioca"));

        Assert.Equal(ResultadoStatus.Conflito, resultado.Status);

        var recarregado = servico.Obter(primeiro.Id, Nutricao).Valor!;
        Assert.Equal("Arroz branco", recarregado.Nome);
    }

    [Fact]
    public void CriarCategoria_ComNomeDuplicado_DevolveConflito()
    {
        var (servico, _, _, _) = Montar();

        var resultado = servico.CriarCategoria(new SalvarCategoriaAlimentoDTO { Nome = "cereais" });

        Assert.Equal(ResultadoStatus.Conflito, resultado.Status);
    }

    [Fact]
    public void ListarCategorias_PorMerendeira_NaoDevolveInativas()
    {
        var (servico, _, categorias, _) = Montar();
        categorias.Desativar("Laticínios");

        var ativas = servico.ListarCategorias(incluirInativas: false, Merenda);
        var comInativas = servico.ListarCategorias(incluirInativas: true, Nutricao);

        Assert.Single(ativas.Valor!);
        Assert.Equal(2, comInativas.Valor!.Count);
    }
}
