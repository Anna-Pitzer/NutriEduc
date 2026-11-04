import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ChevronLeft, Copy, Pencil, Trash } from "lucide-react";
import Button from "../../../components/Button";
import Footer from "../../../components/Footer";
import Header from "../../../components/Header";
import {
    cancelarCardapio,
    criarRevisaoCardapio,
    duplicarCardapio,
    excluirCardapio,
    obterCardapio,
    obterCompatibilidade,
    obterHistoricoCardapio,
    publicarCardapio,
    type Cardapio,
    type Compatibilidade,
    type EventoHistoricoCardapio,
    type SituacaoCardapio,
} from "../../../services/CardapioApi";

const corSituacao: Record<SituacaoCardapio, string> = {
    Rascunho: "bg-bege text-roxo",
    Publicado: "bg-green-100 text-green-800",
    Cancelado: "bg-red-100 text-red-700",
    Substituido: "bg-gray-100 text-gray-600",
};

function dataCurta(iso: string): string {
    const [ano, mes, dia] = iso.split("-");
    return `${dia}/${mes}/${ano}`;
}

/**
 * Detalhe do cardápio (Spec 012, RF11 e RF13): refeições ordenadas, valores por item e
 * totais por refeição e do cardápio. Todos os números vêm do backend (RNF3).
 *
 * Publicar, cancelar e revisar pertencem à Spec 015; aqui só existe duplicação.
 */
