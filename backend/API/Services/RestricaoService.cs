using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Caso de uso do catálogo de restrições e alergias (Spec 010).
///
/// O catálogo é a fonte de verdade do vínculo, mas não é editável pela interface desde a
/// revisão de 2026-11-01 (ADR-015): as rotas de escrita continuam existindo e as opções
/// oferecidas ao aluno vêm da semente da migration. A leitura do histórico por aluno saiu
/// daqui na Spec 023 e vive em <c>HistoricoAlunoService</c>.
/// </summary>
public sealed class RestricaoService
{
    private readonly IRestricaoRepository _restricoes;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public RestricaoService(IRestricaoRepository restricoes,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _restricoes = restricoes;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    /// <summary>
    /// Lista o catálogo. Itens inativos só são devolvidos ao Nutricionista (RF7): os demais
    /// papéis recebem apenas o que pode ser vinculado a um aluno.
    /// </summary>
    public Resultado<List<RestricaoDTO>> Listar(
        CategoriaRestricao? categoria,
        bool incluirInativas,
        EscopoUsuario escopo)
    {
        if (incluirInativas && escopo.Papel != PapelUsuario.Nutricionista)
        {
            return Resultado<List<RestricaoDTO>>.Proibido(
                "Somente a nutrição pode consultar itens inativos.");
        }

        var itens = _restricoes
            .Listar(categoria, incluirInativas)
            .Select(ParaDTO)
            .ToList();

        return Resultado<List<RestricaoDTO>>.Ok(itens);
    }

    /// <summary>Cria um item do catálogo. Duplicidade na mesma categoria é 400 (RF1).</summary>
    public RestricaoDTO Criar(SalvarRestricaoDTO dto)
    {
        var categoria = ExigirCategoria(dto);
        var restricao = new RestricaoAlimentar(dto.Nome, categoria);

        GarantirNomeUnico(restricao.NomeNormalizado, categoria, ignorarId: null);

        var criada = _unidadeDeTrabalho.ExecutarEmTransacao(() => _restricoes.Add(restricao));

        return ParaDTO(criada);
    }

    /// <summary>Edita nome e categoria; o item inexistente devolve 404.</summary>
    public Resultado<RestricaoDTO> Atualizar(int id, SalvarRestricaoDTO dto)
    {
        var restricao = _restricoes.Obter(id);

        if (restricao is null)
        {
            return Resultado<RestricaoDTO>.NaoEncontrado("Restrição não encontrada.");
        }

        var categoria = ExigirCategoria(dto);

        // Valida em um buffer antes de tocar no estado: nome inválido não altera o item.
        var validada = new RestricaoAlimentar(dto.Nome, categoria);
        GarantirNomeUnico(validada.NomeNormalizado, categoria, ignorarId: id);

        restricao.DefinirNome(validada.Nome);
        restricao.DefinirCategoria(categoria);

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _restricoes.Update(restricao));

        return Resultado<RestricaoDTO>.Ok(ParaDTO(restricao));
    }

    public Resultado<RestricaoDTO> Desativar(int id) => AlterarSituacao(id, desativar: true);

    public Resultado<RestricaoDTO> Reativar(int id) => AlterarSituacao(id, desativar: false);

    private Resultado<RestricaoDTO> AlterarSituacao(int id, bool desativar)
    {
        var restricao = _restricoes.Obter(id);

        if (restricao is null)
        {
            return Resultado<RestricaoDTO>.NaoEncontrado("Restrição não encontrada.");
        }

        if (desativar)
        {
            restricao.Desativar();
        }
        else
        {
            restricao.Reativar();
        }

        _restricoes.Update(restricao);

        return Resultado<RestricaoDTO>.Ok(ParaDTO(restricao));
    }

    private void GarantirNomeUnico(string nomeNormalizado, CategoriaRestricao categoria, int? ignorarId)
    {
        if (_restricoes.ExisteNomeNormalizado(nomeNormalizado, categoria, ignorarId))
        {
            throw new ArgumentException(
                "Já existe uma restrição com este nome nesta categoria.");
        }
    }

    private static CategoriaRestricao ExigirCategoria(SalvarRestricaoDTO dto)
    {
        return dto.Categoria
            ?? throw new ArgumentException("A categoria da restrição é obrigatória.");
    }

    private static RestricaoDTO ParaDTO(RestricaoAlimentar restricao)
    {
        return new RestricaoDTO(
            restricao.Id,
            restricao.Nome,
            restricao.Categoria,
            restricao.Ativa);
    }
}
