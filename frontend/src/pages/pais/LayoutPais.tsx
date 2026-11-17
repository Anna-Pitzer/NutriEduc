import { NavLink, Outlet } from "react-router-dom";
import Header from "../../components/Header";
import Footer from "../../components/Footer";

const abas = [
    { rotulo: "Início", caminho: "/pais", fim: true },
    { rotulo: "Cardápios", caminho: "/pais/cardapios", fim: false },
    { rotulo: "Relatório", caminho: "/pais/relatorio", fim: false },
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
        <section className=" bg-fundo gap-20">
            <div className=" flex min-h-screen flex-col">
                <Header />


                <div className="
                w-full mx-auto max-w-250
                mb-10
                px-4 pb-8 md:px-8 
                duration-500 hover:-translate-y-1.5
                bg-white border-4 border-lilaz rounded-2xl p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)]
                ">
                    <div className="">
                        <nav aria-label="Área dos pais" className="mb-6 flex justify-center flex-wrap gap-2 w-full" >
                            {abas.map((aba) => (
                                <NavLink
                                    key={aba.caminho}
                                    to={aba.caminho}
                                    end={aba.fim}
                                    className={({ isActive }) =>
                                        `rounded-xl px-4 py-2 text-sm font-semibold transition focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40 ${isActive
                                            ? "bg-roxo text-white shadow-sm"
                                            : "bg-white text-roxo hover:bg-roxo/10"
                                        }`
                                    }
                                >
                                    {aba.rotulo}
                                </NavLink>
                            ))}
                        </nav>

                    </div>


                    <Outlet />

                </div>
            </div>

            <Footer />
        </section>
    );
}
