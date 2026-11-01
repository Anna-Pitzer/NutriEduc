namespace Ntc.Application;

/// <summary>
/// Contrato de entrada do autocadastro de usuário (Spec 024, F1). Separado de
/// <see cref="UsuarioRespostaDTO"/>: senha existe aqui e não existe lá.
///
/// Os campos são anuláveis porque o binding de JSON pode omiti-los; quem recusa valor
/// ausente é o domínio, com a mensagem da regra.
/// </summary>
public sealed record CriarUsuarioEntrada(
    string? Nome,
    string? Cpf,
    DateOnly DataNascimento,
    string? Email,
    string? Senha,
    string? Telefone,
    string? Foto);
