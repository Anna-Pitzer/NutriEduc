using Ntc.Application;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Autocadastro público (Spec 033, RF1–RF4). Cria, na mesma unidade de trabalho, o
/// <see cref="Usuario"/> sem papel e a <see cref="SolicitacaoDeAcesso"/> pendente. O tipo
/// registra a intenção e não concede papel nem permissão; a conta só autentica depois da
/// aprovação administrativa.
/// </summary>
public sealed class RegistrarAcessoService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IEscolaRepository _escolas;
    private readonly ISolicitacaoDeAcessoRepository _solicitacoes;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public RegistrarAcessoService(
        IUsuarioRepository usuarios,
        IEscolaRepository escolas,
        ISolicitacaoDeAcessoRepository solicitacoes,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _usuarios = usuarios;
        _escolas = escolas;
        _solicitacoes = solicitacoes;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    public Resultado<RegistroDeAcessoDTO> Registrar(CadastroEntrada dto)
    {
        // RF20: o cadastro público é anônimo e só aceita os três tipos autossolicitáveis.
        if (dto.Tipo is not { } tipo || !Enum.IsDefined(tipo))
        {
            return Resultado<RegistroDeAcessoDTO>.Invalido(
                "Informe um tipo de cadastro válido: Nutricionista, Merendeira ou Responsavel.");
        }

        if (dto.UnidadeId is not { } unidadeId || unidadeId <= 0)
        {
            return Resultado<RegistroDeAcessoDTO>.Invalido("Informe a unidade de destino.");
        }

        // RF17: a unidade precisa existir e estar ativa (validação do backend).
        var unidade = _escolas.GetUnidade(unidadeId);

        if (unidade is null)
        {
            return Resultado<RegistroDeAcessoDTO>.NaoEncontrado("Unidade não encontrada.");
        }

        if (!unidade.Ativa)
        {
            return Resultado<RegistroDeAcessoDTO>.Invalido(
                "Unidade desativada não pode receber cadastro.");
        }

        // RF3: CRN obrigatório e válido só para nutricionista; proibido nos demais.
        var erroDeCrn = PoliticaDeAprovacao.ErroDeCrn(tipo, dto.Crn);

        if (erroDeCrn is not null)
        {
            return Resultado<RegistroDeAcessoDTO>.Invalido(erroDeCrn);
        }

        Usuario usuario;

        try
        {
            usuario = new Usuario(
                dto.Nome ?? string.Empty,
                dto.Cpf ?? string.Empty,
                dto.DataNascimento,
                dto.Email ?? string.Empty);

            usuario.DefinirSenha(dto.Senha ?? string.Empty);
            usuario.AtualizarTelefone(dto.Telefone);
            usuario.AtualizarFoto(dto.Foto);
        }
        catch (ArgumentException excecao)
        {
            return Resultado<RegistroDeAcessoDTO>.Invalido(excecao.Message);
        }

        if (_usuarios.ExistsByEmail(usuario.Email!))
        {
            return Resultado<RegistroDeAcessoDTO>.Conflito("E-mail já cadastrado.");
        }

        // D7 (unicidade no serviço, sem índice no banco): CPF repetido devolve 409.
        if (_usuarios.GetByCpf(usuario.Cpf!) is not null)
        {
            return Resultado<RegistroDeAcessoDTO>.Conflito("CPF já cadastrado.");
        }

        var resultado = _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _usuarios.Add(usuario);
            _unidadeDeTrabalho.Salvar();

            var solicitacao = new SolicitacaoDeAcesso(
                usuario.Id,
                tipo,
                unidadeId,
                dto.Crn,
                DateTime.UtcNow);

            _solicitacoes.Add(solicitacao);
            _unidadeDeTrabalho.Salvar();

            return new RegistroDeAcessoDTO(usuario.Id, solicitacao.Id, "Pendente");
        });

        return Resultado<RegistroDeAcessoDTO>.Ok(resultado);
    }
}
