namespace Ntc.Domain.Entity;

public class Usuario {
    public int Id {get;private set;}
    public string? Nome {get;private set;}
    public string? Cpf {get;private set;}
    public DateOnly DataNascimento {get;private set;}
    public string? Email {get;private set;}
    public string? Senha {get;private set;}
    public Usuario()
    {
        
    }
    public Usuario (string email)
    {
        Email = email;
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

    public bool SenhaValida(){
        if (Senha.Length < 8)
        {
            return false;
        }

        return true;
    }

    public void AtualizarSenha(string novaSenha)
    {
        if(novaSenha.Length < 8)
        {
            throw new ArgumentException("A senha deve possuir pelo menos 8 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(novaSenha))
        {
            throw new ArgumentException("A senha não pode ser vazia");
        }

        Senha = novaSenha;
    }
}








