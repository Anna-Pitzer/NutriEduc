import { useState, type FormEvent } from "react";
import { Ban, Check, Pencil, RotateCcw, Trash2, X } from "lucide-react";

export interface ItemDeNome {
    id: number;
    nome: string;
    ativa: boolean;
}

interface TabelaDeNomesProps {
    titulo: string;
    descricao: string;
    rotuloVazio: string;
    itens: ItemDeNome[];
    carregando: boolean;
    /** Sem `aoCriar` não há formulário: a tela só lista (unidade nasce no bootstrap). */
    aoCriar?: (nome: string) => Promise<void>;
    aoRenomear?: (id: number, nome: string) => Promise<void>;
    aoAlternarAtiva?: (item: ItemDeNome) => Promise<void>;
    /** Exclusão definitiva: o backend bloqueia com `409` quando há referência. */
    aoExcluir?: (item: ItemDeNome) => Promise<void>;
}

/**
 * Tabela de nomes com criar, renomear inline, ativar/desativar e excluir. Cada ação
 * aparece só quando o callback correspondente é passado.
 */
export function TabelaDeNomes({
    titulo,
    descricao,
    rotuloVazio,
    itens,
    carregando,
    aoCriar,
    aoRenomear,
    aoAlternarAtiva,
    aoExcluir,
}: TabelaDeNomesProps) {
    const [novoNome, setNovoNome] = useState("");
    const [editandoId, setEditandoId] = useState<number | null>(null);
    const [nomeEditando, setNomeEditando] = useState("");
    const [ocupado, setOcupado] = useState(false);
    const [erro, setErro] = useState("");

    async function executar(acao: () => Promise<void>) {
        setOcupado(true);
        setErro("");

        try {
            await acao();
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Não foi possível concluir a ação.");
        } finally {
            setOcupado(false);
        }
    }

    async function handleCriar(evento: FormEvent) {
        evento.preventDefault();

        const nome = novoNome.trim();

        if (!nome) {
            setErro("Informe o nome.");
            return;
        }

        await executar(async () => {
            await aoCriar?.(nome);
            setNovoNome("");
        });
    }

    async function handleRenomear(evento: FormEvent) {
        evento.preventDefault();

        if (editandoId === null) {
            return;
        }

        const nome = nomeEditando.trim();

        if (!nome) {
            setErro("Informe o nome.");
            return;
        }

        await executar(async () => {
            await aoRenomear?.(editandoId, nome);
            setEditandoId(null);
            setNomeEditando("");
        });
    }

    return (
        <section className="rounded-2xl bg-white p-6 shadow-[0_5px_20px_rgba(1,1,1,0.1)]">
            <h2 className="text-xl font-bold text-primaria">{titulo}</h2>
            <p className="mt-1 text-sm text-gray-500">{descricao}</p>

            {aoCriar && (
                <form onSubmit={handleCriar} className="mt-4 flex flex-col gap-2 sm:flex-row sm:flex-wrap">
                <label htmlFor="nome-novo" className="sr-only">
                    Novo nome
                </label>
                <input
                    id="nome-novo"
                    type="text"
                    value={novoNome}
                    onChange={(evento) => setNovoNome(evento.target.value)}
                    placeholder="Novo nome"
                    className="h-11 w-full flex-1 rounded-2xl border border-gray-200 bg-white px-4 text-sm outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10 sm:min-w-0"
                />
                <button
                    type="submit"
                    disabled={ocupado}
                    className="w-full rounded-2xl bg-primaria px-5 py-3 text-sm font-medium text-white transition-colors hover:bg-[#b31f55] disabled:opacity-50 sm:w-auto"
                >
                    Adicionar
                </button>
            </form>
            )}

            {erro && (
                <p role="alert" className="mt-3 rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700">
                    {erro}
                </p>
            )}

            {carregando ? (
                <p className="mt-6 text-sm text-gray-500">Carregando...</p>
            ) : itens.length === 0 ? (
                <p className="mt-6 text-sm text-gray-500">{rotuloVazio}</p>
            ) : (
                <div className="mt-6 w-full overflow-x-auto">
                <table className="w-full min-w-[36rem] border-collapse text-left text-sm">
                    <thead>
                        <tr className="border-b border-gray-200 text-xs uppercase tracking-wider text-gray-500">
                            <th className="py-2">Nome</th>
                            <th className="py-2">Situação</th>
                            <th className="py-2 text-right">Ações</th>
                        </tr>
                    </thead>
                    <tbody>
                        {itens.map((item) => (
                            <tr key={item.id} className="border-b border-gray-100 last:border-0">
                                <td className="py-3 pr-4">
                                    {editandoId === item.id ? (
                                        <form onSubmit={handleRenomear} className="flex min-w-0 items-center gap-2">
                                            <label htmlFor={`nome-${item.id}`} className="sr-only">
                                                Novo nome
                                            </label>
                                            <input
                                                id={`nome-${item.id}`}
                                                type="text"
                                                value={nomeEditando}
                                                onChange={(evento) => setNomeEditando(evento.target.value)}
                                                autoFocus
                                                className="h-9 w-full min-w-0 flex-1 rounded-xl border border-gray-200 px-3 text-sm outline-none focus:border-roxo"
                                            />
                                            <button
                                                type="submit"
                                                disabled={ocupado}
                                                aria-label="Salvar nome"
                                                className="rounded-lg p-1.5 text-primaria hover:bg-gray-100 disabled:opacity-50"
                                            >
                                                <Check size={18} />
                                            </button>
                                            <button
                                                type="button"
                                                onClick={() => setEditandoId(null)}
                                                aria-label="Cancelar"
                                                className="rounded-lg p-1.5 text-gray-500 hover:bg-gray-100"
                                            >
                                                <X size={18} />
                                            </button>
                                        </form>
                                    ) : (
                                        item.nome
                                    )}
                                </td>
                                <td className="py-3 pr-4">
                                    <span
                                        className={`rounded-full px-3 py-1 text-xs font-medium ${item.ativa
                                            ? "bg-green-100 text-green-700"
                                            : "bg-gray-200 text-gray-600"
                                            }`}
                                    >
                                        {item.ativa ? "Ativa" : "Desativada"}
                                    </span>
                                </td>
                                <td className="py-3 text-right">
                                    <div className="flex flex-wrap justify-end gap-1">
                                        {aoRenomear && (
                                        <button
                                            type="button"
                                            onClick={() => {
                                                setEditandoId(item.id);
                                                setNomeEditando(item.nome);
                                                setErro("");
                                            }}
                                            aria-label={`Renomear ${item.nome}`}
                                            className="rounded-lg p-1.5 text-gray-500 hover:bg-gray-100"
                                        >
                                            <Pencil size={18} />
                                        </button>
                                        )}
                                        {aoAlternarAtiva && (
                                        <button
                                            type="button"
                                            disabled={ocupado}
                                            onClick={() => executar(() => aoAlternarAtiva(item))}
                                            aria-label={item.ativa ? `Desativar ${item.nome}` : `Reativar ${item.nome}`}
                                            className="rounded-lg p-1.5 text-gray-500 hover:bg-gray-100 disabled:opacity-50"
                                        >
                                            {item.ativa ? <Ban size={18} /> : <RotateCcw size={18} />}
                                        </button>
                                        )}
                                        {aoExcluir && (
                                        <button
                                            type="button"
                                            disabled={ocupado}
                                            onClick={() => {
                                                if (!window.confirm(`Excluir “${item.nome}”? Esta ação não pode ser desfeita.`)) {
                                                    return;
                                                }

                                                void executar(() => aoExcluir(item));
                                            }}
                                            aria-label={`Excluir ${item.nome}`}
                                            className="rounded-lg p-1.5 text-red-600 hover:bg-red-50 disabled:opacity-50"
                                        >
                                            <Trash2 size={18} />
                                        </button>
                                        )}
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
                </div>
            )}
        </section>
    );
}
