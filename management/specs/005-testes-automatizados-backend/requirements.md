---
name: testes-automatizados-backend
status: created
references: V4, L7
---

# Requirements — Testes automatizados do backend

## Objetivo

Criar a rede de segurança que hoje não existe. Nenhum teste automatizado no
repositório; a avaliação atribuiu 0,0 à dimensão de testes e toda mudança é uma
aposta.

## Escopo

- Novo projeto `backend.Tests/` (xUnit) na raiz do repositório.
- Testes unitários de domínio: `Usuario`, `Aluno`, `HashSenha`.
- Testes de aplicação: `UsuarioService`, `AlunoService` com repositórios fake.
- Testes de integração de API com `Microsoft.AspNetCore.Mvc.Testing` e SQLite em
  memória, cobrindo os contratos de `UsuarioController` e `AlunoController`.
- Fora de escopo: testes de frontend.

## Requisitos funcionais

- RF1: `dotnet test backend.Tests/backend.Tests.csproj` executa toda a suíte e
  reporta contagem.
- RF2: `Usuario` é coberto em: nome curto e longo, e-mail inválido, telefone com 9 e
  12 dígitos, senha com 7 caracteres, senha nula, foto acima de 5 MB, foto com mime
  não permitido, telefone com máscara.
- RF3: `Aluno` é coberto por todas as invariantes da Spec 004 e pelas operações de
  restrição da Spec 003.
- RF4: `UsuarioService` é coberto em criação válida, e-mail duplicado, senha inválida,
  login válido, login inválido, login legado com re-hash e troca de senha.
- RF5: `AlunoService` é coberto em criação válida, criação inválida (exceção do
  domínio), listagem vazia, obtenção por ID existente e inexistente, exclusão existente
  e inexistente, atualização existente e inexistente.
- RF6: testes de integração verificam status e corpo de erro de
  `POST /api/usuario/login`, `GET /api/aluno` e `PUT /api/aluno/{id}`.

## Requisitos não funcionais

- RNF1: nenhum teste acessa rede ou banco persistente em disco.
- RNF2: repositórios fake implementam as interfaces existentes, sem biblioteca de
  mock obrigatória.
- RNF3: testes são independentes entre si; nenhum depende de ordem de execução.
- RNF4: nomes de teste descrevem comportamento observável.

## Restrições

- Não alterar o projeto `backend/backend.csproj` além de `InternalsVisibleTo` se
  necessário.
- Não introduzir dependência de banco real.
- Sem `--no-verify` em nenhum comando.

## Dependências

- Spec 002 (hash), Spec 003 (restrições) e Spec 004 (invariantes) definem os
  comportamentos testados.

## Casos de borda

- Repositório fake que devolve `null` → serviço devolve `null`/`false` sem exceção.
- Duas instâncias de `Aluno` com a mesma senha → hashes distintos.
- Banco em memória descartado entre testes → sem vazamento de estado.

## Critérios de aceitação

- [x] `backend.Tests/backend.Tests.csproj` existe e compila.
- [x] `dotnet test` executa e passa com contagem reportada.
- [x] Cobertura mínima: todos os itens de RF2 a RF5 têm teste nominal.
- [x] Nenhum teste faz I/O de rede.
- [x] Nenhum teste depende de ordem de execução.

## Validação

```bash
dotnet test backend.Tests/backend.Tests.csproj
```
