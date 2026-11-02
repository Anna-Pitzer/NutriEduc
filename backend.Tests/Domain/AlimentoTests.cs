using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Invariantes de composição nutricional (Spec 011, RF3 e RNF2): nenhuma medida aceita
/// negativo nem mais de duas casas decimais, e o zero é válido.
/// </summary>
public class ValoresNutricionaisTests
{
    [Fact]
    public void Criar_ComValoresValidos_PreservaAsSeisMedidas()
    {
        var valores = ValoresNutricionais.Criar(130, 2.7m, 28.2m, 0.3m, 0.4m, 1);

        Assert.Equal(130m, valores.CaloriasKcal);
        Assert.Equal(2.7m, valores.ProteinasG);
        Assert.Equal(28.2m, valores.CarboidratosG);
        Assert.Equal(0.3m, valores.GordurasG);
        Assert.Equal(0.4m, valores.FibrasG);
        Assert.Equal(1m, valores.SodioMg);
    }

    [Fact]
    public void Criar_ComValorNegativo_Lanca()
    {
        var erro = Assert.Throws<ArgumentException>(
            () => ValoresNutricionais.Criar(130, -1, 28.2m, 0.3m, 0.4m, 1));

        Assert.Contains("negativo", erro.Message);
    }

    [Fact]
    public void Criar_ComTresCasasDecimais_Lanca()
    {
        var erro = Assert.Throws<ArgumentException>(
            () => ValoresNutricionais.Criar(130, 2.7m, 28.235m, 0.3m, 0.4m, 1));

        Assert.Contains("duas casas decimais", erro.Message);
    }

    [Fact]
    public void Criar_ComZero_EPermitido()
    {
        var valores = ValoresNutricionais.Criar(0, 0, 0, 0, 0, 0);

        Assert.Equal(0m, valores.CaloriasKcal);
    }
}

/// <summary>
/// Invariantes do agregado Alimento (Spec 011, RF2, RF4 e RF5): normalização do nome,
/// vínculo único por restrição e atualização que não deixa estado parcial.
/// </summary>
public class AlimentoTests
{
    private static ValoresNutricionais Valores() =>
        ValoresNutricionais.Criar(130, 2.7m, 28.2m, 0.3m, 0.4m, 1);

    [Fact]
    public void Construtor_NormalizaNome()
    {
        var alimento = new Alimento("Açúcar Refinado", 1, Valores(), restricaoIds: null);

        Assert.Equal("Açúcar Refinado", alimento.Nome);
        Assert.Equal("acucar refinado", alimento.NomeNormalizado);
        Assert.True(alimento.Ativo);
        Assert.Empty(alimento.Vinculos);
    }

    [Fact]
    public void Construtor_ComCategoriaInvalida_Lanca()
    {
        Assert.Throws<ArgumentException>(
            () => new Alimento("Arroz", 0, Valores(), restricaoIds: null));
    }

    [Fact]
    public void Construtor_ComValoresNulos_Lanca()
    {
        Assert.Throws<ArgumentException>(
            () => new Alimento("Arroz", 1, valores: null!, restricaoIds: null));
    }

    [Fact]
    public void Construtor_ComVinculoRepetido_Lanca()
    {
        var erro = Assert.Throws<ArgumentException>(
            () => new Alimento("Arroz", 1, Valores(), new[] { 3, 3 }));

        Assert.Contains("já está vinculada", erro.Message);
    }

    [Fact]
    public void Construtor_ComVinculoInvalido_Lanca()
    {
        Assert.Throws<ArgumentException>(
            () => new Alimento("Arroz", 1, Valores(), new[] { 0 }));
    }

    [Fact]
    public void PossuiVinculo_RespondePeloParGravado()
    {
        var alimento = new Alimento("Iogurte", 2, Valores(), new[] { 3, 5 });

        Assert.True(alimento.PossuiVinculo(3));
        Assert.False(alimento.PossuiVinculo(4));
        Assert.Equal(2, alimento.Vinculos.Count);
    }

    [Fact]
    public void AtualizarDados_Valido_TrocaNomeCategoriaValoresEVinculos()
    {
        var alimento = new Alimento("Arroz", 1, Valores(), new[] { 3 });
        var novos = ValoresNutricionais.Criar(120, 2.5m, 25.6m, 1, 1.6m, 2);

        alimento.AtualizarDados("Arroz integral", 2, novos, new[] { 4 });

        Assert.Equal("Arroz integral", alimento.Nome);
        Assert.Equal("arroz integral", alimento.NomeNormalizado);
        Assert.Equal(2, alimento.CategoriaId);
        Assert.Equal(120m, alimento.Valores.CaloriasKcal);
        Assert.True(alimento.PossuiVinculo(4));
        Assert.False(alimento.PossuiVinculo(3));
    }

    [Fact]
    public void AtualizarDados_Invalido_NaoAlteraNada()
    {
        var alimento = new Alimento("Arroz", 1, Valores(), new[] { 3 });

        Assert.Throws<ArgumentException>(
            () => alimento.AtualizarDados("Feijão", 2, Valores(), new[] { 4, 4 }));

        Assert.Equal("Arroz", alimento.Nome);
        Assert.Equal(1, alimento.CategoriaId);
        Assert.True(alimento.PossuiVinculo(3));
        Assert.False(alimento.PossuiVinculo(4));
    }

    [Fact]
    public void Desativar_E_Reativar_MudamSoOEstado()
    {
        var alimento = new Alimento("Arroz", 1, Valores(), null);

        alimento.Desativar();
        Assert.False(alimento.Ativo);

        alimento.Reativar();
        Assert.True(alimento.Ativo);
    }
}
