namespace Ntc.Application;

/// <summary>
/// Regra única de paginação (Spec 011, RNF4). Antes vivia privada em
/// <c>HistoricoAlunoService</c>; com dois consumidores, a regra passa a ter um dono só,
/// em vez de ser copiada por serviço.
///
/// Página fora da faixa lança <see cref="ArgumentException"/>, que o middleware de erro
/// traduz em 400 com o contrato `{ erro }` (ADR-005).
/// </summary>
public static class Paginacao
{
    public const int PaginaMin = 1;
    public const int TamanhoMin = 1;
    public const int TamanhoMax = 100;
    public const int TamanhoPadrao = 20;

    /// <summary>Normaliza os parâmetros ausentes e valida a faixa permitida.</summary>
    public static (int Pagina, int Tamanho) Validar(int? pagina, int? tamanho)
    {
        var paginaEfetiva = pagina ?? PaginaMin;
        var tamanhoEfetivo = tamanho ?? TamanhoPadrao;

        if (paginaEfetiva < PaginaMin)
        {
            throw new ArgumentException("A página deve ser maior que zero.");
        }

        if (tamanhoEfetivo < TamanhoMin || tamanhoEfetivo > TamanhoMax)
        {
            throw new ArgumentException(
                $"O tamanho da página deve estar entre {TamanhoMin} e {TamanhoMax}.");
        }

        return (paginaEfetiva, tamanhoEfetivo);
    }
}
