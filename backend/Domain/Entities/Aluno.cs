namespace Ntc.Domain.Entity;

public class Aluno{
    public int Id {get; private set;}
    public string Nome {get; private set;}
    public DateOnly Nascimento {get;private set;}
    public string Escola {get;private set;}
    public string Serie {get;private set;}
    public string Telefone {get;private set;}
    public string Observacao {get;private set;}
    
    public List<string> Restricoes {get;private set;}
    public bool Anafilaxia {get;private set;}

    public string NomeResponsavel {get;private set;}
    public string TelefoneResponsavel {get;private set;}
}