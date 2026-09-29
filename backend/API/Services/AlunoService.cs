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

    //Aqui voce faz as regras de negocio do sist ema (criar, editar, validar...)

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



}   