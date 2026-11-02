namespace Ntc.Application;

/// <summary>
/// Desfecho de uma operação de serviço que pode ser negada por escopo. Existe para que
/// a decisão de acesso fique no serviço (RNF1) e o controller apenas traduza o status
/// em HTTP, sem lançar exceção como fluxo normal.
/// </summary>
public enum ResultadoStatus
{
    Ok,
    Invalido,
    Proibido,
    NaoEncontrado,
    Conflito
}

public sealed record Resultado<T>(ResultadoStatus Status, T? Valor, string? Erro)
{
    public static Resultado<T> Ok(T valor) => new(ResultadoStatus.Ok, valor, null);

    /// <summary>Entrada recusada por regra de negócio: responde 400 com a mensagem do serviço.</summary>
    public static Resultado<T> Invalido(string erro) =>
        new(ResultadoStatus.Invalido, default, erro);

    public static Resultado<T> Proibido() =>
        new(ResultadoStatus.Proibido, default, "Acesso negado.");

    public static Resultado<T> Proibido(string erro) =>
        new(ResultadoStatus.Proibido, default, erro);

    public static Resultado<T> NaoEncontrado(string erro) =>
        new(ResultadoStatus.NaoEncontrado, default, erro);

    public static Resultado<T> Conflito(string erro) =>
        new(ResultadoStatus.Conflito, default, erro);
}
