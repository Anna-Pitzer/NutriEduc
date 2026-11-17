import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { AlertTriangle, ChevronLeft, Copy, Pencil, Trash } from "lucide-react";
import Button from "../../../components/Button";
import Footer from "../../../components/Footer";
import Header from "../../../components/Header";
import { useAuth } from "../../../context/authContext";
import {
    adaptarCardapio,
    cancelarCardapio,
    criarRevisaoCardapio,
    duplicarCardapio,
    excluirCardapio,
    listarAdaptados,
    obterCardapio,
    obterCompatibilidade,
    obterHistoricoCardapio,
    publicarCardapio,
    type Cardapio,
    type CardapioAdaptadoResumo,
    type Compatibilidade,
    type EventoHistoricoCardapio,
    type SituacaoCardapio,
    type StatusAdaptacao,
} from "../../../services/CardapioApi";

const corSituacao: Record<SituacaoCardapio, string> = {
    Rascunho: "bg-bege text-roxo",
    Publicado: "bg-green-100 text-green-800",
    Cancelado: "bg-red-100 text-red-700",
    Substituido: "bg-gray-100 text-gray-600",
};

const corStatusAdaptacao: Record<StatusAdaptacao, string> = {
    Atualizado: "bg-green-100 text-green-800",
    Desatualizado: "bg-amber-100 text-amber-800",
    PendenteDeRevisao: "bg-red-100 text-red-700",
};

