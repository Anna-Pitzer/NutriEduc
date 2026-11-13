---
name: portal-pais-responsaveis
status: created
references: Spec 035
---

# Tasks — Portal dos Pais/Responsáveis

> **Estado:** implementação concluída em 2026-11-12 (T1–T13 e T15); a validação manual
> no navegador (T14) permanece pendente e registrada em `pending-items.md`.

## Tasks

### Backend

- [x] **T1 — Modelo e migration do feedback.**
  - Arquivos: `backend/Domain/Entities/Feedback.cs`,
    `backend/Infrastructure/DataBase/ConnectionContext.cs`,
    `backend/Migrations/<timestamp>_FeedbackAluno.cs`.
  - Ação: adicionar `AlunoId` (`int?`), `AlunoNome` (`string?`), manter o construtor atual e
    adicionar um overload com `alunoId`/`alunoNome`, FK `OnDelete(SetNull)` e índice
    `IX_Feedbacks_AlunoId`; gerar migration com `dotnet ef migrations add FeedbackAluno`.
  - Critério: banco limpo aplica a migration; feedback antigo materializa com `AlunoId=null`;
    exclusão de aluno mantém o feedback e o snapshot.

- [x] **T2 — Serviço/DTOs/listagem do feedback.**
  - Arquivos: `backend/Application/FeedbackDTOs.cs`, `backend/Domain/EscopoUsuario.cs`,
    `backend/API/Services/FeedbackService.cs`, `backend/API/Controllers/FeedbackController.cs`,
    `backend/Infrastructure/Repositories/FeedbackRepository.cs`.
  - Ação: persistir `AlunoId`/`AlunoNome` no `Criar`; expor `AlunoId`/`Aluno` nos DTOs; aceitar
    `alunoId` opcional na listagem de `GET /api/feedbacks` e aplicar no `FiltroFeedback`.
  - Critério: com aluno vinculado retorna `201` e a associação; sem aluno retorna `201` com
    nulos; aluno de outro responsável retorna `403`; filtro `alunoId` funciona.

- [x] **T3 — Testes do feedback.**
  - Arquivos: `backend.Tests/Integration/FeedbackApiTests.cs`,
    `backend.Tests/Domain/FeedbackTests.cs`.
  - Ação: cobrir invariantes do domínio (aluno/nome), criação com/sem aluno, isolamento por
    vínculo, filtro por aluno e listagem.
  - Critério: novos testes passam; suíte existente permanece com 0 falhas.

### Frontend — estrutura

- [x] **T4 — Layout, rotas e navegação.**
  - Arquivos: `frontend/src/pages/pais/LayoutPais.tsx`,
    `frontend/src/pages/pais/HomePais.tsx`, `frontend/src/routes/routes.tsx`,
    `frontend/src/components/Navbar.tsx`.
  - Ação: criar layout com abas Início/Cardápios/Ocorrências/Solicitações/Feedback; incluir
    `Header` e `SinoNotificacoes`; substituir `HomePais` por `LayoutPais`/`PainelPais` e remover
    import órfão; registrar as rotas sob `ProtegidaPorPapel papeis={["Responsavel"]}`; manter
    `homePorPapel["Responsavel"]`.
  - Critério: `/pais` e sub-rotas abrem apenas para `Responsavel`; outras sessões são redirecionadas
    ou recebem a guarda existente.

- [x] **T5 — Seletor de filho e painel.**
  - Arquivos: `frontend/src/components/SeletorDeFilho.tsx`,
    `frontend/src/pages/pais/PainelPais.tsx`.
  - Ação: carregar `GET /api/aluno`; renderizar cartões com nome/escola/série; seletor único com
    pré-seleção quando houver um filho; estado vazio sem filhos.
  - Critério: os cartões correspondem exatamente à resposta de `GET /api/aluno`; ações ficam
    desabilitadas sem filho; com dois filhos, a troca altera o `alunoId` selecionado.

### Frontend — funcionalidades

- [x] **T6 — Cardápios.**
  - Arquivos: `frontend/src/pages/pais/CardapiosPais.tsx`,
    `frontend/src/components/CardapioDoAluno.tsx`,
    `frontend/src/services/CardapioApi.ts`.
  - Ação: adicionar `buscarCardapioDoAluno(alunoId, data)`; tela filho + data; renderizar
    `CardapioServidoDTO`; filtro local por refeição; estado `Indisponivel`.
  - Critério: RF2–RF6 verificados; `disponivel=false` não renderiza refeições nem o geral;
    filtro por refeição não dispara nova requisição.

