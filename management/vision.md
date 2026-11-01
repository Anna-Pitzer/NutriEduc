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

### V10: Telas de secretaria e de escola
**Problema:** as áreas de secretaria e de escola existem apenas como páginas vazias. A Spec 009 entrega o domínio, a autorização e as rotas de gestão, mas criar escola, unidade, merendeira e vínculo de responsável continua exigindo HTTP manual.

**Valor entregue:** as telas de secretaria (escolas, unidades, usuários da rede) e de escola (unidade, merendeiras, responsáveis, vínculo responsável↔aluno) consomem as rotas já autorizadas, exibindo o erro do backend quando o conflito é de escopo ou de unicidade. O menu e a guarda de rota por papel já existem; esta spec entrega o conteúdo.

**Canonical creation path:** `frontend/src/pages/secretaria/`, `frontend/src/pages/escolas/`, `frontend/src/services/`

**Delivered by:** Spec 021 (telas-secretaria-e-escola)

### V11: Bootstrap de administrador por token
**Problema:** criar o primeiro (ou um novo) `AdminSecretaria` exigia editar o banco ou reiniciar o processo com variáveis de ambiente. Sem isso, um deploy limpo não tinha como abrir a primeira sessão administrativa. E um token permanente não distinguia unidade nem limitava o período de criação.

**Valor entregue:** duas vias de cadastro, ambas com token no corpo.

1. **Token de ambiente** (`Bootstrap__Token`, mínimo 32 caracteres, comparação em tempo constante): a página `/bootstrap` cria um `AdminSecretaria` informando o **nome exato de uma unidade** existente. O mesmo token vale **uma vez por unidade** — a unidade fica registrada e não pode consumir de novo.
2. **Token temporário**: um `AdminSecretaria` vinculado a uma unidade gera, no painel da secretaria, um token de **30 minutos** e **uso único**, exibido uma única vez. Enquanto vale, o botão fica desabilitado com contagem regressiva; ao expirar, o cadastro é bloqueado e o botão volta a liberar um novo token.

O banco guarda apenas o hash SHA-256 do token temporário. A rota responde `404` quando não há nenhuma via disponível, e `Bootstrap__Permitir=false` funciona como trava dura de ambiente. A tela de unidades (`/secretaria/unidades`) existe porque o nome exigido precisa estar cadastrado.

**Decisão de risco registrada:** o token de ambiente não expira — vale para sempre, mas uma vez por unidade. Tratar esse token como credencial de administrador é requisito operacional, não detalhe: rotacione-o após configurar a rede e use o token de 30 minutos para os administradores seguintes.

**Canonical creation path:** `backend/API/Controllers/BootstrapController.cs`, `backend/API/Services/BootstrapService.cs`, `backend/Domain/Entities/ConsumoBootstrap.cs`, `backend/Domain/Entities/TokenDeCadastro.cs`, `frontend/src/pages/comum/BootstrapAdmin.tsx`, `frontend/src/pages/secretaria/HomeSecretaria.tsx`, `frontend/src/pages/secretaria/Unidades.tsx`

**Delivered by:** Spec 022 (bootstrap-admin-por-token)
