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
- Integração usa `WebApplicationFactory<Program>`, exigindo tornar `Program`
  acessível: `public partial class Program { }` em `backend/Program.cs` (única
  alteração permitida no projeto de produção).

```csharp
public class AlunoApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public AlunoApiTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient();
}
```

## Matriz de cobertura

| Alvo | Casos |
| --- | --- |
| `Usuario` | 9 casos de validação + 4 de hash/verificação |
| `Aluno` | 12 casos de invariantes + 4 de restrições |
| `UsuarioService` | 7 casos |
| `AlunoService` | 9 casos |
| API de integração | 6 contratos (status e formato de erro) |

## Isolamento

- Cada teste de integração cria seu próprio `WebApplicationFactory` com banco
  `:memory:` injetado por `ConfigureTestServices`, substituindo o `ConnectionContext`.
- Nenhum teste compartilha `DbContext`, evitando dependência de ordem.

## Riscos

- `WebApplicationFactory` executa `Database.Migrate()` no startup; com SQLite em
  memória a conexão precisa permanecer aberta durante o teste.
- Teste de foto de 5 MB exige geração de base64 grande: usar `new byte[...]` em
  memória, sem arquivo em disco.
