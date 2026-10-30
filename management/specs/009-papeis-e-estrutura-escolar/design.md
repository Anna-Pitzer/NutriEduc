---
name: papeis-e-estrutura-escolar
status: created
references: V6, ADR-002, L4
---

# Design — Papéis e estrutura escolar

## Estrutura

```text
backend/Domain/Entities/
  PapelUsuario.cs            (enum)
  Escola.cs                  (entidade raiz)
  UnidadeEscolar.cs          (entidade)
  VinculoResponsavelAluno.cs (entidade de junção)
  Usuario.cs                 (+ Papel, EscolaId, UnidadeId, Ativo)
backend/Domain/Interface/
  IEscolaRepository.cs
  IVinculoRepository.cs
backend/API/Services/
  EscolaService.cs
  VinculoService.cs
  EscopoService.cs           (resolução de escopo por claims)
backend/API/Controllers/
  EscolaController.cs
  UnidadeController.cs
  VinculoController.cs
```

`EscopoService` é a única fonte de verdade sobre "quem pode ver o quê". Controllers
pedem o escopo e o aplicam; nenhum controller recalcula regra de papel.

## Domínio

```csharp
public enum PapelUsuario { Nutricionista, Merendeira, Responsavel, AdminSecretaria, AdminEscola }

public class Escola
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public bool Ativa { get; private set; }

    public Escola(string nome) { DefinirNome(nome); Ativa = true; }

    public void DefinirNome(string nome)
    {
        nome = (nome ?? string.Empty).Trim();
        if (nome.Length < 2 || nome.Length > 120)
            throw new ArgumentException("O nome da escola deve ter entre 2 e 120 caracteres.");
        Nome = nome;
    }

    public void Desativar() => Ativa = false;
    public void Reativar() => Ativa = true;
}

public class UnidadeEscolar
{
    public int Id { get; private set; }
    public int EscolaId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public bool Ativa { get; private set; }

    public UnidadeEscolar(int escolaId, string nome)
    {
        if (escolaId <= 0) throw new ArgumentException("Unidade exige escola válida.");
        EscolaId = escolaId;
        DefinirNome(nome);
        Ativa = true;
    }
    // DefinirNome: 2..120, trim; Desativar/Reativar
}
```

`Usuario` muda de estado, não de responsabilidade:

```csharp
public PapelUsuario Papel { get; private set; }
public int? EscolaId { get; private set; }
public int? UnidadeId { get; private set; }
public bool Ativo { get; private set; } = true;

// Construtor único de criação (substitui os construtores parciais apontados em PEND-002)
public Usuario(string nome, string cpf, DateOnly dataNascimento, string email,
               PapelUsuario papel, int? escolaId = null, int? unidadeId = null)
{
    AtualizarPerfil(nome, email);
    AtualizarCpf(cpf);
    DataNascimento = dataNascimento;
    DefinirPapel(papel, escolaId, unidadeId);
    Ativo = true;
}

public void DefinirPapel(PapelUsuario papel, int? escolaId, int? unidadeId)
{
    bool exigeEscola = papel is PapelUsuario.AdminEscola or PapelUsuario.Merendeira
                            or PapelUsuario.Responsavel;
    if (exigeEscola && escolaId is null)
        throw new ArgumentException("Este papel exige escola.");
    if (papel is not (PapelUsuario.Merendeira or PapelUsuario.Responsavel) && unidadeId is not null)
        throw new ArgumentException("Unidade só se aplica a merendeira ou responsável.");
    Papel = papel; EscolaId = escolaId; UnidadeId = unidadeId;
}

public void Desativar() => Ativo = false;
public void Reativar() => Ativo = true;
```

O construtor privado sem parâmetros permanece para o EF Core materializar registros
legados; nesse caso `Papel` vem do banco e registros sem papel são tratados como
inválidos apenas na autenticação, nunca apagados.

## Autorização

Políticas registradas em `Program.cs`:

```csharp
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AdminSecretaria", p => p.RequireRole(nameof(PapelUsuario.AdminSecretaria)));
    o.AddPolicy("AdminEscola", p => p.RequireRole(nameof(PapelUsuario.AdminEscola)));
    o.AddPolicy("Nutricionista", p => p.RequireRole(nameof(PapelUsuario.Nutricionista)));
    o.AddPolicy("EquipeEscolar", p => p.RequireRole(
        nameof(PapelUsuario.Nutricionista),
        nameof(PapelUsuario.Merendeira),
        nameof(PapelUsuario.AdminEscola)));
    o.AddPolicy("SessaoValida", p => p.RequireAuthenticatedUser());
});
```

Claims no login (`UsuarioController.PostLogin`): acrescentar

```text
ClaimTypes.NameIdentifier = usuario.Id
ClaimTypes.Role           = usuario.Papel.ToString()
"escola"                  = usuario.EscolaId?.ToString() ?? ""
"unidade"                 = usuario.UnidadeId?.ToString() ?? ""
```

Revalidação de sessão — `OnValidatePrincipal` consulta `IUsuarioRepository` via
`context.HttpContext.RequestServices`; se o usuário não existe ou `Ativo == false`,
chama `context.RejectPrincipal()` e `SignOutAsync`. É o único ponto que invalida
sessões, evitando lógica duplicada em cada endpoint.

## Escopo de dados

```csharp
public sealed record EscopoUsuario(
    int UsuarioId, PapelUsuario Papel, int? EscolaId, int? UnidadeId)
{
    public bool Global => Papel is PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista;
}
```

