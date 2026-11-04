using Ntc.Domain;
using Ntc.Domain.Entity;

namespace Ntc.Infrastructure.Seguranca;

/// <summary>
/// Filtro de escopo reutilizável por repositório (Spec 028, invariante I4). O recorte é
/// aplicado na consulta, nunca depois do <c>ToList</c>. Cada overload corresponde a uma
/// fronteira já existente no domínio; dados de referência (RF9) não passam por aqui.
/// </summary>
public static class FiltroDeEscopo
{
    public static IQueryable<Aluno> Alunos(
        IQueryable<Aluno> alunos,
        IQueryable<VinculoResponsavelAluno> vinculos,
        EscopoUsuario escopo)
    {
        return escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista =>
                escopo.UnidadeId is { } unidadeId
                    ? alunos.Where(aluno => aluno.UnidadeId == unidadeId)
                    : alunos.Where(_ => false),
            PapelUsuario.AdminEscola or PapelUsuario.Merendeira =>
                escopo.EscolaId is { } escolaId
                    ? alunos.Where(aluno => aluno.EscolaId == escolaId)
                    : alunos.Where(_ => false),
            PapelUsuario.Responsavel => FiltrarPorVinculo(alunos, vinculos, escopo.UsuarioId),
            _ => alunos.Where(_ => false)
        };
    }

    public static IQueryable<Escola> Escolas(IQueryable<Escola> escolas, EscopoUsuario escopo)
    {
        return escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista =>
                escopo.UnidadeId is { } unidadeId
                    ? escolas.Where(escola => escola.UnidadeId == unidadeId)
                    : escolas.Where(_ => false),
            PapelUsuario.AdminEscola or PapelUsuario.Merendeira =>
                escopo.EscolaId is { } escolaId
                    ? escolas.Where(escola => escola.Id == escolaId)
                    : escolas.Where(_ => false),
            _ => escolas.Where(_ => false)
        };
    }

    public static IQueryable<Usuario> Usuarios(IQueryable<Usuario> usuarios, EscopoUsuario escopo)
    {
        return escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista =>
                escopo.UnidadeId is { } unidadeId
                    ? usuarios.Where(usuario => usuario.UnidadeId == unidadeId)
                    : usuarios.Where(_ => false),
            PapelUsuario.AdminEscola or PapelUsuario.Merendeira =>
                escopo.EscolaId is { } escolaId
                    ? usuarios.Where(usuario => usuario.EscolaId == escolaId)
                    : usuarios.Where(_ => false),
            _ => usuarios.Where(_ => false)
        };
    }

    public static IQueryable<Cardapio> Cardapios(
        IQueryable<Cardapio> cardapios,
        EscopoUsuario escopo)
    {
        return escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista =>
                escopo.UnidadeId is { } unidadeId
                    ? cardapios.Where(cardapio => cardapio.UnidadeId == unidadeId)
                    : cardapios.Where(_ => false),
            PapelUsuario.AdminEscola or PapelUsuario.Merendeira =>
                escopo.UnidadeId is { } unidadeDaEscola
                    ? cardapios.Where(cardapio => cardapio.UnidadeId == unidadeDaEscola)
                    : cardapios.Where(_ => false),
            _ => cardapios.Where(_ => false)
        };
    }

    private static IQueryable<Aluno> FiltrarPorVinculo(
        IQueryable<Aluno> alunos,
        IQueryable<VinculoResponsavelAluno> vinculos,
        int responsavelUsuarioId)
    {
        var alunosVinculados = vinculos
            .Where(vinculo => vinculo.ResponsavelUsuarioId == responsavelUsuarioId)
            .Select(vinculo => vinculo.AlunoId);

        return alunos.Where(aluno => alunosVinculados.Contains(aluno.Id));
    }
}
