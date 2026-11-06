using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>Repositório em memória de notificações para os testes de serviço.</summary>
public class NotificacaoRepositoryFake : INotificacaoRepository
{
    private readonly List<Notificacao> _itens = new();

    public Notificacao Add(Notificacao notificacao)
    {
        _itens.Add(notificacao);
        return notificacao;
    }

    public List<Notificacao> GetItens() => _itens.ToList();
}
