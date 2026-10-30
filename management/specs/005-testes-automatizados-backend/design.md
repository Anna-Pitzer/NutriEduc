---
name: testes-automatizados-backend
status: created
references: V4, L7
---

# Design — Testes automatizados do backend

## Estrutura do projeto

```text
backend.Tests/
├── backend.Tests.csproj
├── Domain/
│   ├── UsuarioTests.cs
│   ├── AlunoTests.cs
│   └── HashSenhaTests.cs
├── Services/
│   ├── UsuarioServiceTests.cs
│   └── AlunoServiceTests.cs
├── Integration/
│   ├── ApiFactory.cs
│   ├── AlunoApiTests.cs
│   └── UsuarioApiTests.cs
└── Fakes/
    ├── UsuarioRepositoryFake.cs
    └── AlunoRepositoryFake.cs
```

## Pacotes

| Pacote | Uso |
| --- | --- |
| `xunit` | framework de teste |
| `xunit.runner.visualstudio` | descoberta/execução |
| `Microsoft.NET.Test.Sdk` | host de teste |
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory` para integração |
| `Microsoft.EntityFrameworkCore.Sqlite` | banco em memória via `Data Source=:memory:` |

## Padrões

- Arrange / Act / Assert explícitos, sem helpers mágicos.
- Um comportamento por teste; nome no formato
  `Metodo_Cenario_Esperado` (ex.: `AtualizarPerfil_NomeCurto_LancaArgumentException`).
- Fakes implementam `IUsuarioRepository`/`IAlunoRepository` com `List<T>` em memória e
  contadores de chamada para verificar persistência.
- Integração usa `WebApplicationFactory<Program>` através de `ApiFactory`, que
  substitui o `ConnectionContext` por SQLite em memória. Além de tornar `Program`
  parcial, a infraestrutura exigiu duas mudanças no projeto de produção:
  `ConnectionContext` ganhou construtor com `DbContextOptions<T>` e `OnConfiguring`
  respeita `options.IsConfigured`; `UsuarioRepository` passou a receber o contexto
  por injeção (antes instanciava `new ConnectionContext()`).

```csharp
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ApiFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ConnectionContext>>();
            services.RemoveAll<ConnectionContext>();
            services.AddDbContext<ConnectionContext>(o => o.UseSqlite(_connection));
        });
}
```

## Matriz de cobertura

| Alvo | Casos |
| --- | --- |
| `Usuario` | 9 casos de validação + 4 de hash/verificação |
| `Aluno` | 12 casos de invariantes + 4 de restrições |
| `UsuarioService` | 7 casos |
| `AlunoService` | 9 casos |
| API de integração | 12 testes (6 de aluno, 6 de usuário) |

## Isolamento

- Cada classe de teste de integração compartilha uma `ApiFactory` (`IClassFixture`),
  com uma única conexão SQLite `:memory:` aberta enquanto a fábrica existir.
- A fábrica remove os registros de `DbContextOptions<ConnectionContext>` e
  `ConnectionContext` e registra um `AddDbContext` apontando para a conexão em
  memória; assim o `Database.Migrate()` do startup cria o schema no banco de teste.
- Nenhum teste toca `database.db`; a conexão em memória é descartada ao final da
  classe, o que isola as classes entre si.

## Riscos

- `WebApplicationFactory` executa `Database.Migrate()` no startup; com SQLite em
  memória a conexão precisa permanecer aberta durante o teste.
- Teste de foto de 5 MB exige geração de base64 grande: usar `new byte[...]` em
  memória, sem arquivo em disco.
