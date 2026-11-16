using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Invariantes e transições do feedback (Spec 018): limites de texto, resposta dupla e
/// encerramento somente após responder.
/// </summary>
public class FeedbackTests
{
    private static readonly DateTime Utc = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    private static Feedback Criar(
        string? assunto = "Solicito revisão do cardápio",
        string? mensagem = "A refeição de terça não atendeu as crianças.",
        int autorId = 3,
        string autorNome = "Paulo",
        PapelUsuario papel = PapelUsuario.Merendeira,
        int unidadeId = 10,
        int? escolaId = 20) =>
        new(assunto, mensagem, autorId, autorNome, papel, unidadeId, escolaId, Utc);

    private static Feedback CriarComAluno(int? alunoId, string? alunoNome) =>
        new(
            "Solicito revisão do cardápio",
            "A refeição de terça não atendeu as crianças.",
            3,
            "Paulo",
            PapelUsuario.Merendeira,
            10,
            20,
            alunoId,
            alunoNome,
            Utc);

    [Fact]
    public void Criacao_ComAluno_PersisteAssociacao()
    {
        var feedback = CriarComAluno(7, "Ana");

        Assert.Equal(7, feedback.AlunoId);
        Assert.Equal("Ana", feedback.AlunoNome);
    }

    [Fact]
    public void AlunoSemNome_Lanca()
    {
        Assert.Throws<ArgumentException>(() => CriarComAluno(7, null));
    }

    [Fact]
    public void NomeSemAluno_Lanca()
    {
        Assert.Throws<ArgumentException>(() => CriarComAluno(null, "Ana"));
    }

    [Fact]
    public void AlunoIdNaoPositivo_Lanca()
    {
        Assert.Throws<ArgumentException>(() => CriarComAluno(0, "Ana"));
    }

    [Fact]
    public void Criacao_IniciaAberta()
    {
        var feedback = Criar();

        Assert.Equal(StatusFeedback.Aberto, feedback.Status);
        Assert.Equal(10, feedback.UnidadeId);
        Assert.Equal(20, feedback.EscolaId);
        Assert.Equal("Paulo", feedback.AutorNome);
        Assert.Equal(PapelUsuario.Merendeira, feedback.PapelAutor);
    }

    [Fact]
    public void EscolaId_NaoPositivo_ViraNulo()
    {
        Assert.Null(Criar(escolaId: null).EscolaId);
        Assert.Null(Criar(escolaId: 0).EscolaId);
    }

    [Fact]
    public void Assunto_Curto_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(assunto: "abc"));
    }

    [Fact]
    public void Mensagem_Curta_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(mensagem: "abc"));
    }

    [Fact]
    public void Responder_FluxoValido_MarcaRespondido()
    {
        var feedback = Criar();

        feedback.Responder("Ajuste realizado para a próxima semana.", "Nutri", Utc.AddMinutes(1));

        Assert.Equal(StatusFeedback.Respondido, feedback.Status);
        Assert.Equal("Nutri", feedback.RespondidoPor);
    }

    [Fact]
    public void Responder_DuasVezes_Lanca()
    {
        var feedback = Criar();
        feedback.Responder("Ajuste realizado para a próxima semana.", "Nutri", Utc.AddMinutes(1));

        Assert.Throws<TransicaoInvalidaException>(() =>
            feedback.Responder("Outra resposta válida.", "Nutri", Utc.AddMinutes(2)));
    }

    [Fact]
    public void Resposta_Curta_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            Criar().Responder("abc", "Nutri", Utc.AddMinutes(1)));
    }

    [Fact]
    public void Encerrar_Aberto_Lanca()
    {
        Assert.Throws<TransicaoInvalidaException>(() =>
            Criar().Encerrar("Paulo", Utc.AddMinutes(1)));
    }

    [Fact]
    public void Encerrar_Respondido_MarcaEncerrado()
    {
        var feedback = Criar();
        feedback.Responder("Ajuste realizado.", "Nutri", Utc.AddMinutes(1));

        feedback.Encerrar("Paulo", Utc.AddMinutes(2));

        Assert.Equal(StatusFeedback.Encerrado, feedback.Status);
    }
}
