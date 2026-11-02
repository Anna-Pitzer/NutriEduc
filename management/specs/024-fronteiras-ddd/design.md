---
name: fronteiras-ddd
status: planned
references: V13, ADR-004, ADR-009
---

# Design — Fronteiras DDD explícitas

## Princípio

Cada falha do diagnóstico é corrigida movendo a regra para a camada dona — nunca duplicando
comportamento nem criando camada nova. O contrato HTTP observável não muda.

## F1 — Contratos separados por operação

```text
backend/Application/
  aluno/
    CriarAlunoEntrada.cs      (nome, nascimento, turma, telefone, observação, restrições, anafilaxia, responsável, unidadeId)
    AtualizarAlunoEntrada.cs  (mesmos campos + sem Id)
    AlunoResposta.cs          (Id, nome, ..., restrições, unidadeId)  ← usado por POST/GET/PUT
  usuario/
    CriarUsuarioEntrada.cs
    CriarUsuarioAdministradoEntrada.cs
    UsuarioResposta.cs        (sem Senha, sem hash)
```

`DadosAlunoDTO` e `DadosUsuarioDTO` são removidos junto com seus usos. Controllers passam a
tipar entrada e saída; o `PostAdd` de usuário deixa de responder objeto anônimo e passa a
devolver `UsuarioResposta`.

## F2 — `HashDeToken` como objeto de valor

```csharp
// Domain/Entities/HashDeToken.cs
public readonly struct HashDeToken
{
    public static HashDeToken Criar(string valor);          // SHA-256 + base64
    public bool Confere(string valor);                      // comparação em tempo constante
    public override string ToString();                      // persistência
    public static bool TentarLer(string? persistido, out HashDeToken hash); // legado tolerado
}
```

`TokenDeCadastro` passa a receber `HashDeToken` e expõe `ToString()` para persistência. A
geração do segredo continua na aplicação (`GerarValorDeToken`), porque depende de RNG de
plataforma; o que sai da aplicação é o **hash e a comparação**.

## F3 — Unicidade sem `null`

`UsuarioService.CriarUsuario` passa a devolver `Resultado<int>`: `Conflito("E-mail já
cadastrado.")` para duplicidade e `Ok(id)` no sucesso. `UsuarioController` apenas traduz via
`Traduzir`, como os demais controllers já fazem. Nenhuma string de regra permanece no
controller.

## F4 — Referências verificadas antes de mutar

```csharp
// AlunoService
var unidade = dto.UnidadeId is null ? null : _escolas.GetUnidade(dto.UnidadeId.Value)
    ?? throw new ArgumentException("Unidade inexistente.");
if (unidade is { Ativa: false })
    throw new ArgumentException("Unidade desativada não pode receber aluno.");
```

A entidade continua validando o formato (`> 0`); a aplicação valida existência e situação e
falha **antes** de qualquer atribuição. Mesma regra para `Usuario.DefinirPapel` (escola
existente e ativa).

## F5 — Filtros de negócio no lugar de escopo de sessão

```csharp
// Domain/FiltroAluno.cs — vocabulário de negócio, sem papéis
public sealed record FiltroAluno(int? UnidadeId, int? EscolaId, IReadOnlyCollection<int>? AlunoIds);

// API/Services/EscopoService.cs
public FiltroAluno ParaFiltroDeAluno(EscopoUsuario escopo) => escopo.Papel switch { ... };
```

Os portos `IAlunoRepository.Listar(FiltroAluno)` e `IUsuarioRepository.Listar(FiltroUsuarios)`
deixam de receber `EscopoUsuario`. `EscopoUsuario` permanece em `API/Services` (sessão é
conceito de aplicação). O filtro continua sendo aplicado em SQL (RNF4 da Spec 009).

## F6 — Unidade de trabalho

```csharp
// Domain/Interface/IUnidadeDeTrabalho.cs
public interface IUnidadeDeTrabalho
{
    T ExecutarEmTransacao<T>(Func<T> operacao);
}

// Infrastructure/Repositories/UnidadeDeTrabalho.cs  → Database.BeginTransaction + Commit/Rollback
```