- [x] **T7 — Ocorrências: lista e detalhe.**
  - Arquivos: `frontend/src/pages/pais/OcorrenciasPais.tsx`,
    `frontend/src/components/ListaOcorrencias.tsx`,
    `frontend/src/services/OcorrenciaApi.ts`.
  - Ação: adicionar `de`/`ate` a `FiltrosOcorrencia`; consumir `GET /api/ocorrencias` com
    `alunoId`, `tipo`, `status`, `de`, `ate`, `pagina`, `tamanho=20`; detalhe por
    `GET /api/ocorrencias/{id}`; exibir histórico, `autor` e `podeCancelar`; cancelar somente com
    `podeCancelar=true` (Aberta + mesmo dia).
  - Critério: RF7–RF10 verificados; lista não mostra aluno fora do vínculo; `403` no detalhe
    exibe mensagem genérica.

- [x] **T8 — Ocorrências: relato.**
  - Arquivos: `frontend/src/pages/pais/NovaOcorrenciaPais.tsx`,
    `frontend/src/components/FormularioOcorrencia.tsx`.
  - Ação: formulário com filho e descrição (5–1000); enviar `{ tipo: "Aluno", alunoId,
    descricao }`; confirmação e recarga da lista.
  - Critério: RF11–RF14 verificados; `400` preserva o texto; `403` não limpa o formulário.

- [x] **T9 — Solicitação de alergia.**
  - Arquivos: `frontend/src/pages/pais/SolicitacoesPais.tsx`,
    `frontend/src/components/FormularioSolicitacao.tsx`,
    `frontend/src/components/ListaSolicitacoes.tsx`.
  - Ação: carregar catálogo ativo; enviar `InclusaoAlergia` com aluno, restrição e descrição;
    listar com status/resposta; cancelar somente quando `status=Aberta`.
  - Critério: RF15–RF20 verificados; cadastro do aluno não muda antes da aprovação; `409` de
    duplicidade exibe o id existente.

- [x] **T10 — Feedback.**
  - Arquivos: `frontend/src/pages/pais/FeedbackPais.tsx`,
    `frontend/src/components/FormularioFeedback.tsx`,
    `frontend/src/components/ListaFeedback.tsx`,
    `frontend/src/services/FeedbackApi.ts`.
  - Ação: enviar assunto/mensagem e aluno opcional; listar somente os próprios com filtro
    `{ status?, alunoId?, pagina, tamanho }`; exibir resposta e `podeEncerrar`; encerrar o
    respondido.
  - Critério: RF21–RF25 verificados; feedback sem aluno exibe “Sem aluno”; responder não aparece
    na área de pais.

### Qualidade e fechamento

- [x] **T11 — Extração de componentes compartilhados.**
  - Arquivos: os componentes de T7–T10 e as páginas compartilhadas existentes
    (`OcorrenciasPage.tsx`, `SolicitacoesPage.tsx`, `FeedbackPage.tsx`).
  - Ação: mover listas/formulários reutilizáveis para `components/` e consumir nos dois lugares,
    sem duplicar regra.
  - Critério: nenhuma regra de negócio duplicada; páginas existentes continuam com o mesmo
    comportamento.

- [x] **T12 — Estados, erros e acessibilidade.**
  - Arquivos: todos os novos componentes/páginas.
  - Ação: implementar a tabela `Edge Cases & Error States`; labels/aria; foco visível; mensagens
    de erro por campo; datas `dd/MM/yyyy` na exibição.
  - Critério: cada linha da tabela tem comportamento implementado e verificável; nenhum input sem
    label; `401` faz logout.

- [x] **T13 — Gates automatizados.**
  - Comandos: `dotnet build backend/backend.csproj`,
    `dotnet test backend.Tests/backend.Tests.csproj`, `cd frontend && npm run build`,
    `cd frontend && npm run lint`.
  - Critério: 0 erros; suíte com 792+ testes e 0 falhas; lint 0 erros.

- [ ] **T14 — Validação manual.**
  - Roteiro: dois responsáveis, um com dois filhos; percorrer cardápios, ocorrências (leitura e
    relato), alergia (envio, acompanhamento, decisão do ADM) e feedback (envio, resposta,
    encerramento).
  - Critério: todos os critérios de aceitação do `requirements.md` verificados; nenhum dado de
    aluno alheio aparece.

- [x] **T15 — Documentação e status.**
  - Arquivos: `README.md` (se houver mudança de página/endpoint), `management/arch.md`,
    `management/roadmap.md`.
  - Ação: registrar as páginas novas e a extensão do feedback; atualizar o status pelo motor SDD.
  - Critério: documentação coincide com o comportamento; `DocumentationTests` passa.

## Dependencies Between Tasks

```
T1 → T2 → T3
T4 → T5 → T6/T7/T8/T9/T10
T7/T8/T9/T10 → T11
T4..T11 → T12 → T13 → T14 → T15
```

## Completion Criteria

- [ ] Todos os RF e NFR do `requirements.md` verificados (T14 pendente).
- [x] Nenhuma tela exibe aluno fora do vínculo.
- [x] Nenhuma regra de negócio duplicada no frontend.
- [x] Nenhuma rota/dependência nova além do previsto.
- [x] Documentação e status sincronizados.
