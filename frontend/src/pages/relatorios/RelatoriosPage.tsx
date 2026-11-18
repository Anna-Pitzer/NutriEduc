import { useCallback, useEffect, useState } from "react";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import Button from "../../components/Button";
import {
    exportarRelatorio,
    previsualizarRelatorio,
    type FormatoDeExportacao,
    type RelatorioPreview,
    type TipoDeRelatorio,
} from "../../services/RelatorioApi";
import VoltarPaginas from "../../components/VoltarPaginas";

const TIPOS: { valor: TipoDeRelatorio; rotulo: string }[] = [
    { valor: "Cardapios", rotulo: "Cardápios" },
    { valor: "AlunosRestricoes", rotulo: "Alunos e restrições" },
    { valor: "Ocorrencias", rotulo: "Ocorrências" },
    { valor: "SolicitacoesFeedbacks", rotulo: "Solicitações e feedbacks" },
    { valor: "IndicadoresAdministrativos", rotulo: "Indicadores administrativos" },
];

/**
 * Tela de relatórios (Spec 019, RF12): escolhe o relatório, o período, mostra a
 * pré-visualização e baixa em XLSX ou CSV. Nenhum total é recalculado aqui.
 */
export function RelatoriosPage() {
    const [tipo, setTipo] = useState<TipoDeRelatorio>("Cardapios");
    const [de, setDe] = useState("");
    const [ate, setAte] = useState("");
    const [preview, setPreview] = useState<RelatorioPreview | null>(null);
    const [carregando, setCarregando] = useState(false);
    const [exportando, setExportando] = useState(false);
    const [erro, setErro] = useState<string | null>(null);
    const [mensagem, setMensagem] = useState<string | null>(null);

    const carregar = useCallback(async () => {
        setCarregando(true);
        setErro(null);

        try {
            const resultado = await previsualizarRelatorio(tipo, {
                de: de || undefined,
                ate: ate || undefined,
            });

            setPreview(resultado);
        } catch (falha) {
            setPreview(null);
            setErro(falha instanceof Error ? falha.message : "Erro ao carregar o relatório");
        } finally {
            setCarregando(false);
        }
    }, [tipo, de, ate]);

    useEffect(() => {
        void carregar();
    }, [carregar]);

    async function exportar(formato: FormatoDeExportacao) {
        setExportando(true);
        setErro(null);
        setMensagem(null);

        try {
            await exportarRelatorio(
                tipo,
                {
                    de: de || undefined,
                    ate: ate || undefined,
                },
                formato,
            );

            setMensagem(`Exportação ${formato.toUpperCase()} gerada.`);
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Erro ao exportar o relatório");
        } finally {
            setExportando(false);
        }
    }

    return (
        <div className="relative min-h-screen bg-fundo">
            <Header />


            <main className=" 
            max-w-7xl mx-auto mb-10
             bg-white border-4 border-lilaz rounded-3xl p-8 shadow-[0_8px_30px_rgba(11,102,25,0.08)] 
            duration-300 hover:-translate-y-1.5">
                <header className="flex mb-6">
                    <div>
                        <VoltarPaginas />
                    </div>

                    <div>
                        <h1 className="text-2xl font-bold text-verdeClaro">Relatórios</h1>
                        <p className="mt-1 text-sm text-slate-600">
                            Area de recebimento dos feedbacks da plataforma.
                        </p>

                    </div>

                </header>
                {/* divisor */}
                <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                <section className="rounded-3xl bg-white p-6 ">
                    <div className="grid gap-4 md:grid-cols-4">
                        <div className="flex flex-col md:col-span-2">
                            <label htmlFor="tipoRelatorio">Relatório</label>
                            <select
                                id="tipoRelatorio"
                                value={tipo}
                                onChange={(evento) => setTipo(evento.target.value as TipoDeRelatorio)}
                                className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm"
                            >
                                {TIPOS.map((opcao) => (
                                    <option key={opcao.valor} value={opcao.valor}>
                                        {opcao.rotulo}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div className="flex flex-col">
                            <label htmlFor="de">De</label>
                            <input
                                id="de"
                                type="date"
                                value={de}
                                onChange={(evento) => setDe(evento.target.value)}
                                className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm"
                            />
                        </div>

                        <div className="flex flex-col">
                            <label htmlFor="ate">Até</label>
                            <input
                                id="ate"
                                type="date"
                                value={ate}
                                onChange={(evento) => setAte(evento.target.value)}
                                className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm"
                            />
                        </div>
                    </div>

                    <div className="mt-5 flex flex-wrap items-center gap-3">
                        <Button type="button" onClick={() => void carregar()} disabled={carregando}>
                            {carregando ? "Carregando..." : "Atualizar prévia"}
                        </Button>

                        <Button
                            type="button"
                            onClick={() => void exportar("xlsx")}
                            disabled={exportando}
                        >
                            {exportando ? "Exportando..." : "Exportar XLSX"}
                        </Button>

                        <Button
                            type="button"
                            onClick={() => void exportar("csv")}
                            disabled={exportando}
                        >
                            {exportando ? "Exportando..." : "Exportar CSV"}
                        </Button>

                        {preview && (
                            <span className="text-sm text-gray-600">
                                {preview.totalLinhas} linha(s) no filtro
                                {preview.linhas.length < preview.totalLinhas
                                    ? ` — exibindo ${preview.linhas.length}`
                                    : ""}
                            </span>
                        )}
                    </div>

                    {erro && (
                        <p role="alert" className="mt-4 rounded-2xl bg-red-50 px-4 py-3 text-sm text-red-700">
                            {erro}
                        </p>
                    )}

                    {mensagem && (
                        <p className="mt-4 rounded-2xl bg-green-50 px-4 py-3 text-sm text-green-700">
                            {mensagem}
                        </p>
                    )}
                </section>

                {preview && (
                    <section className="overflow-hidden rounded-3xl bg-white ">
                        <div className="border-b border-gray-100 px-6 py-4">
                            <h2 className="text-xl font-bold">{preview.titulo}</h2>
                            {preview.rodape && (
                                <p className="mt-1 text-sm text-gray-600">{preview.rodape}</p>
                            )}
                        </div>

                        <div className="overflow-x-auto">
                            <table className="min-w-full text-left text-sm">
                                <thead className="bg-gray-50">
                                    <tr>
                                        {preview.colunas.map((coluna) => (
                                            <th key={coluna} className="whitespace-nowrap px-4 py-3 font-semibold">
                                                {coluna}
                                            </th>
                                        ))}
                                    </tr>
                                </thead>
                                <tbody>
                                    {preview.linhas.map((linha, indice) => (
                                        <tr key={indice} className="border-t border-gray-100">
                                            {linha.map((celula, coluna) => (
                                                <td key={coluna} className="px-4 py-3 align-top">
                                                    {celula}
                                                </td>
                                            ))}
                                        </tr>
                                    ))}

                                    {preview.linhas.length === 0 && (
                                        <tr>
                                            <td
                                                colSpan={Math.max(preview.colunas.length, 1)}
                                                className="px-4 py-6 text-center text-gray-500"
                                            >
                                                Nenhum registro no período.
                                            </td>
                                        </tr>
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </section>
                )}
            </main>

            <Footer />
        </div>
    );
}
