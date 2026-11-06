namespace Ntc.Domain.Entity;

/// <summary>
/// Alteração persistida de uma solicitação de dados cadastrais (Spec 030, design
/// §Persistência). É append-only: a solicitação é imutável e a aprovação apenas compara o
/// snapshot com o estado atual do aluno.
/// </summary>
public class SolicitacaoAlteracao
{
    public const int ValorMaximo = 500;

    public int Id { get; private set; }

    public int SolicitacaoId { get; private set; }

    public CampoDeDadoDoAluno Campo { get; private set; }

    public string ValorSolicitado { get; private set; } = string.Empty;

    /// <summary>Valor do aluno no momento da criação; base da checagem de divergência (RF7).</summary>
    public string ValorNoPedido { get; private set; } = string.Empty;

    private SolicitacaoAlteracao()
    {
    }

    internal SolicitacaoAlteracao(
        CampoDeDadoDoAluno campo,
        string valorSolicitado,
        string valorNoPedido)
    {
        if (!Enum.IsDefined(campo))
            throw new ArgumentException("Campo de dado do aluno inválido.");

        Campo = campo;
        ValorSolicitado = Normalizar(valorSolicitado);
        ValorNoPedido = Normalizar(valorNoPedido);
    }

    private static string Normalizar(string? valor)
    {
        var texto = (valor ?? string.Empty).Trim();

        if (texto.Length > ValorMaximo)
        {
            throw new ArgumentException(
                $"O valor da alteração deve ter no máximo {ValorMaximo} caracteres.");
        }

        return texto;
    }
}
