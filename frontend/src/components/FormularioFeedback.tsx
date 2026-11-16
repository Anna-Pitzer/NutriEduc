import type { AlunoResposta } from "../services/AlunoApi";

/**
 * Formulário de feedback do responsável (Spec 035, RF21–RF25). O aluno é opcional e
 * serve apenas de contexto; nenhum dado clínico é exibido ou enviado.
 */
export function FormularioFeedback({
    filhos,
    alunoId,
    onAluno,
    assunto,
    onAssunto,
    mensagem,
    onMensagem,
    enviando,
    erro,
    erroAssunto,
    erroMensagem,
    onSubmit,
}: {
    filhos: AlunoResposta[];
    alunoId: number | null;
    onAluno: (id: number | null) => void;
    assunto: string;
    onAssunto: (valor: string) => void;
    mensagem: string;
    onMensagem: (valor: string) => void;
    enviando: boolean;
    erro?: string;
    erroAssunto?: string;
    erroMensagem?: string;
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
            <div className="flex flex-col gap-1">
                <label htmlFor="feedback-filho" className="text-sm font-medium text-slate-700">
                    Filho (opcional)
                </label>
                <select
                    id="feedback-filho"
                    value={alunoId ?? ""}
                    onChange={(evento) =>
                        onAluno(evento.target.value === "" ? null : Number(evento.target.value))
                    }
                    className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                >
                    <option value="">Sem aluno</option>
                    {filhos.map((filho) => (
                        <option key={filho.id} value={filho.id}>
                            {filho.nome}
                        </option>
                    ))}
                </select>
            </div>

            <div className="flex flex-col gap-1">
                <label htmlFor="feedback-assunto" className="text-sm font-medium text-slate-700">
                    Assunto
                </label>
                <input
                    id="feedback-assunto"
                    type="text"
                    value={assunto}
                    onChange={(evento) => onAssunto(evento.target.value)}
                    maxLength={120}
                    aria-invalid={Boolean(erroAssunto)}
                    aria-describedby={erroAssunto ? "erro-assunto" : "contador-assunto"}
                    className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                    placeholder="Ex.: Dúvida sobre o cardápio da semana"
                />
                <span id="contador-assunto" className="text-xs text-slate-500">
                    {assunto.length}/120 — mínimo de 5 caracteres.
                </span>
                {erroAssunto && (
                    <p id="erro-assunto" role="alert" className="text-sm text-red-600">
                        {erroAssunto}
                    </p>
                )}
            </div>

            <div className="flex flex-col gap-1">
                <label htmlFor="feedback-mensagem" className="text-sm font-medium text-slate-700">
                    Mensagem
                </label>
                <textarea
                    id="feedback-mensagem"
                    value={mensagem}
                    onChange={(evento) => onMensagem(evento.target.value)}
                    rows={4}
                    maxLength={1000}
                    aria-invalid={Boolean(erroMensagem)}
                    aria-describedby={erroMensagem ? "erro-mensagem" : "contador-mensagem"}
                    className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                    placeholder="Escreva sua mensagem."
                />
                <span id="contador-mensagem" className="text-xs text-slate-500">
                    {mensagem.length}/1000 — mínimo de 5 caracteres.
                </span>
                {erroMensagem && (
                    <p id="erro-mensagem" role="alert" className="text-sm text-red-600">
                        {erroMensagem}
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
                {enviando ? "Enviando..." : "Enviar feedback"}
            </button>
        </form>
    );
}
