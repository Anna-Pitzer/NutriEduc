using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Caso de uso do catálogo de alimentos e das suas categorias (Spec 011).
///
/// O serviço concentra as decisões: quem lê (RF9), quem pode ver inativos (RF7), qual
/// categoria e quais restrições existem e estão ativas (RF1, RF4) e se o nome já está em
/// uso (RF2). O domínio valida formato e invariantes; o controller apenas traduz
/// <see cref="Resultado{T}"/> em HTTP (ADR-005).
///
/// Alimento e vínculos são gravados na mesma transação via <see cref="IUnidadeDeTrabalho"/>
/// para não existir alimento gravado pela metade.
/// </summary>
public sealed class AlimentoService
{
    private const string OrdenacoesValidas = "nome, nome-desc, categoria ou recentes";

    private readonly IAlimentoRepository _alimentos;
    private readonly ICategoriaAlimentoRepository _categorias;
    private readonly IRestricaoRepository _restricoes;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public AlimentoService(
        IAlimentoRepository alimentos,
        ICategoriaAlimentoRepository categorias,
        IRestricaoRepository restricoes,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _alimentos = alimentos;
        _categorias = categorias;
        _restricoes = restricoes;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    // ---------- Alimentos ----------

    /// <summary>Listagem paginada com busca, categoria, inativos e ordenação.</summary>
    public Resultado<PaginaDTO<AlimentoDTO>> Listar(
        string? busca,
        int? categoriaId,
        int? pagina,
        int? tamanho,
        bool incluirInativos,
        EscopoUsuario escopo,
        string? ordenar = null)
    {
        if (!PodeLer(escopo))
        {
            return Resultado<PaginaDTO<AlimentoDTO>>.Proibido();
        }

        if (incluirInativos && escopo.Papel != PapelUsuario.Nutricionista)
        {
            return Resultado<PaginaDTO<AlimentoDTO>>.Proibido(
                "Somente a nutrição pode consultar itens inativos.");
        }

        if (!TentarLerOrdenacao(ordenar, out var ordenacao))
        {
            return Resultado<PaginaDTO<AlimentoDTO>>.Invalido(
                $"Ordenação inválida: '{ordenar}'. Use {OrdenacoesValidas}.");
        }

        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);

        var filtro = new FiltroAlimento(
            NomeNormalizado: string.IsNullOrWhiteSpace(busca)
                ? null
                : RestricaoAlimentar.Normalizar(busca),
            CategoriaId: categoriaId,
            IncluirInativos: incluirInativos,
            Ordenacao: ordenacao);

        var (itens, total) = _alimentos.Listar(filtro, paginaEfetiva, tamanhoEfetivo);

        return Resultado<PaginaDTO<AlimentoDTO>>.Ok(new PaginaDTO<AlimentoDTO>
        {
            Pagina = paginaEfetiva,
            Tamanho = tamanhoEfetivo,
            Total = total,
            Itens = itens.Select(alimento => ParaDTO(alimento)).ToList()
        });
    }

    /// <summary>
    /// Traduz o parâmetro de consulta em <see cref="OrdenacaoAlimento"/>. Ausente usa o
    /// padrão; valor desconhecido devolve <c>false</c> para o controller responder 400 em
    /// vez de ordenar por outra coisa sem avisar (ADR-005).
    /// </summary>
    private static bool TentarLerOrdenacao(string? texto, out OrdenacaoAlimento ordenacao)
    {
        switch (texto?.Trim().ToLowerInvariant())
        {
            case null or "":
                ordenacao = OrdenacaoAlimento.Nome;
                return true;

            case "nome":
                ordenacao = OrdenacaoAlimento.Nome;
                return true;

            case "nome-desc":
                ordenacao = OrdenacaoAlimento.NomeDescendente;
                return true;

            case "categoria":
                ordenacao = OrdenacaoAlimento.Categoria;
                return true;

            case "recentes":
                ordenacao = OrdenacaoAlimento.Recentes;
                return true;

            default:
                ordenacao = OrdenacaoAlimento.Nome;
                return false;
        }
    }

    public Resultado<AlimentoDTO> Obter(int id, EscopoUsuario escopo)
    {
        if (!PodeLer(escopo))
        {
            return Resultado<AlimentoDTO>.Proibido();
        }

        var alimento = _alimentos.ObterComVinculos(id);

        return alimento is null
            ? Resultado<AlimentoDTO>.NaoEncontrado("Alimento não encontrado.")
            : Resultado<AlimentoDTO>.Ok(ParaDTO(alimento));
    }

