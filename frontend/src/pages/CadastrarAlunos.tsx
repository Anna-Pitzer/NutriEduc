import Footer from "../components/Footer";
import Header from "../components/Header";
import { ChevronLeft, UserRound, Users, Utensils, Settings } from "lucide-react";


export function CadastroAlunos() {

    const opcoes = [
        {
            nome: "Integral",
            caminho: "/perfil",
            icone: UserRound
        },
        {
            nome: "Intolerância à Lactose",
            caminho: "/cadastro",
            icone: Users,
        },
        {
            nome: "APVL",
            caminho: "/cardapio",
            icone: Utensils,
        },
        {
            nome: "Diabétes",
            caminho: "/configuracoes",
            icone: Settings,
        },
        {
            nome: "Intolerância Glúten",
            caminho: "/configuracoes",
            icone: Settings,
        },
        {
            nome: "Outros",
            caminho: "/configuracoes",
            icone: Settings,
        }
    ];

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">

            <Header />

            <button
                type="button"
                onClick={() => window.history.back()}
                className="group flex items-center justify-center cursor-pointer mx-10"
            >
                <ChevronLeft
                    size={50}
                    className="text-[#8F6BC8] transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <div className="flex flex-col mx-30 mb-10">

                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]  gap-y-4">

                    <h2 className="font-bold text-2xl">Informações do aluno</h2>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    <div className="flex gap-4">
                        <input type="text" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-[#8F6BC8] focus:ring-4 focus:ring-[#8F6BC8]/10 my-2" placeholder="Nome Completo" />
                        <input type="text" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-[#8F6BC8] focus:ring-4 focus:ring-[#8F6BC8]/10 my-2" placeholder="Matrícula" />
                    </div>

                    <div className="flex gap-4">
                        <input type="number" name="" id="" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-[#8F6BC8] focus:ring-4 focus:ring-[#8F6BC8]/10 my-2" placeholder="Idade" />
                        <select className="w-full rounded-2xl border border-gray-200 bg-white  text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 
                            hover:bg-gray-200 focus:border-[border] focus:ring-4 focus:ring-[#8F6BC8]/10 cursor-pointer px-3">
                            {/*PUXAR DO BACK*/}
                            <option>Série</option>
                            <option>1 ano</option>
                            <option>2 ano</option>
                            <option>3 ano</option>
                            <option>4 ano</option>
                            <option>5 ano</option>
                            <option>6 ano</option>
                            <option>7 ano</option>
                            <option>8 ano</option>
                            <option>9 ano</option>
                        </select>
                        <select className="w-full rounded-2xl border border-gray-200 bg-white  text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 
                            hover:bg-gray-200 focus:border-[border] focus:ring-4 focus:ring-[#8F6BC8]/10 cursor-pointer px-3">
                            {/*PUXAR DO BACK*/}
                            <option>Escola</option>
                            <option>1 ano</option>
                            <option>2 ano</option>
                            <option>3 ano</option>
                            <option>4 ano</option>
                            <option>5 ano</option>
                            <option>6 ano</option>
                            <option>7 ano</option>
                            <option>8 ano</option>
                            <option>9 ano</option>
                        </select>
                    </div>

                    <div>
                        <input type="tel" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-[#8F6BC8] focus:ring-4 focus:ring-[#8F6BC8]/10 my-2" placeholder="Telefone" />
                        <textarea name="" id="" cols={30} rows={10} className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-[#8F6BC8] focus:ring-4 focus:ring-[#8F6BC8]/10 my-2" placeholder="Observações importantes"></textarea>
                    </div>

                </div>

                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]  gap-y-4">

                    <div className="w-full flex flex-col items-center">

                        <div>
                            <h2 className="text-xl font-bold text-gray-800 mb-4">
                                Restrições Alimentares
                            </h2>

                            <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                            <div className="grid grid-cols-2 gap-x-8 gap-y-3 w-full">
                                {opcoes.map((opcao) => {
                                    const Icon = opcao.icone;

                                    return (
                                        <a
                                            key={opcao.caminho}
                                            href={opcao.caminho}
                                            className="group flex items-center gap-3 rounded-xl px-3 py-3 text-sm font-semibold text-[#8F6BC8] transition-all duration-200 hover:bg-[rgba(172,178,173,0.07)] hover:text-[#E6C229] hover:shadow-sm"
                                        >
                                            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-white text-[#8F6BC8] shadow-sm transition-all duration-200 group-hover:bg-[#8F6BC8] group-hover:text-white">
                                                <Icon size={20} strokeWidth={2} />
                                            </span>

                                            <span className="whitespace-nowrap">
                                                {opcao.nome}
                                            </span>
                                        </a>
                                    );
                                })}
                            </div>
                        </div>
                    </div>
                    {/* Alergia */}
                        <div>
                            <h3 className="text-xl font-bold text-gray-800 mb-4">
                                Alergia Severa/Risco de Anafilaxia
                            </h3>

                            <div className="flex gap-4">

                                {/* SIM */}
                                <label className="cursor-pointer">
                                    <input
                                        type="radio"
                                        name="alergia"
                                        value="sim"
                                        className="peer sr-only"
                                    />

                                    <div className="flex items-center justify-center rounded-2xl border-2 border-gray-200 bg-white px-8 py-4 font-semibold text-gray-600 transition-all duration-200 hover:border-[#8F6BC8] hover:bg-[#8F6BC8]/5 peer-checked:border-[#8F6BC8] peer-checked:bg-[#8F6BC8] peer-checked:text-white">
                                        Sim
                                    </div>
                                </label>

                                {/* NÃO */}
                                <label className="cursor-pointer">
                                    <input
                                        type="radio"
                                        name="alergia"
                                        value="nao"
                                        className="peer sr-only"
                                    />

                                    <div className="flex items-center justify-center rounded-2xl border-2 border-gray-200 bg-white px-8 py-4 font-semibold text-gray-600 transition-all duration-200 hover:border-[#8F6BC8] hover:bg-[#8F6BC8]/5 peer-checked:border-[#8F6BC8] peer-checked:bg-[#8F6BC8] peer-checked:text-white">
                                        Não
                                    </div>
                                </label>

                            </div>
                        </div>
                </div>
                <div>
                    <div>
                        <h2>Contato de Emergencia</h2>
                        <div>
                            <label htmlFor="">Nome Completo / (Parentesco)</label>
                            <input type="text" />
                            <label htmlFor="">Telefone</label>
                            <input type="tel" />
                        </div>
                    </div>
                    <div>
                        <button>Cadastrar Aluno</button>
                        <button>Limpar campos</button>
                        <button>Cancelar</button>
                        <button>Visualizar modelo</button>
                    </div>
                    <div>
                        <div></div>
                        <h2>Importante</h2>
                        <p>Preencha corretamente todas as restrições alimentares do aluno. Essas informações serão utilizadas no planejamento diário do cardápio escolar e na segurança alimentar. Em caso de dúvida, consulte o responsável do aluno ou revise os documentos médicos.</p>
                    </div>
                </div>
            </div>

            <Footer />
        </div>
    );
}