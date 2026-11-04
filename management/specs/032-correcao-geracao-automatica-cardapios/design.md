---
name: correcao-geracao-automatica-cardapios
status: created
references: V21, V18, ADR-022, ADR-019, ADR-004
---

# Design — Correção da geração automática de cardápios

## Princípio

Gerar é **propor e provar**, não sortear e salvar. O domínio recebe candidatos e parâmetros,
tenta fechar cada refeição com tentativas limitadas e determinísticas, e devolve ou um cardápio
que satisfaz todas as invariantes, ou um diagnóstico acionável. O serviço não inventa correção:
ele carrega, chama, valida e persiste em uma única transação.

## Pipeline

```
1. Validar parâmetros (RF1)
2. Validar nutricionista: papel, ativo, unidade, CRN (RF2)
3. Carregar catálogo ativo ordenado por id (RF4)
4. Carregar escolas, alunos e restrições da unidade (RF14)
5. Gerar refeições com até 5 tentativas (RF5, RF6)
6. Montar o cardápio geral
7. Validar o cardápio inteiro (RF7)
8. Abrir transação
9.   Salvar geral + autoria
10.  Para cada (escola, conjunto): adaptar (Spec 029) e salvar
11.  Gravar trilha
12. Confirmar transação (RF10)
13. Responder ids, seed, tentativas, totais, status e diagnóstico (RF12)
```

Qualquer falha entre 1 e 7 devolve 400/422 **antes** de abrir transação. Qualquer falha entre
9 e 11 desfaz tudo.

## Estrutura

```
backend/
  Domain/Services/
    GeradorCardapioAutomatico.cs   (alterado — tentativas + diagnóstico)
    SeletorDeRefeicao.cs           (novo — função pura por refeição)
    ValidadorDeCardapioGerado.cs   (novo — invariantes pós-montagem)
    EntradaGeracao.cs              (+ ordem estável e tentativas)
    ResultadoGeracao.cs            (+ diagnostico por refeição)
  Application/
    GeracaoDTOs.cs                 (+ ids, seed, tentativas, diagnóstico)
  API/Services/
    GeracaoCardapioService.cs      (validação, transação, adaptação)
frontend/src/pages/nutricao/cardapios/
  ParametrosGeracao.tsx            (motivo por refeição, estado dos adaptados)
```

## Seleção determinística

```csharp
static IReadOnlyList<ItemGerado>? TentarRefeicao(
    RefeicaoPadrao refeicao, IReadOnlyList<CandidatoAlimento> pool,
    ParametroGeracao p, int seed, out DiagnosticoRefeicao diagnostico)
{
    var melhor = diagnostico vazio;
    for (var tentativa = 1; tentativa <= 5; tentativa++)
    {
        var semente = unchecked(seed * 31 + tentativa * 17 + refeicao.Ordem);
        var rnd = new Random(semente);
        var disponiveis = pool.ToList();
        Embaralhar(disponiveis, rnd);

        var selecionados = SelecionarCategoriasObrigatorias(disponiveis, p);
        CompletarAteOpcoes(selecionados, disponiveis, p.OpcoesPorRefeicao);

        var total = selecionados.Sum(i => i.Valores.CaloriasKcal);

        if (selecionados.Count == p.OpcoesPorRefeicao && total >= p.Min && total <= p.Max)
            return selecionados;

        melhor = AtualizarDiagnostico(melhor, selecionados, total, p);
    }

    diagnostico = melhor;   // faixa alcançável, categoria ausente, candidatos disponíveis
    return null;
}
```

Regras do seletor:

- `pool` já exclui a janela de repetição e os alimentos usados em refeições anteriores.
- categorias obrigatórias são atendidas primeiro, na ordem declarada;
- a tentativa não altera o conjunto de candidatos, apenas a ordem;
- o diagnóstico guarda a menor e a maior kcal alcançadas e a categoria que faltou;
- nada de `Random.Shared` no domínio.

## Validação pós-montagem

`ValidadorDeCardapioGerado.Validar(cardapio, parametros)` devolve lista de erros:

| Verificação | Erro |
|---|---|
| quantidade de refeições igual à configuração | `refeicoes_incompletas` |
| cada refeição com exatamente `OpcoesPorRefeicao` itens distintos | `itens_insuficientes` |
| todas as categorias obrigatórias presentes | `categoria_ausente` |
| kcal da refeição dentro da faixa | `faixa_calorica` |
| refeição padrão sem nome duplicado | `refeicao_duplicada` |
| item com `AlimentoId > 0` | `item_invalido` |
| período com `Fim >= Inicio` | `periodo_invalido` |
| unidade existente e ativa | `unidade_invalida` |

