using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Vínculo responsável↔aluno (Spec 027, RF7; Spec 028, matriz). Toda decisão de acesso
/// está aqui: o controller só traduz o <see cref="Resultado{T}"/> em HTTP. Escrita de
/// vínculo é exclusiva de <c>AdminSecretaria</c> (D3); os demais papéis leem no escopo.
/// </summary>
public class VinculoService
{
    private readonly IAlunoRepository _alunos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IVinculoRepository _vinculos;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly AutorizacaoService _autorizacao;
    private readonly ITrilhaAcaoRepository _trilha;

    public VinculoService(
        IAlunoRepository alunos,
        IUsuarioRepository usuarios,
        IVinculoRepository vinculos,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        AutorizacaoService autorizacao,
        ITrilhaAcaoRepository trilha)
    {
        _alunos = alunos;
        _usuarios = usuarios;
        _vinculos = vinculos;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _autorizacao = autorizacao;
        _trilha = trilha;
    }

    public Resultado<bool> Vincular(int alunoId, int usuarioId, EscopoUsuario escopo)
    {
        var aluno = _alunos.Obter(alunoId, FiltrosDeConsulta.ParaAluno(escopo));

        if (aluno is null)
        {
            return escopo.EscopoDeUnidade
                ? Resultado<bool>.NaoEncontrado("Aluno não encontrado.")
                : Resultado<bool>.Proibido();
        }

        var negacao = _autorizacao.Verificar(
            Recurso.Vinculo,
            escopo,
            OperacaoDeAcesso.Criar,
            new AlvoDeEscopo(aluno.UnidadeId, aluno.EscolaId));

        if (negacao is not null)
        {
            return negacao == ResultadoStatus.NaoEncontrado
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

        if (!usuario.Ativo)
        {
            return Resultado<bool>.Conflito("Usuário desativado não pode ser vinculado.");
        }

        if (_vinculos.Existe(alunoId, usuarioId))
        {
            return Resultado<bool>.Conflito(
                "Este responsável já está vinculado ao aluno.");
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _vinculos.Add(new VinculoResponsavelAluno(alunoId, usuarioId));
            _trilha.Add(new TrilhaAcao(
                "VinculoCriado",
                escopo.UsuarioId,
                NomeDoAutor(escopo),
                "Responsavel",
                usuarioId,
                null,
                alunoId.ToString(),
                DateTime.UtcNow));
        });

        return Resultado<bool>.Ok(true);
    }

    public Resultado<bool> Desvincular(int alunoId, int usuarioId, EscopoUsuario escopo)
    {
        var aluno = _alunos.Obter(alunoId, FiltrosDeConsulta.ParaAluno(escopo));

        if (aluno is null)
        {
            return escopo.EscopoDeUnidade
                ? Resultado<bool>.NaoEncontrado("Aluno não encontrado.")
                : Resultado<bool>.Proibido();
        }

        var negacao = _autorizacao.Verificar(
            Recurso.Vinculo,
            escopo,
            OperacaoDeAcesso.Excluir,
            new AlvoDeEscopo(aluno.UnidadeId, aluno.EscolaId));

        if (negacao is not null)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<bool>.NaoEncontrado("Aluno não encontrado.")
                : Resultado<bool>.Proibido();
        }

        var removido = false;

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            removido = _vinculos.Remover(alunoId, usuarioId);

            if (!removido)
            {
                return;
            }

            _trilha.Add(new TrilhaAcao(
                "VinculoRemovido",
                escopo.UsuarioId,
                NomeDoAutor(escopo),
                "Responsavel",
                usuarioId,
                alunoId.ToString(),
                null,
                DateTime.UtcNow));
        });

        return removido
            ? Resultado<bool>.Ok(true)
            : Resultado<bool>.NaoEncontrado("Vínculo não encontrado.");
    }

    public Resultado<List<ResponsavelVinculadoDTO>> Listar(int alunoId, EscopoUsuario escopo)
    {
        var aluno = _alunos.Obter(alunoId, FiltrosDeConsulta.ParaAluno(escopo));

        if (aluno is null)
        {
            return escopo.EscopoDeUnidade
                ? Resultado<List<ResponsavelVinculadoDTO>>.NaoEncontrado("Aluno não encontrado.")
                : Resultado<List<ResponsavelVinculadoDTO>>.Proibido();
        }

        var negacao = _autorizacao.Verificar(
            Recurso.Vinculo,
            escopo,
            OperacaoDeAcesso.Ler,
            new AlvoDeEscopo(
                aluno.UnidadeId,
                aluno.EscolaId,
                VinculadoAoResponsavel: escopo.Papel == PapelUsuario.Responsavel));

        if (negacao is not null)
        {
            return negacao == ResultadoStatus.NaoEncontrado
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

    private string NomeDoAutor(EscopoUsuario escopo) =>
        _usuarios.GetById(escopo.UsuarioId)?.Nome ?? "sistema";
}
