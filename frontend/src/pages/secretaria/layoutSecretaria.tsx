import type { ReactNode } from "react";
import { NavLink } from "react-router-dom";
import Footer from "../../components/Footer";
import Header from "../../components/Header";
import Navbar from "../../components/Navbar";

const abas = [
    { rotulo: "Painel", caminho: "/secretaria", fim: true },
    { rotulo: "Escolas", caminho: "/secretaria/escolas", fim: false },
    { rotulo: "Unidades", caminho: "/secretaria/unidades", fim: false },
    { rotulo: "Usuários", caminho: "/secretaria/usuarios", fim: false },
    { rotulo: "Solicitações de acesso", caminho: "/secretaria/solicitacoes-acesso", fim: false },
];

/**
 * Casca comum das telas de secretaria: mesma estrutura de `HomeNutricao`
 * (Header + Navbar + conteúdo + Footer) mais as abas da área.
 */
export function LayoutSecretaria({ children }: { children: ReactNode }) {
    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <div className="relative z-20 flex min-h-screen flex-col">
                <Header />

                <div className="mx-auto flex w-full max-w-350 flex-1 gap-6 px-4 pb-8 md:px-8">
                    <Navbar />

                    <main className="min-w-0 flex-1 rounded-2xl border border-[#E8E8E8]/20 bg-[rgba(214,211,205,0.07)] p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)] backdrop-blur-[1px]">
                        <nav aria-label="Áreas da secretaria" className="mb-6 flex flex-wrap gap-2">
                            {abas.map((aba) => (
                                <NavLink
                                    key={aba.caminho}
                                    to={aba.caminho}
                                    end={aba.fim}
                                    className={({ isActive }) =>
                                        `rounded-xl px-4 py-2 text-sm font-medium transition-colors ${isActive
                                            ? "bg-roxo text-white"
                                            : "bg-white text-gray-600 hover:bg-gray-100"
                                        }`
                                    }
                                >
                                    {aba.rotulo}
                                </NavLink>
                            ))}
                        </nav>

                        {children}
                    </main>
                </div>

                <Footer />
            </div>
        </div>
    );
}