function rotuloStatus(status: StatusAdaptacao | null): string {
    switch (status) {
        case "PendenteDeRevisao":
            return "Pendente de revisão";
        case "Desatualizado":
            return "Desatualizado";
        case "Atualizado":
            return "Atualizado";
        default:
            return "—";
    }
}

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
    const { userLogin } = useAuth();
    const ehNutricionista = userLogin?.papel === "Nutricionista";

    const [cardapio, setCardapio] = useState<Cardapio | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState<string | null>(null);
    const [novoInicio, setNovoInicio] = useState("");
    const [novoFim, setNovoFim] = useState("");
    const [duplicando, setDuplicando] = useState(false);
    const [mensagem, setMensagem] = useState<string | null>(null);
    const [compatibilidade, setCompatibilidade] = useState<Compatibilidade | null>(null);
    const [historico, setHistorico] = useState<EventoHistoricoCardapio[]>([]);
    const [adaptados, setAdaptados] = useState<CardapioAdaptadoResumo[]>([]);
    const [adaptando, setAdaptando] = useState(false);

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

    useEffect(() => {
        async function carregarAdaptados() {
            if (cardapio?.tipo !== "Geral") {
                setAdaptados([]);
                return;
            }

            try {
                setAdaptados(await listarAdaptados(cardapio.id));
            } catch {
                setAdaptados([]);
            }
        }

        carregarAdaptados();
    }, [cardapio?.id, cardapio?.tipo]);

    async function readaptar(escolaId: number) {
        if (!cardapio) return;

        setErro(null);
        setAdaptando(true);

        try {
            setAdaptados(await adaptarCardapio(cardapio.id, { escolaId }));
            setMensagem("Versão adaptada recalculada para a escola.");
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Erro ao adaptar o cardápio");
        } finally {
            setAdaptando(false);
        }
    }

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

                            <p className="mt-1 flex flex-wrap items-center gap-2 text-sm text-gray-600">
                                <span className="rounded-full bg-bege px-3 py-1 text-xs font-semibold text-roxo">
                                    {cardapio.tipo === "Geral" ? "Cardápio geral" : "Cardápio adaptado"}
                                </span>

                                {cardapio.tipo === "Geral" && (
                                    <span>Vale para todas as escolas desta unidade.</span>
                                )}

                                {cardapio.tipo === "Adaptado" && (
                                    <>
                                        <span>Escola #{cardapio.escolaId}</span>
                                        <span>·</span>
                                        <span>Conjunto {cardapio.conjuntoRestricoesChave}</span>
                                        {cardapio.origemId && (
                                            <>
                                                <span>·</span>
                                                <button
                                                    type="button"
                                                    onClick={() => navigate(`/cardapios/${cardapio.origemId}`)}
                                                    className="text-roxo underline hover:text-purple-700"
                                                >
                                                    Ver cardápio de origem
                                                </button>
                                            </>
                                        )}
                                    </>
                                )}
                            </p>

                            {cardapio.tipo === "Adaptado" && cardapio.restricoes.length > 0 && (
                                <div className="mt-3 flex flex-wrap items-center gap-2">
                                    <span className="text-sm text-gray-600">Restrições do conjunto:</span>
                                    {cardapio.restricoes.map((restricao) => (
                                        <span
                                            key={restricao.id}
                                            className="rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-800"
                                            title={restricao.categoria}
                                        >
                                            {restricao.nome}
                                        </span>
                                    ))}
                                </div>
                            )}

                            {cardapio.tipo === "Adaptado" && (
                                <div className="mt-3 flex flex-wrap items-center gap-3">
                                    <span
                                        className={`rounded-full px-3 py-1 text-sm ${
                                            cardapio.statusAdaptacao
                                                ? corStatusAdaptacao[cardapio.statusAdaptacao]
                                                : "bg-gray-100 text-gray-600"
                                        }`}
                                    >
                                        {rotuloStatus(cardapio.statusAdaptacao)}
                                    </span>

                                    {(cardapio.statusAdaptacao === "PendenteDeRevisao" ||
                                        cardapio.statusAdaptacao === "Desatualizado") && (
                                        <span className="text-sm text-red-700">
                                            Esta versão não pode ser publicada nem servida até ser
                                            readaptada.
                                        </span>
                                    )}
                                </div>
                            )}

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

                {cardapio && cardapio.substituicoes.length > 0 && (
                    <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                        <h2 className="font-bold text-2xl">Substituições aplicadas</h2>

                        <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                        <div className="overflow-hidden rounded-2xl border border-[#fce9d3]">
                            <table className="w-full bg-white">
                                <thead>
                                    <tr className="h-10 bg-bege">
                                        <th className="px-3 text-left font-light">Refeição</th>
                                        <th className="px-3 text-left font-light">Original</th>
                                        <th className="px-3 text-left font-light">Substituto</th>
                                        <th className="px-3 text-left font-light">Gramas</th>
                                        <th className="px-3 text-left font-light">Restrições</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {cardapio.substituicoes.map((substituicao, posicao) => (
                                        <tr
                                            key={`${substituicao.refeicaoOrdem}-${substituicao.alimentoOriginalId}-${posicao}`}
                                            className="border-t border-[#fce9d3] h-10"
                                        >
                                            <td className="px-3">{substituicao.refeicaoNome}</td>
                                            <td className="px-3">{substituicao.alimentoOriginal}</td>
                                            <td className="px-3">{substituicao.alimentoSubstituto}</td>
                                            <td className="px-3">{substituicao.gramas} g</td>
                                            <td className="px-3">
                                                {substituicao.restricoesBloqueantes}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}

                {cardapio && cardapio.pendencias.length > 0 && (
                    <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                        <h2 className="flex items-center gap-2 font-bold text-2xl text-red-700">
                            <AlertTriangle size={22} />
                            Pendências da adaptação
                        </h2>

                        <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                        <ul className="flex flex-col gap-2">
                            {cardapio.pendencias.map((pendencia, posicao) => (
                                <li
                                    key={`${pendencia.refeicaoOrdem}-${pendencia.alimentoId ?? "refeicao"}-${posicao}`}
                                    className="rounded-2xl border border-red-200 bg-white p-3 text-sm"
                                >
                                    <p className="font-semibold text-gray-800">
                                        {pendencia.refeicaoNome}
                                        {pendencia.alimento ? ` · ${pendencia.alimento}` : ""}
                                    </p>
                                    <p className="text-red-700">{pendencia.motivo}</p>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}

                {cardapio && cardapio.confirmacoes.length > 0 && (
                    <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                        <h2 className="font-bold text-2xl">Confirmações de incompatibilidade</h2>

                        <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                        <ul className="flex flex-col gap-2">
                            {cardapio.confirmacoes.map((confirmacao, posicao) => (
                                <li
                                    key={`${confirmacao.alimentoId}-${confirmacao.criadoEm}-${posicao}`}
                                    className="rounded-2xl border border-amber-200 bg-white p-3 text-sm"
                                >
                                    <p className="font-semibold text-gray-800">
                                        {confirmacao.alimento}
                                    </p>
                                    <p className="text-gray-600">
                                        Escolas: {confirmacao.escolas} · Conjuntos:{" "}
                                        {confirmacao.conjuntos}
                                    </p>
                                    <p className="text-gray-500">
                                        {confirmacao.autor} ·{" "}
                                        {new Date(confirmacao.criadoEm).toLocaleString("pt-BR")}
                                    </p>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}

                {cardapio?.tipo === "Geral" && (
                    <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                        <h2 className="font-bold text-2xl">Versões adaptadas por escola</h2>

                        <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                        {adaptados.length === 0 && (
                            <p className="text-sm text-gray-500">
                                Nenhuma versão adaptada para este cardápio. Nenhum aluno com
                                restrição nesta unidade ou adaptação ainda não executada.
                            </p>
                        )}

                        {adaptados.length > 0 && (
                            <div className="overflow-hidden rounded-2xl border border-[#fce9d3]">
                                <table className="w-full bg-white">
                                    <thead>
                                        <tr className="h-10 bg-bege">
                                            <th className="px-3 text-left font-light">Escola</th>
                                            <th className="px-3 text-left font-light">Conjunto</th>
                                            <th className="px-3 text-left font-light">Status</th>
                                            <th className="px-3 text-left font-light">Situação</th>
                                            <th className="px-3 text-left font-light">Substituições</th>
                                            <th className="px-3 text-left font-light">Pendências</th>
                                            <th className="px-3 text-center font-light">Abrir</th>
                                            {ehNutricionista && (
                                                <th className="px-3 text-center font-light">
                                                    Readaptar
                                                </th>
                                            )}
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {adaptados.map((versao) => (
                                            <tr
                                                key={versao.id}
                                                className="border-t border-[#fce9d3] h-12"
                                            >
                                                <td className="px-3">{versao.escolaNome}</td>
                                                <td className="px-3">
                                                    {versao.conjuntoRestricoesChave}
                                                </td>
                                                <td className="px-3">
                                                    <span
                                                        className={`rounded-full px-2 py-0.5 text-xs ${
                                                            versao.statusAdaptacao
                                                                ? corStatusAdaptacao[versao.statusAdaptacao]
                                                                : "bg-gray-100 text-gray-600"
                                                        }`}
                                                    >
                                                        {rotuloStatus(versao.statusAdaptacao)}
                                                    </span>
                                                </td>
                                                <td className="px-3">{versao.situacao}</td>
                                                <td className="px-3">{versao.totalSubstituicoes}</td>
                                                <td
                                                    className={
                                                        versao.totalPendencias > 0
                                                            ? "px-3 font-semibold text-red-700"
                                                            : "px-3"
                                                    }
                                                >
                                                    {versao.totalPendencias}
                                                </td>
                                                <td className="px-3 text-center">
                                                    <button
                                                        type="button"
                                                        onClick={() =>
                                                            navigate(`/cardapios/${versao.id}`)
                                                        }
                                                        className="text-roxo underline hover:text-purple-700"
                                                    >
                                                        Abrir
                                                    </button>
                                                </td>
                                                {ehNutricionista && (
                                                    <td className="px-3 text-center">
                                                        <button
                                                            type="button"
                                                            onClick={() => void readaptar(versao.escolaId)}
                                                            disabled={adaptando}
                                                            className="text-roxo underline hover:text-purple-700 disabled:cursor-not-allowed disabled:text-gray-400"
                                                        >
                                                            Recalcular
                                                        </button>
                                                    </td>
                                                )}
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
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