O total é recalculado pelo mesmo `CalculoNutricional` da Spec 012; o valor persistido não é
copiado do gerador. Qualquer erro → 422 com a lista.

## Transação e adaptação

```csharp
return _unidadeDeTrabalho.ExecutarEmTransacao(() =>
{
    _cardapios.Add(geral);

    foreach (var (escola, conjunto) in _contexto.ObterGrupos(unidadeId))
    {
        var adaptado = _adaptacao.Adaptar(geral, escola, conjunto);
        _cardapios.Add(adaptado);
    }

    _auditoria.RegistrarCriacao(geral, adaptados);
});
```

`Adaptar` é a função da Spec 029; o serviço de geração não decide substituição nem severidade.
Se `Adaptar` devolver `PendenteDeRevisao`, a versão é salva assim e o motivo entra na resposta;
se lançar, a transação desfaz o geral (RF10).

## Contrato de resposta

```json
{
  "cardapioGeralId": 120,
  "seed": 482910,
  "tentativasPorRefeicao": [{ "refeicao": "Almoço", "tentativas": 2 }],
  "totaisPorRefeicao": [{ "refeicao": "Almoço", "caloriasKcal": 640.5 }],
  "versaoAdaptadaIds": [
    { "id": 121, "escolaId": 7, "conjunto": "3,9", "status": "Atualizado" },
    { "id": 122, "escolaId": 8, "conjunto": "3,9", "status": "PendenteDeRevisao",
      "motivo": "Sem substituto para o alimento 42 na refeição Almoço." }
  ],
  "avisos": ["Categoria obrigatória 5 atendida por um único alimento."],
  "diagnostico": []
}
```

Em 422, `diagnostico` traz, por refeição, `faixaAlcancavel`, `categoriaAusente`,
`candidatosDisponiveis` e `tentativas`.

## Testes obrigatórios

| Teste | Prova |
|---|---|
| `Gerador_MesmoSeed_MesmoResultado` | RF13, RNF2 |
| `Gerador_FaixaApertada_UsaTentativas` | RF5 |
| `Gerador_FalhaComDiagnosticoPorRefeicao` | RF15 |
| `Gerador_NaoUsaAlimentoDaJanela` | RF4 |
| `Gerador_NaoRemoveCandidatoPorRestricao` | RF3 |
| `Validador_RejeitaCategoriaAusente` | RF6, RF7 |
| `Validador_RejeitaForaDaFaixa` | RF6, RF7 |
| `Parametro_CategoriaInexistente_400` | RF1 |
| `Geracao_CriaGeralEAdaptados_Atomicamente` | RF8, RF10 |
| `Geracao_AdaptacaoPendente_SalvaGeralEBloqueiaPublicacao` | RF9 |
| `Geracao_NutricionistaOutraUnidade_400` | RF2, RF14 |
| `Geracao_SemCrn_400` | RF2 |
| `Geracao_ErroNaoPersisteNada` | RF10, RF7 |
| `Geracao_SeedNulo_DevolveSeed` | RF13 |

## Riscos

- **Cinco tentativas ainda podem falhar:** a faixa pode ser inatingível com o catálogo. O
  diagnóstico mostra a faixa alcançável; a correção é de parâmetro ou catálogo, não de código.
- **Catálogo pequeno:** `refeições × opções` pode esgotar os alimentos. O erro explicita a
  conta (RF15) e evita a mensagem genérica atual.
- **Custo da adaptação:** cresce com `escolas × conjuntos`; a Spec 029 limita a 50 versões e
  carrega catálogo/alunos uma vez.
- **Duplicação com a Spec 013:** o documento antigo continua válido no que não foi superado; a
  tabela de supersessão no `requirements.md` é a referência.
- **Transação longa:** geral + N adaptados na mesma transação. O volume é limitado por 50 e
  pelo tamanho do catálogo; a alternativa de salvar em etapas violaria RF10.
- **Diagnóstico sem PII:** a resposta nomeia refeição e alimento, nunca aluno; a lista de
  alunos afetados fica na compatibilidade (Spec 014).
