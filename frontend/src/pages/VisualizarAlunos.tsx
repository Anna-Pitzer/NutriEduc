
import Footer from "../components/Footer";
import Header from "../components/Header"
import { useEffect, useRef, useState } from "react";

export function VisualizarAlunos() {
  const detalhesRef = useRef<HTMLDetailsElement>(null);


  // ============ RESPOSTAS DO BACK ============
  const escolasPermitidas = [
    "Escola 01",
    "Escola 02",
    "Escola 03",
  ];

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
    "AS/Anafilaxia"
  ] as const; //as const serve para transformar o elementos em indices, ajuda para quando o aluno tem mais doq uma restricao
  type Restricoes = typeof restricoesPermitidas[number]


  // ============ ALUNOS ============
  interface AlunoProps {
    nome: string;
    escola: string;
    serie: string;
    restricoes: Restricoes[];
  }
  // EXEMPLO DE GET DA API
  const respostaAPI: AlunoProps[] = [
    { nome: "Kayke Silva de Mattos Soares", escola: "Escola 01 ", serie: "3 ano", restricoes: ["Integral"] },
    { nome: "Kayke2 DE", escola: "Escola 01", serie: "9 ano", restricoes: ["Intolerância à Lactose", "Intolerância ao Glúten", "APVL", "Integral", "Diabetes"] },
    { nome: "Kayke3 Mattos", escola: "Escola 02", serie: "7 ano", restricoes: ["Integral", "APVL"] },
    { nome: "Kayke4 SOARES", escola: "Escola 03", serie: "6 ano", restricoes: ["Diabetes", "AS/Anafilaxia"] },
  ]

  // PREENCHER OS ALUNOS COM O RESULTADO DA API
  const [alunos, setAlunos] = useState<AlunoProps[]>([])
  useEffect(() => {
    setAlunos(respostaAPI)
  }, [])

  const pegarIniciais = (nome: string) => {
    return nome
      .split(" ")
      .slice(0, 2)
      .map((palavra) => palavra[0])
      .join("")
      .toUpperCase();
  };


  // ============ SISTEMA DE FILTRAGEM ============
  const estadoInicialFiltros: AlunoProps = {
    nome: "",
    escola: "",
    serie: "",
    restricoes: [],
  }
  const [filtro, setFiltro] = useState<AlunoProps>(estadoInicialFiltros)
  const atualizarCampo = (campo: keyof AlunoProps, valor: string) => {
    setFiltro((prev) => ({
      ...prev, [campo]: valor
    }))

  }
  const limparCampos = () => {
    setFiltro(estadoInicialFiltros)
  }

  const alunosFiltrados = alunos.filter((aluno) => {
    const nomeFiltrado = filtro.nome === "" || aluno.nome.includes(filtro.nome)
    const escolaFiltrada = filtro.escola === "" || aluno.escola.includes(filtro.escola)
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

      case "AS/Anafilaxia":
        return "bg-red-200/75 border border-red-500"
    }
  };

  return (
    <div className="flex flex-col items-center min-h-screen bg-fundo">

      <Header />


      <div
        className="flex justify-center  items-center flex-col max-w-7xl bg-[#FAF9F5] mb-30 mx-30 rounded-3xl p-8  shadow-[0_5px_20px_rgba(1,1,1,0.1)]"
      >
        <div className="flex flex-col justify-start w-full">
          <h1 className="font-bold text-2xl text-primaria">Alunos cadastrados</h1>
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
              value={filtro.escola}
              onChange={(e) => atualizarCampo("escola", e.target.value)}
              className="h-12 w-70 rounded-2xl border border-gray-200 bg-white  text-sm text-gray-400 outline-none transition-all duration-200 hover:border-gray-300 
 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-4  shadow-[0_5px_20px_rgba(1,1,1,0.1)]" >
              <option value="">
                Todas as escolas
              </option>
              {escolasPermitidas.map(
                (escola) => (
                  <option
                    key={escola}
                    value={escola}
                  >
                    {escola}
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

        <div className="w-full overflow-hidden rounded-3xl border border-[#fce9d3]  shadow-[0_5px_5px_rgba(1,1,1,0.1)]">
          <table className="w-full bg-white  ">
            <thead className="">
              <tr className="h-12 bg-bege">
                <th className="px-4 text-left font-light">Nome</th>
                <th className="px-4 text-left font-light">Escola</th>
                <th className="px-4 text-left font-light">Série</th>
                <th className="px-4 text-left font-light">Restrições</th>
              </tr>
            </thead>

            <tbody>
              {alunosFiltrados.map((aluno) => (
                <tr key={aluno.nome} className="border-t border-[#fce9d3] h-12">
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
                          • {restricao.replaceAll('Intolerância', 'Into')}
                        </span>
                      ))}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

      </div>
      <footer className="w-full">
        <Footer />
      </footer>

    </div>

  )
}