export function DetalheCardapio() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const [cardapio, setCardapio] = useState<Cardapio | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState<string | null>(null);
    const [novoInicio, setNovoInicio] = useState("");
    const [novoFim, setNovoFim] = useState("");
    const [duplicando, setDuplicando] = useState(false);
    const [mensagem, setMensagem] = useState<string | null>(null);
    const [compatibilidade, setCompatibilidade] = useState<Compatibilidade | null>(null);
    const [historico, setHistorico] = useState<EventoHistoricoCardapio[]>([]);

    useEffect(() => {
        async function carregar() {
            try {
                setCardapio(await obterCardapio(Number(id)));
            } catch (falha) {
                setErro(falha instanceof Error ? falha.message : "Erro ao carregar o cardápio");
            } finally {
                setCarregando(false);
            }
        }

        carregar();
    }, [id]);

    useEffect(() => {
        async function carregarCompatibilidade() {
            try {
                setCompatibilidade(await obterCompatibilidade(Number(id)));
            } catch {
                // A análise é complementar: falha ao carregar não derruba o detalhe.
                setCompatibilidade(null);
            }
        }

        carregarCompatibilidade();
    }, [id]);

    useEffect(() => {
        async function carregarHistorico() {
            try {
                const pagina = await obterHistoricoCardapio(Number(id));
                setHistorico(pagina.itens);
            } catch {
                setHistorico([]);
            }
        }

        carregarHistorico();
    }, [id]);

    async function publicar() {
        if (!cardapio) return;
        setErro(null);

        try {
            const publicado = await publicarCardapio(cardapio.id);
            setCardapio(publicado);
            setMensagem("Cardápio publicado.");
            const pagina = await obterHistoricoCardapio(cardapio.id);
            setHistorico(pagina.itens);
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Erro ao publicar o cardápio");
        }
    }

    async function cancelar() {
        if (!cardapio) return;
        const texto = window.prompt("Motivo do cancelamento:");

        if (texto === null) return;

        setErro(null);

        try {
            const cancelado = await cancelarCardapio(cardapio.id, texto);
            setCardapio(cancelado);
            setMensagem("Cardápio cancelado.");
            const pagina = await obterHistoricoCardapio(cardapio.id);
            setHistorico(pagina.itens);
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Erro ao cancelar o cardápio");
        }
    }

    async function revisar() {
        if (!cardapio) return;
        setErro(null);

        try {
            const revisao = await criarRevisaoCardapio(cardapio.id);
            navigate(`/cardapios/${revisao.id}`);
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Erro ao criar a revisão");
        }
    }

    async function duplicar() {
        if (!cardapio) return;

        if (!novoInicio || !novoFim) {
            setErro("Informe o novo período antes de duplicar.");
            return;
        }

        setErro(null);
        setDuplicando(true);

        try {
            const copia = await duplicarCardapio(cardapio.id, {
                nome: `${cardapio.nome} (cópia)`,
                periodo: { inicio: novoInicio, fim: novoFim },
            });

            setMensagem(`Cópia criada como rascunho: ${copia.nome}.`);
            navigate(`/cardapios/${copia.id}`);
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Erro ao duplicar o cardápio");
        } finally {
            setDuplicando(false);
        }
    }

    async function excluir() {
        if (!cardapio) return;

        if (!window.confirm(`Excluir o cardápio "${cardapio.nome}"?`)) {
            return;
        }

        try {
            await excluirCardapio(cardapio.id);
            navigate("/cardapios");
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Erro ao excluir o cardápio");
        }
    }

    const rascunho = cardapio?.situacao === "Rascunho";

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <Header />

            <button
                type="button"
                onClick={() => navigate("/cardapios")}
                aria-label="Voltar aos cardápios"
                className="group mx-auto flex w-full max-w-7xl cursor-pointer items-center justify-start"
            >
                <ChevronLeft
                    size={50}
                    className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <div className="flex w-full max-w-7xl flex-col mx-auto mb-10 gap-10">
                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <div className="flex flex-wrap items-center justify-between gap-4">
                        <h2 className="font-bold text-2xl">
                            {carregando ? "Carregando..." : (cardapio?.nome ?? "Cardápio")}
                        </h2>

                        {cardapio && (
                            <div className="flex items-center gap-3">
                                <span
                                    className={`rounded-full px-3 py-1 text-sm ${corSituacao[cardapio.situacao]}`}
                                >
                                    {cardapio.situacao}
                                </span>

                                {rascunho && (
                                    <button
                                        type="button"
                                        onClick={() => navigate(`/cardapios/${cardapio.id}/editar`)}
                                        aria-label="Editar cardápio"
                                        className="text-roxo p-2 hover:cursor-pointer hover:bg-purple-100 rounded-full"
                                    >
                                        <Pencil />
                                    </button>
                                )}

                                {rascunho && (
                                    <button
                                        type="button"
                                        onClick={excluir}
                                        aria-label="Excluir cardápio"
                                        className="text-vermelho p-2 hover:cursor-pointer hover:bg-red-100 rounded-full"
                                    >
                                        <Trash />
                                    </button>
                                )}

                                {rascunho && (
                                    <button
                                        type="button"
                                        onClick={publicar}
                                        className="rounded-2xl bg-primaria px-3 py-1 text-sm font-medium text-white hover:bg-[#b31f55]"
                                    >
                                        Publicar
                                    </button>
                                )}

                                {cardapio.situacao === "Publicado" && (
                                    <button
                                        type="button"
                                        onClick={revisar}
                                        className="rounded-2xl bg-roxo px-3 py-1 text-sm font-medium text-white hover:bg-purple-700"
                                    >
                                        Criar revisão
                                    </button>
                                )}

                                {(rascunho || cardapio.situacao === "Publicado") && (
                                    <button
                                        type="button"
                                        onClick={cancelar}
                                        className="rounded-2xl bg-vermelho px-3 py-1 text-sm font-medium text-white hover:bg-red-700"
                                    >
                                        Cancelar
                                    </button>
                                )}
                            </div>
                        )}
                    </div>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    {carregando && <p className="py-4 text-gray-500">Carregando cardápio...</p>}

                    {erro && (
                        <p role="alert" className="py-4 text-red-700">
                            {erro}
                        </p>
                    )}

                    {mensagem && (
                        <p role="status" className="py-2 text-green-700">
                            {mensagem}
                        </p>
                    )}

                    {cardapio && (
                        <>
                            <p className="text-gray-600">
                                Unidade #{cardapio.unidadeId} · {dataCurta(cardapio.periodo.inicio)} a{" "}
                                {dataCurta(cardapio.periodo.fim)}
                            </p>

                            <p className="text-sm text-gray-500">
                                Vale para todas as escolas desta unidade.
                            </p>

                            {cardapio.nutricionista && (
                                <div className="mt-4 rounded-2xl border border-[#fce9d3] bg-white p-4">
                                    <p className="text-xs font-semibold uppercase tracking-wider text-roxo/60">
                                        Cardápio elaborado por
                                    </p>
                                    <p className="mt-1 font-bold text-gray-800">
                                        {cardapio.nutricionista.nome}
                                        {cardapio.nutricionista.crn
                                            ? ` · CRN ${cardapio.nutricionista.crn}`
                                            : ""}
                                    </p>
                                    <p className="text-sm text-gray-600">
                                        {cardapio.nutricionista.email}
                                        {cardapio.nutricionista.cpf
                                            ? ` · CPF ${cardapio.nutricionista.cpf}`
                                            : ""}
                                    </p>
                                </div>
                            )}

                            <div className="mt-6 flex flex-col gap-6">
                                {cardapio.refeicoes.length === 0 && (
                                    <p className="text-sm text-gray-500">
                                        Nenhuma refeição neste cardápio.
                                    </p>
                                )}

                                {cardapio.refeicoes.map((refeicao) => (
                                    <div
                                        key={`${refeicao.ordem}-${refeicao.nome}`}
                                        className="rounded-2xl border border-[#fce9d3] bg-white p-4"
                                    >
                                        <h3 className="font-bold text-lg text-gray-800">
                                            {refeicao.nome}
                                        </h3>

                                        <table className="mt-3 w-full">
                                            <thead>
                                                <tr className="h-10 bg-bege">
                                                    <th className="px-3 text-left font-light">
                                                        Alimento
                                                    </th>
                                                    <th className="px-3 text-left font-light">
                                                        Gramas
                                                    </th>
                                                    <th className="px-3 text-left font-light">kcal</th>
                                                    <th className="px-3 text-left font-light">
                                                        Proteínas
                                                    </th>
                                                    <th className="px-3 text-left font-light">
                                                        Carboidratos
                                                    </th>
                                                    <th className="px-3 text-left font-light">
                                                        Gorduras
                                                    </th>
                                                </tr>
                                            </thead>

                                            <tbody>
                                                {refeicao.itens.map((item) => (
                                                    <tr
                                                        key={`${item.alimentoId}`}
                                                        className="border-t border-[#fce9d3] h-10"
                                                    >
                                                        <td className="px-3">{item.alimento}</td>
                                                        <td className="px-3">{item.gramas} g</td>
                                                        <td className="px-3">
                                                            {item.valores.caloriasKcal}
                                                        </td>
                                                        <td className="px-3">
                                                            {item.valores.proteinasG} g
                                                        </td>
                                                        <td className="px-3">
                                                            {item.valores.carboidratosG} g
                                                        </td>
                                                        <td className="px-3">
                                                            {item.valores.gordurasG} g
                                                        </td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>

                                        <p className="mt-3 text-right text-sm text-gray-600">
                                            Total da refeição:{" "}
                                            <span className="font-semibold text-roxo">
                                                {refeicao.total.caloriasKcal} kcal
                                            </span>
                                        </p>
                                    </div>
                                ))}
                            </div>

                            <p className="mt-6 text-right text-lg">
                                Total do cardápio:{" "}
                                <span className="font-bold text-roxo">
                                    {cardapio.total.caloriasKcal} kcal
                                </span>
                            </p>
                        </>
                    )}
                </div>

                {cardapio && compatibilidade && (
                    <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                        <h2 className="font-bold text-2xl">Compatibilidade</h2>

                        <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                        <p className="text-sm text-gray-600">
                            {compatibilidade.totalAlunosAnalisados === 0
                                ? "Nenhum aluno na unidade para analisar."
                                : `${compatibilidade.totalAlunosAnalisados} aluno(s) analisado(s).`}
                        </p>

                        {compatibilidade.avisos.map((aviso) => (
                            <p key={aviso} className="mt-2 text-sm text-amber-700">
                                {aviso}
                            </p>
                        ))}

                        {compatibilidade.bloqueantes.length > 0 && (
                            <div className="mt-4">
                                <h3 className="font-semibold text-red-700">
                                    {compatibilidade.bloqueantes.length} bloqueante(s)
                                </h3>
                                <ul className="mt-2 list-disc pl-6 text-sm text-gray-700">
                                    {compatibilidade.bloqueantes.map((achado) => (
                                        <li key={`${achado.alimentoId}-${achado.restricao}`}>
                                            {achado.refeicao} · {achado.alimento} — {achado.restricao}
                                            {achado.alunos.length > 0 &&
                                                ` (${achado.alunos.map((aluno) => aluno.nome).join(", ")})`}
                                        </li>
                                    ))}
                                </ul>
                            </div>
                        )}

                        {compatibilidade.alertas.length > 0 && (
                            <div className="mt-4">
                                <h3 className="font-semibold text-amber-700">
                                    {compatibilidade.alertas.length} alerta(s)
                                </h3>
                                <ul className="mt-2 list-disc pl-6 text-sm text-gray-700">
                                    {compatibilidade.alertas.map((achado) => (
                                        <li key={`${achado.alimentoId}-${achado.restricao}`}>
                                            {achado.refeicao} · {achado.alimento} — {achado.restricao}
                                        </li>
                                    ))}
                                </ul>
                            </div>
                        )}

                        <div className="mt-4 grid gap-4 md:grid-cols-2">
                            <div>
                                <h3 className="font-semibold text-green-700">Permitidos</h3>
                                <p className="mt-1 text-sm text-gray-700">
                                    {compatibilidade.alimentosPermitidos.join(", ") || "—"}
                                </p>
                            </div>
                            <div>
                                <h3 className="font-semibold text-red-700">Não permitidos</h3>
                                <p className="mt-1 text-sm text-gray-700">
                                    {compatibilidade.alimentosNaoPermitidos.join(", ") || "—"}
                                </p>
                            </div>
                        </div>
                    </div>
                )}

                {cardapio && historico.length > 0 && (
                    <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                        <h2 className="font-bold text-2xl">Histórico</h2>

                        <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                        <ul className="flex flex-col gap-2">
                            {historico.map((evento) => (
                                <li
                                    key={`${evento.evento}-${evento.criadoEm}`}
                                    className="flex items-center justify-between rounded-2xl border border-[#fce9d3] p-3"
                                >
                                    <div>
                                        <p className="font-semibold text-gray-800">
                                            {evento.evento} · v{evento.versao}
                                        </p>
                                        <p className="text-sm text-gray-500">por {evento.autor}</p>
                                        {evento.motivo && (
                                            <p className="text-sm text-gray-600">Motivo: {evento.motivo}</p>
                                        )}
                                    </div>
                                    <span className="text-sm text-gray-400">
                                        {new Date(evento.criadoEm).toLocaleString("pt-BR")}
                                    </span>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}

                {cardapio && (
                    <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                        <h2 className="font-bold text-2xl">Duplicar para outro período</h2>

                        <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                        <div className="flex flex-wrap gap-4">
                            <input
                                type="date"
                                value={novoInicio}
                                onChange={(evento) => setNovoInicio(evento.target.value)}
                                aria-label="Novo início"
                                className="rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                            />

                            <input
                                type="date"
                                value={novoFim}
                                onChange={(evento) => setNovoFim(evento.target.value)}
                                aria-label="Novo fim"
                                className="rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                            />
                        </div>

                        <div className="mt-4 flex justify-center gap-4">
                            <Button
                                type="button"
                                variant="ver"
                                onClick={duplicar}
                                disabled={duplicando}
                            >
                                <span className="flex items-center gap-2">
                                    <Copy size={18} />
                                    {duplicando ? "Duplicando..." : "Duplicar cardápio"}
                                </span>
                            </Button>
                        </div>
                    </div>
                )}
            </div>

            <Footer />
        </div>
    );
}
