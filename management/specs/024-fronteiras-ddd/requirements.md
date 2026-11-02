---
name: fronteiras-ddd
status: planned
references: V13, ADR-004, ADR-009
---

# Requirements — Fronteiras DDD explícitas

## Objetivo

A análise do backend encontrou sete pontos em que a fronteira entre camadas está borrada. O
domínio está protegido (sem setters públicos, sem EF, invariantes nos construtores), mas
regra de negócio escapa para serviços e controllers, autorização entra no domínio,
repositórios decidem transação, e dois serviços concentram casos de uso demais. Esta spec
corrige esses pontos sem mudar contrato HTTP nem banco.

## Escopo

- Backend apenas: `Application`, `Domain`, `API/Services`, `API/Controllers`,
  `Infrastructure/Repositories`.
- Fora de escopo: frontend, novas funcionalidades, mudança de schema (nenhuma migration de
  dados é necessária; a spec não altera tabelas).

## Diagnóstico — falhas encontradas

| # | Falha | Evidência |
|---|---|---|
| F1 | DTO único serve entrada e saída | `Application/DadosAlunoDTO.cs` usado em `AlunoService.ParaDTO` e no `POST /api/aluno/registrar`; `DadosUsuarioDTO` idem; `UsuarioController.PostAdd` responde objeto anônimo |
| F2 | Hash e geração de token fora do domínio | `BootstrapService.HashDoToken` (`BootstrapService.cs:393`) e `GerarValorDeToken` (`:383`); `TokenDeCadastro` aceita qualquer string como `hash` |
| F3 | Unicidade de e-mail sinalizada por `null`, com a mensagem no controller | `UsuarioService.CriarUsuario` devolve `int?`; `UsuarioController.cs:31-33` traduz `null` em `"E-mail já cadastrado."` |
| F4 | Referências externas não validadas pela entidade | `Aluno.DefinirUnidade` só exige `> 0`; `Usuario.DefinirPapel` só exige escola presente, sem verificar existência ou situação |
| F5 | Autorização dentro do domínio | `Domain/EscopoUsuario.cs`; `IAlunoRepository.Listar(EscopoUsuario)`, `IUsuarioRepository.Listar(FiltroUsuarios, EscopoUsuario)` |
| F6 | Sem unidade de trabalho | 19 `SaveChanges` nos repositórios; `BootstrapService.Criar` grava usuário, consumo e token em chamadas separadas |
| F7 | Política de exclusão e escopo no mesmo lugar | `ExclusaoService.ExcluirUsuario(int, EscopoUsuario)` mistura “o que impede excluir” com “quem pode excluir” |
| F8 | Material de autenticação exposto | `Usuario.Senha` com getter público (`Usuario.cs:16`) |
| F9 | Serviços com casos de uso demais | `UsuarioService.cs` 370+ linhas (criar, autenticar, perfil, listar, ativar); `BootstrapService.cs` 400+ linhas |

## Requisitos funcionais

- RF1: cada operação de escrita tem contrato próprio de entrada e de saída em `Application`
  (`CriarAlunoEntrada`, `AtualizarAlunoEntrada`, `AlunoResposta`; `CriarUsuarioEntrada`,
  `UsuarioResposta`). Nenhum DTO de entrada é devolvido por rota.
- RF2: `HashDeToken` é objeto de valor de domínio, com `Criar(valor)` e `Confere(valor)`;
  `TokenDeCadastro` recebe `HashDeToken` e nunca uma string solta. A geração do segredo
  permanece na aplicação (depende de RNG), mas o hash é responsabilidade do domínio.
- RF3: unicidade de e-mail é expressa por resultado explícito (`Resultado`), sem `null`
  ambíguo; a mensagem da regra pertence ao serviço, nunca ao controller.
- RF4: `Aluno.DefinirUnidade` e `Usuario.DefinirPapel` recebem referências já verificadas
  pela aplicação; a entidade valida o formato (positivo) e a aplicação valida existência e
  situação via porto de repositório, com falha explícita antes de mutar estado.
- RF5: portos de repositório do domínio deixam de receber `EscopoUsuario`; recebem filtros
  de negócio (`FiltroAluno`, `FiltroUsuarios` sem papel do solicitante). A tradução
  papel→filtro vive em `API/Services` (aplicação), onde a sessão é conhecida.
- RF6: existe `IUnidadeDeTrabalho` com `ExecutarEmTransacao(Func<T>)`; casos de uso que
  tocam mais de um agregado (bootstrap e vínculo) executam dentro dela. Nenhum repositório
  chama `SaveChanges` dentro de operação de escrita do caso de uso.
- RF7: política de exclusão vira serviço de domínio (`PoliticaDeExclusao`), sem escopo; a
  aplicação decide se o solicitante pode excluir antes de chamar a política.
- RF8: `Usuario` deixa de expor o hash; expõe apenas `SenhaConfere` e um estado
  `SenhaLegada`. Nenhum DTO carrega hash.
- RF9: `UsuarioService` e `BootstrapService` são divididos por intenção (ex.:
  `CadastroUsuarioService`, `AutenticacaoService`, `PerfilUsuarioService`,
  `BootstrapTokenService`), preservando rotas, status e mensagens.

## Requisitos não funcionais

- RNF1: nenhuma mudança de contrato HTTP: mesmas rotas, status, formato `{ erro }` e campos
  de resposta observáveis hoje (cobertos por `Integration/`).
- RNF2: nenhuma migration nova; o schema é preservado.
- RNF3: `Domain` continua sem `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore` ou DTO.
- RNF4: cada correção entra com teste que falha antes e passa depois.
- RNF5: tamanho de serviço é consequência, não meta: a divisão é por caso de uso, não por
  número de linhas.

## Restrições

- Correções cirúrgicas: a spec não reescreve o módulo de aluno nem o de restrições, que
  acabaram de estabilizar.
- Nenhum repositório abre conexão própria (L4) e nenhum controller ganha regra de negócio.

## Dependências

- Specs 007 (contrato de erro e DI), 009 (papéis e escopo) e 010/023 (histórico).

## Casos de borda

- E-mail duplicado na criação administrada → mesmo 409/mensagem de hoje.
- Unidade inexistente ou desativada → 400 antes de gravar aluno.
- Falha no meio de uma transação de bootstrap → nenhum usuário criado e nenhum token
  consumido.
- Token temporário com hash legado (sem o novo objeto de valor) → continua validando.
- Exclusão de usuário pelo próprio autor → 409 com a mesma mensagem de hoje.

## Critérios de aceitação

- [ ] `POST /api/usuario/registrar` e `/api/aluno/registrar` devolvem o mesmo JSON de hoje.
- [ ] Nenhum tipo de `Application` é devolvido e consumido como entrada na mesma rota.
- [ ] `BootstrapService` não contém mais cálculo de hash (movido para `HashDeToken`).
- [ ] `UsuarioController` não contém mensagem de regra de negócio.
- [ ] `IAlunoRepository` e `IUsuarioRepository` não recebem `EscopoUsuario`.
- [ ] Bootstrap com falha simulada no token não deixa usuário criado.
- [ ] `Usuario` não expõe propriedade de senha/hash.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Executar a suíte completa (`dotnet test backend.Tests/backend.Tests.csproj`), que cobre as
rotas afetadas em `Integration/`, e conferir manualmente os fluxos de bootstrap e de criação
de aluno. Frontend não muda; `npm run build` e `npm run lint` entram como gate de regressão.
