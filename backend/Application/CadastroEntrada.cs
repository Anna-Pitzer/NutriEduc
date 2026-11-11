using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>
/// Contrato de entrada do cadastro público (Spec 033, RF1–RF4). Estende o autocadastro
/// anterior com o tipo solicitado, a unidade de destino e o CRN (quando nutricionista).
/// Os campos são anuláveis porque o binding de JSON pode omiti-los; quem recusa valor
/// ausente é o domínio e o serviço.
/// </summary>
public sealed record CadastroEntrada(
    string? Nome,
    string? Cpf,
    DateOnly DataNascimento,
    string? Email,
    string? Senha,
    string? Telefone,
    string? Foto,
    TipoCadastroSolicitado? Tipo,
    int? UnidadeId,
    string? Crn);

/// <summary>Confirmação do cadastro público (Spec 033, RF4): usuário e solicitação criados.</summary>
public sealed record RegistroDeAcessoDTO(int UsuarioId, int SolicitacaoId, string Status);
