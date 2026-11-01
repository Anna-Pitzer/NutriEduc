using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

public interface ITokenDeCadastroRepository
{
    /// <summary>Token ainda válido (não usado, não expirado) já emitido para a unidade.</summary>
    TokenDeCadastro? ObterValidoDaUnidade(int unidadeId, DateTime agora);

    /// <summary>Busca pelo hash, sem revelar o valor em claro em nenhum momento.</summary>
    TokenDeCadastro? ObterPorHash(string hash);

    /// <summary>Existe algum token válido? Usado para decidir se a rota responde 404.</summary>
    bool ExisteValido(DateTime agora);

    void Add(TokenDeCadastro token);

    void Update(TokenDeCadastro token);

    bool ExistePorUnidade(int unidadeId);
    bool ExistePorCriador(int usuarioId);
}
