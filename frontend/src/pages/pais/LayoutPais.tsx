import { NavLink, Outlet } from "react-router-dom";
import Header from "../../components/Header";
import Navbar from "../../components/Navbar";
import Footer from "../../components/Footer";

const abas = [
    { rotulo: "Início", caminho: "/pais", fim: true },
    { rotulo: "Cardápios", caminho: "/pais/cardapios", fim: false },
    { rotulo: "Ocorrências", caminho: "/pais/ocorrencias", fim: false },
    { rotulo: "Solicitações", caminho: "/pais/solicitacoes", fim: false },
    { rotulo: "Feedback", caminho: "/pais/feedback", fim: false },
];

/**
 * Layout da área dos pais (Spec 035, RF28). Reutiliza o cabeçalho (que já contém o
 * `SinoNotificacoes`), o menu e o rodapé existentes, acrescentando as abas do portal.
 */
export function LayoutPais() {
    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <div className="relative z-20 flex min-h-screen flex-col">
                <Header />

                <div className="mx-auto flex w-full max-w-7xl flex-1 gap-6 px-4 pb-8 md:px-8">
                    <Navbar />

                    <main className="min-w-0 flex-1 rounded-2xl border border-[#E8E8E8]/20 bg-[rgba(214,211,205,0.07)] p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)] backdrop-blur-[1px]">
                        <nav aria-label="Área dos pais" className="mb-6 flex flex-wrap gap-2">
                            {abas.map((aba) => (
                                <NavLink
                                    key={aba.caminho}
                                    to={aba.caminho}
                                    end={aba.fim}
                                    className={({ isActive }) =>
                                        `rounded-xl px-4 py-2 text-sm font-semibold transition focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40 ${
                                            isActive
                                                ? "bg-roxo text-white shadow-sm"
                                                : "bg-white text-roxo hover:bg-roxo/10"
                                        }`
                                    }
                                >
                                    {aba.rotulo}
                                </NavLink>
                            ))}
                        </nav>

                        <Outlet />
                    </main>
                </div>

                <Footer />
            </div>
        </div>
    );
}
