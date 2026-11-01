using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Caso de uso de aluno. Orquestra: carrega o catálogo ativo pelo port
/// <see cref="IRestricaoRepository"/>, delega ao domínio a resolução dos nomes
/// (<see cref="CatalogoDeRestricoes"/>) e à entidade as invariantes de aluno (ADR-004).
/// Nenhuma regra de vínculo ou de negócio mora aqui.
///
/// Referências institucionais (unidade) são verificadas antes de qualquer mutação: a
/// entidade valida o formato, a aplicação valida existência e situação (Spec 024, F4).
///
/// O histórico é registrado no próprio agregado antes de persistir, então aluno e eventos
/// são gravados na mesma <c>SaveChanges</c> (RNF2): não existe aluno alterado sem evento.
/// </summary>
public class AlunoService
{
    private readonly IAlunoRepository _alunoRepository;
    private readonly IRestricaoRepository _catalogoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEscolaRepository _escolaRepository;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public AlunoService(
        IAlunoRepository alunoRepository,
        IRestricaoRepository catalogoRepository,
        IUsuarioRepository usuarioRepository,
        IEscolaRepository escolaRepository,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _alunoRepository = alunoRepository;
        _catalogoRepository = catalogoRepository;
        _usuarioRepository = usuarioRepository;
        _escolaRepository = escolaRepository;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    /// <summary>
    /// Cria o aluno e devolve o contrato de saída com o id gerado. O repositório chama
    /// <c>SaveChanges</c> dentro de <c>Add</c>, então o id já está preenchido.
    /// </summary>
    public AlunoResposta CriarAluno(AlunoEntrada entrada, EscopoUsuario escopo)
    {
        var restricoes = ResolverRestricoes(entrada.Restricoes);

        GarantirUnidade(entrada.UnidadeId);

        Aluno aluno = new(
            entrada.Nome,
            entrada.Nascimento,
            new Turma(entrada.Escola, entrada.Serie),
            entrada.Telefone,
            entrada.Observacao,
            restricoes,
            entrada.Anafilaxia,
            new Responsavel(entrada.NomeResponsavel, entrada.TelefoneResponsavel)
        );

        aluno.DefinirUnidade(entrada.UnidadeId);

        // Criação gera um evento "Criado" e um por restrição (Spec 023, RF2).
        var autor = NomeDoAutor(escopo);
        aluno.RegistrarCriacao(autor);
        aluno.RegistrarHistorico(
            Array.Empty<AlteracaoDeDado>(), restricoes, Array.Empty<string>(), autor);

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _alunoRepository.Add(aluno));

        return ParaResposta(aluno);
    }

    /// <summary>Lista apenas o que o escopo do solicitante permite (Spec 009).</summary>
    public List<AlunoResposta> ListarAlunos(EscopoUsuario escopo)
    {
        return _alunoRepository.Listar(FiltrosDeConsulta.ParaAluno(escopo)).Select(ParaResposta).ToList();
    }

    public AlunoResposta? ObterAlunoPorId(int id, EscopoUsuario escopo)
    {
        Aluno? aluno = _alunoRepository.Obter(id, FiltrosDeConsulta.ParaAluno(escopo));
        return aluno == null ? null : ParaResposta(aluno);
    }

    /// <summary>
    /// Delega a atualização ao domínio: <see cref="Aluno.AtualizarDados"/> valida tudo antes
    /// de alterar o estado e devolve o diff, usado para o histórico (Spec 023, RF3).
    /// Retorna <c>null</c> quando o id não existe ou está fora do escopo.
    /// </summary>
    public AlunoResposta? AtualizarAluno(int id, AlunoEntrada entrada, EscopoUsuario escopo)
    {
        Aluno? aluno = _alunoRepository.Obter(id, FiltrosDeConsulta.ParaAluno(escopo));
        if (aluno == null)
        {
            return null;
        }

        var restricoes = ResolverRestricoes(entrada.Restricoes);

        GarantirUnidade(entrada.UnidadeId);

        var (alteracoes, adicionadas, removidas) = aluno.AtualizarDados(
            entrada.Nome,
            entrada.Nascimento,
            new Turma(entrada.Escola, entrada.Serie),
            entrada.Telefone,
            entrada.Observacao,
            restricoes,
            entrada.Anafilaxia,
            new Responsavel(entrada.NomeResponsavel, entrada.TelefoneResponsavel),
            entrada.UnidadeId
        );

        // Campo ou conjunto inalterado devolve lista vazia e não gera evento (RF3).
        aluno.RegistrarHistorico(alteracoes, adicionadas, removidas, NomeDoAutor(escopo));

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _alunoRepository.UpdateAluno(aluno));

        return ParaResposta(aluno);
    }

    public bool ExcluirAluno(int id, EscopoUsuario escopo)
    {
        Aluno? aluno = _alunoRepository.Obter(id, FiltrosDeConsulta.ParaAluno(escopo));
        if (aluno == null) return false;

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _alunoRepository.DeleteAluno(aluno));
        return true;
    }

    /// <summary>
    /// Referência institucional verificada antes de mutar o aluno (Spec 024, F4): a
    /// entidade valida só o formato do id; existência e situação são regra de aplicação.
    /// </summary>
    private void GarantirUnidade(int? unidadeId)
    {
        if (unidadeId is null)
        {
            return;
        }

        var unidade = _escolaRepository.GetUnidade(unidadeId.Value)
            ?? throw new ArgumentException("Unidade inexistente.");

        if (!unidade.Ativa)
        {
            throw new ArgumentException("Unidade desativada não pode receber aluno.");
        }
    }

    /// <summary>
    /// Uma consulta ao catálogo, uma decisão no domínio: o serviço não sabe o que é válido,
    /// apenas entrega o conjunto carregado ao <see cref="CatalogoDeRestricoes"/>.
    /// </summary>
    private IReadOnlyList<string> ResolverRestricoes(IEnumerable<string>? nomes)
    {
        var informados = CatalogoDeRestricoes.NomesInformados(nomes);

        if (informados.Count == 0)
        {
            return Array.Empty<string>();
        }

        var entradas = _catalogoRepository.ObterAtivasPorNomes(
            CatalogoDeRestricoes.NomesNormalizados(informados));

        return new CatalogoDeRestricoes(entradas).ResolverNomes(informados);
    }

    /// <summary>Snapshot do autor do evento: o nome é copiado, nunca referenciado por FK.</summary>
    private string NomeDoAutor(EscopoUsuario? escopo)
    {
        if (escopo is null)
        {
            return "sistema";
        }

        return _usuarioRepository.GetById(escopo.UsuarioId)?.Nome ?? "sistema";
    }

    private static AlunoResposta ParaResposta(Aluno aluno)
    {
        return new AlunoResposta
        {
            Id = aluno.Id,
            Nome = aluno.Nome,
            Nascimento = aluno.Nascimento,
            Escola = aluno.Turma.Escola,
            Serie = aluno.Turma.Serie,
            Telefone = aluno.Telefone,
            Observacao = aluno.Observacao,
            Restricoes = aluno.Restricoes.ToList(),
            Anafilaxia = aluno.Anafilaxia,
            NomeResponsavel = aluno.Responsavel.Nome,
            TelefoneResponsavel = aluno.Responsavel.Telefone,
            UnidadeId = aluno.UnidadeId
        };
    }
}
