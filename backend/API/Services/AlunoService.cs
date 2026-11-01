using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

public class AlunoService
{
    private readonly IAlunoRepository _alunoRepository;

    public AlunoService(IAlunoRepository alunoRepository)
    {
        _alunoRepository = alunoRepository;
    }

    /// <summary>
    /// Cria o aluno e devolve o DTO com o id gerado. O repositório chama
    /// <c>SaveChanges</c> dentro de <c>Add</c>, então o id já está preenchido.
    /// </summary>
    public DadosAlunoDTO CriarAluno(DadosAlunoDTO dto)
    {
        Aluno aluno = new(
            dto.Nome,
            dto.Nascimento,
            new Turma(dto.Escola, dto.Serie),
            dto.Telefone,
            dto.Observacao,
            dto.Restricoes,
            dto.Anafilaxia,
            new Responsavel(dto.NomeResponsavel, dto.TelefoneResponsavel)
        );

        aluno.DefinirUnidade(dto.UnidadeId);

        _alunoRepository.Add(aluno);

        return ParaDTO(aluno);
    }

    /// <summary>Lista apenas o que o escopo do solicitante permite (Spec 009).</summary>
    public List<DadosAlunoDTO> ListarAlunos(EscopoUsuario escopo)
    {
        return _alunoRepository.Listar(escopo).Select(ParaDTO).ToList();
    }

    public DadosAlunoDTO? ObterAlunoPorId(int id, EscopoUsuario escopo)
    {
        Aluno? aluno = _alunoRepository.Obter(id, escopo);
        return aluno == null ? null : ParaDTO(aluno);
    }

    /// <summary>
    /// Delega a atualização ao domínio: <see cref="Aluno.AtualizarDados"/> valida
    /// tudo antes de alterar o estado, então uma exceção deixa o objeto intacto.
    /// Retorna <c>null</c> quando o id não existe ou está fora do escopo.
    /// </summary>
    public DadosAlunoDTO? AtualizarAluno(int id, DadosAlunoDTO dto, EscopoUsuario escopo)
    {
        Aluno? aluno = _alunoRepository.Obter(id, escopo);
        if (aluno == null)
        {
            return null;
        }

        aluno.AtualizarDados(
            dto.Nome,
            dto.Nascimento,
            new Turma(dto.Escola, dto.Serie),
            dto.Telefone,
            dto.Observacao,
            dto.Restricoes,
            dto.Anafilaxia,
            new Responsavel(dto.NomeResponsavel, dto.TelefoneResponsavel)
        );

        aluno.DefinirUnidade(dto.UnidadeId);

        _alunoRepository.UpdateAluno(aluno);

        return ParaDTO(aluno);
    }

    public bool ExcluirAluno(int id, EscopoUsuario escopo)
    {
        Aluno? aluno = _alunoRepository.Obter(id, escopo);
        if (aluno == null) return false;

        _alunoRepository.DeleteAluno(aluno);
        return true;
    }

    private static DadosAlunoDTO ParaDTO(Aluno aluno)
    {
        return new DadosAlunoDTO(
            aluno.Nome,
            aluno.Nascimento,
            aluno.Turma.Escola,
            aluno.Turma.Serie,
            aluno.Telefone,
            aluno.Observacao,
            aluno.Restricoes.ToList(),
            aluno.Anafilaxia,
            aluno.Responsavel.Nome,
            aluno.Responsavel.Telefone)
        {
            Id = aluno.Id,
            UnidadeId = aluno.UnidadeId
        };
    }
}
