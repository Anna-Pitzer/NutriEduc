---
name: fronteiras-ddd
status: planned
references: V13, ADR-004, ADR-009
---

# Tasks — Fronteiras DDD explícitas

## Pré-condição

- [x] T0: suíte atual verde (299 testes) e `arch.md` §2 como régua de camadas.
  - Validação: `dotnet test backend.Tests/backend.Tests.csproj`.

## Domínio

- [x] T1 (F8): encapsular a senha em `Usuario` — campo privado `_senha`, mapeamento por
  `PropertyAccessMode.Field`, sem propriedade pública.
  - Validação: `UsuarioTests` continua verde; teste novo garante que não há getter público.
- [x] T2 (F2): criar `HashDeToken` (criar, conferir em tempo constante, ler legado) e fazer
  `TokenDeCadastro` receber o objeto de valor.
  - Validação: `HashDeTokenTests`; `TokenDeCadastroTests` cobre legado.
- [x] T3 (F7): extrair `PoliticaDeExclusao` para `Domain/Services`, sem `EscopoUsuario`.
  - Validação: testes de cada bloqueio (escola com usuário/unidade, unidade com aluno/token,
    usuário com vínculo/last admin).
- [x] T4 (F5): criar `FiltroAluno` e trocar a assinatura dos portos de aluno e usuário.
  - Validação: teste de modelo/consulta garante que o filtro continua indo para o SQL.

## Aplicação

- [x] T5 (F3): `CriarUsuario` devolve `Resultado<int>`; controller só traduz.
  - Validação: duplicidade → 409 com a mesma mensagem; teste HTTP existente verde.
- [x] T6 (F4): validar unidade e escola antes de mutar aluno/usuário.
  - Validação: unidade inexistente e desativada → 400 sem gravar.
- [x] T7 (F5): `EscopoService` traduz `EscopoUsuario` em `FiltroAluno`; serviços passam o
  filtro aos portos.
  - Validação: `PapeisApiTests` sem alteração.
- [x] T8 (F6): criar `IUnidadeDeTrabalho`/`UnidadeDeTrabalho` e remover `SaveChanges` dos
  repositórios de escrita.
  - Validação: teste de rollback (operação lança → nada persistido).
- [x] T9 (F6): `BootstrapService.Criar` e `VinculoService.Vincular` dentro da transação.
  - Validação: falha simulada no token não deixa usuário criado.
- [x] T10 (F9): dividir `UsuarioService` e `BootstrapService` por intenção.
  - Validação: rotas e mensagens idênticas; DI atualizada em `Program.cs`.

## Contratos

- [x] T11 (F1): substituir `DadosAlunoDTO`/`DadosUsuarioDTO` por contratos de entrada e
  resposta por operação; remover respostas anônimas.
  - Validação: `ContratoRotasTests` e os testes de integração de aluno/usuário verdes.

## API

- [x] T12: ajustar controllers para os novos contratos, sem regra de negócio.
  - Validação: `dotnet build` sem avisos; revisão de que nenhum controller tem mensagem de
    regra de negócio.

## Fechamento

- [x] T13: atualizar `arch.md` §2 (tabela de camadas) e §5 (contratos e dependências).
  - Validação: documentos citam `IUnidadeDeTrabalho`, filtros de negócio e contratos por
    operação.
- [x] T14: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Domínio sem vocabulário de sessão, sem hash manipulado fora de objeto de valor e sem
  exposição de material de autenticação.
- Nenhum repositório decide transação; casos de uso multiagregado são atômicos.
- Nenhum controller contém mensagem de regra de negócio.
- Contrato HTTP idêntico ao de hoje e suíte completa verde.
