class Aluno{
    public string Nome {get;set;}
    public DateOnly Nascimento {get;set;}
    public string Escola {get;set;}
    public string Serie {get;set;}
    public string Telefone {get;set;}
    public string Observacao {get;set;}
    
    public List<string> Restricoes {get;set;}
    public bool Anafilaxia {get;set;}

    public string NomeResponsavel {get;set;}
    public string TelefoneResponsavel {get;set;}
}