import { Users, UserRound, Utensils, ChevronRight, CircleUserRound, Building2, School, Apple, HeartHandshake, Salad, ClipboardList, TriangleAlert, MessageSquare } from "lucide-react"
import { Link } from "react-router-dom";
import { useAuth, type PapelUsuario } from "../context/authContext";

type OpcaoMenu = {
    nome: string;
    caminho: string;
    icone: typeof UserRound;
    papeis: PapelUsuario[];
};

export default function Navbar() {
    const { userLogin } = useAuth();

    const opcoes: OpcaoMenu[] = [
        {
            nome: "Cadastro de alunos com restrição",
            caminho: "/aluno/registrar",
            icone: Users,
            papeis: ["Nutricionista", "AdminSecretaria"],
        },
        {
            nome: "Catálogo de alimentos",
            caminho: "/alimentos",
            icone: Salad,
            papeis: ["Nutricionista", "Merendeira"],
        },
        {
            nome: "Cardápio",
            caminho: "/cardapios",
            icone: Utensils,
            papeis: ["Nutricionista", "AdminSecretaria", "Merendeira"],
        },
        {
            nome: "Vizualizar Alunos",
            caminho: "/aluno/vizualizar",
            icone: CircleUserRound,
            papeis: ["Nutricionista", "AdminSecretaria"],
        },
        {
            nome: "Solicitações",
            caminho: "/solicitacoes",
            icone: ClipboardList,
            papeis: ["Nutricionista", "AdminSecretaria", "AdminEscola", "Merendeira"],
        },
        {
            nome: "Ocorrências",
            caminho: "/ocorrencias",
            icone: TriangleAlert,
            papeis: ["Nutricionista", "AdminSecretaria", "AdminEscola", "Merendeira"],
        },
        {
            nome: "Feedback",
            caminho: "/feedbacks",
            icone: MessageSquare,
            papeis: ["Nutricionista", "AdminSecretaria", "AdminEscola", "Merendeira"],
        },
        {
            nome: "Merendeiras",
            caminho: "/merendeiras",
            icone: Apple,
            papeis: ["Merendeira"],
        },
        {
            nome: "Pais",
            caminho: "/pais",
            icone: HeartHandshake,
            papeis: ["Responsavel"],
        },
        {
            nome: "Secretaria",
            caminho: "/secretaria",
            icone: Building2,
            papeis: ["AdminSecretaria"],
        },
        {
            nome: "Escolas",
            caminho: "/escolas",
            icone: School,
            papeis: ["AdminEscola"],
        },
    ];

    // Conveniência de navegação: o backend recusa requisição fora do papel (RF10).
    const visiveis = opcoes.filter(
        (opcao) => userLogin?.papel && opcao.papeis.includes(userLogin.papel)
    );

    return (
        <aside className="relative w-auto px-5 
        duration-300
        hover:-translate-y-1.5 ">
            <div className="rounded-2xl border-4 border-lilaz  bg-white p-3 shadow-[0_8px_30px_rgba(11,102,25,0.08)] backdrop-blur-[1px]">
                <div className=" px-3 py-2 bg-white rounded rounded-b-none">
                    <p className="text-xs font-semibold uppercase tracking-wider text-roxo/60 text-center">Menu Principal</p>
                </div>

                <nav className="flex flex-col gap-1.5 bg-white rounded rounded-t-none">
                    {visiveis.map((opcao) => {
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