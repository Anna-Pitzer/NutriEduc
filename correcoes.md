# Avaliação — NutriEduc

**Nota final: 6,72**  ·  Bloco POO (70%): **6,70**  ·  Bloco Frontend (30%): **6,75**

Régua: `correcao/REGUA.md` · Base: livro *Design e Programação Orientados a Objetos*, caps. 1–9.

---

## Notas por dimensão

| Bloco POO | Nota | | Bloco Frontend | Nota |
|---|---|---|---|---|
| A — Modelagem do domínio (10%) | 7,0 | | I — CRUD funcional (50%) | 6,0 |
| B — Estado/comportamento/construtor (15%) | 8,0 | | J — Integração front↔back (25%) | 7,0 |
| C — Encapsulamento/invariantes (20%) | 7,0 | | K — Organização do frontend (25%) | 8,0 |
| D — Responsabilidades/coesão (20%) | 7,5 | | | |
| E — Colaboração entre objetos (15%) | 6,0 | | | |
| F — Abstração (10%) | 7,0 | | | |
| G — Polimorfismo (5%) | 6,0 | | | |
| H — Testes das regras (5%) | 0,0 | | | |

## O que o grupo fez bem
- **`Usuario` é uma entidade de verdade.** Quatro construtores e comportamento com validação no ponto certo: `AtualizarPerfil` (nome de 2 a 100 caracteres, e-mail validado com `MailAddress.TryCreate`), `AtualizarTelefone` (10 ou 11 dígitos), `AtualizarFoto` (tipo da imagem e limite de 5 MB), `AtualizarSenha`.
- **Validação antes de alterar.** `AtualizarPerfil` valida tudo e só então atribui — exatamente a sequência que o cap. 4 exige.
- **Estado fechado:** todas as propriedades com `private set`.
- **Camadas e injeção de dependência por contrato:** `AddScoped<IUsuarioRepository, UsuarioRepository>()` e `AddScoped<IAlunoRepository, AlunoRepository>()` — as duas interfaces têm consumidor abstrato real, o que é o uso legítimo do cap. 8.
- **Serviço que coordena sem invadir:** `UsuarioService` cria o objeto, pede a operação à entidade (`usuario.AtualizarPerfil(...)`) e persiste pelo repositório.
- **Frontend bem organizado:** `pages/`, `components/`, `services/`, `context/`, `routes/`, com serviços de API separados por recurso (`AlunoApi`, `CardapioApi`, `Perfil`).

## O que faltou
- **CRUD parcial — não fecha C+R+U+D na mesma entidade.** Aluno tem Criar, Ler e Excluir (`AlunoApi.ts`) e cardápio tem Criar, Ler e Excluir (`CardapioApi.ts`); o *Update* existe apenas em **perfil**, que é outro recurso. Este é o único grupo do conjunto que não atende o requisito combinado por completo (I = 6,0).
- **`Aluno` é anêmico e não valida nada.** Dez campos obrigatórios (nome, escola, série, responsável…) são aceitos vazios no construtor, e não há nenhum método de domínio.
- **Coleção exposta de forma mutável:** `List<string> Restricoes` com `private set` continua permitindo `aluno.Restricoes.Add(...)` de fora. O cap. 6 pede `IReadOnlyList` mais uma operação de domínio.
- **`SenhaValida()` acessa `Senha.Length` sem verificar nulo** — se `Senha` for nula, quebra em execução.
- **Senha em texto claro** comparada no serviço (`dto.Senha == usuario.Senha`). Não é critério de POO, mas é uma fragilidade relevante.
- **As entidades não se associam entre si** (não há `Aluno` ligado a responsável/turma como objeto), então a dimensão E fica limitada.
- **Nenhum teste** automatizado.

## O que faria a nota subir
1. **Fechar o CRUD de aluno.** Implementar `PUT /api/aluno/{id}` no backend e um `atualizarAluno` em `AlunoApi.ts` com uma tela de edição. É a correção de maior impacto imediato: sobe I de 6,0 para 9–10.
2. **Dar vida ao `Aluno`:** construtor que valide os campos obrigatórios e um método `AtualizarDados(...)` com `private set`. Sobe A e C.
3. **Trocar `List<string> Restricoes` por `IReadOnlyList<string>` + `AdicionarRestricao(string)`**, protegendo a coleção como pede o cap. 6. Sobe C e E.
4. **Guard clause em `SenhaValida()`** (`if (string.IsNullOrEmpty(Senha)) return false;`).
5. **Testes de validação do `Usuario`:** nome curto, e-mail inválido, telefone com 9 dígitos, senha com menos de 8 caracteres, foto acima de 5 MB. A regra já está isolada na entidade — é barato testar e sobe H de 0 para ~9.
