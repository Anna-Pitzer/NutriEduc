using Ntc.Application;
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
    private readonly IAlunoRepository _alunos;
    private readonly IVinculoRepository _vinculos;
    private readonly ITokenDeCadastroRepository _tokens;
    private readonly IConsumoBootstrapRepository _consumos;
    private readonly ICardapioRepository _cardapios;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public ExclusaoService(
        IEscolaRepository escolas,
        IUsuarioRepository usuarios,
        IAlunoRepository alunos,
        IVinculoRepository vinculos,
        ITokenDeCadastroRepository tokens,
        IConsumoBootstrapRepository consumos,
        ICardapioRepository cardapios,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _escolas = escolas;
        _usuarios = usuarios;
        _alunos = alunos;
        _vinculos = vinculos;
        _tokens = tokens;
        _consumos = consumos;
        _cardapios = cardapios;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    public Resultado<bool> ExcluirEscola(int id)
    {
        var escola = _escolas.GetEscola(id);

        if (escola is null)
        {
            return Resultado<bool>.NaoEncontrado("Escola não encontrada.");
        }

        var impedimento = PoliticaDeExclusao.ParaEscola(
            temUsuarios: _usuarios.ExistePorEscola(id),
            // Unidade deixou de pertencer a escola (ADR-013); só as legadas ainda a citam.
            temUnidadesLegadas: _escolas.ExisteUnidadeDaEscola(id));

        if (impedimento is not null)
        {
            return Resultado<bool>.Conflito(impedimento);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _escolas.Delete(escola));

        return Resultado<bool>.Ok(true);
    }

    public Resultado<bool> ExcluirUnidade(int id)
    {
        var unidade = _escolas.GetUnidade(id);

        if (unidade is null)
        {
            return Resultado<bool>.NaoEncontrado("Unidade não encontrada.");
        }

        var impedimento = PoliticaDeExclusao.ParaUnidade(
            temUsuarios: _usuarios.ExistePorUnidade(id),
            temAlunos: _alunos.ExistePorUnidade(id),
            temTokenOuConsumo: _tokens.ExistePorUnidade(id) || _consumos.UnidadeJaConsumiu(id),
            temCardapios: _cardapios.ExisteCardapioDaUnidade(id));

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

        var administra = escopo.Administra &&
            (escopo.Global || usuario.EscolaId == escopo.EscolaId);

        if (!administra)
        {
            return Resultado<bool>.Proibido();
        }

        var impedimento = PoliticaDeExclusao.ParaUsuario(
            alvoId: usuario.Id,
            alvoEhAdminSecretaria: usuario.Papel == PapelUsuario.AdminSecretaria,
            solicitanteId: escopo.UsuarioId,
            totalAdminSecretaria: _usuarios.ContarPorPapel(PapelUsuario.AdminSecretaria),
            temVinculos: _vinculos.ExistePorResponsavel(id),
            temTokens: _tokens.ExistePorCriador(id));

        if (impedimento is not null)
        {
            return Resultado<bool>.Conflito(impedimento);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _usuarios.Delete(usuario));

        return Resultado<bool>.Ok(true);
    }
}
