namespace Ntc.Model;

public class Usuario {
    public Usuario()
    {
        
    }

    public Usuario(string email, string senha)
    {
        Email = email;
        Senha = senha;
    }
    public Usuario (string nome, string cpf, DateOnly dataNascimento, string email, string senha)
    {   
        Nome = nome;
        Cpf = cpf;
        DataNascimento = dataNascimento;
        Email = email;
        Senha = senha;
    }

    public int Id {get;private set;}
    public string? Nome {get;private set;}
    public string? Cpf {get;private set;}
    public DateOnly DataNascimento {get;private set;}
    public string? Email {get;private set;}
    public string? Senha {get;private set;}

}








