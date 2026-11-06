namespace Ntc.Domain.Entity;

/// <summary>Tipo do evento registrado na trilha do aluno (Spec 023, RF1).</summary>
public enum TipoEventoAluno
{
    Criado,
    Alterado,
    RestricaoAdicionada,
    RestricaoRemovida
}

/// <summary>
/// Trilha append-only de alterações do aluno (Spec 023, ADR-016).
///
/// Substitui o antigo <c>HistoricoRestricaoAluno</c>: uma única tabela cobre criação,
/// alteração de qualquer campo e mudança de restrições. <see cref="AutorNome"/> e os
/// valores anterior/novo são snapshots — renomear o autor ou o catálogo não reescreve o
/// passado, e nenhuma FK liga o evento a eles (ADR-009).
/// </summary>
public class HistoricoAluno
{
    private HistoricoAluno()
    {
    }

    public int Id { get; private set; }

    public int AlunoId { get; private set; }

    public TipoEventoAluno Tipo { get; private set; }

    /// <summary>Nome do campo alterado; nulo em <see cref="TipoEventoAluno.Criado"/>.</summary>
    public string? Campo { get; private set; }

    public string? ValorAnterior { get; private set; }

    public string? ValorNovo { get; private set; }

    /// <summary>Nome de quem executou a alteração (snapshot), nunca uma FK de usuário.</summary>
    public string AutorNome { get; private set; } = string.Empty;

    public DateTime CriadoEmUtc { get; private set; }

    /// <summary>
    /// Origem do evento quando ele não nasce de edição direta; por exemplo,
    /// <c>Solicitacao:12</c> para a aplicação da Spec 030 (RF6). Nulo em edições diretas.
    /// </summary>
    public string? Origem { get; private set; }

    public HistoricoAluno(
        int alunoId,
        TipoEventoAluno tipo,
        string? campo,
        string? valorAnterior,
        string? valorNovo,
        string? autorNome,
        DateTime criadoEmUtc,
        string? origem = null)
    {
        AlunoId = alunoId;
        Tipo = tipo;
        Campo = string.IsNullOrWhiteSpace(campo) ? null : campo.Trim();
        ValorAnterior = Normalizar(valorAnterior);
        ValorNovo = Normalizar(valorNovo);
        AutorNome = string.IsNullOrWhiteSpace(autorNome) ? "sistema" : autorNome.Trim();
        Origem = string.IsNullOrWhiteSpace(origem) ? null : origem.Trim();
        CriadoEmUtc = criadoEmUtc.Kind == DateTimeKind.Utc
            ? criadoEmUtc
            : criadoEmUtc.ToUniversalTime();
    }

    /// <summary>Snapshot textual: ausência vira vazio, nunca nulo, para o DTO ser estável.</summary>
    private static string? Normalizar(string? valor) => valor ?? string.Empty;
}

/// <summary>
/// Alteração de um campo detectada pelo domínio (Spec 023, RNF1). É retorno de diff, não
/// DTO: só a entidade e o histórico conhecem esta forma.
/// </summary>
public sealed record AlteracaoDeDado(string Campo, string ValorAnterior, string ValorNovo);
