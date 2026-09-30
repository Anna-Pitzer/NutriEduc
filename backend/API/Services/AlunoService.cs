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

        public List<DadosAlunoDTO> ListarAlunos()
        {
            var alunos = _alunoRepository.GetAlunos() ?? new();
            return alunos.Select(a => new DadosAlunoDTO(
                a.Nome, a.Nascimento, a.Escola, a.Serie,
                a.Telefone, a.Observacao, a.Anafilaxia,
                a.NomeResponsavel, a.TelefoneResponsavel
            ) { Id = a.Id }).ToList();
        }

        public DadosAlunoDTO? ObterAlunoPorId(int id)
        {
            Aluno? aluno = _alunoRepository.GetAluno(id);
            if (aluno == null) return null;

            return new DadosAlunoDTO(
                aluno.Nome, aluno.Nascimento, aluno.Escola, aluno.Serie,
                aluno.Telefone, aluno.Observacao, aluno.Anafilaxia,
                aluno.NomeResponsavel, aluno.TelefoneResponsavel
            ) { Id = aluno.Id };
        }

        public bool ExcluirAluno(int id)
        {
            Aluno? aluno = _alunoRepository.GetAluno(id);
            if (aluno == null) return false;

            _alunoRepository.DeleteAluno(aluno);
            return true;
        }
    }