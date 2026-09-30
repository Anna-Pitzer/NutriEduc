namespace Ntc.Domain.Entity;

public class Aluno{
    public int Id {get; private set;}
    public string Nome {get; private set;}
    public DateOnly Nascimento {get;private set;}
    public string Escola {get;private set;}
    public string Serie {get;private set;}
    public string Telefone {get;private set;}
    public string Observacao {get;private set;}
    
    //public List<string> Restricoes {get;private set;} = new();
    public bool Anafilaxia {get;private set;}

    public string NomeResponsavel {get;private set;}
    public string TelefoneResponsavel {get;private set;}

    private Aluno(){ }
    public Aluno(string nome, DateOnly nascimento, string escola, string serie, string telefone, string observacao, bool anafilaxia, string nomeResponsavel, string telefoneResponsavel)
    {
        Nome = nome;
        Nascimento = nascimento;
        Escola = escola;
        Serie = serie;
        Telefone = telefone;
        Observacao = observacao;
        Anafilaxia = anafilaxia;
        NomeResponsavel = nomeResponsavel;
        TelefoneResponsavel = telefoneResponsavel;
    }
}