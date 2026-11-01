using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Exclusão definitiva de cadastros (decisão de 2026-10-31, complementando o ADR-008):
/// só é permitida quando nada referencia o registro; caso contrário responde `409`
/// dizendo o que impede. A checagem vive aqui para os serviços de cadastro manterem uma
/// única responsabilidade.
/// </summary>
public sealed class ExclusaoService
{
    private readonly IEscolaRepository _escolas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IAlunoRepository _alunos;
    private readonly IVinculoRepository _vinculos;
    private readonly ITokenDeCadastroRepository _tokens;
    private readonly IConsumoBootstrapRepository _consumos;

    public ExclusaoService(
        IEscolaRepository escolas,
        IUsuarioRepository usuarios,
        IAlunoRepository alunos,
        IVinculoRepository vinculos,
        ITokenDeCadastroRepository tokens,
        IConsumoBootstrapRepository consumos)
    {
        _escolas = escolas;
        _usuarios = usuarios;
        _alunos = alunos;
        _vinculos = vinculos;
        _tokens = tokens;
        _consumos = consumos;
    }

    public Resultado<bool> ExcluirEscola(int id)
    {
        var escola = _escolas.GetEscola(id);

        if (escola is null)
        {
            return Resultado<bool>.NaoEncontrado("Escola não encontrada.");
        }

        if (_usuarios.ExistePorEscola(id))
        {
            return Resultado<bool>.Conflito(
                "Há usuários vinculados a esta escola. Exclua ou mova essas contas antes.");
        }

        // Unidade deixou de pertencer a escola (ADR-013); só unidades legadas ainda a citam.
        if (_escolas.ExisteUnidadeDaEscola(id))
        {
            return Resultado<bool>.Conflito(
                "Há unidades com esta escola como origem. Exclua essas unidades antes.");
        }

        _escolas.Delete(escola);

        return Resultado<bool>.Ok(true);
    }

    public Resultado<bool> ExcluirUnidade(int id)
    {
        var unidade = _escolas.GetUnidade(id);

        if (unidade is null)
        {
            return Resultado<bool>.NaoEncontrado("Unidade não encontrada.");
        }

        if (_usuarios.ExistePorUnidade(id))
        {
            return Resultado<bool>.Conflito(
                "Há usuários vinculados a esta unidade. Mova ou exclua essas contas antes.");
        }

        if (_alunos.ExistePorUnidade(id))
        {
            return Resultado<bool>.Conflito(
                "Há alunos vinculados a esta unidade. Mova ou exclua esses alunos antes.");
        }

        if (_tokens.ExistePorUnidade(id) || _consumos.UnidadeJaConsumiu(id))
        {
            return Resultado<bool>.Conflito(
                "Esta unidade já participou do cadastro por token e não pode ser excluída.");
        }

        _escolas.DeleteUnidade(unidade);

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

        if (usuario.Id == escopo.UsuarioId)
        {
            return Resultado<bool>.Conflito("Não é possível excluir a própria conta.");
        }

        if (usuario.Papel == PapelUsuario.AdminSecretaria &&
            _usuarios.ContarPorPapel(PapelUsuario.AdminSecretaria) <= 1)
        {
            return Resultado<bool>.Conflito(
                "Não é possível excluir o último administrador da secretaria.");
        }

        if (_vinculos.ExistePorResponsavel(id))
        {
            return Resultado<bool>.Conflito(
                "Este responsável ainda está vinculado a alunos. Remova os vínculos antes.");
        }

        if (_tokens.ExistePorCriador(id))
        {
            return Resultado<bool>.Conflito(
                "Este usuário já emitiu token de cadastro e não pode ser excluído.");
        }

        _usuarios.Delete(usuario);

        return Resultado<bool>.Ok(true);
    }
}
