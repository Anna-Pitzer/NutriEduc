import { Users, UserRound, Utensils, ChevronRight, CircleUserRound } from "lucide-react"
import { Link } from "react-router-dom";

export default function Navbar() {

    const opcoes = [
        {
            nome: "Perfil",
            caminho: "/Perfil",
            icone: UserRound
        },
        {
            nome: "Cadastro de alunos com restrição",
            caminho: "/cadastro",
            icone: Users,
        },
        {
            nome: "Cardápio",
            caminho: "/cardapio",
            icone: Utensils,
        },
        {
            nome: "Vizualizar Alunos",
            caminho: "/vizualunos",
            icone: CircleUserRound,
        }
    ];

    return (
        <aside className="relative z-30  w-auto px-5">
            <div className="rounded-2xl border border-[#E8E8E8]/10 bg-[rgba(214,211,205,0.07)] p-3 shadow-[0_8px_30px_rgba(11,102,25,0.08)] backdrop-blur-[1px] mt-10">
                <div className="m-3 px-3 py-2">
                    <p className="text-xs font-semibold uppercase tracking-wider text-roxo/60">Menu Principal</p>
                </div>

                <nav className="flex flex-col gap-1.5">
                    {opcoes.map((opcao) => {
                        const Icon = opcao.icone;

                        return (
                            <Link
                                key={opcao.caminho}
                                to={opcao.caminho}
                                className="group flex items-center gap-3 rounded-xl px-3 py-3 text-sm font-semibold text-roxo transition-all duration-200 hover:bg-[rgba(172,178,173,0.07)] hover:text-amarelo hover:shadow-sm"
                            >
                                <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-white text-roxo shadow-sm transition-all duration-200 group-hover:bg-roxo group-hover:text-white">
                                    <Icon size={20} strokeWidth={2} />
                                </span>

                                <span className="flex-1 whitespace-nowrap">
                                    {opcao.nome}
                                </span>

                                <ChevronRight
                                    size={17}
                                    className="opacity-0 -translate-x-1 text-verdeClaro transition-all duration-200 group-hover:translate-x-0 group-hover:opacity-100"
                                />
                            </Link>
                        );
                    })}
                </nav>
            </div>
        </aside>
    );
}