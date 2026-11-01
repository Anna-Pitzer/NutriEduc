namespace Ntc.Application;

public class DadosAlunoDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public DateOnly Nascimento { get; set; }
    public string Escola { get; set; } = "";
    public string Serie { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Observacao { get; set; } = "";
    public List<string> Restricoes { get; set; } = new();
    public bool Anafilaxia { get; set; }
    public string NomeResponsavel { get; set; } = "";
    public string TelefoneResponsavel { get; set; } = "";

    /// <summary>
    /// Unidade institucional opcional (Spec 009). Sem unidade, o aluno só é visível
    /// para Nutricionista e AdminSecretaria; `Escola` continua sendo rótulo de exibição.
    /// </summary>
    public int? UnidadeId { get; set; }

    public DadosAlunoDTO() { }

    public DadosAlunoDTO(string nome, DateOnly nascimento, string escola, string serie, string telefone, string observacao, List<string> restricoes, bool anafilaxia, string nomeResponsavel, string telefoneResponsavel)
    {
        Nome = nome;
        Nascimento = nascimento;
        Escola = escola;
        Serie = serie;
        Telefone = telefone;
        Observacao = observacao;
        Restricoes = restricoes;
        Anafilaxia = anafilaxia;
        NomeResponsavel = nomeResponsavel;
        TelefoneResponsavel = telefoneResponsavel;
    }
}