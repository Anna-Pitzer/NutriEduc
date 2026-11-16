using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Regras de escola e unidade (Spec 027). A autorização por papel é da política do
/// controller; aqui ficam unicidade por unidade, existência, estado ativo e o escopo de
/// leitura da própria unidade/escola.
/// </summary>
public class EscolaService
{
    private readonly IEscolaRepository _escolas;
    private readonly IAlunoRepository _alunos;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public EscolaService(
        IEscolaRepository escolas,
        IAlunoRepository alunos,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _escolas = escolas;
        _alunos = alunos;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    public bool FiltroNoEscopo(int? unidadeId, EscopoUsuario escopo)
    {
        if (unidadeId is not > 0)
        {
            return true;
        }

        if (escopo.UnidadeId is { } unidadeFixa)
        {
            return unidadeFixa == unidadeId;
        }

        if (escopo.EscolaId is { } escolaFixa)
        {
            return _escolas.GetEscola(escolaFixa)?.UnidadeId == unidadeId;
        }

        return false;
    }

    public List<EscolaRespostaDTO> Listar(EscopoUsuario escopo, int? unidadeId = null)
    {
        // Filtro da requisição estreita o escopo; nunca o amplia (Spec 027, RF13) e é
        // resolvido no banco (RNF1).
        if (unidadeId is > 0)
        {
            if (escopo.UnidadeId is { } unidadeFixa && unidadeFixa != unidadeId)
            {
                return new List<EscolaRespostaDTO>();
            }

            if (escopo.Papel is PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista)
            {
                return _escolas.ListarPorUnidade(unidadeId.Value).Select(ParaDTO).ToList();
            }

            if (escopo.EscolaId is { } escolaId)
            {
                var escola = _escolas.GetEscola(escolaId);

                return escola is not null && escola.UnidadeId == unidadeId
                    ? new List<EscolaRespostaDTO> { ParaDTO(escola) }
                    : new List<EscolaRespostaDTO>();
            }

            return new List<EscolaRespostaDTO>();
        }

        var escolas = escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista
                when escopo.UnidadeId is { } unidadeDoEscopo => _escolas.ListarPorUnidade(unidadeDoEscopo),
            PapelUsuario.AdminEscola or PapelUsuario.Merendeira
                when escopo.EscolaId is { } escolaDoEscopo => _escolas.ListarPorEscola(escolaDoEscopo),
            _ => new List<Escola>()
        };

        return escolas.Select(ParaDTO).ToList();
    }

    public Resultado<EscolaRespostaDTO> Criar(EscolaDTO dto, EscopoUsuario escopo)
    {
        // RF2/RF9: a unidade é obrigatória no payload, mas a autorização continua vindo da
        // sessão. O valor do cliente só é aceito quando coincide com o escopo da sessão.
        if (dto.UnidadeId is null)
        {
            return Resultado<EscolaRespostaDTO>.Invalido(
                "Informe a unidade da escola.");
        }

        if (escopo.UnidadeId is not { } unidadeDaSessao)
        {
            return Resultado<EscolaRespostaDTO>.Proibido();
        }

        if (unidadeDaSessao != dto.UnidadeId)
        {
            return Resultado<EscolaRespostaDTO>.Proibido();
        }

        var unidadeId = unidadeDaSessao;

        var unidade = _escolas.GetUnidade(unidadeId);

        if (unidade is null)
        {
            return Resultado<EscolaRespostaDTO>.NaoEncontrado("Unidade não encontrada.");
        }

        if (!unidade.Ativa)
        {
            return Resultado<EscolaRespostaDTO>.Invalido(
                "Unidade desativada não pode receber escola.");
        }

        if (_escolas.ExisteNome(dto.Nome, unidadeId))
        {
            return Resultado<EscolaRespostaDTO>.Conflito(
                "Já existe uma escola com este nome nesta unidade.");
        }

        var escola = _unidadeDeTrabalho.ExecutarEmTransacao(
            () => _escolas.Add(new Escola(dto.Nome, unidadeId)));

        return Resultado<EscolaRespostaDTO>.Ok(ParaDTO(escola));
    }

