import type {
    CardapioDaVersao,
    CardapioServido,
    RefeicaoCardapio,
    ValoresItem,
} from "../services/CardapioApi";

function numero(valor: number): string {
    return valor.toLocaleString("pt-BR", { maximumFractionDigits: 1 });
}

function resumoNutricional(valores: ValoresItem): string {
    return [
        `${numero(valores.caloriasKcal)} kcal`,
        `P ${numero(valores.proteinasG)} g`,
        `C ${numero(valores.carboidratosG)} g`,
        `G ${numero(valores.gordurasG)} g`,
    ].join(" · ");
}

/** Nomes de refeição das versões visíveis, na ordem em que aparecem (Spec 035, RF4). */
function nomesDeRefeicao(versoes: Array<CardapioDaVersao | null>): string[] {
    const nomes: string[] = [];

    for (const versao of versoes) {
        for (const refeicao of versao?.refeicoes ?? []) {
            if (!nomes.includes(refeicao.nome)) {
                nomes.push(refeicao.nome);
            }
        }
    }

    return nomes;
}

/**
 * Apresenta as versões publicadas para o aluno (Spec 035, RF3–RF6, revisão do portal):
 * o cardápio geral e, quando existir, o adaptado ao conjunto de restrições do filho.
 * O geral nunca substitui a versão adaptada ausente — ele aparece como referência e o
 * alerta permanece visível. O filtro de refeição é local e não dispara nova requisição.
 */
export function CardapioDoAluno({
    resultado,
    refeicao,
    onRefeicao,
}: {
    resultado: CardapioServido;
    refeicao: string | "todas";
    onRefeicao: (valor: string | "todas") => void;
}) {
    const geral = resultado.cardapioGeral;
    const adaptado = resultado.cardapioAdaptado;

    if (!geral && !adaptado) {
        return (
            <article className="rounded-2xl border border-amber-200 bg-amber-50 p-5">
                <h2 className="text-lg font-semibold text-amber-900">
                    Cardápio indisponível para {resultado.alunoNome}
                </h2>

                {resultado.motivo && (
                    <p className="mt-2 text-sm text-amber-800">{resultado.motivo}</p>
                )}

                {resultado.alerta && (
                    <p role="alert" className="mt-2 text-sm font-medium text-amber-900">
                        Os dados de escola/unidade do aluno precisam de atenção.
                    </p>
                )}

                <p className="mt-3 text-xs text-amber-700">
                    Nenhuma refeição é exibida enquanto não houver versão publicada.
                </p>
            </article>
        );
    }

    const refeicoes = nomesDeRefeicao([geral, adaptado]);

    return (
        <div className="space-y-5">
            {refeicoes.length > 1 && (
                <div className="flex justify-end">
                    <label
                        htmlFor="filtro-refeicao"
                        className="flex flex-col gap-1 text-sm font-medium text-slate-700"
                    >
                        Refeição
                        <select
                            id="filtro-refeicao"
                            value={refeicao}
                            onChange={(evento) =>
                                onRefeicao(
                                    evento.target.value === "todas"
                                        ? "todas"
                                        : evento.target.value,
                                )
                            }
                            className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                        >
                            <option value="todas">Todas</option>
                            {refeicoes.map((nome) => (
                                <option key={nome} value={nome}>
                                    {nome}
                                </option>
                            ))}
                        </select>
                    </label>
                </div>
            )}

            {geral && (
                <CartaoVersao
                    rotulo="Cardápio geral"
                    cardapio={geral}
                    alunoNome={resultado.alunoNome}
                    refeicao={refeicao}
                />
            )}

            {adaptado && (
                <CartaoVersao
                    rotulo={`Cardápio adaptado de ${resultado.alunoNome}`}
                    cardapio={adaptado}
                    alunoNome={resultado.alunoNome}
                    refeicao={refeicao}
                />
            )}

            {geral && resultado.requerAdaptacao && !adaptado && (
                <p
                    role="alert"
                    className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900"
                >
                    {resultado.motivo ?? "Não há versão adaptada publicada para este aluno."}{" "}
                    O cardápio geral acima é apenas referência; ele não substitui a versão
                    adaptada.
                </p>
            )}
        </div>
    );
}

function CartaoVersao({
    rotulo,
    cardapio,
    alunoNome,
    refeicao,
}: {
    rotulo: string;
    cardapio: CardapioDaVersao;
    alunoNome: string;
    refeicao: string | "todas";
}) {
    const visiveis =
        refeicao === "todas"
            ? cardapio.refeicoes
            : cardapio.refeicoes.filter((item) => item.nome === refeicao);

    return (
        <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <header className="flex flex-wrap items-start justify-between gap-3">
                <div>
                    <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                        {rotulo}
                    </p>
                    <h2 className="text-lg font-semibold text-roxo">{cardapio.nome}</h2>
                    <p className="text-sm text-slate-500">
                        {alunoNome} · {cardapio.tipo}
                    </p>
                </div>
            </header>

            {cardapio.tipo === "Adaptado" && cardapio.restricoes.length > 0 && (
                <section className="mt-4 rounded-xl bg-sky-50 px-4 py-3">
                    <h3 className="text-sm font-semibold text-sky-900">
                        Restrições consideradas nesta versão
                    </h3>
                    <p className="mt-1 text-sm text-sky-800">
                        {cardapio.restricoes
                            .map((restricao) => `${restricao.nome} (${restricao.categoria})`)
                            .join(" · ")}
                    </p>
                </section>
            )}

            <div className="mt-4 space-y-4">
                {visiveis.length === 0 ? (
                    <p className="text-sm text-slate-500">
                        Nenhuma refeição para o filtro selecionado.
                    </p>
                ) : (
                    visiveis.map((item) => <Refeicao key={item.id} refeicao={item} />)
                )}
            </div>
        </article>
    );
}

function Refeicao({ refeicao }: { refeicao: RefeicaoCardapio }) {
    return (
        <section className="rounded-xl border border-slate-100 p-4">
            <h3 className="font-semibold text-roxo">{refeicao.nome}</h3>

            <ul className="mt-2 divide-y divide-slate-100">
                {refeicao.itens.map((item) => (
                    <li key={item.alimentoId} className="flex flex-wrap justify-between gap-2 py-2 text-sm">
                        <span className="text-slate-800">
                            {item.alimento} — {numero(item.gramas)} g
                        </span>
                        <span className="text-slate-500">{resumoNutricional(item.valores)}</span>
                    </li>
                ))}
            </ul>

            <p className="mt-2 text-right text-sm text-slate-600">
                Total da refeição:{" "}
                <span className="font-semibold text-slate-800">
                    {resumoNutricional(refeicao.total)}
                </span>
            </p>
        </section>
    );
}
