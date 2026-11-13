import type { AlunoResposta } from "../services/AlunoApi";
import type { Restricao } from "../services/RestricaoApi";
import { SeletorDeFilho } from "./SeletorDeFilho";

/**
 * Formulário de inclusão de alergia/restrição (Spec 035, RF15–RF20). Só aceita item ativo
 * do catálogo; o pedido não altera o cadastro do aluno antes da aprovação do ADM.
 */
export function FormularioSolicitacao({
    filhos,
    restricoes,
    alunoId,
    onAluno,
    restricaoNome,
    onRestricao,
    descricao,
    onDescricao,
    enviando,
    erro,
    erroFilho,
    erroRestricao,
    erroDescricao,
    anafilaxia,
    onAnafilaxia,
    permitirAnafilaxia,
    onSubmit,
}: {
    filhos: AlunoResposta[];
    restricoes: Restricao[];
    alunoId: number | null;
    onAluno: (id: number) => void;
    restricaoNome: string;
    onRestricao: (valor: string) => void;
    descricao: string;
    onDescricao: (valor: string) => void;
    enviando: boolean;
    erro?: string;
    erroFilho?: string;
    erroRestricao?: string;
    erroDescricao?: string;
    anafilaxia?: boolean;
    onAnafilaxia?: (valor: boolean) => void;
    /** Mostra o aviso de anafilaxia (área dos pais). */
    permitirAnafilaxia?: boolean;
    onSubmit: () => void;
}) {
    const ativas = restricoes.filter((restricao) => restricao.ativa);

    return (
        <form
            className="space-y-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
            onSubmit={(evento) => {
                evento.preventDefault();
                onSubmit();
            }}
            noValidate
        >
            <SeletorDeFilho filhos={filhos} value={alunoId} onChange={onAluno} id="solicitacao-filho" />

            {erroFilho && (
                <p role="alert" className="text-sm text-red-600">
                    {erroFilho}
                </p>
            )}

            <div className="flex flex-col gap-1">
                <label htmlFor="solicitacao-restricao" className="text-sm font-medium text-slate-700">
                    Restrição ou alergia
                </label>
                <select
                    id="solicitacao-restricao"
                    value={restricaoNome}
                    onChange={(evento) => onRestricao(evento.target.value)}
                    aria-invalid={Boolean(erroRestricao)}
                    aria-describedby={erroRestricao ? "erro-restricao" : undefined}
                    className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                >
                    <option value="">Selecione</option>
                    {ativas.map((restricao) => (
                        <option key={restricao.id} value={restricao.nome}>
                            {restricao.nome} ({restricao.categoria})
                        </option>
                    ))}
                </select>
                {erroRestricao && (
                    <p id="erro-restricao" role="alert" className="text-sm text-red-600">
                        {erroRestricao}
                    </p>
                )}
            </div>

            {permitirAnafilaxia && (
                <label className="flex items-start gap-2 rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">
                    <input
                        type="checkbox"
                        checked={anafilaxia ?? false}
                        onChange={(evento) => onAnafilaxia?.(evento.target.checked)}
                        className="mt-0.5 h-4 w-4 rounded border-amber-400 accent-roxo focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40"
                    />
                    <span>
                        Esta alergia é <strong>anafilática</strong> (reação grave). A marcação
                        será registrada no aluno quando a secretaria aprovar o pedido.
                    </span>
                </label>
            )}

            <div className="flex flex-col gap-1">
                <label htmlFor="solicitacao-descricao" className="text-sm font-medium text-slate-700">
                    Descrição
                </label>
                <textarea
                    id="solicitacao-descricao"
                    value={descricao}
                    onChange={(evento) => onDescricao(evento.target.value)}
                    rows={4}
                    maxLength={1000}
                    aria-invalid={Boolean(erroDescricao)}
                    aria-describedby={erroDescricao ? "erro-solicitacao-descricao" : "contador-solicitacao"}
                    className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                    placeholder="Explique a alergia/restrição e o contexto do pedido."
                />
                <span id="contador-solicitacao" className="text-xs text-slate-500">
                    {descricao.length}/1000 — mínimo de 5 caracteres.
                </span>
                {erroDescricao && (
                    <p id="erro-solicitacao-descricao" role="alert" className="text-sm text-red-600">
                        {erroDescricao}
                    </p>
                )}
            </div>

            {erro && (
                <p role="alert" className="rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700">
                    {erro}
                </p>
            )}

            <button
                type="submit"
                disabled={enviando || ativas.length === 0}
                className="rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white transition hover:opacity-90 focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40 disabled:opacity-50"
            >
                {enviando ? "Enviando..." : "Enviar solicitação"}
            </button>

            {ativas.length === 0 && (
                <p className="text-sm text-slate-500">
                    Nenhum item ativo no catálogo de restrições.
                </p>
            )}
        </form>
    );
}
