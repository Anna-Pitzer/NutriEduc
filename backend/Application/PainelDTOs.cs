using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>
/// Painel da secretaria (Spec 025, RF1/RF2): contagens da unidade do solicitante, sem
/// nenhum dado pessoal de aluno.
/// </summary>
public class PainelSecretariaDTO
{
    public string Unidade { get; set; } = string.Empty;

    public int Alunos { get; set; }

    public List<AlunosPorEscolaDTO> AlunosPorEscola { get; set; } = new();

    public List<UsuariosAtivosPorPapelDTO> UsuariosAtivosPorPapel { get; set; } = new();

    public int CardapiosPublicadosUltimosSeteDias { get; set; }

    public int OcorrenciasAbertas { get; set; }

    public int SolicitacoesPendentes { get; set; }
}

public class AlunosPorEscolaDTO
{
    public int? EscolaId { get; set; }

    public string Escola { get; set; } = string.Empty;

    public int Total { get; set; }
}

public class UsuariosAtivosPorPapelDTO
{
    public PapelUsuario Papel { get; set; }

    public int Total { get; set; }
}

/// <summary>Item do cardápio do dia (Spec 025, RF3/RF4).</summary>
public class ItemDoCardapioDoDiaDTO
{
    public string Alimento { get; set; } = string.Empty;

    public decimal Gramas { get; set; }

    /// <summary>Quantos alunos da unidade têm restrição sobre o alimento; nunca quem.</summary>
    public int AlunosComRestricao { get; set; }
}

public class RefeicaoDoCardapioDoDiaDTO
{
    public string Nome { get; set; } = string.Empty;

    public List<ItemDoCardapioDoDiaDTO> Itens { get; set; } = new();
}

public class CardapioDoDiaDTO
{
    public string Nome { get; set; } = string.Empty;

    public DateTime? PublicadoEm { get; set; }

    public List<RefeicaoDoCardapioDoDiaDTO> Refeicoes { get; set; } = new();
}

/// <summary>Cardápio do dia da unidade da merendeira (Spec 025, RF3).</summary>
public class PainelMerendeiraDTO
{
    public DateOnly Data { get; set; }

    public string Unidade { get; set; } = string.Empty;

    public CardapioDoDiaDTO? Cardapio { get; set; }

    public string? Motivo { get; set; }
}

public class RestricaoDoFilhoDTO
{
    public string Nome { get; set; } = string.Empty;

    public CategoriaRestricao Categoria { get; set; }
}

public class FilhoDoPainelDTO
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Escola { get; set; }

    public string? Unidade { get; set; }

    public List<RestricaoDoFilhoDTO> Restricoes { get; set; } = new();

    /// <summary>
    /// Verdadeiro quando o aluno tem restrição: o painel não devolve o cardápio geral,
    /// porque ele não é a versão servida (Spec 029, D4).
    /// </summary>
    public bool RequerAdaptacao { get; set; }

    public CardapioDoDiaDTO? Cardapio { get; set; }

    public string? Motivo { get; set; }
}

/// <summary>Visão da família (Spec 025, RF5).</summary>
public class PainelFamiliaDTO
{
    public DateOnly Data { get; set; }

    public List<FilhoDoPainelDTO> Filhos { get; set; } = new();
}

public class AlteracaoDeRestricaoDTO
{
    public TipoEventoAluno Tipo { get; set; }

    public string Restricao { get; set; } = string.Empty;

    public string Autor { get; set; } = string.Empty;

    public string? Origem { get; set; }

    public DateTime CriadoEm { get; set; }
}

public class OcorrenciaDoFilhoDTO
{
    public int Id { get; set; }

    public TipoOcorrencia Tipo { get; set; }

    public StatusOcorrencia Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public DateTime CriadaEm { get; set; }
}

/// <summary>Relatório do próprio filho (Spec 025, RF6).</summary>
public class RelatorioDoFilhoDTO
{
    public int AlunoId { get; set; }

    public string AlunoNome { get; set; } = string.Empty;

    public DateOnly De { get; set; }

    public DateOnly Ate { get; set; }

    public List<RestricaoDoFilhoDTO> Restricoes { get; set; } = new();

    public List<AlteracaoDeRestricaoDTO> Alteracoes { get; set; } = new();

    public List<OcorrenciaDoFilhoDTO> Ocorrencias { get; set; } = new();
}
