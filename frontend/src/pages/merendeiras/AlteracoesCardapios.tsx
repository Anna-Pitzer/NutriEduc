import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ChevronLeft } from "lucide-react";
import Footer from "../../components/Footer";
import Header from "../../components/Header";
import {
    listarAlteracoesCardapios,
    type AlteracaoCardapio,
} from "../../services/CardapioApi";
import { useAuth } from "../../context/authContext";
import { homePorPapel } from "../../routes/homePorPapel";

/**
 * Feed de alterações de cardápios (Spec 015, RF11/T10). A merendeira vê apenas a própria
 * unidade; a nutrição vê a rede inteira. O recorte é resolvido no backend.
 */
export function AlteracoesCardapios() {
    const navigate = useNavigate();
    const { userLogin } = useAuth();
    const inicio = userLogin?.papel ? homePorPapel[userLogin.papel] : "/";

    const [desde, setDesde] = useState(() => {
        const data = new Date();
        data.setDate(data.getDate() - 7);
        return data.toISOString().slice(0, 10);
    });

    const [alteracoes, setAlteracoes] = useState<AlteracaoCardapio[]>([]);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState<string | null>(null);

    useEffect(() => {
        async function carregar() {
            setCarregando(true);
            setErro(null);

            try {
                const pagina = await listarAlteracoesCardapios(`${desde}T00:00:00Z`);
                setAlteracoes(pagina.itens);
            } catch (falha) {
                setErro(falha instanceof Error ? falha.message : "Erro ao carregar as alterações");
            } finally {
                setCarregando(false);
            }
        }

        carregar();
    }, [desde]);

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <Header />

            <button
                type="button"
                onClick={() => navigate(inicio)}
                aria-label="Voltar ao início"
                className="group mx-auto flex w-full max-w-7xl cursor-pointer items-center justify-start"
            >
                <ChevronLeft
                    size={50}
                    className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <div className="mx-auto w-full max-w-7xl rounded-3xl bg-white p-8 shadow-[0_5px_20px_rgba(1,1,1,0.1)] mb-10">
                <div className="flex flex-wrap items-center justify-between gap-4">
                    <div>
                        <h1 className="font-bold text-2xl text-gray-800">Alterações de cardápios</h1>
                        <h2 className="text-lg text-gray-500">
                            Acompanhe o que mudou nos cardápios da sua unidade.
                        </h2>
                    </div>

                    <div className="flex flex-col">
                        <label htmlFor="inDesde">Desde</label>
                        <input
                            id="inDesde"
                            type="date"
                            value={desde}
                            onChange={(evento) => setDesde(evento.target.value)}
                            className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                        />
                    </div>
                </div>

                <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                {carregando && <p className="py-4 text-gray-500">Carregando alterações...</p>}
                {erro && (
                    <p role="alert" className="py-4 text-red-700">
                        {erro}
                    </p>
                )}
                {!carregando && !erro && alteracoes.length === 0 && (
                    <p className="py-4 text-gray-500">Nenhuma alteração no período.</p>
                )}

                {alteracoes.length > 0 && (
                    <ul className="flex flex-col gap-2">
                        {alteracoes.map((alteracao) => (
                            <li
                                key={alteracao.id}
                                className="flex items-center justify-between rounded-2xl border border-[#fce9d3] p-4"
                            >
                                <div>
                                    <p className="font-semibold text-gray-800">
                                        {alteracao.cardapioNome} · {alteracao.unidadeNome}
                                    </p>
                                    <p className="text-sm text-gray-500">
                                        {alteracao.evento} — v{alteracao.versao} por {alteracao.autor}
                                    </p>
                                    {alteracao.motivo && (
                                        <p className="text-sm text-gray-600">Motivo: {alteracao.motivo}</p>
                                    )}
                                </div>
                                <span className="text-sm text-gray-400">
                                    {new Date(alteracao.criadoEm).toLocaleString("pt-BR")}
                                </span>
                            </li>
                        ))}
                    </ul>
                )}
            </div>

            <Footer />
        </div>
    );
}
