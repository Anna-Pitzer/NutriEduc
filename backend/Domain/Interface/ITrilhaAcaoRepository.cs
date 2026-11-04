using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>Trilha append-only de ações privilegiadas (Spec 028, RF11).</summary>
public interface ITrilhaAcaoRepository
{
    void Add(TrilhaAcao acao);

    /// <summary>Leitura administrativa da trilha, em ordem de ocorrência.</summary>
    List<TrilhaAcao> Listar();
}
