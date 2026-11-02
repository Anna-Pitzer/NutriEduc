namespace Backend.Tests.Support;

/// <summary>
/// Relógio controlável para testar expiração sem esperar 30 minutos. Parte da hora real
/// (o cookie de sessão também usa <see cref="TimeProvider"/>; uma data fixa no passado
/// invalidaria o ticket) e avança só quando o teste pede.
/// </summary>
public sealed class RelogioFalso : TimeProvider
{
    private DateTimeOffset _agora = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _agora;

    public void Avancar(TimeSpan intervalo) => _agora = _agora.Add(intervalo);
}
