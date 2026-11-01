using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

public class TokenDeCadastroRepository : ITokenDeCadastroRepository
{
    private readonly ConnectionContext _contexto;

    public TokenDeCadastroRepository(ConnectionContext contexto)
    {
        _contexto = contexto;
    }

    public TokenDeCadastro? ObterValidoDaUnidade(int unidadeId, DateTime agora)
    {
        return _contexto.TokensDeCadastro
            .Where(token => token.UnidadeId == unidadeId &&
                            token.UsadoEm == null &&
                            token.ExpiraEm > agora)
            .OrderByDescending(token => token.ExpiraEm)
            .FirstOrDefault();
    }

    public TokenDeCadastro? ObterPorHash(string hash)
    {
        return _contexto.TokensDeCadastro
            .FirstOrDefault(token => token.Hash == hash);
    }

    public bool ExisteValido(DateTime agora)
    {
        return _contexto.TokensDeCadastro
            .Any(token => token.UsadoEm == null && token.ExpiraEm > agora);
    }

    public void Add(TokenDeCadastro token)
    {
        _contexto.TokensDeCadastro.Add(token);
    }

    public void Update(TokenDeCadastro token)
    {
        _contexto.TokensDeCadastro.Update(token);
    }

    public bool ExistePorUnidade(int unidadeId) =>
        _contexto.TokensDeCadastro.Any(token => token.UnidadeId == unidadeId);

    public bool ExistePorCriador(int usuarioId) =>
        _contexto.TokensDeCadastro.Any(token => token.CriadoPorUsuarioId == usuarioId);
}
