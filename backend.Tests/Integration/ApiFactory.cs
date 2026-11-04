using Backend.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using Ntc.Database;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Integration;

/// <summary>
/// Sobe a API real com SQLite em memória. A conexão é aberta antes do host iniciar
/// (o startup chama <c>Database.Migrate()</c>) e permanece aberta durante toda a
/// vida da fábrica, porque SQLite em memória é descartado ao fechar a conexão.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Token de bootstrap usado pelos testes da Spec 022.</summary>
    public const string TokenBootstrap = "token-de-teste-com-mais-de-32-caracteres";

    /// <summary>Permite subclasses desabilitarem a rota de bootstrap (token ausente).</summary>
    protected virtual string? TokenDoBootstrap => TokenBootstrap;

    /// <summary>`null` deixa a chave ausente; subclasses podem negar a criação.</summary>
    protected virtual bool? PermitirBootstrap => null;

    /// <summary>Quando definido, substitui o relógio do sistema (testes de expiração).</summary>
    protected virtual TimeProvider? Relogio => null;

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    /// <summary>
    /// Semeia o catálogo usado pelos testes de aluno (Spec 010). Sem catálogo, toda gravação
    /// de aluno com restrição seria recusada com 400 — que é justamente o comportamento
    /// correto em produção para um catálogo vazio.
    /// </summary>
    private static readonly (string Nome, CategoriaRestricao Categoria)[] CatalogoInicial =
    {
        ("Lactose", CategoriaRestricao.Alergia),
        ("Amendoim", CategoriaRestricao.Alergia),
        ("Gluten", CategoriaRestricao.Restricao),
        ("Diabetes", CategoriaRestricao.Restricao)
    };

    private bool _catalogoSemeado;

    private (int EscolaId, int UnidadeId)? _padrao;

    public ApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (TokenDoBootstrap is { } token)
        {
            builder.UseSetting("Bootstrap:Token", token);
        }

        if (PermitirBootstrap is { } permitir)
        {
            builder.UseSetting("Bootstrap:Permitir", permitir ? "true" : "false");
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ConnectionContext>>();
            services.RemoveAll<ConnectionContext>();
            services.AddDbContext<ConnectionContext>(options => options.UseSqlite(_connection));

            if (Relogio is { } relogio)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(relogio);
            }
        });
    }

    /// <summary>Cria o catálogo inicial uma única vez por fábrica (idempotente).</summary>
    public void GarantirCatalogo()
    {
        if (_catalogoSemeado)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<IRestricaoRepository>();
        var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

        foreach (var (nome, categoria) in CatalogoInicial)
        {
            if (!repositorio.ExisteNomeNormalizado(
                    RestricaoAlimentar.Normalizar(nome), categoria, ignorarId: null))
            {
                unidadeDeTrabalho.ExecutarEmTransacao(
                    () => repositorio.Add(new RestricaoAlimentar(nome, categoria)));
            }
        }

        _catalogoSemeado = true;
    }

    /// <summary>
    /// Cria uma escola (para escopo de usuários) e uma unidade da rede para os testes do
    /// bootstrap v2. O nome da unidade pode ser fixado para exercitar a busca pelo nome exato.
    /// </summary>
    public (int EscolaId, int UnidadeId) CriarEscolaComUnidade(string? nomeDaUnidade = null)
    {
        GarantirCatalogo();

        using var scope = Services.CreateScope();
        var escolas = scope.ServiceProvider.GetRequiredService<IEscolaRepository>();
        var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

        var unidade = unidadeDeTrabalho.ExecutarEmTransacao(
            () => escolas.AddUnidade(new UnidadeEscolar(
                nomeDaUnidade ?? $"Unidade {Guid.NewGuid():N}")));
        var escola = unidadeDeTrabalho.ExecutarEmTransacao(
            () => escolas.Add(new Escola(
                $"Escola de teste {Guid.NewGuid():N}", unidade.Id)));

        return (escola.Id, unidade.Id);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }

    /// <summary>
    /// Escola e unidade padrão da mesma cadeia, criadas uma única vez por fábrica. O
    /// cadastro de aluno exige escola, e a unidade é sempre derivada dela.
    /// </summary>
    private (int EscolaId, int UnidadeId) Padrao => _padrao ??= CriarEscolaComUnidade();

    public int UnidadeIdPadrao => Padrao.UnidadeId;

    /// <summary>Escola padrão da mesma unidade usada pelos testes de aluno.</summary>
    public int EscolaIdPadrao => Padrao.EscolaId;

    public void DefinirPapel(
        string email,
        PapelUsuario papel,
        int? escolaId = null,
        int? unidadeId = null)
    {
        using var scope = Services.CreateScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        var usuario = usuarios.GetByEmail(email)
            ?? throw new InvalidOperationException($"Usuário {email} não existe.");

        usuario.DefinirPapel(papel, escolaId, unidadeId);
        scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>()
            .ExecutarEmTransacao(() => usuarios.Update(usuario));
    }

    /// <summary>
    /// Cria um cliente com cookie de sessão válido. O cadastro público cria a conta sem
    /// papel (Spec 009); o papel é atribuído aqui via repositório, como faria um
    /// administrador, e só então o login é feito.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        PapelUsuario papel = PapelUsuario.Nutricionista,
        int? escolaId = null,
        int? unidadeId = null)
    {
        var (client, _) = await CriarClienteAutenticadoAsync(papel, escolaId, unidadeId);
        return client;
    }

    /// <summary>
    /// Mesma autenticação, devolvendo também o id do usuário criado — necessário quando o
    /// teste precisa referenciar a conta autenticada (por exemplo, autoria do cardápio).
    /// </summary>
    public async Task<(HttpClient Client, int UsuarioId)> CriarClienteAutenticadoAsync(
        PapelUsuario papel = PapelUsuario.Nutricionista,
        int? escolaId = null,
        int? unidadeId = null)
    {
        var (client, email) = await RegistrarAsync();

        if (papel is PapelUsuario.AdminEscola or PapelUsuario.Merendeira &&
            escolaId is null)
        {
            var padrao = CriarEscolaComUnidade();
            escolaId = padrao.EscolaId;
            unidadeId ??= padrao.UnidadeId;
        }
        else if (papel is PapelUsuario.AdminSecretaria
                 or PapelUsuario.Nutricionista
                 or PapelUsuario.Responsavel &&
                 unidadeId is null)
        {
            unidadeId = UnidadeIdPadrao;
        }

        int usuarioId;

        using (var scope = Services.CreateScope())
        {
            var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
            var usuario = usuarios.GetByEmail(email)
                ?? throw new InvalidOperationException($"Usuário {email} não foi criado.");

            usuario.DefinirPapel(papel, escolaId, unidadeId);
            usuarioId = usuario.Id;
            scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>()
                .ExecutarEmTransacao(() => usuarios.Update(usuario));
        }

        var login = await client.PostAsJsonAsync("/api/usuario/login", new
        {
            email,
            senha = "senha12345"
        });
        login.EnsureSuccessStatusCode();

        return (client, usuarioId);
    }

    /// <summary>
    /// Registra um usuário pelo endpoint público e devolve o cliente e o e-mail. Útil
    /// para testar a conta sem papel e para promover o usuário manualmente.
    /// </summary>
    public async Task<(HttpClient Client, string Email)> RegistrarAsync()
    {
        GarantirCatalogo();

        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var email = $"aluno.api.{Guid.NewGuid():N}@nutrieduc.local";

        var registro = await client.PostAsJsonAsync("/api/usuario/registrar", new
        {
            nome = "Usuario de Teste",
            cpf = "12345678901",
            dataNascimento = "1990-01-01",
            email,
            senha = "senha12345",
            telefone = (string?)null,
            foto = (string?)null
        });
        registro.EnsureSuccessStatusCode();

        return (client, email);
    }

    /// <summary>Faz login de um usuário já existente e devolve o cliente com cookie.</summary>
    public async Task<HttpClient> EntrarAsync(string email, string senha = "senha12345")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var login = await client.PostAsJsonAsync("/api/usuario/login", new { email, senha });
        login.EnsureSuccessStatusCode();

        return client;
    }
}

/// <summary>Fábrica sem token: a rota de bootstrap deve responder 404.</summary>
public sealed class ApiFactorySemBootstrap : ApiFactory
{
    protected override string? TokenDoBootstrap => null;
}

/// <summary>Fábrica com a criação de administrador negada por configuração.</summary>
public sealed class ApiFactoryBootstrapNegado : ApiFactory
{
    protected override bool? PermitirBootstrap => false;
}

/// <summary>Fábrica com relógio controlável, para testar a expiração de 30 minutos.</summary>
public sealed class ApiFactoryComRelogio : ApiFactory
{
    public RelogioFalso RelogioFalso { get; } = new();

    protected override TimeProvider Relogio => RelogioFalso;
}
