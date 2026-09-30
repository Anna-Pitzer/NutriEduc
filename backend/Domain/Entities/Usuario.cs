namespace Ntc.Domain.Entity;

public class Usuario {
    public int Id {get;private set;}
    public string? Nome {get;private set;}
    public string? Cpf {get;private set;}
    public DateOnly DataNascimento {get;private set;}
    public string? Email {get;private set;}
    public string? Telefone{get;private set;}
    public string? Foto{get;private set;}
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

    public void AtualizarPerfil(string nome, string email)
    {
        nome = nome.Trim();
        email = email.Trim().ToLowerInvariant();

        if (nome.Length < 2 || nome.Length > 100)
        {
            throw new ArgumentException(
                "O nome deve ter entre 2 e 100 caracteres."
            );
        }

        if (email.Length > 150 ||
            !System.Net.Mail.MailAddress.TryCreate(email, out var endereco) ||
            endereco.Address != email)
        {
            throw new ArgumentException("Informe um e-mail válido.");
        }

        Nome = nome;
        Email = email;
    }
    
    public void AtualizarTelefone(string? telefone)
{
    if (string.IsNullOrWhiteSpace(telefone))
    {
        Telefone = null;
        return;
    }

    var numeros = new string(
        telefone.Where(char.IsDigit).ToArray()
    );

    if (numeros.Length != 10 && numeros.Length != 11)
    {
        throw new ArgumentException(
            "O telefone deve ter 10 ou 11 dígitos."
        );
    }

    Telefone = numeros;
}
    public void AtualizarFoto(string? foto)
{
    if (string.IsNullOrWhiteSpace(foto))
    {
        Foto = null;
        return;
    }

    var prefixosPermitidos = new[]
    {
        "data:image/jpeg;base64,",
        "data:image/png;base64,",
        "data:image/webp;base64,"
    };

    var prefixo = prefixosPermitidos.FirstOrDefault(
        p => foto.StartsWith(p, StringComparison.Ordinal)
    );

    if (prefixo == null)
    {
        throw new ArgumentException(
            "Selecione uma imagem PNG, JPG ou WEBP."
        );
    }

    byte[] imagem;

    try
    {
        imagem = Convert.FromBase64String(
            foto.Substring(prefixo.Length)
        );
    }
    catch (FormatException)
    {
        throw new ArgumentException("Os dados da imagem são inválidos.");
    }

    if (imagem.Length == 0 || imagem.Length > 5 * 1024 * 1024)
    {
        throw new ArgumentException(
            "A imagem deve ter conteúdo e no máximo 5MB."
        );
    }

    Foto = foto;
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








