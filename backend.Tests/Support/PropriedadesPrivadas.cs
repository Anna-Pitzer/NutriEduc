namespace Backend.Tests.Support;

/// <summary>
/// Acesso a setters privados das entidades, exclusivo dos testes.
///
/// Existe para reproduzir dois estados que só o EF Core ou um banco legado produziriam:
/// atribuir <c>Id</c> ao inserir no repositório fake e gravar uma senha em claro anterior
/// à Spec 002. Uma implementação única evita que os fakes divirjam na forma de refletir.
/// </summary>
internal static class PropriedadesPrivadas
{
    public static void Definir(object alvo, string propriedade, object? valor)
    {
        alvo.GetType()
            .GetProperty(
                propriedade,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true)!
            .Invoke(alvo, new[] { valor });
    }
}