    public Resultado<AlimentoDTO> Criar(SalvarAlimentoDTO dto)
    {
        var categoria = _categorias.Obter(dto.CategoriaId);

        if (categoria is null || !categoria.Ativa)
        {
            return Resultado<AlimentoDTO>.Invalido("Categoria de alimento inexistente ou inativa.");
        }

        var valores = ValoresDe(dto.Valores);

        var ids = IdsDeRestricoes(dto.RestricaoIds, out var erroDeIds);

        if (erroDeIds is not null)
        {
            return Resultado<AlimentoDTO>.Invalido(erroDeIds);
        }

        if (ResolverRestricoes(ids) is not { } restricoes)
        {
            return Resultado<AlimentoDTO>.Invalido(
                "Restrição inexistente ou inativa para o vínculo.");
        }

        var alimento = new Alimento(dto.Nome, dto.CategoriaId, valores, ids);

        if (_alimentos.ExisteNomeNormalizado(alimento.NomeNormalizado, ignorarId: null))
        {
            return Resultado<AlimentoDTO>.Conflito("Já existe um alimento com este nome.");
        }

        var criado = _unidadeDeTrabalho.ExecutarEmTransacao(() => _alimentos.Add(alimento));

        return Resultado<AlimentoDTO>.Ok(ParaDTO(criado, categoria, restricoes));
    }

    public Resultado<AlimentoDTO> Atualizar(int id, SalvarAlimentoDTO dto)
    {
        var alimento = _alimentos.ObterComVinculos(id);

        if (alimento is null)
        {
            return Resultado<AlimentoDTO>.NaoEncontrado("Alimento não encontrado.");
        }

        var categoria = _categorias.Obter(dto.CategoriaId);

        if (categoria is null || !categoria.Ativa)
        {
            return Resultado<AlimentoDTO>.Invalido("Categoria de alimento inexistente ou inativa.");
        }

        var valores = ValoresDe(dto.Valores);

        var ids = IdsDeRestricoes(dto.RestricaoIds, out var erroDeIds);

        if (erroDeIds is not null)
        {
            return Resultado<AlimentoDTO>.Invalido(erroDeIds);
        }

        if (ResolverRestricoes(ids) is not { } restricoes)
        {
            return Resultado<AlimentoDTO>.Invalido(
                "Restrição inexistente ou inativa para o vínculo.");
        }

        // Valida em um buffer antes de tocar no estado: nome duplicado ou entrada
        // inválida não altera o alimento já gravado (ADR-004).
        var validado = new Alimento(dto.Nome, dto.CategoriaId, valores, ids);

        if (_alimentos.ExisteNomeNormalizado(validado.NomeNormalizado, ignorarId: id))
        {
            return Resultado<AlimentoDTO>.Conflito("Já existe um alimento com este nome.");
        }

        alimento.AtualizarDados(dto.Nome, dto.CategoriaId, valores, ids);

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _alimentos.Update(alimento));

