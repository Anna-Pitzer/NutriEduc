---
name: higiene-frontend
status: created
references: V7, L6, L8
---

# Requirements — Higiene do frontend e documentação viva

## Objetivo

O frontend chama rotas que não existem, expõe uma paleta de desenvolvimento na
produção e a tabela de endpoints do `README.md` não tem gate: ela envelhece sem que
nada falhe. Esta spec remove o que não pertence à produção e cria a verificação que
mantém documentação e controllers alinhados.

## Escopo

- Frontend: remover rota/página de cores; remover `buscarEstatisticas` e o bloco que
  o consome; tratar a indisponibilidade temporária das rotas de cardápio (Spec 012)
  sem erro no console; remover funções de `CardapioApi.ts` sem consumidor.
- Documentação: atualizar a tabela de endpoints e criar um teste que a compara com
  os controllers.
- Fora de escopo: implementar cardápio (Spec 012), estatísticas de usuário (não há
  spec que as preveja), autorização (Spec 006).

## Requisitos funcionais

- RF1: a rota `/colors` e o componente `pages/dev/colorsTESTE.tsx` são removidos;
  nenhuma rota de desenvolvimento permanece em produção.
- RF2: `buscarEstatisticas` é removida de `Perfil.ts` e o bloco que a consome sai de
  `Perfil.tsx`; a tela de perfil não chama endpoint inexistente.
- RF3: `CardapioApi.ts` mantém apenas funções com consumidor:
  `buscarAlimentos`, `buscarCardapio`, `salvarCardapio` e `buscarHistoricoCardapios`.
  `excluirCardapio`, `salvarCardapioCompleto`, `buscarCardapioHistorico` e
  `excluirCardapioHistorico` são removidas.
- RF4: enquanto a Spec 012 não existir, as chamadas de cardápio tratam 404/5xx como
  indisponibilidade: a tela mostra estado vazio ("Cardápio disponível em breve") e
  não registra erro não tratado no console.
- RF5: as chamadas internas de cardápio usam `credentials: "include"`; a rota
  pública de cardápios publicados (Spec 012) fica sem credencial por decisão dela.
- RF6: a tabela de endpoints do `README.md` lista todos os pares método+rota dos
  controllers e nenhum par inexistente, com coluna de sessão exigida.
- RF7: um teste automatizado lê o `README.md` e compara com os atributos
  `[Http*]`/`[Route]` dos controllers; qualquer divergência falha a suíte.
- RF8: nenhuma tela existente quebra: login, perfil, alunos e home continuam
  renderizando com os mesmos fluxos.

## Requisitos não funcionais

- RNF1: nenhuma dependência nova no frontend.
- RNF2: o teste de documentação não usa rede; lê arquivos do repositório.
- RNF3: nenhuma regra de negócio é movida para o frontend.
- RNF4: remoções não deixam imports, tipos ou arquivos órfãos (`npm run lint` e
  `tsc -b` verdes).

## Restrições

- Não alterar contratos de API existentes.
- Não implementar as rotas de cardápio; apenas tratar a ausência.
- Não remover telas de cardápio; elas são o ponto de entrada da Spec 012.

## Dependências

- Spec 006 (para a coluna de sessão dos endpoints de aluno) e Spec 007 (formato de
  erro) tornam a tabela do README verificável sem ambiguidade.
- Spec 012 substitui o tratamento de indisponibilidade pelas rotas reais.

## Casos de borda

- `/colors` acessada por URL antiga → 404 do roteador, sem página de dev.
- Cardápio indisponível → lista vazia e mensagem, sem `throw` no console.
- `README.md` sem uma rota nova → teste falha nomeando o par ausente.
- Rota documentada que não existe → teste falha nomeando o par excedente.
- `Perfil.tsx` sem o bloco de estatísticas → nenhuma chamada de rede extra.
- Função removida ainda importada → `tsc -b` falha na compilação.

## Critérios de aceitação

- [ ] `/colors` não existe em `routes.tsx` nem há import de `colorsTESTE`.
- [ ] `buscarEstatisticas` não existe em `Perfil.ts` nem é importada.
- [ ] `CardapioApi.ts` não contém as quatro funções sem consumidor.
- [ ] Tela de cardápio mostra estado vazio quando a API devolve 404.
- [ ] `README.md` lista todos os endpoints reais e nenhum inexistente.
- [ ] Teste de documentação passa e falha se um endpoint for removido do README.
- [ ] `npm run build`, `npm run lint` e `dotnet test` sem erro.

## Validação

Abrir as telas de login, perfil, alunos e cardápio; confirmar ausência de 404/erros no
console; alterar uma linha do README e confirmar que o teste de documentação falha.
Gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj`, `cd frontend && npm run build && npm run lint`.
