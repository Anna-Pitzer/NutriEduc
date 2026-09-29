

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

    //Aqui voce faz as regras de negocio do sistema (criar, editar, validar...)

    public bool CriarAluno(DadosAlunoDTO dto)
    {
        //Aluno aluno = new(
            //passa os parametros de dto para criar o objeto aluno
        //);

        return false;
    }



}   