        return Resultado<AlimentoDTO>.Ok(ParaDTO(alimento, categoria, restricoes));
    }

    /// <summary>Desativar é soft delete (RF6): o alimento continua legível por id.</summary>
    public Resultado<AlimentoDTO> Desativar(int id) => AlterarSituacao(id, desativar: true);

    public Resultado<AlimentoDTO> Reativar(int id) => AlterarSituacao(id, desativar: false);

    // ---------- Categorias ----------

    public Resultado<List<CategoriaAlimentoDTO>> ListarCategorias(
        bool incluirInativas,
        EscopoUsuario escopo)
    {
        if (!PodeLer(escopo))
        {
            return Resultado<List<CategoriaAlimentoDTO>>.Proibido();
        }

        if (incluirInativas && escopo.Papel != PapelUsuario.Nutricionista)
        {
            return Resultado<List<CategoriaAlimentoDTO>>.Proibido(
                "Somente a nutrição pode consultar itens inativos.");
        }

        var categorias = _categorias
            .Listar(incluirInativas)
            .Select(ParaDTO)
            .ToList();

        return Resultado<List<CategoriaAlimentoDTO>>.Ok(categorias);
    }

    public Resultado<CategoriaAlimentoDTO> CriarCategoria(SalvarCategoriaAlimentoDTO dto)
    {
        var categoria = new CategoriaAlimento(dto.Nome);

        if (_categorias.ExisteNomeNormalizado(categoria.NomeNormalizado, ignorarId: null))
        {
            return Resultado<CategoriaAlimentoDTO>.Conflito(
                "Já existe uma categoria com este nome.");
        }

        var criada = _unidadeDeTrabalho.ExecutarEmTransacao(() => _categorias.Add(categoria));

        return Resultado<CategoriaAlimentoDTO>.Ok(ParaDTO(criada));
    }

    public Resultado<CategoriaAlimentoDTO> AtualizarCategoria(
        int id,
        SalvarCategoriaAlimentoDTO dto)
    {
        var categoria = _categorias.Obter(id);

        if (categoria is null)
        {
            return Resultado<CategoriaAlimentoDTO>.NaoEncontrado("Categoria não encontrada.");
        }

        var validada = new CategoriaAlimento(dto.Nome);

        if (_categorias.ExisteNomeNormalizado(validada.NomeNormalizado, ignorarId: id))
        {
            return Resultado<CategoriaAlimentoDTO>.Conflito(
                "Já existe uma categoria com este nome.");
        }

        categoria.DefinirNome(validada.Nome);

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _categorias.Update(categoria));

        return Resultado<CategoriaAlimentoDTO>.Ok(ParaDTO(categoria));
    }

    public Resultado<CategoriaAlimentoDTO> DesativarCategoria(int id) =>
        AlterarSituacaoCategoria(id, desativar: true);

    public Resultado<CategoriaAlimentoDTO> ReativarCategoria(int id) =>
        AlterarSituacaoCategoria(id, desativar: false);

    // ---------- Internos ----------

    /// <summary>
    /// Leitura do catálogo segue a matriz (Spec 028): administração, escola, nutrição e
    /// merenda leem; responsável não. Escrita permanece exclusiva de nutrição.
    /// </summary>
    private static bool PodeLer(EscopoUsuario escopo) =>
        escopo.Papel is PapelUsuario.AdminSecretaria
            or PapelUsuario.AdminEscola
            or PapelUsuario.Nutricionista
            or PapelUsuario.Merendeira;

    private static ValoresNutricionais ValoresDe(ValoresNutricionaisDTO? valores)
    {
        if (valores is null)
        {
            throw new ArgumentException("Os valores nutricionais são obrigatórios.");
        }

        return ValoresNutricionais.Criar(
            valores.CaloriasKcal,
            valores.ProteinasG,
            valores.CarboidratosG,
            valores.GordurasG,
            valores.FibrasG,
            valores.SodioMg);
    }

    private static List<int> IdsDeRestricoes(List<int>? ids, out string? erro)
    {
        erro = null;
        var lista = ids?.ToList() ?? new List<int>();

        if (lista.Any(id => id <= 0))
        {
            erro = "Identificador de restrição inválido.";
            return new List<int>();
        }

        if (lista.Distinct().Count() != lista.Count)
        {
            erro = "O mesmo vínculo não pode aparecer duas vezes.";
            return new List<int>();
        }

        return lista;
    }

    /// <summary>
    /// Carrega as restrições do vínculo. Devolve <c>null</c> quando alguma não existe ou
    /// está inativa — vínculo com item inativo é recusado (RF4, Spec 010 RF3).
    /// </summary>
    private Dictionary<int, RestricaoAlimentar>? ResolverRestricoes(IEnumerable<int> ids)
    {
        var restricoes = new Dictionary<int, RestricaoAlimentar>();

        foreach (var id in ids)
        {
            if (_restricoes.Obter(id) is not { Ativa: true } restricao)
            {
                return null;
            }

            restricoes[id] = restricao;
        }

        return restricoes;
    }

    private Resultado<AlimentoDTO> AlterarSituacao(int id, bool desativar)
    {
        var alimento = _alimentos.ObterComVinculos(id);

        if (alimento is null)
        {
            return Resultado<AlimentoDTO>.NaoEncontrado("Alimento não encontrado.");
        }

        if (desativar)
        {
            alimento.Desativar();
        }
        else
        {
            alimento.Reativar();
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _alimentos.Update(alimento));

        return Resultado<AlimentoDTO>.Ok(ParaDTO(alimento));
    }

    private Resultado<CategoriaAlimentoDTO> AlterarSituacaoCategoria(int id, bool desativar)
    {
        var categoria = _categorias.Obter(id);

        if (categoria is null)
        {
            return Resultado<CategoriaAlimentoDTO>.NaoEncontrado("Categoria não encontrada.");
        }

        if (desativar)
        {
            categoria.Desativar();
        }
        else
        {
            categoria.Reativar();
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _categorias.Update(categoria));

        return Resultado<CategoriaAlimentoDTO>.Ok(ParaDTO(categoria));
    }

    private static AlimentoDTO ParaDTO(
        Alimento alimento,
        CategoriaAlimento? categoria = null,
        IReadOnlyDictionary<int, RestricaoAlimentar>? restricoes = null)
    {
        var categoriaResolvida = categoria ?? alimento.Categoria;

        return new AlimentoDTO
        {
            Id = alimento.Id,
            Nome = alimento.Nome,
            Ativo = alimento.Ativo,
            Categoria = categoriaResolvida is null
                ? new CategoriaAlimentoDTO()
                : ParaDTO(categoriaResolvida),
            Valores = new ValoresNutricionaisDTO(alimento.Valores),
            Vinculos = alimento.Vinculos
                .Select(vinculo =>
                {
                    var restricao = restricoes is not null &&
                                    restricoes.TryGetValue(vinculo.RestricaoId, out var encontrada)
                        ? encontrada
                        : vinculo.Restricao;

                    return new VinculoAlimentoDTO
                    {
                        RestricaoId = vinculo.RestricaoId,
                        Restricao = restricao?.Nome ?? string.Empty,
                        Categoria = restricao?.Categoria
                    };
                })
                .OrderBy(vinculo => vinculo.Restricao)
                .ToList()
        };
    }

    private static CategoriaAlimentoDTO ParaDTO(CategoriaAlimento categoria) =>
        new(categoria.Id, categoria.Nome, categoria.Ativa);
}
