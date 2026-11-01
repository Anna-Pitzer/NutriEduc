using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>Motivo da recusa de login (Spec 009, RF1/RF5).</summary>
public enum ResultadoLogin
{
    Ok,
    CredenciaisInvalidas,
    SemPapel,
    Inativo
}

public class UsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEscolaRepository _escolaRepository;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IEscolaRepository escolaRepository)
    {
        _usuarioRepository = usuarioRepository;
        _escolaRepository = escolaRepository;
    }

    // ------------------------------------------------------------------ autocadastro

    /// <summary>
    /// Autocadastro público: cria a conta <b>sem papel</b>. A conta existe, mas só
    /// autentica depois que um administrador define o papel (Spec 009). Devolve
    /// <c>null</c> quando o e-mail já está cadastrado.
    /// </summary>
    public int? CriarUsuario(DadosUsuarioDTO dto)
    {
        Usuario usuario = new(
            dto.Nome ?? string.Empty,
            dto.Cpf ?? string.Empty,
            dto.DataNascimento,
            dto.Email ?? string.Empty
        );

        usuario.DefinirSenha(dto.Senha ?? string.Empty);
        usuario.AtualizarTelefone(dto.Telefone);
        usuario.AtualizarFoto(dto.Foto);

        if (_usuarioRepository.ExistsByEmail(usuario.Email!))
        {
            return null;
        }

        _usuarioRepository.Add(usuario);
        return usuario.Id;
    }

    // ------------------------------------------------------------------------- login

    /// <summary>
    /// Confere apenas as credenciais. Não considera papel nem situação ativa: quem
    /// decide se a sessão pode ser emitida é <see cref="Autenticar"/>.
    /// </summary>
    public bool VerificarLogin(LoginUsuarioDTO dto)
    {
        Usuario? usuario = _usuarioRepository.GetByEmail(dto.Email ?? string.Empty);

        if (usuario == null)
        {
            return false;
        }

        if (usuario.SenhaConfere(dto.Senha))
        {
            return true;
        }

        // Registro gravado antes da Spec 002: autentica em claro uma única vez e
        // persiste o hash, de modo que o próximo login use SenhaConfere.
        if (usuario.SenhaLegada(dto.Senha))
        {
            usuario.DefinirSenha(dto.Senha!);
            _usuarioRepository.Update(usuario);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Login completo: credenciais, situação ativa e papel definido. Só devolve
    /// <see cref="ResultadoLogin.Ok"/> quando a sessão pode ser emitida.
    /// </summary>
    public ResultadoLogin Autenticar(LoginUsuarioDTO dto, out Usuario? usuario)
    {
        usuario = null;

        if (!VerificarLogin(dto))
        {
            return ResultadoLogin.CredenciaisInvalidas;
        }

        var encontrado = _usuarioRepository.GetByEmail(dto.Email ?? string.Empty);

        if (encontrado is null)
        {
            return ResultadoLogin.CredenciaisInvalidas;
        }

        if (!encontrado.Ativo)
        {
            return ResultadoLogin.Inativo;
        }

        if (encontrado.Papel is null)
        {
            return ResultadoLogin.SemPapel;
        }

        usuario = encontrado;
        return ResultadoLogin.Ok;
    }

    // ------------------------------------------------------------------------ perfil

    public UsuarioRespostaDTO? ObterUsuarioPorId(int id)
    {
        return ParaDTO(_usuarioRepository.GetById(id));
    }

    public string? ObterNomePeloEmail(string email)
    {
        return _usuarioRepository.GetByEmail(email)?.Nome;
    }

    public int? ObterIdPeloEmail(string email)
    {
        return _usuarioRepository.GetByEmail(email)?.Id;
    }

    public UsuarioRespostaDTO? AtualizarPerfil(int id, AtualizarPerfilDTO dto)
    {
        var usuario = _usuarioRepository.GetById(id);

        if (usuario == null)
        {
            return null;
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        var usuarioComMesmoEmail = _usuarioRepository.GetByEmail(email);

        if (usuarioComMesmoEmail != null &&
            usuarioComMesmoEmail.Id != id)
        {
            throw new ArgumentException(
                "Este e-mail já está cadastrado em outra conta."
            );
        }

        usuario.AtualizarPerfil(dto.Nome, email);
        usuario.AtualizarTelefone(dto.Telefone);
        if (dto.Foto != null)
        {
            usuario.AtualizarFoto(dto.Foto);
        }
        _usuarioRepository.Update(usuario);

        return ObterUsuarioPorId(id);
    }

    // ------------------------------------------------------------------ administração

    /// <summary>
    /// Cadastro feito por administrador (RF4). AdminSecretaria cria qualquer papel;
    /// AdminEscola cria apenas Responsável/Merendeira da própria escola. A decisão de
    /// acesso fica aqui, não no controller (RNF1).
    /// </summary>
    public Resultado<int> CriarUsuarioAdministrado(CriarUsuarioDTO dto, EscopoUsuario escopo)
    {
        var papel = dto.Papel!.Value;

        if (!PodeCriar(papel, dto.EscolaId, escopo))
        {
            return Resultado<int>.Proibido();
        }

        if (_usuarioRepository.ExistsByEmail(dto.Email))
        {
            return Resultado<int>.Conflito("E-mail já cadastrado.");
        }

        if (dto.EscolaId is not null && _escolaRepository.GetEscola(dto.EscolaId.Value) is null)
        {
            return Resultado<int>.NaoEncontrado("Escola não encontrada.");
        }

        if (dto.UnidadeId is not null)
        {
            var unidade = _escolaRepository.GetUnidade(dto.UnidadeId.Value);

            if (unidade is null)
            {
                return Resultado<int>.NaoEncontrado("Unidade não encontrada.");
            }

            // Unidade é da rede: a checagem só existe quando a unidade ainda guarda a escola
            // de origem (dado legado da Spec 009).
            if (dto.EscolaId is not null && unidade.EscolaId is not null && unidade.EscolaId != dto.EscolaId)
            {
                return Resultado<int>.Conflito("A unidade não pertence à escola informada.");
            }
        }

        Usuario usuario = new(
            dto.Nome,
            dto.Cpf,
            dto.DataNascimento,
            dto.Email,
            papel,
            dto.EscolaId,
            dto.UnidadeId
        );

        usuario.DefinirSenha(dto.Senha);
        usuario.AtualizarTelefone(dto.Telefone);
        usuario.AtualizarFoto(dto.Foto);

        _usuarioRepository.Add(usuario);

        return Resultado<int>.Ok(usuario.Id);
    }

    private static bool PodeCriar(PapelUsuario papel, int? escolaId, EscopoUsuario escopo)
    {
        return escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria => true,

            PapelUsuario.AdminEscola =>
                papel is PapelUsuario.Responsavel or PapelUsuario.Merendeira &&
                escolaId is not null &&
                escolaId == escopo.EscolaId,

            _ => false
        };
    }

    /// <summary>
    /// Atualização de usuário (RF4/RF5). O dono altera apenas os próprios dados; papel
    /// e escopo só mudam por administrador do escopo.
    /// </summary>
    public Resultado<UsuarioRespostaDTO> AtualizarUsuario(
        int id,
        AtualizarUsuarioDTO dto,
        EscopoUsuario escopo)
    {
        var alvo = _usuarioRepository.GetById(id);

        if (alvo is null)
        {
            return Resultado<UsuarioRespostaDTO>.NaoEncontrado("Usuário não encontrado.");
        }

        var dono = escopo.UsuarioId == id;
        var administra = escopo.Administra &&
            (escopo.Global || alvo.EscolaId == escopo.EscolaId);

        if (!dono && !administra)
        {
            return Resultado<UsuarioRespostaDTO>.Proibido();
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        var comMesmoEmail = _usuarioRepository.GetByEmail(email);

        if (comMesmoEmail is not null && comMesmoEmail.Id != id)
        {
            return Resultado<UsuarioRespostaDTO>.Conflito(
                "Este e-mail já está cadastrado em outra conta.");
        }

        var mudaPapel = dto.Papel is not null;

        if (mudaPapel && !administra)
        {
            return Resultado<UsuarioRespostaDTO>.Proibido();
        }

        if (mudaPapel && !PodeDefinirPapel(dto.Papel!.Value, dto.EscolaId, escopo))
        {
            return Resultado<UsuarioRespostaDTO>.Proibido();
        }

        alvo.AtualizarPerfil(dto.Nome, email);
        alvo.AtualizarTelefone(dto.Telefone);

        if (dto.Foto != null)
        {
            alvo.AtualizarFoto(dto.Foto);
        }

        if (mudaPapel)
        {
            alvo.DefinirPapel(dto.Papel, dto.EscolaId, dto.UnidadeId);
        }

        _usuarioRepository.Update(alvo);

        return Resultado<UsuarioRespostaDTO>.Ok(ParaDTO(alvo)!);
    }

    private static bool PodeDefinirPapel(
        PapelUsuario papel,
        int? escolaId,
        EscopoUsuario escopo)
    {
        return escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria => true,

            PapelUsuario.AdminEscola =>
                papel is PapelUsuario.Responsavel or PapelUsuario.Merendeira &&
                escolaId == escopo.EscolaId,

            _ => false
        };
    }

    /// <summary>Ativar/desativar usuário (RF5): exclusivo de administrador do escopo.</summary>
    public Resultado<UsuarioRespostaDTO> DefinirAtivo(
        int id,
        bool ativo,
        EscopoUsuario escopo)
    {
        var alvo = _usuarioRepository.GetById(id);

        if (alvo is null)
        {
            return Resultado<UsuarioRespostaDTO>.NaoEncontrado("Usuário não encontrado.");
        }

        if (!escopo.Administra)
        {
            return Resultado<UsuarioRespostaDTO>.Proibido();
        }

        if (!escopo.Global && alvo.EscolaId != escopo.EscolaId)
        {
            return Resultado<UsuarioRespostaDTO>.Proibido();
        }

        if (ativo)
        {
            alvo.Reativar();
        }
        else
        {
            alvo.Desativar();
        }

        _usuarioRepository.Update(alvo);

        return Resultado<UsuarioRespostaDTO>.Ok(ParaDTO(alvo)!);
    }

    /// <summary>Listagem filtrada por escopo (RF7).</summary>
    public List<UsuarioRespostaDTO> Listar(FiltroUsuarios filtro, EscopoUsuario escopo)
    {
        return _usuarioRepository
            .Listar(filtro, escopo)
            .Select(usuario => ParaDTO(usuario)!)
            .ToList();
    }

    private static UsuarioRespostaDTO? ParaDTO(Usuario? usuario)
    {
        if (usuario is null)
        {
            return null;
        }

        return new UsuarioRespostaDTO(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.Telefone,
            usuario.Foto,
            usuario.Papel,
            usuario.EscolaId,
            usuario.UnidadeId,
            usuario.Ativo);
    }
}
