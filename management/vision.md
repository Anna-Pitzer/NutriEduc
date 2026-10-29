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
regra de domínio testada, não uma checagem visual.

**Canonical creation path:** `backend/Domain/Entities/`, `backend/API/Controllers/`, `backend/API/Services/`

**Delivered by:** Spec 009 (papeis-e-escopo-dados), Spec 010 (dominio-alimentos-cardapios), Spec 011 (compatibilidade-cardapio-restricoes), Spec 012 (ocorrencias-solicitacoes-relatorios)

---

### V7: Documentação que não envelhece

**Problema:** `README.md` descreve endpoints corretos, mas o frontend chama rotas
inexistentes e uma rota com prefixo errado; documentação e contrato se separam
silenciosamente.

**Valor entregue:** a documentação é atualizada na mesma mudança que altera o
contrato, e a tabela de endpoints coincide com o mapa de controllers.

**Canonical creation path:** `README.md`, `frontend/src/routes/routes.tsx`, `frontend/src/services/`

**Delivered by:** Spec 008 (higiene-frontend)
