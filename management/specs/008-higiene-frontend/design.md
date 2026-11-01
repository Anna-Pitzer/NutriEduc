---
name: higiene-frontend
status: created
references: V7, L6, L8
---

# Design — Higiene do frontend e documentação viva

## Estrutura

```text
frontend/src/routes/routes.tsx                 (- /colors, - import dev)
frontend/src/pages/dev/colorsTESTE.tsx         (removido)
frontend/src/services/Perfil.ts                (- buscarEstatisticas)
frontend/src/pages/comum/Perfil.tsx            (- import, - bloco de estatísticas)
frontend/src/services/CardapioApi.ts           (- 4 funções, + tratamento 404)
README.md                                      (tabela de endpoints)
backend.Tests/Integration/DocumentationTests.cs (novo)
```

## Frontend

### Rota de desenvolvimento

Remover o import de `PaletaDeCor` e a `<Route path="/colors" />`. O arquivo
`pages/dev/colorsTESTE.tsx` é apagado; se a pasta `pages/dev/` ficar vazia, ela também
sai. A URL antiga passa a cair no roteador sem correspondência.

### Estatísticas

`Perfil.tsx` importa `buscarEstatisticas` e a chama em um `useEffect` cujo endpoint
não existe em nenhum controller. Não há spec que preveja estatísticas por usuário —
os indicadores da Spec 019 são administrativos. A remoção é:

- apagar `buscarEstatisticas` de `Perfil.ts`;
- remover o import, o estado e o bloco de renderização correspondente em
  `Perfil.tsx`.

### Cardápio indisponível

Até a Spec 012, `buscarAlimentos`, `buscarCardapio` e `buscarHistoricoCardapios`
recebem 404. O tratamento fica em um helper:

```ts
async function buscarOuVazio<T>(url: string): Promise<T[]> {
    const resposta = await fetch(url, { credentials: "include" });

    if (resposta.status === 404) return [];

    if (!resposta.ok) throw new Error(await extrairErro(resposta, "Serviço indisponível"));

    return resposta.json();
}
```

As telas exibem estado vazio: `HomeNutricao` mostra "Nenhum cardápio criado ainda" e
`Cardapio` deixa os quatro cartões vazios. O texto literal "Cardápio disponível em
breve" não foi usado porque as telas ainda não distinguem "carregado e vazio" de
"indisponível" — decisão registrada em D2 e resolvida pela Spec 012, que remove o
fallback de 404 e reaponta as rotas. 5xx chega ao `catch` da tela, que o registra no
console sem exceção não tratada. `salvarCardapio` mantém o erro visível ao usuário
(ação explícita não pode falhar em silêncio).

Remover as quatro funções sem consumidor e os tipos que só elas usam. `Serie` e
`CardapioHistorico` permanecem porque `HomeNutricao` usa o histórico.

## Documentação viva

O teste lê `README.md` e os controllers por reflexão:

```csharp
[Fact]
public void Readme_ListaTodosOsEndpoints()
{
    var readme = File.ReadAllText(CaminhoDoReadme());
    var codigo = RotasDosControllers();          // HashSet: "GET /api/aluno"
    var documentadas = LinhaDocumentada.Matches(readme)
        .Select(m => $"{m.Groups["verbo"].Value} {m.Groups["rota"].Value}")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    Assert.Empty(codigo.Except(documentadas));   // ausentes no README
    Assert.Empty(documentadas.Except(codigo));   // excedentes no README
}
```

`RotasDosControllers` combina `[Route]` da classe com `[Http*]` do método, normaliza
`[controller]` e usa `{id}` para parâmetros. `LinhaDocumentada` é uma regex sobre as
linhas da tabela markdown, então a comparação vale nos dois sentidos — uma rota
documentada que não existe também falha.

Limite conhecido: o teste valida **presença do par método+rota**, não semântica da
descrição.
Ele impede o envelhecimento silencioso (L6) sem exigir parser de markdown completo.

## README

A tabela passa a ter as colunas `Método | Caminho | Descrição | Autenticação` (a coluna
de sessão se chama `Autenticação`) e é atualizada na mesma mudança que altera
controller. As rotas de aluno aparecem como `Sim` desde a Spec 006.

## Sequência

1. Remover `/colors` e a página.
2. Remover estatísticas de perfil.
3. Podar `CardapioApi.ts` e adicionar o fallback de 404.
4. Atualizar README.
5. Criar `DocumentationTests`.
6. Gates.

## Riscos

- **Teste de documentação frágil**: mudanças de formatação no README podem quebrar o
  teste. O contrato é o par `MÉTODO /rota`, e a mensagem do teste nomeia o que faltou.
- **Fallback esconder erro real**: só 404 vira lista vazia; 5xx continua erro visível
  (na tela, como mensagem; não como exceção não tratada).
- **Remoção de função ainda usada**: `tsc -b` e `npm run lint` são gates obrigatórios.
