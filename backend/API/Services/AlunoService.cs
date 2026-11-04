using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso de aluno. Orquestra: carrega o catálogo ativo pelo port
/// <see cref="IRestricaoRepository"/>, delega ao domínio a resolução dos nomes
/// (<see cref="CatalogoDeRestricoes"/>) e à entidade as invariantes de aluno (ADR-004).
/// Nenhuma regra de vínculo ou de negócio mora aqui.
///
/// Referências institucionais (escola e unidade derivada) são verificadas antes de
/// qualquer mutação: a entidade valida o formato, a aplicação valida existência e
/// situação (Spec 024, F4; Spec 027).
///
/// O histórico é registrado no próprio agregado antes de persistir, então aluno e eventos
/// são gravados na mesma <c>SaveChanges</c> (RNF2): não existe aluno alterado sem evento.
/// </summary>
public class AlunoService
{
    private readonly IAlunoRepository _alunoRepository;
    private readonly IRestricaoRepository _catalogoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly HierarquiaService _hierarquia;
    private readonly AutorizacaoService _autorizacao;
    private readonly ITrilhaAcaoRepository _trilha;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public AlunoService(
        IAlunoRepository alunoRepository,
        IRestricaoRepository catalogoRepository,
        IUsuarioRepository usuarioRepository,
        HierarquiaService hierarquia,
        AutorizacaoService autorizacao,
        ITrilhaAcaoRepository trilha,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _alunoRepository = alunoRepository;
        _catalogoRepository = catalogoRepository;
        _usuarioRepository = usuarioRepository;
        _hierarquia = hierarquia;
        _autorizacao = autorizacao;
        _trilha = trilha;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    /// <summary>
    /// Cria o aluno e devolve o contrato de saída com o id gerado. O repositório chama
    /// <c>SaveChanges</c> dentro de <c>Add</c>, então o id já está preenchido.
    /// </summary>
    public AlunoResposta CriarAluno(AlunoEntrada entrada, EscopoUsuario escopo)
    {
        var restricoes = ResolverRestricoes(entrada.Restricoes);
        var escola = _hierarquia.ObterEscolaAtiva(entrada.EscolaId);
        var unidadeId = _hierarquia.DerivarUnidade(escola);

        Exigir(Recurso.Aluno, escopo, OperacaoDeAcesso.Criar,
            new AlvoDeEscopo(unidadeId, escola.Id));

        Aluno aluno = new(
            entrada.Nome,
            entrada.Nascimento,
            new Turma(entrada.Escola, entrada.Serie),
            entrada.Telefone,
            entrada.Observacao,
            restricoes,
            entrada.Anafilaxia,
            new Responsavel(entrada.NomeResponsavel, entrada.TelefoneResponsavel),
            escola.Id
        );

        aluno.DefinirUnidade(unidadeId);

        // Criação gera um evento "Criado" e um por restrição (Spec 023, RF2).
        var autor = NomeDoAutor(escopo);
        aluno.RegistrarCriacao(autor);
        aluno.RegistrarHistorico(
            Array.Empty<AlteracaoDeDado>(), restricoes, Array.Empty<string>(), autor);

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _alunoRepository.Add(aluno));

        return ParaResposta(aluno);
    }

