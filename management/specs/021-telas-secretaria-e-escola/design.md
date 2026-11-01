---
name: telas-secretaria-e-escola
status: created
references: V10, V6, ADR-002
---

# Design — Telas de secretaria

## Arquivos

| Arquivo | Responsabilidade |
| --- | --- |
| `services/EscolaApi.ts` | Escolas e unidades: listar, criar (só escola), renomear, ativar/desativar, excluir |
| `services/UsuarioAdminApi.ts` | Usuários: listar com filtro, criar, trocar papel, ativar/desativar, excluir |
| `components/TabelaDeNomes.tsx` | Tabela de nomes (nome + ativa) usada por escolas |
| `pages/secretaria/layoutSecretaria.tsx` | Header, Navbar, abas, conteúdo e Footer |
| `pages/secretaria/HomeSecretaria.tsx` | Painel com atalhos e totais |
| `pages/secretaria/Escolas.tsx` | `TabelaDeNomes` + `EscolaApi` |
| `pages/secretaria/Usuarios.tsx` | Filtros (papel, escola, situação, busca), criação, troca de papel, ativar/desativar e excluir na linha |
| `pages/secretaria/Unidades.tsx` | `TabelaDeNomes` + `EscolaApi`, sem criação (unidade nasce no bootstrap) |
| `routes/routes.tsx` | Três rotas sob a guarda `AdminSecretaria` |

`pages/secretaria/Unidades.tsx` foi removido na execução original (D6) e **voltou na
Spec 022**: lista a rede, renomeia, ativa/desativa e exclui; não cria — a unidade nasce
no bootstrap do primeiro administrador.

## Rotas

| Rota | Componente |
| --- | --- |
| `/secretaria` | `HomeSecretaria` |
| `/secretaria/escolas` | `Escolas` |
| `/secretaria/unidades` | `Unidades` |
| `/secretaria/usuarios` | `Usuarios` |

As três ficam dentro do mesmo `<Route element={<ProtegidaPorPapel papeis={["AdminSecretaria"]} />}>`.

## Serviços

Mesmo contrato de `AlunoApi.ts`: `credentials: "include"`, erro tipado
`ErroRequisicao` via `extrairErro`, e uma função por endpoint. Nenhum estado global é
introduzido: cada página carrega o que precisa no `useEffect` e atualiza a lista local
após escrever, evitando refetch completo.

```ts
// EscolaApi.ts
export interface EscolaResposta { id: number; nome: string; ativa: boolean }

listarEscolas(): Promise<EscolaResposta[]>
criarEscola(nome: string): Promise<EscolaResposta>
renomearEscola(id: number, nome: string): Promise<EscolaResposta>
definirEscolaAtiva(id: number, ativa: boolean): Promise<EscolaResposta>

// UsuarioAdminApi.ts
listarUsuarios(filtro): Promise<UsuarioResposta[]>
criarUsuario(dados: NovoUsuario): Promise<{ id: number }>
atualizarUsuario(id: number, dados: AtualizacaoUsuario): Promise<UsuarioResposta>
definirUsuarioAtivo(id: number, ativa: boolean): Promise<UsuarioResposta>
```

O endpoint de ativação é `POST /api/escolas/{id}/desativar` ou `/{id}/reativar` (e o
mesmo para usuário); o serviço escolhe o verbo pelo booleano, para o componente não
conhecer a URL.

## Troca de papel

`PUT /api/usuarios/{id}` aceita `nome`, `email`, `telefone`, `foto`, `papel`,
`escolaId` e `unidadeId`. O formulário envia:

```ts
{
  nome: usuario.nome,
  email: usuario.email,
  telefone: usuario.telefone,   // obrigatório: `null` limpa o telefone no backend
  papel: edicao.papel,
  escolaId: exigeEscola(edicao.papel) ? edicao.escolaId : null,
}
```

Três consequências que a implementação precisa respeitar:

1. **Telefone vai junto.** `AtualizarUsuario` chama `AtualizarTelefone(dto.Telefone)` e
   a propriedade é sobrescrita; omitir o campo apagaria o telefone do usuário.
2. **Escola some quando o papel não a exige.** `DefinirPapel` recusa escola em
   `Nutricionista`/`AdminSecretaria`, então o serviço envia `null` nesses casos.
3. **Conta sem nome ou e-mail não é editável.** O backend valida as duas propriedades;
   a linha fica com o botão desabilitado em vez de produzir 400.

A edição é inline: `edicao = { id, papel, escolaId } | null`, no máximo uma linha por
vez. Papel sugerido para conta sem papel: `Merendeira` (somente leitura), o de menor
privilégio que ainda exige uma escolha explícita de escola.

## Formulário de criação

```ts
const exigeEscola = (papel) => ["AdminEscola", "Merendeira", "Responsavel"].includes(papel);
```

Espelha `Usuario.DefinirPapel` (Spec 009). Se o formulário divergir, o backend responde
400/403 e a mensagem aparece no topo do formulário — o frontend não reimplementa a
regra, apenas evita oferecer um campo inválido. Unidade não é oferecida na UI; ao criar
sem unidade, o usuário fica visível apenas para o escopo da escola.

## Erros

| Status | Tratamento |
| --- | --- |
| 401 | `logout()`; `ProtectedRoutes` redireciona |
| 403 | mensagem do backend no formulário/lista |
| 409 | mensagem do backend (nome ou e-mail duplicado) |
| 400 | mensagem de validação do domínio |
| rede | mensagem padrão do serviço |

## Decisões

1. **Uma tabela genérica** `TabelaDeNomes` em vez de duplicar a lista de escolas.
2. **Renomear inline**, sem modal: um estado `editandoId` + `nomeEditando` na tabela.
3. **Troca de papel inline na tabela**, ao lado de ativar/desativar: o caso de uso é
   olhar a lista de contas sem papel e liberar uma a uma.
4. **Sem edição de nome/e-mail/CPF/senha** de outra conta — não há caso de uso definido.
5. **Sem paginação**: o volume da rede escolar cabe em uma lista; paginação exige
   contrato de backend que não existe.
6. **Tela de unidades sem criação**: removida na execução original e reintroduzida na
   Spec 022 apenas com listar/renomear/ativar/desativar/excluir.

## Riscos

| Risco | Mitigação |
| --- | --- |
| Lista grande de usuários | Filtro por papel/escola/situação/busca usa o backend (`GET /api/usuarios?busca=&ativo=`) |
| Escola desativada ainda aceita novo usuário | Backend não bloqueia; registrar como pendência, não silenciar na UI |
| Duplicidade de nome com acento/caixa | Unicidade é do backend (`NOCASE`); a UI só mostra o 409 |
| Perda de telefone na troca de papel | O corpo sempre inclui `telefone` atual (D5); a verificação é manual, registrada em `task.md` |
