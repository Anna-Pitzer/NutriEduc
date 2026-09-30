using Ntc.Application;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

public class AlunoService
{
    private readonly IAlunoRepository _alunoRepository;

    public AlunoService(IAlunoRepository alunoRepository)
    {
        _alunoRepository = alunoRepository;
    }

    public bool CriarAluno(DadosAlunoDTO dto)
    {
        Aluno aluno = new(
            dto.Nome,
            dto.Nascimento, 
            dto.Escola,
            dto.Serie,
            dto.Telefone,
            dto.Observacao,
            dto.Anafilaxia,
            dto.NomeResponsavel,
            dto.TelefoneResponsavel
        );

        _alunoRepository.Add(aluno);
        return true;
    }

    public DadosAlunoDTO? ObterAlunoPorId(int id)
    {
        Aluno? aluno = _alunoRepository.GetAluno(id);
        if (aluno == null)
        {
            return null;
        }

        DadosAlunoDTO? dto = new(
            aluno.Nome,
            aluno.Nascimento,
            aluno.Escola,
            aluno.Serie,
            aluno.Telefone,
            aluno.Observacao,
            aluno.Anafilaxia,
            aluno.NomeResponsavel,
            aluno.TelefoneResponsavel
        );

        return dto;
    } 
}