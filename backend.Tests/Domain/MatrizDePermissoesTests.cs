using Ntc.Application.Seguranca;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Conformidade da matriz de permissões (Spec 028, RF4/RNF1/RNF4). Função pura, sem IO.
/// </summary>
public class MatrizDePermissoesTests
{
    [Fact]
    public void Matriz_TabelaEDocumento_Concordam()
    {
        foreach (var recurso in Enum.GetValues<Recurso>())
        {
            foreach (var papel in Enum.GetValues<PapelUsuario>())
            {
                var esperado = MatrizDePermissoes.Documentada
                    .GetValueOrDefault((recurso, papel), NivelDeAcesso.Nenhum);

                Assert.Equal(esperado, MatrizDePermissoes.Nivel(recurso, papel));
            }
        }
    }

    [Fact]
    public void Matriz_FuncaoPura_NegaPorPadrao()
    {
        Assert.Equal(NivelDeAcesso.Nenhum,
            MatrizDePermissoes.Nivel(Recurso.Diagnostico, PapelUsuario.Merendeira));
        Assert.Equal(NivelDeAcesso.Nenhum,
            MatrizDePermissoes.Nivel(Recurso.SolicitacaoDeAcesso, PapelUsuario.AdminEscola));
        Assert.Equal(NivelDeAcesso.Nenhum,
            MatrizDePermissoes.Nivel(Recurso.PapelDoUsuario, PapelUsuario.Nutricionista));
    }

    [Theory]
    [InlineData(NivelDeAcesso.Nenhum, false, false, false, false)]
    [InlineData(NivelDeAcesso.Solicitar, true, false, false, false)]
    [InlineData(NivelDeAcesso.Ler, true, false, false, false)]
    [InlineData(NivelDeAcesso.Escrita, false, true, true, true)]
    [InlineData(NivelDeAcesso.LerCriar, true, true, false, false)]
    [InlineData(NivelDeAcesso.LerEscrever, true, true, true, true)]
    public void Nivel_TraduzOperacoesSemDependerDaOrdemNumerica(
        NivelDeAcesso nivel,
        bool leitura,
        bool criacao,
        bool atualizacao,
        bool exclusao)
    {
        Assert.Equal(leitura, nivel.PermiteLeitura());
        Assert.Equal(criacao, nivel.PermiteCriacao());
        Assert.Equal(atualizacao, nivel.PermiteAtualizacao());
        Assert.Equal(exclusao, nivel.Permite(OperacaoDeAcesso.Excluir));
    }

    [Fact]
    public void Matriz_NaoTemCelulaNenhumDocumentada()
    {
        Assert.All(
            MatrizDePermissoes.Documentada,
            celula => Assert.NotEqual(NivelDeAcesso.Nenhum, celula.Value));
    }

    [Fact]
    public void Matriz_Solicitacao_RespeitaDecisaoSeparadaPorTipo()
    {
        // Spec 028 (linha "Solicitações 017/030") e Spec 017 (T10/RF5): a decisão pertence
        // ao dono do tipo (Nutricionista para cardápio/ingrediente, AdminSecretaria para
        // restrição/cadastro). Merendeira e Responsável apenas criam e cancelam as próprias,
        // nunca decidem — portanto LerCriar, não LerEscrever.
        Assert.Equal(NivelDeAcesso.LerEscrever,
            MatrizDePermissoes.Nivel(Recurso.Solicitacao, PapelUsuario.AdminSecretaria));
        Assert.Equal(NivelDeAcesso.LerCriar,
            MatrizDePermissoes.Nivel(Recurso.Solicitacao, PapelUsuario.AdminEscola));
        Assert.Equal(NivelDeAcesso.LerEscrever,
            MatrizDePermissoes.Nivel(Recurso.Solicitacao, PapelUsuario.Nutricionista));
        Assert.Equal(NivelDeAcesso.LerCriar,
            MatrizDePermissoes.Nivel(Recurso.Solicitacao, PapelUsuario.Merendeira));
        Assert.Equal(NivelDeAcesso.LerCriar,
            MatrizDePermissoes.Nivel(Recurso.Solicitacao, PapelUsuario.Responsavel));
    }
}
