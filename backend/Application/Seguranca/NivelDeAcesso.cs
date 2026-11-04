namespace Ntc.Application.Seguranca;

/// <summary>
/// Nível de acesso de um papel a um recurso (Spec 028, RF4). A ordem numérica não é usada
/// para autorização: as regras são explícitas em <see cref="PermiteLeitura"/> e
/// <see cref="PermiteEscrita"/> para que um novo valor não altere permissões por acidente.
/// </summary>
public enum NivelDeAcesso
{
    /// <summary>Sem acesso.</summary>
    Nenhum = 0,

    /// <summary>Lê no escopo e apenas solicita alteração; não escreve direto.</summary>
    Solicitar = 1,

    /// <summary>Lê no escopo.</summary>
    Ler = 2,

    /// <summary>Escreve no escopo, sem leitura.</summary>
    Escrita = 3,

    /// <summary>Lê no escopo e cria registro novo; não altera registro existente.</summary>
    LerCriar = 4,

    /// <summary>Lê e escreve no escopo.</summary>
    LerEscrever = 5
}

/// <summary>Operações que a matriz autoriza (Spec 028, RF4).</summary>
public enum OperacaoDeAcesso
{
    Ler,
    Criar,
    Atualizar,
    Excluir
}

public static class NivelDeAcessoExtensions
{
    public static bool PermiteLeitura(this NivelDeAcesso nivel) =>
        nivel is NivelDeAcesso.Solicitar
            or NivelDeAcesso.Ler
            or NivelDeAcesso.LerCriar
            or NivelDeAcesso.LerEscrever;

    public static bool PermiteCriacao(this NivelDeAcesso nivel) =>
        nivel is NivelDeAcesso.Escrita
            or NivelDeAcesso.LerCriar
            or NivelDeAcesso.LerEscrever;

    public static bool PermiteAtualizacao(this NivelDeAcesso nivel) =>
        nivel is NivelDeAcesso.Escrita or NivelDeAcesso.LerEscrever;

    public static bool Permite(this NivelDeAcesso nivel, OperacaoDeAcesso operacao) =>
        operacao switch
        {
            OperacaoDeAcesso.Ler => nivel.PermiteLeitura(),
            OperacaoDeAcesso.Criar => nivel.PermiteCriacao(),
            OperacaoDeAcesso.Atualizar or OperacaoDeAcesso.Excluir =>
                nivel.PermiteAtualizacao(),
            _ => false
        };
}
