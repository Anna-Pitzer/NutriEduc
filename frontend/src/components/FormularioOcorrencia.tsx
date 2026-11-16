import type { AlunoResposta } from "../services/AlunoApi";
import { SeletorDeFilho } from "./SeletorDeFilho";

/**
 * Formulário de ocorrência do responsável (Spec 035, RF11–RF14). O tipo é sempre
 * `Aluno`; a página monta o payload e trata a resposta. Erros de campo ficam
 * associados aos inputs por `aria-describedby`.
 */
export function FormularioOcorrencia({
    filhos,
    alunoId,
    onAluno,
    descricao,
    onDescricao,
    enviando,
    erro,
    erroFilho,
    erroDescricao,
    onSubmit,
}: {
    filhos: AlunoResposta[];
    alunoId: number | null;
    onAluno: (id: number) => void;
    descricao: string;
    onDescricao: (valor: string) => void;
    enviando: boolean;
    erro?: string;
    erroFilho?: string;
    erroDescricao?: string;
    onSubmit: () => void;
}) {
    return (
        <form
            className="space-y-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
            onSubmit={(evento) => {
                evento.preventDefault();
                onSubmit();
            }}
            noValidate
        >
            <SeletorDeFilho filhos={filhos} value={alunoId} onChange={onAluno} id="ocorrencia-filho" />

            {erroFilho && (
                <p id="erro-filho" role="alert" className="text-sm text-red-600">
                    {erroFilho}
                </p>
            )}

            <div className="flex flex-col gap-1">
                <label htmlFor="ocorrencia-descricao" className="text-sm font-medium text-slate-700">
                    Descrição
                </label>
                <textarea
                    id="ocorrencia-descricao"
                    value={descricao}
                    onChange={(evento) => onDescricao(evento.target.value)}
                    rows={4}
                    maxLength={1000}
                    aria-invalid={Boolean(erroDescricao)}
                    aria-describedby={erroDescricao ? "erro-descricao" : "contador-descricao"}
                    className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                    placeholder="Descreva o ocorrido com o contexto necessário."
                />
                <span id="contador-descricao" className="text-xs text-slate-500">
                    {descricao.length}/1000 — mínimo de 5 caracteres.
                </span>
                {erroDescricao && (
                    <p id="erro-descricao" role="alert" className="text-sm text-red-600">
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
                disabled={enviando}
                className="rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white transition hover:opacity-90 focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40 disabled:opacity-50"
            >
                {enviando ? "Enviando..." : "Registrar ocorrência"}
            </button>
        </form>
    );
}
