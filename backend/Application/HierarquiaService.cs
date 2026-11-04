using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Application;

/// <summary>
/// Derivação e validação da cadeia unidade → escola → aluno (Spec 027). É o único lugar
/// que resolve a unidade a partir da escola; nenhum DTO de entrada informa a unidade de
/// um aluno.
/// </summary>
public sealed class HierarquiaService
{
    private readonly IEscolaRepository _escolas;
    private readonly IAlunoRepository _alunos;
    private readonly IUsuarioRepository _usuarios;

    public HierarquiaService(
        IEscolaRepository escolas,
        IAlunoRepository alunos,
        IUsuarioRepository usuarios)
    {
        _escolas = escolas;
        _alunos = alunos;
        _usuarios = usuarios;
    }

    /// <summary>Carrega a escola sem validar situação — usado só para conferir filtro.</summary>
    public Escola? ObterEscola(int escolaId) => _escolas.GetEscola(escolaId);

    /// <summary>
    /// Carrega a escola e garante que ela existe, está ativa e pertence a uma unidade
    /// existente e ativa (Spec 027, RF2 e RF3).
    /// </summary>
    public Escola ObterEscolaAtiva(int? escolaId)
    {
        if (escolaId is null)
        {
            throw new ArgumentException(
                "O aluno exige uma escola: sem ela as restrições não se aplicam ao cardápio.");
        }

        var escola = _escolas.GetEscola(escolaId.Value)
            ?? throw new ArgumentException("Escola inexistente.");

        if (!escola.Ativa)
        {
            throw new ArgumentException("Escola desativada não pode receber aluno.");
        }

        if (escola.UnidadeId is null)
        {
            throw new ArgumentException(
                "Escola sem unidade não pode receber aluno. Regularize o cadastro da escola.");
        }

        var unidade = _escolas.GetUnidade(escola.UnidadeId.Value)
            ?? throw new ArgumentException("Unidade inexistente.");

        if (!unidade.Ativa)
        {
            throw new ArgumentException("Unidade desativada não pode receber aluno.");
        }

        return escola;
    }

    /// <summary>Unidade derivada da escola, sempre não nula após <see cref="ObterEscolaAtiva"/>.</summary>
    public int DerivarUnidade(Escola escola) =>
        escola.UnidadeId
        ?? throw new ArgumentException("Escola sem unidade não pode receber aluno.");

    /// <summary>
    /// Garante que a escola de destino pertence à mesma unidade do aluno na transferência
    /// (Spec 027, RF11).
    /// </summary>
    public void ExigirMesmaUnidade(int? unidadeAtual, int unidadeDestino)
    {
        if (unidadeAtual != unidadeDestino)
        {
            throw new ArgumentException(
                "Transferência entre unidades é proibida nesta spec.");
        }
    }

    /// <summary>
    /// Contagens de legado. Não corrige dados e não inventa escola/unidade (RNF3).
    /// </summary>
    public DiagnosticoHierarquiaDTO Diagnosticar() =>
        new(
            _escolas.ContarSemUnidade(),
            _alunos.ContarSemEscola(),
            _usuarios.ContarSemVinculoObrigatorio());
}
