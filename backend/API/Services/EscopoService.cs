using System.Security.Claims;
using Ntc.Domain;
using Ntc.Domain.Entity;

/// <summary>
/// Converte as claims da sessão em <see cref="EscopoUsuario"/>. Usuário sem papel, sem
/// id ou com papel desconhecido devolve <c>null</c> — o controller responde 401.
/// É a única fonte de verdade sobre "quem é o solicitante".
/// </summary>
public sealed class EscopoService
{
    public EscopoUsuario? Obter(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var papel = principal.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(id, out var usuarioId))
        {
            return null;
        }

        if (!Enum.TryParse<PapelUsuario>(papel, ignoreCase: true, out var valor) ||
            !Enum.IsDefined(valor))
        {
            return null;
        }

        return new EscopoUsuario(
            usuarioId,
            valor,
            LerId(principal.FindFirstValue("escola")),
            LerId(principal.FindFirstValue("unidade")));
    }

    private static int? LerId(string? texto)
    {
        return int.TryParse(texto, out var valor) && valor > 0 ? valor : null;
    }
}
