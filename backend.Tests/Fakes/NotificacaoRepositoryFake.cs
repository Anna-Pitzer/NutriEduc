using Backend.Tests.Support;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>Repositório em memória de notificações para os testes de serviço e de leitura.</summary>
public class NotificacaoRepositoryFake : INotificacaoRepository
{
    private readonly List<Notificacao> _itens = new();
    private int _proximoId = 1;

    public Notificacao Add(Notificacao notificacao)
    {
        PropriedadesPrivadas.Definir(notificacao, nameof(Notificacao.Id), _proximoId++);
        _itens.Add(notificacao);
        return notificacao;
    }

    public void Update(Notificacao notificacao)
    {
    }

    public Notificacao? Obter(int id, int destinatarioId) =>
        _itens.FirstOrDefault(item => item.Id == id && item.DestinatarioId == destinatarioId);

    public (List<Notificacao> Itens, int Total) Listar(
        int destinatarioId,
        bool? naoLidas,
        int pagina,
        int tamanho)
    {
        var consulta = _itens.Where(item => item.DestinatarioId == destinatarioId);

        if (naoLidas is { } apenasNaoLidas)
        {
            consulta = consulta.Where(item => item.Lida == !apenasNaoLidas);
        }

        var ordenadas = consulta
            .OrderByDescending(item => item.CriadoEmUtc)
            .ThenByDescending(item => item.Id)
            .ToList();

        return (
            ordenadas.Skip((pagina - 1) * tamanho).Take(tamanho).ToList(),
            ordenadas.Count);
    }

    public int ContarNaoLidas(int destinatarioId) =>
        _itens.Count(item => item.DestinatarioId == destinatarioId && !item.Lida);

    public bool ExistePorDestinatario(int destinatarioId) =>
        _itens.Any(item => item.DestinatarioId == destinatarioId);

    public bool ExistePorReferencia(
        int destinatarioId,
        TipoNotificacao tipo,
        string referenciaTipo,
        int referenciaId) =>
        _itens.Any(item =>
            item.DestinatarioId == destinatarioId &&
            item.Tipo == tipo &&
            item.ReferenciaTipo == referenciaTipo &&
            item.ReferenciaId == referenciaId);

    public void MarcarTodasComoLidas(int destinatarioId)
    {
        foreach (var item in _itens.Where(item =>
                     item.DestinatarioId == destinatarioId && !item.Lida))
        {
            item.MarcarComoLida();
        }
    }

    /// <summary>Somente para asserções dos testes.</summary>
    public List<Notificacao> GetItens() => _itens.ToList();
}
