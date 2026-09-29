import Header from "../components/Header";
import Navbar from "../components/Navbar";
import Footer from "../components/Footer";
import background2 from '../assets/background2.png';
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Calendar, ChevronRight } from "lucide-react";
import { buscarHistoricoCardapios, type CardapioHistorico } from "../services/CardapioApi";
import Button from "../components/Button";

export function HomePage() {
  const navigate = useNavigate();

  const[historico, setHistorico] = useState<CardapioHistorico[]>([]);
  const[carregando, setCarregando] = useState(true);

  async function carregarHistorico() {
    try {
      setCarregando(true)
      const dados = await buscarHistoricoCardapios();
      setHistorico(dados.slice(0, 6));
    } catch (erro) {
      console.error("Erro ao carregar histórico:", erro);
    } finally {
      setCarregando(false);
    }
  }

  useEffect(() => {
    carregarHistorico();
  }, []);


  return (
    <div className="relative min-h-screen bg-[#FAF9F5]">
      <div
        className="fixed inset-0 z-0 bg-cover bg-center bg-no-repeat opacity-60"
        style={{
          backgroundImage: `url(${background2})`,
        }}
      />
      <div className="relative z-20 flex min-h-screen flex-col">
        <Header />
        <div className="mx-auto flex w-full max-w-350 flex-1 gap-6 px-4 pb-8 md:px-8">
          <Navbar />
          <main className="flex-1 rounded-2xl border border-[#E8E8E8]/20 bg-[rgba(214,211,205,0.07)] p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)] backdrop-blur-[1px]">
            <div className="mb-6 flex flex-col">
              <h2 className="text-2xl font-bold text-vermelho">
                Bem-vindo ao NutriEduc!
              </h2>
              <p className="mt-2 text-sm text-gray-500">
                Gerencie as informações da plataforma de forma simples e organizada.
              </p>

              <div className="">
                <div className="flex items-center justify-between mb-6">
                  <div>
                    <h3 className="text-xl font-bold text-gray-800 pt-4">Cardápios recentes</h3>
                    <p className="text-sm text-gray-500 mt-1"></p>
                  </div>
                </div>

                {carregando ? (
                  <div className="flex items-center justify-center py-10">
                    <p className="text-gray-500">Carregando cardápios...</p>
                  </div>
                ) : historico.length === 0 ? (
                  <div className="rounded-2xl border-2 border-dashed border-gray-200 p-6 flex flex-col items-center text-center">
                    <p className="text-gray-500 font-medium">Nenhum cardápio criado ainda</p>
                    <p className="text-sm text-gray-400 mt-2">Comece criando um novo cardápio</p>
                    <Button type="button" variant="normal" onClick={() => navigate("/cardapio")} className="mt-4">
                      Criar Cardápio
                    </Button>
                  </div>
                ) : (
                  <div className="space-y-3">
                    {historico.map((cardapio) => (
                      <div key={cardapio.id} className="group flex items-center justify-between p-4" onClick={() => navigate(`/cardapio/visualizar/${cardapio.id}`)}>
                          <div className="flex-1">
                            <div className="flex items-center gap-3 mb-2">
                              <h4 className="font-semibold text-gray-800">{obterNomeTipo(cardapio.tipo)}</h4>
                              <span className="text-xs px-2 py-1 rounded-full bg-roxo/10 text-roxo">
                              {obterTotalAlimentos(cardapio.refeicoes)}{" "} alimentos
                              </span>
                            </div>

                            <div className="flex flex-col sm:flex-row sm:items-center gap-2 text-sm text-gray-500">
                              <div className="flex items-center gap-1">
                                  <Calendar size={14} />
                                  <span>{cardapio.data}</span>
                              </div>
                              <div className="hidden sm:block">•</div>
                            </div>
                          </div>

                           <div className="flex items-center gap-2 ml-4">
                              <button type="button" onClick={(e) => {e.stopPropagation();navigate(`/cardapio/visualizar/${cardapio.id}`);}} className="rounded-2xl bg-primaria px-5 py-3 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:bg-[#b31f55] hover:shadow-md active:scale-[0.98]">
                                  Visualizar
                              </button>
                              <ChevronRight
                                  size={20}
                                  className="text-gray-300 group-hover:text-roxo transition-colors"
                              />
                          </div>
                      </div>
                    ))} 
                    {historico.length > 0 && (
                      <div className="pt-4 border-t border-gray-200">
                          <Button
                            type="button"
                            onClick={() => navigate("/cardapio")} variant="ver" className="flex items-center gap-1">
                              Ver todos os cardápios
                              <ChevronRight size={18} />
                          </Button>
                      </div>
                    )}
                  </div>
                )}
              </div>
            </div>
          </main>
        </div>
        <Footer />
      </div>
    </div>
  );
}

function obterNomeTipo(tipo: string): string {
    const nomes: Record<string, string> = {
        "creche-1-3": "Creche (1 a 3 anos)",
        "creche-4-5": "Creche (4 a 5 anos)",
        "integral": "Escola Integral",
        "geral": "Escola Geral"
    };
    return nomes[tipo] || tipo;
}

function obterTotalAlimentos(refeicoes: Record<string, any[]>): number {
    return Object.values(refeicoes).reduce((total, items) => total + items.length, 0);
}
