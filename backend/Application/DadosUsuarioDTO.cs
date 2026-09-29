using Ntc.Domain.Entity;

namespace Ntc.Application;
public class DadosUsuarioDTO
{   
    public string? Nome {get; set;}
    public string? Cpf {get; set;}
    public DateOnly DataNascimento {get; set;}
    public string? Email {get; set;} 
    public string? Senha {get; set;}

    public DadosUsuarioDTO(){}
    public DadosUsuarioDTO(string nome, string cpf, DateOnly datanascimento, string email)
    {
        Nome = nome;
        Cpf = cpf;
        DataNascimento = datanascimento;
        Email = email;
    }
}

