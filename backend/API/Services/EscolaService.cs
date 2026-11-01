using Ntc.Application;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Regras de escola e unidade (RF3). A autorização por papel é da política do
/// controller; aqui ficam apenas unicidade, existência e estado ativo.
/// </summary>
public class EscolaService
{
    private readonly IEscolaRepository _escolas;

    public EscolaService(IEscolaRepository escolas)
    {
        _escolas = escolas;
    }

    public List<EscolaRespostaDTO> Listar()
    {
        return _escolas.Listar().Select(ParaDTO).ToList();
    }

    public Resultado<EscolaRespostaDTO> Criar(EscolaDTO dto)
    {
        if (_escolas.ExisteNome(dto.Nome))
        {
            return Resultado<EscolaRespostaDTO>.Conflito(
                "Já existe uma escola com este nome.");
        }

        var escola = _escolas.Add(new Escola(dto.Nome));

        return Resultado<EscolaRespostaDTO>.Ok(ParaDTO(escola));
    }

    public Resultado<EscolaRespostaDTO> Atualizar(int id, EscolaDTO dto)
    {
        var escola = _escolas.GetEscola(id);

        if (escola is null)
        {
            return Resultado<EscolaRespostaDTO>.NaoEncontrado("Escola não encontrada.");
        }

        if (_escolas.ExisteNome(dto.Nome, id))
        {
            return Resultado<EscolaRespostaDTO>.Conflito(
                "Já existe uma escola com este nome.");
        }

        escola.DefinirNome(dto.Nome);
        _escolas.Update(escola);

        return Resultado<EscolaRespostaDTO>.Ok(ParaDTO(escola));
    }

    public Resultado<EscolaRespostaDTO> DefinirAtiva(int id, bool ativa)
    {
        var escola = _escolas.GetEscola(id);

        if (escola is null)
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

        _escolas.Update(escola);

        return Resultado<EscolaRespostaDTO>.Ok(ParaDTO(escola));
    }

    public List<UnidadeRespostaDTO> ListarUnidades()
    {
        return _escolas.ListarUnidades().Select(ParaDTO).ToList();
    }

    public Resultado<UnidadeRespostaDTO> AtualizarUnidade(int id, UnidadeDTO dto)
    {
        var unidade = _escolas.GetUnidade(id);

        if (unidade is null)
        {
            return Resultado<UnidadeRespostaDTO>.NaoEncontrado("Unidade não encontrada.");
        }

        if (_escolas.ExisteNomeUnidade(dto.Nome, id))
        {
            return Resultado<UnidadeRespostaDTO>.Conflito(
                "Já existe uma unidade com este nome.");
        }

        unidade.DefinirNome(dto.Nome);
        _escolas.UpdateUnidade(unidade);

        return Resultado<UnidadeRespostaDTO>.Ok(ParaDTO(unidade));
    }

    public Resultado<UnidadeRespostaDTO> DefinirAtivaUnidade(int id, bool ativa)
    {
        var unidade = _escolas.GetUnidade(id);

        if (unidade is null)
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

        _escolas.UpdateUnidade(unidade);

        return Resultado<UnidadeRespostaDTO>.Ok(ParaDTO(unidade));
    }

    private static EscolaRespostaDTO ParaDTO(Escola escola) =>
        new(escola.Id, escola.Nome, escola.Ativa);

    private static UnidadeRespostaDTO ParaDTO(UnidadeEscolar unidade) =>
        new(unidade.Id, unidade.Nome, unidade.Ativa);
}
