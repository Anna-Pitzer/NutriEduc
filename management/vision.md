# Visão — NutriEduc

Documento de visão do produto e da engenharia. Cada bloco `Vxx` declara um valor
entregável e as specs que o entregam. A visão é estável; as specs evoluem.

Fontes: `correcoes.md`, `implementacoes.md`, `README.md` e verificação do código.

---

### V1: Domínio de aluno com invariantes reais

**Problema:** `Aluno` é uma entidade anêmica. Dez campos obrigatórios aceitam vazio e
não existe um único método de domínio. O estado inválido é representável, portanto
inevitável.

**Valor entregue:** todo `Aluno` existente em memória ou no banco está em estado
válido, porque a validação acontece no construtor e em `AtualizarDados`, antes de
qualquer atribuição.

**Regra derivada:** validar tudo e só então atribuir — mesmo padrão já correto em
`Usuario.AtualizarPerfil`.

**Canonical creation path:** `backend/Domain/Entities/Aluno.cs`, `backend/Infrastructure/DataBase/ConnectionContext.cs`

**Delivered by:** Spec 003 (restricoes-aluno-protegidas), Spec 004 (dominio-aluno-invariantes)

---

### V2: CRUD completo na mesma entidade

**Problema:** alunos têm Criar, Ler e Excluir; cardápios também. O Update existe
apenas em perfil, que é outro recurso. O requisito de CRUD funcional não fecha.

**Valor entregue:** `C+R+U+D` completo de `Aluno`, exercitado pela interface, com
persistência confirmada por leitura posterior.

**Canonical creation path:** `backend/API/Controllers/AlunoController.cs`, `backend/API/Services/AlunoService.cs`, `frontend/src/services/AlunoApi.ts`, `frontend/src/pages/nutricao/alunos/VisualizarAlunos.tsx`, `frontend/src/routes/routes.tsx`

**Delivered by:** Spec 001 (crud-aluno-completo)

---

### V3: Credenciais não recuperáveis

**Problema:** senha persistida em texto claro e comparada com `==` em
`UsuarioService.VerificarLogin`.

**Valor entregue:** toda senha persistida é resultado de PBKDF2-SHA256 com salt único
por usuário; nenhum caminho compara senhas em claro, exceto a janela única de
migração de dados legados, que re-hasheia no primeiro login bem-sucedido.

**Canonical creation path:** `backend/Domain/Entities/HashSenha.cs`, `backend/Domain/Entities/Usuario.cs`, `backend/API/Services/UsuarioService.cs`

**Delivered by:** Spec 002 (seguranca-senha-hash)

---

### V4: Regressão detectável

**Problema:** nenhum teste automatizado. A avaliação atribuiu 0,0 à dimensão de
testes e toda mudança é uma aposta.

**Valor entregue:** as regras de domínio e os casos de uso críticos têm cobertura
automatizada; toda correção acompanha um teste que falha antes e passa depois.

**Canonical creation path:** `backend.Tests/backend.Tests.csproj`, `backend.Tests/Domain/`, `backend.Tests/Services/`, `backend.Tests/Fakes/`

**Delivered by:** Spec 005 (testes-automatizados-backend)

---

### V5: Dados de menores exigem sessão

**Problema:** todos os endpoints de `AlunoController` são públicos. Listagem,
consulta e exclusão de dados de crianças respondem a requisição anônima.

**Valor entregue:** nenhuma rota de dados de aluno responde sem sessão válida.

**Canonical creation path:** `backend/API/Controllers/AlunoController.cs`, `frontend/src/services/AlunoApi.ts`

**Delivered by:** Spec 006 (autorizacao-endpoints-aluno)

---

### V6: Escopo de produto modelado, não apenas desenhado

**Problema:** `implementacoes.md` descreve cinco perfis de usuário, cardápios,
alimentos, ocorrências e relatórios. O código modela um único tipo de usuário e não
possui domínio de cardápio, embora o frontend já chame seis rotas inexistentes.

**Valor entregue:** cardápios e alimentos existem como domínio com cálculo
nutricional próprio; a compatibilidade entre cardápio e restrições alimentares é uma
regra de domínio testada, não uma checagem visual. O ciclo operacional — ocorrências,
solicitações, feedback, notificações e relatórios exportáveis — existe como fluxo com
estado, histórico append-only e escopo por papel.

**Canonical creation path:** `backend/Domain/Entities/`, `backend/API/Controllers/`, `backend/API/Services/`

**Delivered by:** Spec 009 (papeis-e-estrutura-escolar), Spec 010 (restricoes-e-alergias), Spec 011 (catalogo-alimentos), Spec 012 (dominio-cardapio), Spec 013 (geracao-automatica-cardapios), Spec 014 (compatibilidade-cardapio-restricoes), Spec 015 (publicacao-historico-cardapio), Spec 016 (ocorrencias), Spec 017 (solicitacoes), Spec 018 (feedback-e-notificacoes), Spec 019 (relatorios-e-exportacao)

---

### V7: Documentação que não envelhece

**Problema:** `README.md` descreve endpoints corretos, mas o frontend chama rotas
inexistentes e uma rota com prefixo errado; documentação e contrato se separam
silenciosamente.

**Valor entregue:** a documentação é atualizada na mesma mudança que altera o
contrato, e a tabela de endpoints coincide com o mapa de controllers.

**Canonical creation path:** `README.md`, `frontend/src/routes/routes.tsx`, `frontend/src/services/`

**Delivered by:** Spec 008 (higiene-frontend)

### V8: Contrato de erro único e infraestrutura de DI

**Problema:** cada controller inventa o próprio texto e status de erro, e `UsuarioRepository` instancia `new ConnectionContext()` em vez de receber o `DbContext` pelo construtor, ignorando o ciclo de vida Scoped.

**Valor entregue:** um middleware global traduz `ArgumentException` em 400 e exceções não tratadas em 500, sempre no formato `{ "erro": "mensagem" }`; os repositórios recebem o `ConnectionContext` por injeção de dependência e os serviços são registrados com o mesmo ciclo de vida do contexto.

**Canonical creation path:** `backend/API/Middleware/`, `backend/Program.cs`, `backend/Infrastructure/Repository/UsuarioRepository.cs`

**Delivered by:** Spec 007 (contrato-erro-e-infra-di)

### V9: Contrato de rota uniforme e verificável

**Problema:** as onze rotas existentes seguem três padrões distintos. Duas devolvem corpo `string` no sucesso, o `id` é `{id:int}` em duas rotas e `{id}` em três, e o contrato `{ "erro": "mensagem" }` do Spec 007 vaza em validação de modelo (`ProblemDetails`), em `405` e em `415` (corpo vazio). `GET /api/usuario/perfil/{id}` ainda é anônimo e devolve CPF de qualquer usuário.

**Valor entregue:** um padrão único e observável — criação responde `201` com `Location` e objeto, toda rota com `id` usa `{id:int}`, e todo erro sob `/api/` responde `{ erro }` em português, inclusive modelo inválido, JSON malformado, verbo errado, content-type errado e rota inexistente. O perfil passa a exigir sessão e dono, sem expor `cpf` nem `senha`.

**Canonical creation path:** `backend/API/Middleware/`, `backend/API/Controllers/`, `backend/API/Services/`

**Delivered by:** Spec 020 (padronizacao-contrato-rotas)
