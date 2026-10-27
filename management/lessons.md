# Lições — NutriEduc

Lições de implementação derivadas do estado atual do repositório e das avaliações
registradas. Cada lição tem um identificador estável (`Lx`) para ser referenciado
por specs e por revisões.

---

## L1: Validação no serviço cria entidade anêmica

**Observado:** `AlunoService.CriarAluno` repassa o DTO direto para o construtor.
Nenhuma validação existe; `Aluno` não tem um único método de domínio.

**Lição:** se o serviço valida, a entidade é apenas um saco de dados e qualquer novo
caminho de escrita (importação, seed, endpoint novo) nasce inválido.

**Regra:** invariante mora na entidade, no construtor e no método de atualização.

---

## L2: `private set` não protege coleção

**Observado:** `List<string> Restricoes { get; private set; }` é mutável de fora
(`aluno.Restricoes.Add(...)`), então a invariante pode ser quebrada sem tocar
nenhum método da entidade.

**Lição:** encapsulamento de coleção exige tipo readonly exposto, não setter privado.

**Regra:** `IReadOnlyList<T>` + método de domínio que aplica as invariantes.

---

## L3: Validação de senha sem guard clause quebra em produção

**Observado:** `SenhaValida()` acessa `Senha.Length` antes de checar nulo;
`AtualizarSenha` valida comprimento antes de checar vazio. Usuário criado por um
construtor sem senha derruba a chamada.

**Lição:** validação de campo anulável começa pelo guard clause, sempre. Ordem de
checagem é parte da regra.

**Regra:** `string.IsNullOrEmpty` primeiro; depois comprimento e formato.

---

## L4: Repositório que instancia o `DbContext` ignora o ciclo de vida

**Observado:** `UsuarioRepository` faz `new ConnectionContext()` no campo, enquanto
`AlunoRepository` recebe o contexto por construtor e é registrado como `Scoped`.

**Lição:** duas instâncias de contexto sobre o mesmo arquivo SQLite geram escrita
concorrente e estado divergente entre requisições, além de nunca serem descartadas.

**Regra:** repositório recebe a dependência; ninguém constrói infraestrutura por
conta própria.

---

## L5: Frontend não é fonte de verdade de integridade

**Observado:** o frontend valida nome, e-mail e telefone; o backend aceita qualquer
coisa em `Aluno`. O endpoint também é público.

**Lição:** validação no cliente melhora a experiência e não impede dado inválido.

**Regra:** toda regra que o sistema precisa garantir pertence ao backend.

---

## L6: Contrato de API documentado envelhece sem gate

**Observado:** `README.md` lista endpoints corretos, mas o frontend chama seis rotas
que não existem e uma rota com prefixo errado (`/usuarios/{id}/estatisticas`).

**Lição:** documentação sem verificação cruzada com o código diverge silenciosamente;
o sintoma aparece como 404 no console, não como erro de build.

**Regra:** alteração de contrato atualiza a tabela de endpoints na mesma mudança;
toda chamada do frontend precisa de endpoint correspondente ou de remoção.

---

## L7: Comportamento só existe se for testado

**Observado:** `Usuario` tem comportamento correto e validador bem escrito, mas
nenhum teste. A avaliação atribuiu 0,0 à dimensão de testes.

**Lição:** regra correta e não testada é hipótese. O custo de testar é baixo quando a
regra já está isolada na entidade.

**Regra:** toda regra de domínio nasce com teste unitário no mesmo commit.

---

## L8: CRUD incompleto é dívida que invisível na interface

**Observado:** as telas existem e funcionam; a ausência do `Update` de aluno só é
perceptível quando alguém tenta editar um cadastro.

**Lição:** CRUD deve ser validado como um conjunto, por entidade, e não por tela.

**Regra:** critério de aceitação de CRUD nomeia explicitamente `C`, `R`, `U` e `D`
na mesma entidade.

---

## L9: Regra reimplementada ao lado do utilitário que já a resolve

**Observado:** `Validacao.tsx` e `Sanitizacao.tsx` **são** importados por
`frontend/src/pages/login.tsx` (linhas 11 e 17). O problema é outro:
`frontend/src/pages/Perfil.tsx` declara as próprias `sanitizarNome` (linha 58),
`sanitizarEmail` (linha 66) e `sanitizarTelefone` (linha 70) sem reutilizar
`Sanitizacao.tsx`, e as demais telas com formulário repetem o padrão.

**Lição:** o risco não é utilitário sem consumidor — é utilitário com consumidor
insuficiente. Regra escrita duas vezes diverge na primeira correção que tocar apenas
uma das cópias.

**Regra:** antes de declarar um utilitário morto, verificar os imports. A versão
anterior desta lição afirmava que os dois componentes não eram importados por
ninguém; era falso e teria levado à remoção de código em uso.
