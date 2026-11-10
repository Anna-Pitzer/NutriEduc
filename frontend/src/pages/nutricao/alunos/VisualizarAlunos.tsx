
import { Trash, ChevronLeft, Pencil, History } from "lucide-react";
import Footer from "../../../components/Footer";
import Header from "../../../components/Header"
import { useEffect, Fragment, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ToastContainer, Zoom } from "react-toastify";
import { confirmarExclusao } from "../../../components/Notificacoes";
import { buscarAlunos, excluirAluno, type AlunoResposta } from "../../../services/AlunoApi";
import { listarEscolas, type EscolaResposta } from "../../../services/EscolaApi";
import { naoAutorizado } from "../../../services/erroApi";
import { buscarHistoricoAluno, descreverEvento, type EventoAluno } from "../../../services/HistoricoAlunoApi";
import { useAuth } from "../../../context/authContext";

export function VisualizarAlunos() {
  const detalhesRef = useRef<HTMLDetailsElement>(null);
  const navegar = useNavigate();
  const { userLogin, logout } = useAuth();
  const podeRegistrarOcorrencia = userLogin?.papel === "Nutricionista";


  // ============ RESPOSTAS DO BACK ============
  const seriesPermitidas = [
    "1 ano",
    "2 ano",
    "3 ano",
    "4 ano",
    "5 ano",
    "6 ano",
    "7 ano",
    "8 ano",
    "9 ano",
  ];

  const restricoesPermitidas = [
    "Integral",
    "Intolerância à Lactose",
    "APVL",
    "Diabetes",
    "Intolerância ao Glúten",
    "Outros",
    "AS/Anafilaxia"
  ] as const;
  type Restricoes = string


  // ============ ALUNOS ============
  type AlunoProps = AlunoResposta & { restricoes: Restricoes[] };
  type FiltroAluno = Pick<AlunoProps, "nome" | "serie" | "restricoes"> & {
    escolaId: number | null;
  };

  const [alunos, setAlunos] = useState<AlunoProps[]>([])
  const [escolas, setEscolas] = useState<EscolaResposta[]>([])
  const [carregando, setCarregando] = useState(true)
  const [erro, setErro] = useState("")

  // Histórico append-only por aluno (Spec 010, RF6/RF8), carregado sob demanda.
  const [historico, setHistorico] = useState<Record<number, EventoAluno[]>>({})
  const [historicoAberto, setHistoricoAberto] = useState<number | null>(null)

  const alternarHistorico = async (alunoId: number) => {
    if (historicoAberto === alunoId) {
      setHistoricoAberto(null)
      return
    }

    setHistoricoAberto(alunoId)

    if (historico[alunoId]) return

    try {
      const pagina = await buscarHistoricoAluno(alunoId)
      setHistorico((atual) => ({ ...atual, [alunoId]: pagina.itens }))
    } catch (error) {
      if (naoAutorizado(error)) {
        void logout()
        return
      }

      setErro("Não foi possível carregar o histórico do aluno.")
    }
  }
  useEffect(() => {
    listarEscolas()
      .then(setEscolas)
      .catch((error) => {
        if (naoAutorizado(error)) {
          void logout();
          return;
        }

        setEscolas([]);
      });
  }, [logout])

  useEffect(() => {
    let ativo = true;

    buscarAlunos()
      .then((dados) => {
        if (!ativo) return;

        setAlunos(dados.map((aluno) => ({
          ...aluno,
          restricoes: [
            ...aluno.restricoes,
            ...(aluno.anafilaxia ? ["AS/Anafilaxia" as const] : []),
          ],
        })));
      })
      .catch((error) => {
        if (naoAutorizado(error)) {
          void logout();
          return;
        }

        console.error("Erro ao buscar alunos:", error);
        if (ativo) setErro("Não foi possível carregar os alunos.");
      })
      .finally(() => {
        if (ativo) setCarregando(false);
      });

    return () => {
      ativo = false;
    };
  }, [logout])

  const pegarIniciais = (nome: string) => {
    return nome
      .split(" ")
      .slice(0, 2)
      .map((palavra) => palavra[0])
      .join("")
      .toUpperCase();
  };


  // ============ SISTEMA DE FILTRAGEM ============
  const estadoInicialFiltros: FiltroAluno = {
    nome: "",
    escolaId: null,
    serie: "",
    restricoes: [],
  }
  const [filtro, setFiltro] = useState<FiltroAluno>(estadoInicialFiltros)
  const atualizarCampo = (campo: "nome" | "serie", valor: string) => {
    setFiltro((prev) => ({
      ...prev, [campo]: valor
    }))

  }
  const atualizarEscola = (escolaId: number | null) => {
    setFiltro((prev) => ({
      ...prev,
      escolaId,
    }))
  }
  const limparCampos = () => {
    setFiltro(estadoInicialFiltros)
  }

  const alunosFiltrados = alunos.filter((aluno) => {
    const nomeFiltrado = filtro.nome === "" || aluno.nome.includes(filtro.nome)
    const escolaFiltrada = filtro.escolaId === null || aluno.escolaId === filtro.escolaId
    const serieFiltrada = filtro.serie === "" || aluno.serie.includes(filtro.serie)
    const restricoesValidas = filtro.restricoes.length === 0 || filtro.restricoes.some((restricao) => aluno.restricoes.includes(restricao));

    return (
      nomeFiltrado &&
      escolaFiltrada &&
      serieFiltrada &&
      restricoesValidas
    );
  })


  // ENFETIE
  const corRestricao = (restricao: Restricoes) => {
    switch (restricao) {
      case "Integral":
        return "bg-green-200/75";

      case "Intolerância à Lactose":
        return "bg-yellow-200/75";

      case "APVL":
        return "bg-blue-200/75";

      case "Diabetes":
        return "bg-purple-200/75";

      case "Intolerância ao Glúten":
        return "bg-orange-200/75";

      case "Outros":
        return "bg-gray-200/75";

      case "AS/Anafilaxia":
        return "bg-red-200/75 border border-red-500"
    }
  };




  async function handleDelete(id: number) {
    const confirmou = await confirmarExclusao()

    if (!confirmou) {
      return
    }
    try {
      await excluirAluno(id);
      setAlunos((alunosAtuais) => alunosAtuais.filter((aluno) => aluno.id !== id));
      setErro("");
    } catch (error) {
      if (naoAutorizado(error)) {
        void logout();
        return;
      }

      console.error("Erro ao excluir aluno:", error);
      setErro("Não foi possível excluir o aluno.");
    }

  }

  const cancelar = () => { window.history.back(); };

  return (
    <div className="flex flex-col items-center min-h-screen bg-fundo">

      <Header />

      <div className="mx-auto w-full max-w-7xl">
        <button
            type="button"
            onClick={cancelar}
            aria-label="Voltar"
            className="group flex cursor-pointer items-center justify-center"
        >
            <ChevronLeft
                size={50}
                className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
            />
        </button>
    </div>


      <div
        className="flex w-full max-w-7xl flex-col items-center justify-center bg-[#FAF9F5] mb-30 mx-auto rounded-3xl p-8 shadow-[0_5px_20px_rgba(1,1,1,0.1)]"
      >
        <div className="flex flex-col justify-start w-full">
          <h1 className="font-bold text-2xl text-gray-800">Alunos cadastrados</h1>
          <h2 className="text-lg">Consulte e filtre os alunos por escola, restrição alimentar ou turma!</h2>

        </div>

        {/* divisor */}
        <div className="my-5 w-full border border-gray-200 rounded-3xl" />

        <div className="flex flex-row gap-4 items-center justify-center">

          {/* filtro nome */}
          <div className="flex flex-col">
            <label htmlFor="inNome">Buscar por nome</label>
            <input
              id="inNome"
              type="text"
              value={filtro.nome}
              onChange={(e) => atualizarCampo("nome", e.target.value)}
              placeholder="Digite o nome do aluno..."
              className=" rounded-2xl h-12 w-70 border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none transition-all duration-200 hover:border-gray-300 
 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10  shadow-[0_5px_20px_rgba(1,1,1,0.1)]"
            />
          </div>

          {/* filtro escola */}
          <div className="flex flex-col ">
            <label htmlFor="inEscola">Buscar por escola</label>
            <select
              id="inEscola"
              value={filtro.escolaId ?? ""}
              onChange={(e) => atualizarEscola(e.target.value === "" ? null : Number(e.target.value))}
              className="h-12 w-70 rounded-2xl border border-gray-200 bg-white  text-sm text-gray-400 outline-none transition-all duration-200 hover:border-gray-300 
 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-4  shadow-[0_5px_20px_rgba(1,1,1,0.1)]" >
              <option value="">
                Todas as escolas
              </option>
              {escolas.map(
                (escola) => (
                  <option
                    key={escola.id}
                    value={escola.id}
                  >
                    {escola.nome}
                  </option>
                )
              )}
            </select>
          </div>

          {/* filtro série */}
          <div className="flex flex-col">
            <label htmlFor="inputSerie">Buscar por série</label>
            <select
              id="inputSerie"
              value={filtro.serie}
              onChange={(e) => atualizarCampo("serie", e.target.value)}
              className="h-12 w-70 rounded-2xl border border-gray-200 bg-white  text-sm text-gray-400 outline-none transition-all duration-200 hover:border-gray-300 
 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-4  shadow-[0_5px_20px_rgba(1,1,1,0.1)]"

            >
              <option value="">
                Todas as séries
              </option>
              {seriesPermitidas.map((serie) => (
                <option key={serie} value={serie}>
                  {serie.replace(" ano", "°ano")}
                </option>
              ))}
            </select>

          </div>

          {/* filtro resticoes */}
          <div className="flex flex-col items-center text-start">
            <label htmlFor="inRestricoes" className="text-start w-full">
              Restrição / condição
            </label>
            <details
              ref={detalhesRef}
              id="inRestricoes"
              className="group z-10 h-12 w-70 rounded-2xl border border-gray-200 bg-white text-sm text-gray-400 outline-none transition-all duration-200
 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer  shadow-[0_5px_20px_rgba(1,1,1,0.1)]"
            >
              <summary className="flex h-full w-full  items-center list">
                <span className="m-2 transition-transform duration-200  group-open:rotate-90">
                  ▶
                </span>

                <span>Opções</span>

              </summary>

              <div className=" z-10 w-full rounded-2xl border border-gray-200 bg-white p-4  shadow-[0_5px_20px_rgba(1,1,1,0.3)]">
                {restricoesPermitidas.map((restricao) => (

                  <label
                    key={restricao}
                    className="flex cursor-pointer items-center gap-2 py-2"
                  >
                    <input
                      type="checkbox"
                      checked={filtro.restricoes.includes(restricao)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setFiltro({
                            ...filtro,
                            restricoes: [
                              ...filtro.restricoes,
                              restricao,
                            ],
                          });
                        } else {
                          setFiltro({
                            ...filtro,
                            restricoes: filtro.restricoes.filter(
                              (item) => item !== restricao
                            ),
                          });
                        }
                      }}
                    />

                    <span>{restricao}</span>
                  </label>


                ))}
                <button
                  className="text-center w-full text-black p-3 bg-bege rounded-3xl"
                  type="button"
                  onClick={() => detalhesRef.current?.removeAttribute("open")}
                >
                  Fechar
                </button>
              </div>


            </details>
          </div>


        </div>

        <button
          className="text-sm text-vermelho hover:cursor-pointer hover:underline mt-4"
          onClick={() => limparCampos()}
        >Limpar filtro</button>

        <h2 className="w-full py-4 text-lg">
          <span className="text-roxo">{alunosFiltrados.length}</span> de {alunos.length} alunos exibidos
        </h2>

        {carregando && <p className="w-full py-4 text-gray-500">Carregando alunos...</p>}
        {erro && <p role="alert" className="w-full py-4 text-red-700">{erro}</p>}
        {!carregando && !erro && alunos.length === 0 && (
          <p className="w-full py-4 text-gray-500">Nenhum aluno cadastrado.</p>
        )}

        <div className="w-full overflow-hidden rounded-3xl border border-[#fce9d3]  shadow-[0_5px_5px_rgba(1,1,1,0.1)]">
          <table className="w-full bg-white  ">
            <thead className="">
              <tr className="h-12 bg-bege">
                <th className="px-4 text-left font-light">Nome</th>
                <th className="px-4 text-left font-light">Escola</th>
                <th className="px-4 text-left font-light">Série</th>
                <th className="px-4 text-left font-light">Restrições</th>
                <th className="px-4 text-center font-light">Histórico</th>
                <th className="px-4 text-center font-light">Editar</th>
                <th className="px-4 text-center font-light">Excluir</th>
                {podeRegistrarOcorrencia && (<th className="px-4 text-center font-light">Ocorrência</th>)}
              </tr>
            </thead>

            <tbody>
              {alunosFiltrados.map((aluno) => (
                <Fragment key={aluno.id}>
                <tr className="border-t border-[#fce9d3] h-12">
                  <td className="p-4 max-w-50 truncate">
                    <div className="">
                      <span className="p-2 bg-roxo/40 text-roxo rounded-full mr-4">{pegarIniciais(aluno.nome)}</span>{aluno.nome}
                    </div>
                  </td>
                  <td className="p-4 ">{aluno.escola}</td>
                  <td className="p-4">{aluno.serie}</td>
                  <td className="p-4">
                    <div className="flex flex-wrap gap-2">
                      {aluno.restricoes.map((restricao) => (
                        <span
                          key={restricao}
                          className={`rounded-full px-3 py-1 text-sm ${corRestricao(restricao)}`}
                        >
                          • {restricao.replaceAll('Intolerância', 'Intol.')}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td className="p-4 text-center">
                    <button
                      onClick={() => void alternarHistorico(aluno.id)}
                      aria-label={`Histórico de ${aluno.nome}`}
                      aria-expanded={historicoAberto === aluno.id}
                      className="text-roxo p-2 hover:cursor-pointer hover:bg-purple-100 rounded-full"><History /></button>
                  </td>
                  <td className="p-4 text-center">
                    <button
                      onClick={() => navegar(`/alunos/editar/${aluno.id}`)}
                      aria-label={`Editar ${aluno.nome}`}
                      className="text-roxo p-2 hover:cursor-pointer hover:bg-purple-100 rounded-full "><Pencil /></button>
                  </td>
                  <td className="p-4 text-center">
                    <button
                      onClick={() => handleDelete(aluno.id)}
                      className="text-vermelho p-2 hover:cursor-pointer hover:bg-red-100 rounded-full "><Trash /></button>
                  </td>
                  {podeRegistrarOcorrencia && (
                  <td className="p-4 text-center">
                    <button
                      onClick={() => navegar(`/ocorrencias?alunoId=${aluno.id}`)}
                      aria-label={`Registrar ocorrência para ${aluno.nome}`}
                      className="text-roxo p-2 hover:cursor-pointer hover:bg-purple-100 rounded-full text-sm font-medium"
                    >
                      Registrar
                    </button>
                  </td>
                  )}
                </tr>

                {historicoAberto === aluno.id && (
                  <tr className="bg-[#FAF9F5]">
                    <td colSpan={podeRegistrarOcorrencia ? 8 : 7} className="p-4">
                      <h3 className="font-bold text-roxo mb-2">Histórico de alterações</h3>

                      {!historico[aluno.id] ? (
                        <p className="text-gray-500">Carregando histórico...</p>
                      ) : historico[aluno.id].length === 0 ? (
                        <p className="text-gray-500">Nenhuma alteração registrada.</p>
                      ) : (
                        <ol className="flex flex-col gap-2">
                          {historico[aluno.id].map((evento, indice) => {
                            const cor =
                              evento.tipo === "RestricaoRemovida"
                                ? "bg-red-100 text-red-700"
                                : evento.tipo === "RestricaoAdicionada"
                                  ? "bg-green-100 text-green-800"
                                  : evento.tipo === "Criado"
                                    ? "bg-roxo/10 text-roxo"
                                    : "bg-yellow-100 text-yellow-800";

                            return (
                              <li
                                key={`${evento.tipo}-${evento.campo}-${evento.criadoEm}-${indice}`}
                                className="flex flex-wrap items-center gap-2 text-sm"
                              >
                                <span className={`rounded-full px-3 py-1 text-xs font-semibold ${cor}`}>
                                  {descreverEvento(evento)}
                                </span>

                                {evento.tipo === "Alterado" && (
                                  <span className="text-gray-700">
                                    <span className="line-through text-gray-400">
                                      {evento.valorAnterior || "vazio"}
                                    </span>
                                    {" → "}
                                    <span className="font-semibold">
                                      {evento.valorNovo || "vazio"}
                                    </span>
                                  </span>
                                )}

                                <span className="text-gray-500">por {evento.autor}</span>
                                <span className="text-gray-400">
                                  {new Date(evento.criadoEm).toLocaleString("pt-BR")}
                                </span>
                              </li>
                            );
                          })}
                        </ol>
                      )}
                    </td>
                  </tr>
                )}
                </Fragment>
              ))}
            </tbody>
          </table>
        </div>
        <ToastContainer
          position="top-center"
          autoClose={2000}
          hideProgressBar={false}
          newestOnTop
          closeOnClick={false}
          rtl={false}
          pauseOnFocusLoss
          draggable
          pauseOnHover
          theme="light"
          transition={Zoom}
          toastClassName="!shadow-none !bg-bege !font-[DynaPuff] !w-100"

        />
      </div>
      <footer className="w-full">
        <Footer />
      </footer>

    </div>

  )
}