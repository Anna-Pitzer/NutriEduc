using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>Item do catálogo devolvido pela API (Spec 010, RF7).</summary>
public class RestricaoDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public CategoriaRestricao Categoria { get; set; }
    public bool Ativa { get; set; }

    public RestricaoDTO() { }

    public RestricaoDTO(int id, string nome, CategoriaRestricao categoria, bool ativa)
    {
        Id = id;
        Nome = nome;
        Categoria = categoria;
        Ativa = ativa;
    }
}

/// <summary>Entrada de criação e edição do catálogo.</summary>
public class SalvarRestricaoDTO
{
    public string Nome { get; set; } = string.Empty;
    public CategoriaRestricao? Categoria { get; set; }
}

/// <summary>Evento do histórico de restrições do aluno (RF5, RF6).</summary>
public class HistoricoRestricaoDTO
{
    public string Restricao { get; set; } = string.Empty;
    public AcaoRestricao Acao { get; set; }
    public string Autor { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}

/// <summary>
/// Evento da trilha única de alterações do aluno (Spec 023, RF1 e RF8): criação, campo
/// alterado com valor anterior e novo, ou restrição adicionada/removida.
/// </summary>
public class HistoricoAlunoDTO
{
    public TipoEventoAluno Tipo { get; set; }
    public string? Campo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNovo { get; set; }
    public string Autor { get; set; } = string.Empty;
    public string? Origem { get; set; }
    public DateTime CriadoEm { get; set; }
}

/// <summary>Página de qualquer listagem paginada (RNF4).</summary>
public class PaginaDTO<T>
{
    public int Pagina { get; set; }
    public int Tamanho { get; set; }
    public int Total { get; set; }
    public List<T> Itens { get; set; } = new();
}
