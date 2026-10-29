using Ntc.Domain.Entity;

namespace Backend.Tests.Support;

/// <summary>
/// Recursos exclusivos dos testes. Gravar uma senha em claro simula um registro criado
/// antes da Spec 002, quando ainda não havia hash — o único jeito de produzir esse
/// estado sem usar o construtor público antigo.
/// </summary>
internal static class UsuarioFabrica
{
    public static void DefinirSenhaEmClaro(Usuario usuario, string senha)
    {
        typeof(Usuario)
            .GetProperty(nameof(Usuario.Senha))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(usuario, new object[] { senha });
    }
}
