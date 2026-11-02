namespace Ntc.Application;

/// <summary>
/// Token temporário recém-gerado pelo painel. O valor em claro aparece **uma única vez**,
/// nesta resposta: o banco guarda apenas o hash.
/// </summary>
public sealed record TokenDeCadastroGerado(string Token, DateTime ExpiraEm);

/// <summary>
/// Estado do token da unidade, para o painel desenhar botão, contagem regressiva e
/// bloqueio. Nunca devolve o valor do token.
/// </summary>
public sealed record TokenDeCadastroAtual(bool Ativo, DateTime? ExpiraEm);
