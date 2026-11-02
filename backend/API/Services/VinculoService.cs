using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Vínculo responsável↔aluno (RF6). Toda decisão de acesso está aqui: o controller só
/// traduz o <see cref="Resultado{T}"/> em HTTP. AdminEscola só vincula dentro da própria
/// escola; AdminSecretaria, por ser global, também pode.
/// </summary>
public class VinculoService
{
    private readonly IAlunoRepository _alunos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IVinculoRepository _vinculos;
    private readonly IEscolaRepository _escolas;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public VinculoService(
        IAlunoRepository alunos,
        IUsuarioRepository usuarios,
        IVinculoRepository vinculos,
        IEscolaRepository escolas,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _alunos = alunos;
        _usuarios = usuarios;
        _vinculos = vinculos;
        _escolas = escolas;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    public Resultado<bool> Vincular(int alunoId, int usuarioId, EscopoUsuario escopo)
    {
        if (escopo.Papel is not (PapelUsuario.AdminEscola or PapelUsuario.AdminSecretaria))
        {
            return Resultado<bool>.Proibido();
        }

        var aluno = _alunos.Obter(alunoId, FiltrosDeConsulta.ParaAluno(escopo));

        if (aluno is null)
        {
            return escopo.Global
                ? Resultado<bool>.NaoEncontrado("Aluno não encontrado.")
                : Resultado<bool>.Proibido();
        }

        var usuario = _usuarios.GetById(usuarioId);

        if (usuario is null)
        {
            return Resultado<bool>.NaoEncontrado("Usuário não encontrado.");
        }

        if (usuario.Papel != PapelUsuario.Responsavel)
        {
            return Resultado<bool>.Conflito("O usuário informado não é responsável.");
        }

        // Unidade é da rede (Spec 022): quando a unidade não guarda escola de origem, não
        // existe escola do aluno para comparar, e a checagem de escola não se aplica. O
        // acesso à rota já é limitado pelo escopo do solicitante.
        var escolaDoAluno = EscolaDoAluno(aluno);

        if (escolaDoAluno is not null && usuario.EscolaId != escolaDoAluno)
        {
            return Resultado<bool>.Conflito(
                "O responsável pertence a outra escola.");
        }

        if (_vinculos.Existe(alunoId, usuarioId))
        {
            return Resultado<bool>.Conflito(
                "Este responsável já está vinculado ao aluno.");
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _vinculos.Add(new VinculoResponsavelAluno(alunoId, usuarioId)));

        return Resultado<bool>.Ok(true);
    }

    public Resultado<bool> Desvincular(int alunoId, int usuarioId, EscopoUsuario escopo)
    {
        if (escopo.Papel is not (PapelUsuario.AdminEscola or PapelUsuario.AdminSecretaria))
        {
            return Resultado<bool>.Proibido();
        }

        var aluno = _alunos.Obter(alunoId, FiltrosDeConsulta.ParaAluno(escopo));

        if (aluno is null)
        {
            return escopo.Global
                ? Resultado<bool>.NaoEncontrado("Aluno não encontrado.")
                : Resultado<bool>.Proibido();
        }

        if (!_vinculos.Remover(alunoId, usuarioId))
        {
            return Resultado<bool>.NaoEncontrado("Vínculo não encontrado.");
        }

        return Resultado<bool>.Ok(true);
    }

    public Resultado<List<ResponsavelVinculadoDTO>> Listar(int alunoId, EscopoUsuario escopo)
    {
        var aluno = _alunos.Obter(alunoId, FiltrosDeConsulta.ParaAluno(escopo));

        if (aluno is null)
        {
            return escopo.Global
                ? Resultado<List<ResponsavelVinculadoDTO>>.NaoEncontrado("Aluno não encontrado.")
                : Resultado<List<ResponsavelVinculadoDTO>>.Proibido();
        }

        var responsaveis = _vinculos
            .ListarPorAluno(alunoId)
            .Select(vinculo => _usuarios.GetById(vinculo.ResponsavelUsuarioId))
            .Where(usuario => usuario is not null)
            .Select(usuario => new ResponsavelVinculadoDTO(
                usuario!.Id, usuario.Nome, usuario.Email))
            .ToList();

        return Resultado<List<ResponsavelVinculadoDTO>>.Ok(responsaveis);
    }

    private int? EscolaDoAluno(Aluno aluno)
    {
        if (aluno.UnidadeId is null)
        {
            return null;
        }

        return _escolas.GetUnidade(aluno.UnidadeId.Value)?.EscolaId;
    }
}