- `Nutricionista` e `AdminSecretaria`: `Global = true`.
- `AdminEscola`: filtra por `EscolaId`.
- `Merendeira`: filtra por `UnidadeId` (e pela escola da unidade).
- `Responsavel`: filtra pela tabela de vínculos.

`IAlunoRepository.Listar(EscopoUsuario escopo)` aplica o filtro na consulta SQL
(`Where`), nunca depois do `ToList()`.

## Contrato de API

| Método | Rota | Política |
|---|---|---|
| `POST` | `/api/escolas` | `AdminSecretaria` |
| `GET` | `/api/escolas` | `SessaoValida` |
| `PUT` | `/api/escolas/{id}` | `AdminSecretaria` |
| `POST` | `/api/escolas/{id}/desativar` | `AdminSecretaria` |
| `POST` | `/api/escolas/{id}/reativar` | `AdminSecretaria` |
| `POST` | `/api/escolas/{id}/unidades` | `AdminSecretaria` |
| `GET` | `/api/escolas/{id}/unidades` | `SessaoValida` |
| `PUT` | `/api/unidades/{id}` | `AdminSecretaria` |
| `POST` | `/api/unidades/{id}/desativar` | `AdminSecretaria` |
| `POST` | `/api/usuarios` | `AdminSecretaria` ou `AdminEscola` |
| `GET` | `/api/usuarios` | `AdminSecretaria` ou `AdminEscola` |
| `PUT` | `/api/usuarios/{id}` | dono do perfil ou admin do escopo |
| `POST` | `/api/usuarios/{id}/ativar` | admin do escopo |
| `POST` | `/api/usuarios/{id}/desativar` | admin do escopo |
| `POST` | `/api/alunos/{id}/responsaveis/{usuarioId}` | `AdminEscola` do aluno |
| `DELETE` | `/api/alunos/{id}/responsaveis/{usuarioId}` | `AdminEscola` do aluno |
| `GET` | `/api/alunos/{id}/responsaveis` | `EquipeEscolar` do aluno |
| `GET` | `/api/usuario/me` | `SessaoValida` (acrescenta papel e escopo) |

`POST /api/usuarios` recebe `papel`; `AdminEscola` só pode enviar `Responsavel` ou
`Merendeira` e `escolaId` igual à própria. Violação → 403. Corpo inválido → 400
`{ erro }`.

## Persistência

```csharp
public DbSet<Escola> Escolas { get; set; }
public DbSet<UnidadeEscolar> Unidades { get; set; }
public DbSet<VinculoResponsavelAluno> Vinculos { get; set; }
```

- `Escolas.Nome` único (índice); `Unidades` único por `(EscolaId, Nome)`.
- `Vinculos` único por `(AlunoId, ResponsavelUsuarioId)`; `AlunoId` com FK e
  `OnDelete(Cascade)`.
- `Usuario.EscolaId`/`UnidadeId` com `OnDelete(Restrict)`: escola não é apagada com
  usuários vinculados.
- `Aluno` recebe `UnidadeId` anulável (FK opcional, `OnDelete(SetNull)`).
- Migration: `PapeisEEscolas`.

Seed do primeiro `AdminSecretaria`: `AdminInicial:Email` e `AdminInicial:Senha` em
configuração; quando ausentes, o startup registra aviso e não cria usuário. Senha
nunca é escrita no código nem no repositório.

## Frontend

- `frontend/src/context/SessaoContext.tsx`: expõe `papel`, `escolaId`, `unidadeId`.
- `frontend/src/routes/ProtegidaPorPapel.tsx`: componente de rota que recebe
  `papeis: PapelUsuario[]`; sem papel permitido → `/login`.
- `pages/secretaria/` e `pages/escolas/` deixam de ser placeholders: listas,
  formulários e ações de ativação/desativação.
- Menu (`components/Menu.tsx`) deriva itens de `papel`; a fonte de verdade continua
  sendo o backend.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | `Usuario` sem escola para papel que exige escola lança `ArgumentException` |
| Domínio | `Unidade` filha de escola inexistente (id ≤ 0) lança `ArgumentException` |
| Domínio | `Escola` com nome vazio/curto lança `ArgumentException` |
| Serviço | `AdminEscola` criando `AdminSecretaria` → 403 sem persistir |
| Serviço | filtro de `Merendeira` usa `UnidadeId` no repositório |
| HTTP | matriz 401/403/200 por papel em `/api/escolas` e `/api/usuarios` |
| HTTP | sessão de usuário desativado rejeitada na requisição seguinte |
| HTTP | vínculo duplicado → 409 |

## Sequência

1. Entidades e invariantes (`Escola`, `UnidadeEscolar`, `Usuario`, vínculo).
2. Migration `PapeisEEscolas` e configuração do `ConnectionContext`.
3. Claims, políticas e `OnValidatePrincipal`.
4. `EscopoService` e filtros de consulta.
5. Endpoints e autorização por política.
6. Frontend: contexto de sessão, guarda de rota, telas de secretaria/escola.
7. Testes e gates.

## Riscos

- **Sessões antigas sem claims de papel**: usuários existentes perdem acesso até novo
  login. Mitigação: `OnValidatePrincipal` emite log explícito e o login é o único
  caminho de reemissão.
- **Vazamento por 404**: escolher 403 para recurso fora do escopo revela existência;
  404 revela ausência. A decisão da RF9 é 403 e está registrada; se o produto exigir
  ocultação, muda-se em um único lugar (`EscopoService`).
- **`Aluno.UnidadeId` nulo em dados legados**: escola não vê o aluno até vinculá-lo;
  a nutrição continua vendo. Isso é preferível a inferir escola pelo texto de
  `Turma.Escola`, que não é chave.
