---
name: seguranca-senha-hash
status: created
references: V3, ADR-003, L3
---

# Requirements — Senha com hash

## Objetivo

Eliminar a classe de defeito "credencial recuperável em claro". Hoje `Usuario.Senha`
é persistida crua, `UsuarioService.VerificarLogin` compara com `==` e `SenhaValida()`
acessa `Senha.Length` sem guard clause.

## Escopo

- Domínio: tipo de valor de hash, `DefinirSenha`, `SenhaConfere`, remoção de
  `SenhaValida()`.
- Aplicação: `CriarUsuario` e `VerificarLogin` passam a usar hash.
- Persistência: migration para ampliar a coluna `Senha`; re-hash de dados legados no
  login.
- Fora de escopo: endpoint de troca de senha (Spec 006).

## Requisitos funcionais

- RF1: a senha persistida nunca é igual ao valor informado pelo usuário.
- RF2: o hash usa PBKDF2-SHA256, salt aleatório por usuário e no mínimo 100.000
  iterações, no formato `pbkdf2-sha256$<iteracoes>$<salt>$<hash>` com partes em base64.
- RF3: `Usuario.SenhaConfere(string)` devolve `false` para `null`, vazio ou hash
  inválido, sem lançar exceção.
- RF4: `Usuario.DefinirSenha(string)` valida guard clause, comprimento mínimo de 8 e
  só então deriva e atribui.
- RF5: login com registro legado (sem o prefixo do formato) compara em claro uma única
  vez, e em caso de sucesso persiste o hash e passa a autenticar por hash.
- RF6: a comparação do hash usa comparação em tempo constante.

## Requisitos não funcionais

- RNF1: sem dependência nova; usar `System.Security.Cryptography`.
- RNF2: nenhum método público expõe salt ou hash bruto fora do domínio.
- RNF3: formato do hash é versionado pelo prefixo, permitindo migração futura.

## Restrições

- Não logar senha, hash ou salt em nenhuma camada.
- Não alterar o contrato de `POST /api/usuario/login`.

## Dependências

- Nenhuma. Esta spec é independente e pode ser implementada em paralelo à 001.

## Casos de borda

- Senha `null` ou vazia → `ArgumentException` em `DefinirSenha`; `false` em
  `SenhaConfere`.
- Hash truncado ou com partes faltando → `SenhaConfere` devolve `false`.
- Usuário legado com senha válida → login funciona e a senha é migrada.
- Usuário legado com senha inválida → login falha, sem migração.

## Critérios de aceitação

- [ ] `Senha` persistida em cadastro novo difere do valor informado.
- [ ] `SenhaConfere` retorna `false` para `null`, vazio e hash malformado, sem exceção.
- [ ] Dois usuários com a mesma senha possuem hashes diferentes (salt distinto).
- [ ] Login com registro legado re-hasheia e o registro passa a ter o prefixo do formato.
- [ ] `SenhaValida()` não existe mais no código.
- [ ] Nenhum ponto do código compara `dto.Senha == usuario.Senha`.
- [ ] `dotnet test` verde cobrindo hash, verificação, salt e migração.

## Validação

Testes unitários de domínio e de `UsuarioService` com repositório fake; gate
`dotnet build backend/backend.csproj` e `dotnet test`.