    public Resultado<EscolaRespostaDTO> Atualizar(int id, EscolaDTO dto, EscopoUsuario escopo)
    {
        var escola = _escolas.GetEscola(id);

        if (escola is null || !PodeAdministrar(escola, escopo))
        {
            return Resultado<EscolaRespostaDTO>.NaoEncontrado("Escola não encontrada.");
        }

        var unidadeDestino = dto.UnidadeId ?? escola.UnidadeId ?? escopo.UnidadeId;

        if (unidadeDestino is null)
        {
            return Resultado<EscolaRespostaDTO>.Invalido("Informe a unidade da escola.");
        }

        if (_escolas.ExisteNome(dto.Nome, unidadeDestino.Value, id))
        {
            return Resultado<EscolaRespostaDTO>.Conflito(
                "Já existe uma escola com este nome nesta unidade.");
        }

        if (escola.UnidadeId != unidadeDestino)
        {
            var unidade = _escolas.GetUnidade(unidadeDestino.Value);

            if (unidade is null)
            {
                return Resultado<EscolaRespostaDTO>.NaoEncontrado("Unidade não encontrada.");
            }

            if (!unidade.Ativa)
            {
                return Resultado<EscolaRespostaDTO>.Invalido(
                    "Unidade desativada não pode receber escola.");
            }

            try
            {
                escola.DefinirUnidade(unidadeDestino.Value, _alunos.ExistePorEscola(id));
            }
            catch (ArgumentException excecao)
            {
                return Resultado<EscolaRespostaDTO>.Conflito(excecao.Message);
            }
        }

        escola.DefinirNome(dto.Nome);
        _unidadeDeTrabalho.ExecutarEmTransacao(() => _escolas.Update(escola));

        return Resultado<EscolaRespostaDTO>.Ok(ParaDTO(escola));
    }

    public Resultado<EscolaRespostaDTO> DefinirAtiva(int id, bool ativa, EscopoUsuario escopo)
    {
        var escola = _escolas.GetEscola(id);

        if (escola is null || !PodeAdministrar(escola, escopo))
        {
            return Resultado<EscolaRespostaDTO>.NaoEncontrado("Escola não encontrada.");
        }

        if (ativa)
        {
            escola.Reativar();
        }
        else
        {
            escola.Desativar();
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _escolas.Update(escola));

        return Resultado<EscolaRespostaDTO>.Ok(ParaDTO(escola));
    }

    public List<UnidadeRespostaDTO> ListarUnidades(EscopoUsuario escopo)
    {
        var unidades = _escolas.ListarUnidades();

        if (escopo.UnidadeId is { } unidadeId)
        {
            unidades = unidades.Where(unidade => unidade.Id == unidadeId).ToList();
        }

        return unidades.Select(ParaDTO).ToList();
    }

    /// <summary>
    /// Consulta pública mínima para o cadastro (Spec 033): apenas unidades ativas, com id e
    /// nome. Não expõe dados administrativos e não passa pela rota autenticada.
    /// </summary>
    public List<UnidadeAtivaDTO> ListarUnidadesAtivas() =>
        _escolas.ListarUnidades()
            .Where(unidade => unidade.Ativa)
            .OrderBy(unidade => unidade.Nome)
            .Select(unidade => new UnidadeAtivaDTO(unidade.Id, unidade.Nome))
            .ToList();

    public Resultado<UnidadeRespostaDTO> AtualizarUnidade(int id, UnidadeDTO dto, EscopoUsuario escopo)
    {
        var unidade = _escolas.GetUnidade(id);

        if (unidade is null || (escopo.UnidadeId is { } unidadeDaSessao && unidadeDaSessao != id))
        {
            return Resultado<UnidadeRespostaDTO>.NaoEncontrado("Unidade não encontrada.");
        }

        if (_escolas.ExisteNomeUnidade(dto.Nome, id))
        {
            return Resultado<UnidadeRespostaDTO>.Conflito(
                "Já existe uma unidade com este nome.");
        }

        unidade.DefinirNome(dto.Nome);
        _unidadeDeTrabalho.ExecutarEmTransacao(() => _escolas.UpdateUnidade(unidade));

        return Resultado<UnidadeRespostaDTO>.Ok(ParaDTO(unidade));
    }

    public Resultado<UnidadeRespostaDTO> DefinirAtivaUnidade(int id, bool ativa, EscopoUsuario escopo)
    {
        var unidade = _escolas.GetUnidade(id);

        if (unidade is null || (escopo.UnidadeId is { } unidadeDaSessao && unidadeDaSessao != id))
        {
            return Resultado<UnidadeRespostaDTO>.NaoEncontrado("Unidade não encontrada.");
        }

        if (ativa)
        {
            unidade.Reativar();
        }
        else
        {
            unidade.Desativar();
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _escolas.UpdateUnidade(unidade));

        return Resultado<UnidadeRespostaDTO>.Ok(ParaDTO(unidade));
    }

    private static bool PodeAdministrar(Escola escola, EscopoUsuario escopo)
    {
        if (escopo.UnidadeId is { } unidadeId)
        {
            return escola.UnidadeId == unidadeId;
        }

        return escola.UnidadeId is null;
    }

    private static EscolaRespostaDTO ParaDTO(Escola escola) =>
        new(escola.Id, escola.Nome, escola.UnidadeId, escola.Ativa);

    private static UnidadeRespostaDTO ParaDTO(UnidadeEscolar unidade) =>
        new(unidade.Id, unidade.Nome, unidade.Ativa);
}
