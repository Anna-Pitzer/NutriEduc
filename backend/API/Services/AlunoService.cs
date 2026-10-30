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

            _alunoRepository.Add(aluno);

            return ParaDTO(aluno);
        }

        public List<DadosAlunoDTO> ListarAlunos()
        {
            var alunos = _alunoRepository.GetAlunos() ?? new();
            return alunos.Select(ParaDTO).ToList();
        }

        public DadosAlunoDTO? ObterAlunoPorId(int id)
        {
            Aluno? aluno = _alunoRepository.GetAluno(id);
            return aluno == null ? null : ParaDTO(aluno);
        }

        /// <summary>
        /// Delega a atualização ao domínio: <see cref="Aluno.AtualizarDados"/> valida
        /// tudo antes de alterar o estado, então uma exceção deixa o objeto intacto.
        /// Retorna <c>null</c> quando o id não existe (o controller responde 404).
        /// </summary>
        public DadosAlunoDTO? AtualizarAluno(int id, DadosAlunoDTO dto)
        {
            Aluno? aluno = _alunoRepository.GetAluno(id);
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

            _alunoRepository.UpdateAluno(aluno);

            return ParaDTO(aluno);
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
                aluno.Responsavel.Telefone
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