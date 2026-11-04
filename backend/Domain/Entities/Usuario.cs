namespace Ntc.Domain.Entity;

/// <summary>
/// Usuário do sistema. O estado de autorização (papel, escola, unidade e ativo) vive
/// aqui: um usuário sem papel é um cadastro válido que ainda não pode autenticar
/// (Spec 009, RF1), e não um usuário "Nutricionista" por omissão.
/// </summary>
public class Usuario {
    public int Id {get;private set;}
    public string? Nome {get;private set;}
    public string? Cpf {get;private set;}
    public DateOnly DataNascimento {get;private set;}
    public string? Email {get;private set;}
    public string? Telefone{get;private set;}
    public string? Foto{get;private set;}

    /// <summary>
    /// Registro profissional no Conselho Regional de Nutricionistas. Nulo para papéis que não
    /// são nutrição e para cadastros anteriores. É snapshot no cardápio, nunca FK.
    /// </summary>
    public string? Crn {get;private set;}

    /// <summary>
    /// Hash da senha. Interno de propósito (Spec 024, F8): material de autenticação não é
    /// parte do contrato da entidade — o exterior só pergunta <see cref="SenhaConfere"/>.
    /// Persistido na coluna <c>Senha</c> por configuração explícita no mapeamento.
    /// </summary>
    internal string? Senha {get;private set;}

    /// <summary>
    /// Nulo significa "aguardando definição de papel". O tipo é anulável de propósito:
    /// o valor 0 do enum é <see cref="PapelUsuario.Nutricionista"/> e um `default`
    /// acidental daria um escopo indevido a registros legados.
    /// </summary>
    public PapelUsuario? Papel {get;private set;}

    public int? EscolaId {get;private set;}

    public int? UnidadeId {get;private set;}

    public bool Ativo {get;private set;} = true;

    /// <summary>Somente para o EF Core materializar registros existentes.</summary>
    private Usuario()
    {

    }

    /// <summary>
    /// Único construtor público de criação. O papel é opcional para permitir o
    /// autocadastro; a conta só autentica depois que um administrador define o papel.
    /// </summary>
    public Usuario(string nome, string cpf, DateOnly dataNascimento, string email,
                   PapelUsuario? papel = null, int? escolaId = null, int? unidadeId = null,
                   string? crn = null)
    {
        // `DateOnly` é struct: um JSON sem `dataNascimento` chega como 0001-01-01 e passaria
        // pela validação de modelo. A invariante é do domínio (ADR-004), não do DTO.
        if (dataNascimento == default || dataNascimento > DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ArgumentException("Data de nascimento inválida.");
        }

        AtualizarPerfil(nome, email);
        Cpf = cpf;
        DataNascimento = dataNascimento;
        DefinirPapel(papel, escolaId, unidadeId);
        DefinirCrn(crn);
        Ativo = true;
    }   

    /// <summary>
    /// Define o papel e o escopo (Spec 027, RF6). A unidade é o escopo de
    /// `AdminSecretaria` e `Nutricionista`; escola é o escopo de `AdminEscola` e
    /// `Merendeira`; `Responsavel` guarda apenas a unidade administrativa de origem e
    /// nunca uma escola.
    /// </summary>
    public void DefinirPapel(PapelUsuario? papel, int? escolaId, int? unidadeId)
    {
        if (papel is null)
        {
            if (escolaId is not null || unidadeId is not null)
            {
                throw new ArgumentException(
                    "Usuário sem papel não pode ter escola ou unidade.");
            }

            Papel = null;
            EscolaId = null;
            UnidadeId = null;
            return;
        }

        switch (papel.Value)
        {
            case PapelUsuario.AdminSecretaria:
            case PapelUsuario.Nutricionista:
                if (escolaId is not null)
                {
                    throw new ArgumentException("Este papel não pertence a uma escola.");
                }

                if (unidadeId is null)
                {
                    throw new ArgumentException("Este papel exige unidade.");
                }

                break;

            case PapelUsuario.AdminEscola:
            case PapelUsuario.Merendeira:
                if (escolaId is null)
                {
                    throw new ArgumentException("Este papel exige escola.");
                }

                break;

            case PapelUsuario.Responsavel:
                if (escolaId is not null)
                {
                    throw new ArgumentException(
                        "O responsável não pertence a uma escola.");
                }

                if (unidadeId is null)
                {
                    throw new ArgumentException(
                        "O responsável exige a unidade administrativa de origem.");
                }

                break;
        }

        Papel = papel;
        EscolaId = escolaId;
        UnidadeId = unidadeId;
    }

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;

    /// <summary>
    /// Registro do conselho de nutrição, usado para identificar a autora do cardápio.
    /// Valor em branco significa "não informado" e é gravado como nulo.
    /// </summary>
    public void DefinirCrn(string? crn)
    {
        if (string.IsNullOrWhiteSpace(crn))
        {
            Crn = null;
            return;
        }

        var valor = crn.Trim().ToUpperInvariant().Replace(" ", string.Empty);

        if (valor.Length < 3 || valor.Length > 20)
        {
            throw new ArgumentException("O CRN deve ter entre 3 e 20 caracteres.");
        }

        Crn = valor;
    }

    public void AtualizarPerfil(string nome, string email)
    {
        nome = (nome ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim().ToLowerInvariant();

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
    
    
    /// <summary>
    /// Define a senha a partir do valor informado pelo usuário. Ordem obrigatória
    /// (Spec 002): guard clause → comprimento → derivação → atribuição. Um valor
    /// inválido não altera o estado persistido.
    /// </summary>
    public void DefinirSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
        {
            throw new ArgumentException("A senha não pode ser vazia.", nameof(senha));
        }

        if (senha.Length < 8)
        {
            throw new ArgumentException(
                "A senha deve possuir pelo menos 8 caracteres.", nameof(senha));
        }

        Senha = HashSenha.Criar(senha).ToString();
    }

    /// <summary>
    /// Confere a senha contra o hash persistido. Nunca lança: senha vazia ou valor
    /// persistido fora do formato de hash resultam em <c>false</c>.
    /// </summary>
    public bool SenhaConfere(string? senha)
    {
        if (string.IsNullOrEmpty(senha))
        {
            return false;
        }

        if (!HashSenha.TentarLer(Senha, out var hash))
        {
            return false;
        }

        return hash.Confere(senha);
    }

    /// <summary>
    /// Detecta um registro gravado antes da Spec 002, quando a senha era persistida em
    /// claro. Só é verdadeiro enquanto o valor gravado não estiver no formato de hash.
    /// </summary>
    public bool SenhaLegada(string? senha)
    {
        if (string.IsNullOrEmpty(senha) || Senha is null)
        {
            return false;
        }

        return !HashSenha.TentarLer(Senha, out _) && Senha == senha;
    }
}








