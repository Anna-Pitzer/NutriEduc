using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Exclusão definitiva de cadastros (ADR-014). Este serviço faz apenas a orquestração:
/// carrega o registro, decide <b>quem pode excluir</b> (escopo do solicitante), consulta a
/// política de domínio sobre <b>o que impede a exclusão</b> e executa (Spec 024, F7).
/// </summary>
public sealed class ExclusaoService
{
    private readonly IEscolaRepository _escolas;
    private readonly IUsuarioRepository _usuarios;
    private readonly INotificacaoRepository _notificacoes;
    private readonly IAlunoRepository _alunos;
    private readonly IVinculoRepository _vinculos;
    private readonly ITokenDeCadastroRepository _tokens;
    private readonly IConsumoBootstrapRepository _consumos;
    private readonly ICardapioRepository _cardapios;
    private readonly AutorizacaoService _autorizacao;
    private readonly ITrilhaAcaoRepository _trilha;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public ExclusaoService(
        IEscolaRepository escolas,
        IUsuarioRepository usuarios,
        INotificacaoRepository notificacoes,
        IAlunoRepository alunos,
        IVinculoRepository vinculos,
        ITokenDeCadastroRepository tokens,
        IConsumoBootstrapRepository consumos,
        ICardapioRepository cardapios,
        AutorizacaoService autorizacao,
        ITrilhaAcaoRepository trilha,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _escolas = escolas;
        _usuarios = usuarios;
        _notificacoes = notificacoes;
        _alunos = alunos;
        _vinculos = vinculos;
        _tokens = tokens;
        _consumos = consumos;
        _cardapios = cardapios;
        _autorizacao = autorizacao;
        _trilha = trilha;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    public Resultado<bool> ExcluirEscola(int id, EscopoUsuario escopo)
    {
        var escola = _escolas.GetEscola(id);

        if (escola is null)
        {
            return Resultado<bool>.NaoEncontrado("Escola não encontrada.");
        }

        var negacao = _autorizacao.Verificar(
            Recurso.Escola,
            escopo,
            OperacaoDeAcesso.Excluir,
            new AlvoDeEscopo(escola.UnidadeId, escola.Id));

        if (negacao is not null)
        {
            return Negado(negacao);
        }

        var impedimento = PoliticaDeExclusao.ParaEscola(
            temUsuarios: _usuarios.ExistePorEscola(id),
            // Unidade deixou de pertencer a escola (ADR-013); só as legadas ainda a citam.
            temUnidadesLegadas: _escolas.ExisteUnidadeDaEscola(id),
            temAlunos: _alunos.ExistePorEscola(id));

        if (impedimento is not null)
        {
            return Resultado<bool>.Conflito(impedimento);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _escolas.Delete(escola));

        return Resultado<bool>.Ok(true);
    }

    public Resultado<bool> ExcluirUnidade(int id, EscopoUsuario escopo)
    {
        var unidade = _escolas.GetUnidade(id);

        if (unidade is null)
        {
            return Resultado<bool>.NaoEncontrado("Unidade não encontrada.");
        }

        var negacao = _autorizacao.Verificar(
            Recurso.Unidade,
            escopo,
            OperacaoDeAcesso.Excluir,
            new AlvoDeEscopo(unidade.Id, null));

        if (negacao is not null)
        {
            return Negado(negacao);
        }

        var impedimento = PoliticaDeExclusao.ParaUnidade(
            temUsuarios: _usuarios.ExistePorUnidade(id),
            temAlunos: _alunos.ExistePorUnidade(id),
            temTokenOuConsumo: _tokens.ExistePorUnidade(id) || _consumos.UnidadeJaConsumiu(id),
            temCardapios: _cardapios.ExisteCardapioDaUnidade(id),
            temEscolas: _escolas.ListarPorUnidade(id).Count > 0);

        if (impedimento is not null)
        {
            return Resultado<bool>.Conflito(impedimento);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _escolas.DeleteUnidade(unidade));

        return Resultado<bool>.Ok(true);
    }

    public Resultado<bool> ExcluirUsuario(int id, EscopoUsuario escopo)
    {
        var usuario = _usuarios.GetById(id);

        if (usuario is null)
        {
            return Resultado<bool>.NaoEncontrado("Usuário não encontrado.");
        }

        var negacao = _autorizacao.Verificar(
            Recurso.ContaUsuario,
            escopo,
            OperacaoDeAcesso.Excluir,
            new AlvoDeEscopo(usuario.UnidadeId, usuario.EscolaId));

        if (negacao is not null)
        {
            return Negado(negacao);
        }

        var impedimento = PoliticaDeExclusao.ParaUsuario(
            alvoId: usuario.Id,
            alvoEhAdminSecretaria: usuario.Papel == PapelUsuario.AdminSecretaria,
            solicitanteId: escopo.UsuarioId,
            totalAdminSecretaria: _usuarios.ContarPorPapel(PapelUsuario.AdminSecretaria),
            temVinculos: _vinculos.ExistePorResponsavel(id),
            temTokens: _tokens.ExistePorCriador(id),
            temNotificacoes: _notificacoes.ExistePorDestinatario(id));

        if (impedimento is not null)
        {
            return Resultado<bool>.Conflito(impedimento);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _usuarios.Delete(usuario);
            _trilha.Add(new TrilhaAcao(
                "UsuarioExcluido",
                escopo.UsuarioId,
                _usuarios.GetById(escopo.UsuarioId)?.Nome ?? "sistema",
                "Usuario",
                usuario.Id,
                usuario.Papel?.ToString(),
                null,
                DateTime.UtcNow));
        });

        return Resultado<bool>.Ok(true);
    }

    private static Resultado<bool> Negado(ResultadoStatus? status) =>
        status == ResultadoStatus.NaoEncontrado
            ? Resultado<bool>.NaoEncontrado("Recurso não encontrado.")
            : Resultado<bool>.Proibido();
}
