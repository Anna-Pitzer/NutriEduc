using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Garante que a tabela de endpoints do README não envelheça: compara cada par
/// método+rota documentado com os atributos `[Http*]`/`[Route]` dos controllers.
/// </summary>
public class DocumentationTests
{
    private static readonly Regex LinhaDocumentada = new(
        @"\|\s*`(?<verbo>GET|POST|PUT|DELETE|PATCH)`\s*\|\s*`(?<rota>/api/[^`]+)`",
        RegexOptions.Compiled);

    [Fact]
    public void Readme_ListaTodosOsEndpoints()
    {
        var caminho = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../README.md"));

        Assert.True(File.Exists(caminho), $"README não encontrado em {caminho}");

        var readme = File.ReadAllText(caminho);

        var codigo = RotasDosControllers();
        var documentadas = LinhaDocumentada.Matches(readme)
            .Select(m => $"{m.Groups["verbo"].Value} {m.Groups["rota"].Value}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ausentes = codigo
            .Except(documentadas, StringComparer.OrdinalIgnoreCase)
            .OrderBy(rota => rota)
            .ToList();

        var excedentes = documentadas
            .Except(codigo, StringComparer.OrdinalIgnoreCase)
            .OrderBy(rota => rota)
            .ToList();

        Assert.True(ausentes.Count == 0,
            "Rotas sem linha no README: " + string.Join(", ", ausentes));
        Assert.True(excedentes.Count == 0,
            "Rotas no README sem controller: " + string.Join(", ", excedentes));
    }

    private static HashSet<string> RotasDosControllers()
    {
        var rotas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var controllers = typeof(Program).Assembly.GetTypes()
            .Where(tipo => !tipo.IsAbstract && typeof(ControllerBase).IsAssignableFrom(tipo));

        foreach (var controller in controllers)
        {
            var rotaBase = controller.GetCustomAttribute<RouteAttribute>()?.Template
                ?? throw new InvalidOperationException($"{controller.Name} sem [Route].");

            rotaBase = rotaBase.Replace(
                "[controller]",
                controller.Name.Replace("Controller", "").ToLowerInvariant(),
                StringComparison.OrdinalIgnoreCase);

            var metodos = controller.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var metodo in metodos)
            {
                foreach (var atributo in metodo.GetCustomAttributes<HttpMethodAttribute>())
                {
                    var rota = Combinar(rotaBase, atributo.Template);

                    foreach (var verbo in atributo.HttpMethods)
                        rotas.Add($"{verbo} {rota}");
                }
            }
        }

        return rotas;
    }

    private static string Combinar(string rotaBase, string? template)
    {
        var partes = new[] { rotaBase, template ?? "" }
            .SelectMany(parte => parte.Split('/', StringSplitOptions.RemoveEmptyEntries));

        var rota = "/" + string.Join("/", partes);

        // Normaliza constraints: {id:int} e {id:min(1)} viram {id}.
        return Regex.Replace(rota, @"\{([^}:]+)(:[^}]+)?\}", "{$1}");
    }
}