    /// <summary>
    /// Confere se os filtros de consulta informados pertencem ao escopo da sessão
    /// (Spec 028, RF10). Filtro fora do escopo responde 403; nunca é ignorado nem amplia.
    /// </summary>
    public bool FiltroNoEscopo(int? escolaId, int? unidadeId, EscopoUsuario escopo)
    {
        if (unidadeId is > 0 && escopo.UnidadeId != unidadeId)
        {
            return false;
        }

        if (escolaId is not > 0)
        {
            return true;
        }

        var escola = _hierarquia.ObterEscola(escolaId.Value);

        if (escola is null)
        {
            return false;
        }

        return escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista =>
                escola.UnidadeId == escopo.UnidadeId,
            PapelUsuario.AdminEscola or PapelUsuario.Merendeira =>
                escola.Id == escopo.EscolaId,
            _ => false
        };
    }

    /// <summary>
    /// Lista apenas o que o escopo do solicitante permite (Spec 027). `escolaId` e
    /// `unidadeId` da requisição estreitam o recorte; nunca o ampliam (RF13).
    /// </summary>
    public List<AlunoResposta> ListarAlunos(
        EscopoUsuario escopo,
        int? escolaId = null,
        int? unidadeId = null)
    {
        var filtro = FiltrosDeConsulta.ParaAluno(escopo, escolaId, unidadeId);

        return _alunoRepository.Listar(filtro).Select(ParaResposta).ToList();
    }

    /// <summary>
    /// Leitura por id com o contrato de fronteira da Spec 027/028: recurso inexistente é
    /// 404; recurso existente fora do escopo é 404 na fronteira de unidade e 403 na
    /// fronteira de escola ou de vínculo do responsável.
    /// </summary>
    public Resultado<AlunoResposta> ObterAlunoPorId(int id, EscopoUsuario escopo)
    {
        Aluno? aluno = _alunoRepository.Obter(id, FiltrosDeConsulta.ParaAluno(escopo));

        if (aluno != null)
        {
            return Resultado<AlunoResposta>.Ok(ParaResposta(aluno));
        }

        if (_alunoRepository.ObterPorId(id) is null)
        {
            return Resultado<AlunoResposta>.NaoEncontrado("Aluno não encontrado.");
        }

        return escopo.Papel is PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista
            ? Resultado<AlunoResposta>.NaoEncontrado("Aluno não encontrado.")
            : Resultado<AlunoResposta>.Proibido();
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

        Exigir(Recurso.Aluno, escopo, OperacaoDeAcesso.Atualizar,
            new AlvoDeEscopo(aluno.UnidadeId, aluno.EscolaId));

        var restricoes = ResolverRestricoes(entrada.Restricoes);
        var escola = _hierarquia.ObterEscolaAtiva(entrada.EscolaId);
        var unidadeId = _hierarquia.DerivarUnidade(escola);

        // RF11: a edição não é porta de transferência entre unidades; para isso existe
        // a rota dedicada, limitada à mesma unidade.
        _hierarquia.ExigirMesmaUnidade(aluno.UnidadeId, unidadeId);

        var (alteracoes, adicionadas, removidas) = aluno.AtualizarDados(
            entrada.Nome,
            entrada.Nascimento,
            new Turma(entrada.Escola, entrada.Serie),
            entrada.Telefone,
            entrada.Observacao,
            restricoes,
            entrada.Anafilaxia,
            new Responsavel(entrada.NomeResponsavel, entrada.TelefoneResponsavel),
            unidadeId,
            escola.Id
        );

        // Campo ou conjunto inalterado devolve lista vazia e não gera evento (RF3).
        aluno.RegistrarHistorico(alteracoes, adicionadas, removidas, NomeDoAutor(escopo));

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _alunoRepository.UpdateAluno(aluno));

        return ParaResposta(aluno);
    }

    /// <summary>
    /// Transfere o aluno entre escolas da mesma unidade (Spec 027, RF11), registrando a
    /// mudança na trilha do aluno. Escola de outra unidade é recusada.
    /// </summary>
    public AlunoResposta? TransferirEscola(
        int id,
        int escolaId,
        EscopoUsuario escopo)
    {
        var aluno = _alunoRepository.Obter(id, FiltrosDeConsulta.ParaAluno(escopo));
        if (aluno == null)
        {
            return null;
        }

        Exigir(Recurso.Aluno, escopo, OperacaoDeAcesso.Atualizar,
            new AlvoDeEscopo(aluno.UnidadeId, aluno.EscolaId));

        var destino = _hierarquia.ObterEscolaAtiva(escolaId);
        var unidadeDestino = _hierarquia.DerivarUnidade(destino);

        _hierarquia.ExigirMesmaUnidade(aluno.UnidadeId, unidadeDestino);

        var escolaAnterior = aluno.EscolaId?.ToString() ?? string.Empty;
        aluno.TransferirEscola(destino.Id, unidadeDestino, mesmaUnidade: true);

        aluno.RegistrarHistorico(
            new[]
            {
                new AlteracaoDeDado(
                    "Escola vinculada",
                    escolaAnterior,
                    destino.Id.ToString())
            },
            Array.Empty<string>(),
            Array.Empty<string>(),
            NomeDoAutor(escopo));

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _alunoRepository.UpdateAluno(aluno);
            _trilha.Add(new TrilhaAcao(
                "AlunoTransferido",
                escopo.UsuarioId,
                NomeDoAutor(escopo),
                "Aluno",
                aluno.Id,
                escolaAnterior,
                destino.Id.ToString(),
                DateTime.UtcNow));
        });

        return ParaResposta(aluno);
    }

    public bool ExcluirAluno(int id, EscopoUsuario escopo)
    {
        Aluno? aluno = _alunoRepository.Obter(id, FiltrosDeConsulta.ParaAluno(escopo));
        if (aluno == null) return false;

        Exigir(Recurso.Aluno, escopo, OperacaoDeAcesso.Excluir,
            new AlvoDeEscopo(aluno.UnidadeId, aluno.EscolaId));

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _alunoRepository.DeleteAluno(aluno));
        return true;
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

    /// <summary>
    /// Aplica a matriz antes de qualquer mutação (Spec 028, I5). A negação vira 403 ou
    /// 404 no middleware, sem revelar existência entre unidades.
    /// </summary>
    private void Exigir(
        Recurso recurso,
        EscopoUsuario escopo,
        OperacaoDeAcesso operacao,
        AlvoDeEscopo alvo)
    {
        if (_autorizacao.Verificar(recurso, escopo, operacao, alvo) is { } status)
        {
            throw new AcessoNegadoException(status == ResultadoStatus.NaoEncontrado);
        }
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
            EscolaId = aluno.EscolaId,
            UnidadeId = aluno.UnidadeId
        };
    }
}
