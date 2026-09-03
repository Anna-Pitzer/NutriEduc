import Button from "../components/Button";
import { CardapiosDiv } from "../components/CardapiosDiv";
import Footer from "../components/Footer";
import Header from "../components/Header";
import { ChevronLeft } from "lucide-react";
import { useState, useEffect } from "react";

interface Responsavel {
    id: number;
    nome: string;
    cargo: string;
    crn: string;
}

export function Cardapio() {

    const cancelar = () => window.history.back();

    const[dataSelecionada, setDataSelecionada] = useState("");
    const[responsavel, setResponsavel] = useState<Responsavel | null>(null);

    const gerarDatas = () => {
        const datas: string[] = []; 
        const inicio = new Date(2026, 7, 1);
        for (let i = 0; i < 12; i++) {
            const data = new Date(inicio);
            data.setDate(inicio.getDate() + i * 15);

            datas.push(data.toLocaleDateString("pt-BR"));
        }
        return datas;
    }

    const datas = gerarDatas();

    useEffect(() => {
        const PegarResponsavel = async () => {
            try {
                const token = localStorage.getItem("token");

                const resposta = await fetch(
                    "http://localhost:8080/responsaveis",
                    {
                        headers: {
                            Authorization: `Bearer ${token}`,
                        },
                    }
                );

                if (!resposta.ok) {
                    throw new Error("Erro ao buscar responsáveis");
                };

                const dados: Responsavel = await resposta.json();
                setResponsavel(dados);

            } catch (error) {
                console.error("Erro ao buscar responsáveis:", error);
            };
        }

        PegarResponsavel();
    }, []); 

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <Header />

            <button
                type="button"
                onClick={cancelar}
                className="group flex items-center justify-center cursor-pointer mx-10"
            >
                <ChevronLeft
                    size={50}
                    className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <div className="flex flex-col mx-30 mb-10 gap-10">
                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="font-bold text-2xl">Configuração do cardápio</h2>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    <div className="flex gap-4">
                        {/*ANOS*/}
                        <select name="" id="" className="w-full rounded-2xl border border-gray-200 bg-white  text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-[border] focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3" >
                            <option value="">Selecione uma Opcao</option>
                            <option value="">Creche (1 a 3 anos)</option>
                            <option value="">Creche (4 a 5 anos)</option>
                            <option value="">Escola Integral</option>
                            <option value="">Escola Geral</option>
                        </select>
                        
                        {/*SEMANA*/}
                        <select name="" id="" className="w-full rounded-2xl border border-gray-200 bg-white  text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-[border] focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3" value={dataSelecionada} onChange={(e) => setDataSelecionada(e.target.value)}>
                            {datas.map((data, index) => (
                                <option key={index} value={data}>
                                    {data}
                                </option>
                            ))}
                        </select>

                        {/*RESPONSÁVEL*/}
                        {responsavel && (
                            <div className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                            hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2">
                                <p>{responsavel.nome}</p>
                                <p>{responsavel.cargo}</p>
                                <p>{responsavel.crn}</p>
                            </div>
                        )}
                        <Button type="button" variant="normal">
                            Selecionar
                        </Button>
                    </div>

                    <div className=" border-2 border-verdeClaro rounded-2xl p-4 bg-verdeClaro/15 my-4">

                        <h3 className="font-bold text-verdeEscuro">Requisitos PNAE</h3>
                        <p>Lorem ipsum dolor sit amet consectetur adipisicing elit. Illo a repellendus itaque earum corporis officia quae necessitatibus, quis eum commodi aliquam unde. Debitis ea minima fugiat aspernatur. Eligendi, iusto maxime.</p>

                    </div>
                
                </div>

                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="font-bold text-2xl">Refeições PNAE</h2>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />
                    {/*EXEMPLO: FAZER COM COMPONENTE*/}
                    <div>
                        {/*Café*/}
                        <CardapiosDiv />

                        {/*Almoço*/}
                        <div>
                            <h3>Almoço</h3>
                            <p>Opções</p>
                            <p>Quantidade</p>
                        </div>

                        {/*Lanche*/}
                        <div>
                            <h3>Lanche da tarde</h3>
                            <p>Opções</p>
                            <p>Quantidade</p>
                        </div>

                        {/*Jantar*/}
                        <div>
                            <h3>Jantar</h3>
                            <p>Opções</p>
                            <p>Quantidade</p>
                        </div>
                    </div>

                </div>

                <div>
                    <h2>Composição nutricional</h2>

                    <div>
                        <p>Calorias</p>
                        <p>Carboidratos</p>
                        <p>Proteínas</p>
                        <p>Gorduras totais</p>
                        <p>Fibras</p>
                        <p>Sódio</p>
                    </div>
                </div>

                <div>
                    <h2>Analise de Conformidade PNAE</h2>
                    <div>
                        <p>Conforme resolução 09/2015</p>
                        <p>Conforme resolução 08/2015</p>
                        <p>Conforme resolução 07/2015</p>
                        <p>Conforme resolução 06/2015</p>
                    </div>
                </div>

                <div>
                    <h2>Exportar em Formatos PNAE</h2>

                    <div>Exportar Excel</div>
                    <div>Exportar word</div>
                    <div>Exportar pdf</div>
                    <div>Exportar Relatório FNDE</div>
                    <div>Enviar para CAE</div>
                </div>

                <div>
                    <button>Salvar Cardapio</button>
                    <button>Validar Conformidade</button>
                    <button>Gerar</button>
                    <button>Cancelar</button>
                </div>

            </div>

            <Footer />
        </div>
    );
}