using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Contexto de restrições por escola para a montagem manual (Spec 029, RF11 e RNF7).
///
/// Lê os alunos e o catálogo uma única vez por requisição e devolve, por escola ativa da
/// unidade, os alunos com o conjunto canônico de restrições. É o mesmo insumo do cálculo
/// dos grupos que geram as versões adaptadas.
/// </summary>
public sealed class ContextoDeRestricoesService
{
    private readonly IAlunoRepository _alunos;
    private readonly IEscolaRepository _escolas;
    private readonly IRestricaoRepository _restricoes;
    private readonly AutorizacaoService _autorizacao;

    public ContextoDeRestricoesService(
        IAlunoRepository alunos,
        IEscolaRepository escolas,
        IRestricaoRepository restricoes,
        AutorizacaoService autorizacao)
    {
        _alunos = alunos;
        _escolas = escolas;
        _restricoes = restricoes;
        _autorizacao = autorizacao;
    }

    public Resultado<ContextoDeRestricoesDTO> Obter(
        int unidadeId,
        int? escolaId,
        EscopoUsuario escopo)
    {
        var negacao = _autorizacao.Verificar(
            Recurso.CardapioAdaptado,
            escopo,
            OperacaoDeAcesso.Ler,
            new AlvoDeEscopo(unidadeId, escolaId));

        if (negacao == ResultadoStatus.NaoEncontrado)
        {
            return Resultado<ContextoDeRestricoesDTO>.NaoEncontrado("Unidade não encontrada.");
        }

        if (negacao is not null)
        {
            return Resultado<ContextoDeRestricoesDTO>.Proibido();
        }

        if (unidadeId <= 0 || _escolas.GetUnidade(unidadeId) is null)
        {
            return Resultado<ContextoDeRestricoesDTO>.NaoEncontrado("Unidade não encontrada.");
        }

        if (escolaId is { } idEscola)
        {
            var escola = _escolas.GetEscola(idEscola);

            if (escola is null || escola.UnidadeId != unidadeId)
            {
                return Resultado<ContextoDeRestricoesDTO>.NaoEncontrado("Escola não encontrada.");
            }
        }

        var escolas = _escolas.ListarPorUnidade(unidadeId)
            .Where(escola => escola.Ativa && (escolaId is null || escola.Id == escolaId))
            .OrderBy(escola => escola.Nome, StringComparer.Ordinal)
            .ToList();

        var alunos = _alunos.Listar(new FiltroAluno(UnidadeId: unidadeId, EscolaId: escolaId));

        var porNome = RestricoesDoAluno.PorNomeNormalizado(
            _restricoes.Listar(categoria: null, incluirInativas: true));

        var contexto = new ContextoDeRestricoesDTO
        {
            UnidadeId = unidadeId,
            Escolas = escolas.Select(escola =>
            {
                var alunosDaEscola = alunos
                    .Where(aluno => aluno.EscolaId == escola.Id)
                    .OrderBy(aluno => aluno.Nome, StringComparer.Ordinal)
                    .Select(aluno => ParaAluno(aluno, porNome))
                    .ToList();

                return new EscolaNoContextoDTO
                {
                    EscolaId = escola.Id,
                    EscolaNome = escola.Nome,
                    SemRestricoes = alunosDaEscola.All(aluno => aluno.Restricoes.Count == 0),
                    Alunos = alunosDaEscola
                };
            }).ToList()
        };

        return Resultado<ContextoDeRestricoesDTO>.Ok(contexto);
    }

    private static AlunoNoContextoDTO ParaAluno(
        Aluno aluno,
        IReadOnlyDictionary<string, RestricaoAlimentar> porNome)
    {
        var restricoes = RestricoesDoAluno.DoAluno(aluno, porNome);

        return new AlunoNoContextoDTO
        {
            AlunoId = aluno.Id,
            Nome = aluno.Nome,
            ConjuntoRestricoesChave = ConjuntoDeRestricoes
                .DeIds(restricoes.Select(restricao => restricao.Id))
                .Chave,
            Restricoes = restricoes.Select(restricao => new RestricaoDaVersaoDTO
            {
                Id = restricao.Id,
                Nome = restricao.Nome,
                Categoria = restricao.Categoria
            }).ToList()
        };
    }
}