Regras:
- Repositórios deixam de chamar `SaveChanges` em métodos de escrita; a persistência é
  decidida pelo caso de uso (repositórios ganham `Adicionar`, `Atualizar`, `Remover` e o
  `Salvar()` da unidade de trabalho).
- `BootstrapService.Criar` e `VinculoService.Vincular` executam dentro de
  `ExecutarEmTransacao`, tornando usuário + consumo + token (ou vínculo composto) atômicos.
- Operações de agregado único continuam com uma única gravação, sem transação explícita.

## F7 — Exclusão como política de domínio

```csharp
// Domain/Services/PoliticaDeExclusao.cs — sem escopo, sem HTTP
public sealed class PoliticaDeExclusao
{
    public Resultado<bool> PodeExcluirEscola(...);
    public Resultado<bool> PodeExcluirUnidade(...);
    public Resultado<bool> PodeExcluirUsuario(int id, int solicitanteId, bool ehUltimoAdminSecretaria, ...);
}
```

`ExclusaoService` (aplicação) carrega os dados necessários, consulta a política e decide o
escopo de quem pede. As mensagens de bloqueio saem da política, não do controller.

## F8 — Senha encapsulada

`Usuario.Senha` deixa de ser pública. A entidade expõe `SenhaConfere(string)` e
`SenhaLegada(string?)`; a persistência usa `PropertyAccessMode.Field` com campo privado
`_senha`, como já é feito com `Aluno._restricoes`. Nenhum DTO carrega hash.

## F9 — Serviços por intenção

```text
API/Services/
  CadastroUsuarioService.cs     (criar conta pública e administrada)
  AutenticacaoService.cs        (login, senha legada, sessão)
  PerfilUsuarioService.cs       (atualizar perfil, telefone, foto)
  AdministracaoUsuarioService.cs(listar, ativar, papel, escopo)
  BootstrapTokenService.cs      (token de ambiente e temporário)
```

Cada serviço depende só dos portos que usa. Rotas, status e mensagens preservados; os testes
de integração existentes são a rede de segurança.

## Ordem de execução

1. F8 (senha) e F2 (hash de token) — domínio isolado, baixo risco.
2. F1 (contratos) — mecânico, coberto por testes HTTP.
3. F3 e F4 — regras pequenas, testes de serviço.
4. F6 (unidade de trabalho) — mexe em repositórios; fazer antes de F7/F9.
5. F5 (filtros) — mexe em portos e repositórios.
6. F7 (política) e F9 (divisão de serviços) — por último, sem mudança de comportamento.

## Testes obrigatórios

| Camada | Caso |
|---|---|
| Domínio | `HashDeToken.Criar/Confere`, hash legado, comparação em tempo constante |
| Domínio | `Usuario` sem setter de senha; `SenhaConfere` continua funcionando |
| Domínio | `PoliticaDeExclusao` para cada bloqueio existente |
| Serviço | `CriarUsuario` duplicado devolve `Conflito`, não `null` |
| Serviço | unidade inexistente/desativada → 400 sem gravar |
| Infraestrutura | `ExecutarEmTransacao` faz rollback quando a operação lança |
| HTTP | contrato de `POST /api/usuario/registrar`, `POST /api/aluno/registrar` e bootstrap |

## Riscos

- **Refatoração ampla**: mitigado pela ordem acima e pelos 299 testes existentes; cada passo
  é verificável isoladamente.
- **Unidade de trabalho muda o ponto de commit**: um `SaveChanges` esquecido faria escrita
  silenciosamente não persistida. Mitigação: teste de integração por caso de uso e revisão
  explícita de cada serviço migrado.
- **Filtros de negócio**: risco de divergir do escopo atual. Mitigação: os testes de escopo
  por papel (`PapeisApiTests`) rodam sem alteração.
