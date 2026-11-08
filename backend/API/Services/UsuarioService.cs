using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

public class UsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEscolaRepository _escolaRepository;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly AutorizacaoService _autorizacao;
    private readonly ITrilhaAcaoRepository _trilha;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IEscolaRepository escolaRepository,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        AutorizacaoService autorizacao,
        ITrilhaAcaoRepository trilha)
    {
        _usuarioRepository = usuarioRepository;
        _escolaRepository = escolaRepository;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _autorizacao = autorizacao;
        _trilha = trilha;
    }

    // ------------------------------------------------------------------ autocadastro

    /// <summary>
    /// Autocadastro público: cria a conta <b>sem papel</b>. A conta existe, mas só
    /// autentica depois que um administrador define o papel (Spec 009). Devolve
    /// <see cref="Resultado{T}.Invalido"/> quando o e-mail já está cadastrado — a regra e
    /// a mensagem são do serviço, não do controller.
    /// </summary>
    public Resultado<int> CriarUsuario(CriarUsuarioEntrada dto)
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
            return Resultado<int>.Invalido("E-mail já cadastrado.");
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _usuarioRepository.Add(usuario));
        return Resultado<int>.Ok(usuario.Id);
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
        _unidadeDeTrabalho.ExecutarEmTransacao(() => _usuarioRepository.Update(usuario));

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

        var negacao = _autorizacao.Verificar(
            Recurso.ContaUsuario,
            escopo,
            OperacaoDeAcesso.Criar,
            new AlvoDeEscopo(escopo.UnidadeId, escopo.EscolaId));

        if (negacao is not null)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<int>.NaoEncontrado("Unidade não encontrada.")
                : Resultado<int>.Proibido();
        }

        if (_usuarioRepository.ExistsByEmail(dto.Email))
        {
            return Resultado<int>.Conflito("E-mail já cadastrado.");
        }

        // RF5/RNF3: unicidade do CRN normalizado, verificada antes de construir a entidade.
        if (!string.IsNullOrWhiteSpace(dto.Crn) &&
            _usuarioRepository.GetByCrn(CrnDeNutricionista.Normalizar(dto.Crn)) is not null)
        {
            return Resultado<int>.Conflito("Este CRN já está vinculado a outro usuário.");
        }

        var resolucao = ResolverEscopoInstitucional(
            papel, dto.EscolaId, dto.UnidadeId, escopo);

        if (resolucao.Falha is { } falha)
        {
            return falha switch
            {
                ResultadoStatus.Proibido => Resultado<int>.Proibido(),
                ResultadoStatus.NaoEncontrado => Resultado<int>.NaoEncontrado(resolucao.Erro!),
                _ => Resultado<int>.Invalido(resolucao.Erro!)
            };
        }

        Usuario usuario = new(
            dto.Nome,
            dto.Cpf,
            dto.DataNascimento,
            dto.Email,
            papel,
            resolucao.EscolaId,
            resolucao.UnidadeId,
            dto.Crn
        );

        usuario.DefinirSenha(dto.Senha);
        usuario.AtualizarTelefone(dto.Telefone);
        usuario.AtualizarFoto(dto.Foto);

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _usuarioRepository.Add(usuario));

        return Resultado<int>.Ok(usuario.Id);
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
        var mudaPapel = dto.Papel is not null;

        // RF3: ninguém altera o próprio papel, nem o próprio ADM.
        if (dono && mudaPapel)
        {
            return Resultado<UsuarioRespostaDTO>.Proibido();
        }

        var recurso = dono
            ? Recurso.PerfilProprio
            : mudaPapel
                ? Recurso.PapelDoUsuario
                : Recurso.ContaUsuario;

        var alvoDeEscopo = dono
            ? AlvoDeEscopo.SemFronteira
            : new AlvoDeEscopo(alvo.UnidadeId, alvo.EscolaId);

        var negacao = _autorizacao.Verificar(
            recurso, escopo, OperacaoDeAcesso.Atualizar, alvoDeEscopo);

        if (negacao is not null)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<UsuarioRespostaDTO>.NaoEncontrado("Usuário não encontrado.")
                : Resultado<UsuarioRespostaDTO>.Proibido();
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        var comMesmoEmail = _usuarioRepository.GetByEmail(email);

        if (comMesmoEmail is not null && comMesmoEmail.Id != id)
        {
            return Resultado<UsuarioRespostaDTO>.Conflito(
                "Este e-mail já está cadastrado em outra conta.");
        }

        var papelAnterior = alvo.Papel?.ToString();
        var crnAnterior = alvo.Crn;

        alvo.AtualizarPerfil(dto.Nome, email);
        alvo.AtualizarTelefone(dto.Telefone);

        // RF16: CRN é campo administrativo — o dono do perfil nunca altera o próprio CRN.
        // O valor enviado pelo dono é ignorado, nunca gravado.
        if (!dono)
        {
            var papelFinal = dto.Papel ?? alvo.Papel;
            var recebeCrn = !string.IsNullOrWhiteSpace(dto.Crn);

            // RF6: CRN é exclusivo de Nutricionista. Quem sai de Nutricionista (RF4) tem o
            // CRN descartado pela troca de papel; os demais papéis não podem receber CRN.
            if (recebeCrn && papelFinal != PapelUsuario.Nutricionista &&
                alvo.Papel != PapelUsuario.Nutricionista)
            {
                return Resultado<UsuarioRespostaDTO>.Invalido(
                    CrnDeNutricionista.MensagemSomenteNutricionista);
            }

            // RF5/RNF3: unicidade do CRN normalizado, verificada antes de mutar a entidade.
            if (recebeCrn &&
                _usuarioRepository.GetByCrn(CrnDeNutricionista.Normalizar(dto.Crn)) is { } porCrn &&
                porCrn.Id != id)
            {
                return Resultado<UsuarioRespostaDTO>.Conflito(
                    "Este CRN já está vinculado a outro usuário.");
            }

            alvo.DefinirCrn(dto.Crn);
        }

        if (dto.Foto != null)
        {
            alvo.AtualizarFoto(dto.Foto);
        }

        if (mudaPapel)
        {
            var resolucao = ResolverEscopoInstitucional(
                dto.Papel!.Value, dto.EscolaId, dto.UnidadeId, escopo);

            if (resolucao.Falha is { } falha)
            {
                return falha switch
                {
                    ResultadoStatus.Proibido => Resultado<UsuarioRespostaDTO>.Proibido(),
                    ResultadoStatus.NaoEncontrado =>
                        Resultado<UsuarioRespostaDTO>.NaoEncontrado(resolucao.Erro!),
                    _ => Resultado<UsuarioRespostaDTO>.Invalido(resolucao.Erro!)
                };
            }

            alvo.DefinirPapel(dto.Papel, resolucao.EscolaId, resolucao.UnidadeId);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _usuarioRepository.Update(alvo);

            var papelNovo = alvo.Papel?.ToString();

            if (mudaPapel && papelNovo != papelAnterior)
            {
                _trilha.Add(new TrilhaAcao(
                    "PapelAlterado",
                    escopo.UsuarioId,
                    NomeDoAutor(escopo),
                    "Usuario",
                    alvo.Id,
                    papelAnterior,
                    papelNovo,
                    DateTime.UtcNow));
            }

            // RF11: toda alteração de CRN entra na trilha append-only com valor anterior e novo.
            if (alvo.Crn != crnAnterior)
            {
                _trilha.Add(new TrilhaAcao(
                    "CrnAlterado",
                    escopo.UsuarioId,
                    NomeDoAutor(escopo),
                    "Usuario",
                    alvo.Id,
                    crnAnterior,
                    alvo.Crn,
                    DateTime.UtcNow));
            }
        });

        return Resultado<UsuarioRespostaDTO>.Ok(ParaDTO(alvo)!);
    }

    /// <summary>
    /// Referências institucionais verificadas antes de mutar o usuário (Spec 024, F4):
    /// existência e situação são regra de aplicação; a entidade valida só o formato.
    /// </summary>
    private string? ErroDeReferencias(int? escolaId, int? unidadeId)
    {
        if (escolaId is not null)
        {
            var escola = _escolaRepository.GetEscola(escolaId.Value);

            if (escola is null) return "Escola não encontrada.";
            if (!escola.Ativa) return "Escola desativada não pode receber usuário.";
        }

        if (unidadeId is not null)
        {
            var unidade = _escolaRepository.GetUnidade(unidadeId.Value);

            if (unidade is null) return "Unidade não encontrada.";
            if (!unidade.Ativa) return "Unidade desativada não pode receber usuário.";
        }

        return null;
    }

    private string NomeDoAutor(EscopoUsuario escopo) =>
        _usuarioRepository.GetById(escopo.UsuarioId)?.Nome ?? "sistema";

    private sealed record ResolucaoEscopo(
        int? EscolaId,
        int? UnidadeId,
        ResultadoStatus? Falha,
        string? Erro);

    /// <summary>
    /// Traduz o pedido administrativo para o vínculo institucional válido (Spec 027):
    /// papel de escola deriva a unidade da escola; papel de unidade recusa escola.
    /// </summary>
    private ResolucaoEscopo ResolverEscopoInstitucional(
        PapelUsuario papel,
        int? escolaId,
        int? unidadeId,
        EscopoUsuario escopo)
    {
        if (papel is PapelUsuario.AdminEscola or PapelUsuario.Merendeira)
        {
            if (escolaId is null)
            {
                return Falha(ResultadoStatus.Invalido, "Este papel exige escola.");
            }

            var escola = _escolaRepository.GetEscola(escolaId.Value);

            if (escola is null)
            {
                return Falha(ResultadoStatus.NaoEncontrado, "Escola não encontrada.");
            }

            if (!escola.Ativa)
            {
                return Falha(ResultadoStatus.Invalido,
                    "Escola desativada não pode receber usuário.");
            }

            if (escola.UnidadeId is null)
            {
                return Falha(ResultadoStatus.Invalido,
                    "Escola sem unidade não pode receber usuário.");
            }

            if (escopo.Papel == PapelUsuario.AdminEscola &&
                escola.Id != escopo.EscolaId)
            {
                return Falha(ResultadoStatus.Proibido, "Escola fora do escopo.");
            }

            if (escopo.Papel == PapelUsuario.AdminSecretaria &&
                escola.UnidadeId != escopo.UnidadeId)
            {
                return Falha(ResultadoStatus.Proibido, "Unidade fora do escopo.");
            }

            return new ResolucaoEscopo(escola.Id, escola.UnidadeId, null, null);
        }

        var unidadeDestino = unidadeId ?? escopo.UnidadeId;

        if (unidadeDestino is null)
        {
            return Falha(ResultadoStatus.Invalido, "Este papel exige unidade.");
        }

        var unidade = _escolaRepository.GetUnidade(unidadeDestino.Value);

        if (unidade is null)
        {
            return Falha(ResultadoStatus.NaoEncontrado, "Unidade não encontrada.");
        }

        if (!unidade.Ativa)
        {
            return Falha(ResultadoStatus.Invalido,
                "Unidade desativada não pode receber usuário.");
        }

        if (escopo.UnidadeId is { } unidadeDaSessao && unidadeDaSessao != unidade.Id)
        {
            return Falha(ResultadoStatus.Proibido, "Unidade fora do escopo.");
        }

        return new ResolucaoEscopo(null, unidade.Id, null, null);
    }

    private static ResolucaoEscopo Falha(ResultadoStatus status, string erro) =>
        new(null, null, status, erro);

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

        var negacao = _autorizacao.Verificar(
            Recurso.ContaUsuario,
            escopo,
            OperacaoDeAcesso.Atualizar,
            new AlvoDeEscopo(alvo.UnidadeId, alvo.EscolaId));

        if (negacao is not null)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<UsuarioRespostaDTO>.NaoEncontrado("Usuário não encontrado.")
                : Resultado<UsuarioRespostaDTO>.Proibido();
        }

        var ativoAnterior = alvo.Ativo;

        if (ativo)
        {
            alvo.Reativar();
        }
        else
        {
            alvo.Desativar();
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _usuarioRepository.Update(alvo);

            if (ativoAnterior != alvo.Ativo)
            {
                _trilha.Add(new TrilhaAcao(
                    alvo.Ativo ? "UsuarioReativado" : "UsuarioDesativado",
                    escopo.UsuarioId,
                    NomeDoAutor(escopo),
                    "Usuario",
                    alvo.Id,
                    ativoAnterior ? "ativo" : "inativo",
                    alvo.Ativo ? "ativo" : "inativo",
                    DateTime.UtcNow));
            }
        });

        return Resultado<UsuarioRespostaDTO>.Ok(ParaDTO(alvo)!);
    }

    /// <summary>
    /// Confere filtros de consulta contra o escopo da sessão (Spec 028, RF10). Filtro fora
    /// do escopo responde 403; não é ignorado nem amplia.
    /// </summary>
    public bool FiltroNoEscopo(int? escolaId, int? unidadeId, EscopoUsuario escopo)
    {
        if (unidadeId is > 0 && escopo.UnidadeId != unidadeId)
        {
            return false;
        }

        if (escolaId is not > 0)
        {
            return true;
        }

        var escola = _escolaRepository.GetEscola(escolaId.Value);

        if (escola is null)
        {
            return false;
        }

        return escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista =>
                escola.UnidadeId == escopo.UnidadeId,
            PapelUsuario.AdminEscola or PapelUsuario.Merendeira =>
                escola.Id == escopo.EscolaId,
            _ => false
        };
    }

    /// <summary>Listagem filtrada por escopo (RF7).</summary>
    public List<UsuarioRespostaDTO> Listar(FiltroUsuarios filtro, EscopoUsuario escopo)
    {
        return _usuarioRepository
            .Listar(FiltrosDeConsulta.ParaUsuario(filtro, escopo))
            .Select(usuario => ParaDTO(usuario)!)
            .ToList();
    }

    /// <summary>
    /// Nutricionistas ativas que podem ser escolhidas como responsáveis por um cardápio
    /// (Spec 027). Reaproveita a listagem filtrada em vez de abrir uma segunda consulta.
    /// </summary>
    public List<NutricionistaDTO> ListarNutricionistas(EscopoUsuario escopo)
    {
        return _usuarioRepository
            .Listar(FiltrosDeConsulta.ParaUsuario(
                new FiltroUsuarios(
                    Papel: PapelUsuario.Nutricionista,
                    EscolaId: null,
                    UnidadeId: null,
                    Ativo: true,
                    Busca: null),
                escopo))
            // RF8: a seleção devolve apenas nutricionistas com CRN válido.
            .Where(usuario => CrnDeNutricionista.EhValido(usuario.Crn))
            .Select(usuario => new NutricionistaDTO(
                usuario.Id,
                usuario.Nome,
                usuario.Email,
                usuario.Cpf,
                usuario.Crn,
                true))
            .ToList();
    }

    /// <summary>
    /// Diagnóstico de legado (RF10): nutricionistas ativos, da unidade do solicitante, sem
    /// CRN ou com CRN em formato inválido. Não corrige dados nem inventa registro.
    /// </summary>
    public List<DiagnosticoCrnDTO> DiagnosticarCrn(EscopoUsuario escopo)
    {
        return _usuarioRepository
            .Listar(FiltrosDeConsulta.ParaUsuario(
                new FiltroUsuarios(
                    Papel: PapelUsuario.Nutricionista,
                    EscolaId: null,
                    UnidadeId: null,
                    Ativo: true,
                    Busca: null),
                escopo))
            .Where(usuario => !CrnDeNutricionista.EhValido(usuario.Crn))
            .Select(usuario => new DiagnosticoCrnDTO(
                usuario.Id,
                usuario.Nome,
                usuario.UnidadeId,
                string.IsNullOrWhiteSpace(usuario.Crn) ? "Sem CRN" : "CRN inválido"))
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
            usuario.Ativo,
            usuario.Crn,
            usuario.CrnValido);
    }
}
