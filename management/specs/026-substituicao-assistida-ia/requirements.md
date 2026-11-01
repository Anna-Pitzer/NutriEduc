---
name: substituicao-assistida-ia
status: planned
references: V15, ADR-011, ADR-004
---

# Requirements — Substituição assistida por IA

## Objetivo

Quando um item do cardápio é bloqueado por restrição, o nutricionista troca manualmente:
precisa lembrar quais alimentos são equivalentes e conferir cada um contra as restrições.
A geração (Spec 013) e a compatibilidade (Spec 014) são determinísticas por decisão de
projeto (ADR-011) — introduzir IA sem fronteira colocaria não-determinismo no caminho de uma
regra de segurança alimentar. Esta spec entrega **apoio à decisão**, nunca decisão.

## Escopo

- Backend: porta de sugestão, provedor de IA plugável e opcional, fallback determinístico,
  revalidação, auditoria da sugestão.
- Frontend: ação "sugerir substituição" na montagem do cardápio, com justificativa visível
  e aprovação explícita.
- Fora de escopo: gerar o cardápio inteiro por IA (Spec 013 é determinística), alterar a
  severidade da compatibilidade, treinar modelo, enviar dados de aluno a terceiros.

## Requisitos funcionais

- RF1: a sugestão parte de **candidatos já válidos**: mesma categoria do alimento
  substituído, faixa de valores nutricionais próxima, sem restrição bloqueante (categoria
  `Alergia`) aplicável ao cardápio.
- RF2: a IA recebe apenas esse conjunto de candidatos e o contexto do alimento — nunca
  dados de aluno, de responsável ou de ocorrência.
- RF3: a IA devolve, para cada candidato, um ranking e uma justificativa curta; candidato
  fora do conjunto recebido é descartado antes de qualquer uso.
- RF4: a escolha sugerida é revalidada pela função determinística da Spec 014 antes de ser
  apresentada como aplicável; sugestão que viole bloqueio é descartada e registrada.
- RF5: aplicar a substituição exige aprovação humana explícita; nada é gravado no cardápio
  sem esse aceite.
- RF6: sem provedor configurado, sem rede, com timeout ou erro, o fluxo continua pelo
  `SugestorDeterministico` (equivalência por categoria e valores), sem bloquear a tela.
- RF7: toda sugestão aceita ou recusada é registrada de forma append-only com alimento de
  origem, candidato sugerido, origem da sugestão (`Ia` ou `Deterministica`), autor e data.
- RF8: existe teto de sugestões por sessão/dia e por requisição; atingido o teto, o sistema
  responde com o fallback determinístico e o motivo.
- RF9: a chave do provedor vive em user-secrets ou variável de ambiente; nunca em código,
  log, resposta HTTP ou artefato versionado.
- RF10: alimento de origem inexistente, inativo ou item de outro cardápio → 404/403
  conforme o escopo do módulo de cardápio.

## Requisitos não funcionais

- RNF1: nenhuma escrita de cardápio depende de resposta de provedor externo.
- RNF2: chamada ao provedor tem timeout curto e cancelamento; a requisição do usuário nunca
  fica pendurada.
- RNF3: resultado da IA é dado, não estado: pode ser descartado sem efeito colateral.
- RNF4: testes usam provedor falso; nenhum teste depende de rede ou de credencial real.
- RNF5: custo estimado por chamada é registrado para permitir teto e auditoria.
- RNF6: prompt e resposta não contêm segredo nem dado pessoal; o log guarda apenas
  contagem, duração e resultado.

## Restrições

- Migration nova para a trilha de sugestões; migrations existentes não são reescritas.
- Nenhuma dependência nova de frontend.
- A compatibilidade continua sendo a única fonte de severidade (ADR-011).

## Dependências

- Spec 012 (cardápio e itens) e Spec 014 (compatibilidade) — obrigatórias.
- Spec 011 (catálogo e valores nutricionais) para os candidatos.
- Spec 013 (geração) é consumidora opcional, não pré-requisito.

## Casos de borda

- Catálogo sem candidato equivalente → lista vazia com motivo, 200.
- Todos os candidatos bloqueados → lista vazia; nenhuma sugestão inválida é oferecida.
- Provedor devolve candidato inventado → descartado, sem afetar a resposta.
- Provedor devolve ranking vazio → fallback determinístico assume.
- Item já substituído por outra requisição → 409 na aplicação.
- Responsável ou merendeira tentando sugerir → 403.

## Critérios de aceitação

- [ ] `POST /api/cardapios/{id}/substituicoes/sugerir` por `Nutricionista` devolve
      candidatos com justificativa e origem da sugestão.
- [ ] Sem provedor configurado, a mesma rota responde pelo fallback determinístico.
- [ ] Nenhuma sugestão oferecida tem restrição vinculada de categoria `Alergia` entre as
      aplicáveis.
- [ ] Aplicar substituição sem aprovação não grava no cardápio.
- [ ] Sugestão aceita e recusada aparecem no histórico com autor e data.
- [ ] Payload enviado ao provedor não contém nome, CPF, e-mail, telefone nem id de aluno.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Com o catálogo da Spec 011 e um cardápio da Spec 012 com item bloqueado, gerar sugestão com
e sem provedor configurado, conferir os candidatos, aprovar uma substituição e verificar a
trilha de auditoria. Executar os gates do projeto.
