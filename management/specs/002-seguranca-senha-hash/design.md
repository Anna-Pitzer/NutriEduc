---
name: seguranca-senha-hash
status: created
references: V3, ADR-003, L3
---

# Design — Senha com hash

## Estrutura

Novo tipo de valor interno em `backend/Domain/Entity/`:

```text
HashSenha (struct readonly)
  Prefixo     : string  = "pbkdf2-sha256"
  Iteracoes   : int     = 100_000
  Salt        : byte[16]
  Derivado    : byte[32]

  static HashSenha Criar(string senha)
  bool Confere(string senha)              // CryptographicOperations.FixedTimeEquals
  static bool TentarLer(string? valor, out HashSenha hash)
  override string ToString()              // serialização para persistência
```

`struct readonly` evita estado compartilhado mutável e deixa explícito que hash é
valor, não identidade.

## Domínio — `backend/Domain/Entities/Usuario.cs`

```csharp
public void DefinirSenha(string senha)
{
    if (string.IsNullOrWhiteSpace(senha))
        throw new ArgumentException("A senha não pode ser vazia.");
    if (senha.Length < 8)
        throw new ArgumentException("A senha deve possuir pelo menos 8 caracteres.");

    Senha = HashSenha.Criar(senha).ToString();
}

public bool SenhaConfere(string? senha)
{
    if (string.IsNullOrEmpty(senha)) return false;
    if (!HashSenha.TentarLer(Senha, out var hash)) return false;
    return hash.Confere(senha);
}

public bool SenhaLegada(string? senha) =>
    !string.IsNullOrEmpty(senha) && !HashSenha.TentarLer(Senha, out _) && Senha == senha;
```

Ordem obrigatória: guard clause → comprimento → derivação → atribuição (corrige L3).
`SenhaValida()` é removida; nenhum consumidor permanece.

## Aplicação — `backend/API/Services/UsuarioService.cs`

```csharp
public bool CriarUsuario(DadosUsuarioDTO dto)
{
    var usuario = new Usuario(dto.Nome, dto.Cpf, dto.DataNascimento, dto.Email);
    usuario.DefinirSenha(dto.Senha ?? string.Empty);   // lança 400 via handler global

    if (_usuarioRepository.ExistsByEmail(usuario.Email!)) return false;

    _usuarioRepository.Add(usuario);
    return true;
}

public bool VerificarLogin(LoginUsuarioDTO dto)
{
    var usuario = _usuarioRepository.GetByEmail(dto.Email ?? string.Empty);
    if (usuario == null) return false;

    if (usuario.SenhaConfere(dto.Senha)) return true;

    if (usuario.SenhaLegada(dto.Senha))
    {
        usuario.DefinirSenha(dto.Senha!);   // re-hash na primeira autenticação válida
        _usuarioRepository.Update(usuario);
        return true;
    }

    return false;
}
```

## Persistência

- Nova migration `HashSenhaUsuario`: amplia `Senha` para `TEXT` sem limite prático
  (o valor formatado tem ~120 caracteres). SQLite não exige alteração de tipo para
  `TEXT`; a migration documenta a mudança semântica do conteúdo.
- Nenhum `Down` destrutivo: reverter exigiria re-hash, fora de escopo.

## Testes obrigatórios (Spec 005)

| Caso | Esperado |
| --- | --- |
| `DefinirSenha("1234567")` | `ArgumentException` |
| `DefinirSenha(null)` | `ArgumentException` |
| Cadastro válido | `Senha` com prefixo `pbkdf2-sha256$` |
| Mesma senha em dois usuários | hash diferente |
| `SenhaConfere` correto/incorreto | `true` / `false` |
| `SenhaConfere(null)` | `false`, sem exceção |
| Login legado válido | autentica e migra |
| Login legado inválido | falha sem migrar |

## Riscos

- Custo de 100.000 iterações por login: aceitável no escopo; medir antes de aumentar.
- Senhas legadas continuam expostas até o primeiro login de cada usuário; reportar em
  release notes.
