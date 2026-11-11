import { useState, useEffect } from "react"
import { KeyRound, Lock, Mail, User } from "lucide-react"
import { useAuth, type TipoCadastro } from "../../context/authContext"
import { homePorPapel } from "../../routes/homePorPapel"
import { listarUnidadesAtivas, type UnidadeAtiva } from "../../services/UnidadeApi"

import background from '../../assets/background1.png'
import { Navigate } from "react-router-dom"

import {
  sanitizarNome,
  sanitizarEmail,
} from "../../components/Sanitizacao";

import {
  validarNome,
  validarEmail,
  validarSenha,
} from "../../components/Validacao";

const tiposCadastro: { valor: TipoCadastro; rotulo: string; descricao: string }[] = [
  { valor: "Nutricionista", rotulo: "Nutricionista", descricao: "Responsável técnico pelos cardápios" },
  { valor: "Merendeira", rotulo: "Merendeira", descricao: "Prepara e serve as refeições na escola" },
  { valor: "Responsavel", rotulo: "Responsável", descricao: "Acompanha a alimentação do aluno" },
];

export function LoginPage() {
  const [isLoginPage, setIsLoginPage] = useState<boolean>(true)
  const [etapaCadastro, setEtapaCadastro] = useState<1 | 2>(1)

  const [fullName, setFullName] = useState<string>("")
  const [email, setEmail] = useState<string>("")
  const [password, setPassword] = useState<string>("")
  const [cpf, setCpf] = useState<string>("")
  const [birthDate, setBirthDate] = useState<string>("")
  const [tipoCadastro, setTipoCadastro] = useState<TipoCadastro | null>(null)
  const [unidades, setUnidades] = useState<UnidadeAtiva[]>([])
  const [unidadeId, setUnidadeId] = useState<number | "">("")
  const [crn, setCrn] = useState<string>("")
  const [cadastrando, setCadastrando] = useState(false)

  const { isAuthenticated, loading, login, register, userLogin } = useAuth()

  useEffect(() => {
    if (!isLoginPage && etapaCadastro === 2 && unidades.length === 0) {
      listarUnidadesAtivas()
        .then(setUnidades)
        .catch(() => setUnidades([]));
    }
  }, [isLoginPage, etapaCadastro, unidades.length])

  useEffect(() => {
    console.log("isAuthenticated:", isAuthenticated)
  }, [isAuthenticated])

  async function handleLogin(e: React.SubmitEvent<HTMLFormElement>) {
    e.preventDefault()

    const emailSanitizado = sanitizarEmail(email);

    const erroEmail = validarEmail(emailSanitizado);
    if (erroEmail) {
      alert(erroEmail);
      return;
    }

    const erroSenha = validarSenha(password);
    if (erroSenha) {
      alert(erroSenha);
      return;
    }

    try {
      await login({
        email: emailSanitizado,
        password: password,
      });
    } catch (erro) {
      alert(
        erro instanceof Error
          ? erro.message
          : "Não foi possível conectar ao backend."
      );
    }
  }

  function escolherTipo(tipo: TipoCadastro) {
    setTipoCadastro(tipo);
    setCrn("");
    setEtapaCadastro(2);
  }

  function voltarParaTipo() {
    setEtapaCadastro(1);
  }

  async function handleRegister(e: React.SubmitEvent<HTMLFormElement>) {
    e.preventDefault();

    if (cadastrando) return;

    if (!tipoCadastro) {
      alert("Selecione o tipo de cadastro.");
      return;
    }

    const nomeSanitizado = sanitizarNome(fullName);
    const emailSanitizado = sanitizarEmail(email);
    const cpfLimpo = cpf.replace(/\D/g, "");

    const erroNome = validarNome(nomeSanitizado);
    if (erroNome) {
      alert(erroNome);
      return;
    }

    const erroEmail = validarEmail(emailSanitizado);
    if (erroEmail) {
      alert(erroEmail);
      return;
    }

    const erroSenha = validarSenha(password);
    if (erroSenha) {
      alert(erroSenha);
      return;
    }

    if (cpfLimpo.length !== 11) {
      alert("Informe o CPF com 11 dígitos.");
      return;
    }

    if (!birthDate) {
      alert("Informe a data de nascimento.");
      return;
    }

    if (unidadeId === "") {
      alert("Selecione a unidade de destino.");
      return;
    }

    if (tipoCadastro === "Nutricionista" && crn.trim() === "") {
      alert("Informe o CRN da nutricionista.");
      return;
    }

    setCadastrando(true);

    try {
      await register({
        name: nomeSanitizado,
        cpf: cpfLimpo,
        birthDate,
        email: emailSanitizado,
        password,
        tipo: tipoCadastro,
        unidadeId: Number(unidadeId),
        crn: tipoCadastro === "Nutricionista" ? crn.trim() : null,
      });

      alert("Cadastro realizado! Aguarde a autorização da unidade para entrar.");

      setFullName("");
      setCpf("");
      setBirthDate("");
      setPassword("");
      setCrn("");
      setUnidadeId("");
      setTipoCadastro(null);
      setEtapaCadastro(1);
      setEmail(emailSanitizado);
      setIsLoginPage(true);
    } catch (erro) {
      alert(
        erro instanceof Error
          ? erro.message
          : "Não foi possível conectar ao backend."
      );
    } finally {
      setCadastrando(false);
    }
  }

  if (loading){
    return <p role="status">Carregando...</p>
  }
  if (isAuthenticated) {
    return <Navigate to={userLogin?.papel ? homePorPapel[userLogin.papel] : "/"} replace />
  }

  return (
    <section
      className="h-screen flex flex-col justify-center"
      style={{
        backgroundImage: `url(${background})`,
      }}
    >
      <div className="bg-branco w-100 min-h-120 flex justify-center items-center flex-col mx-auto rounded-xl shadow-gray shadow-2xl/50">
        <h1 className="text-4xl font-semibold p-4">
          <b className="bg-verdeEscuro bg-clip-text text-transparent ">Nutri</b>
          <b className="bg-linear-to-r from-verdeClaro/90 to-verdeClaro bg-clip-text text-transparent ">Educ</b>
        </h1>

        <div className="bg-primaria w-full h-full flex items-center justify-center rounded-tl-[50px] rounded-b-xl">
          {isLoginPage ? (
            <form onSubmit={handleLogin} className="flex flex-col gap-8 w-[90%]">
              <p className="flex border-b border-white gap-2 w-full bg-branco p-3 rounded-md ">
                <Mail color="gray" />
                <input
                  className="text-gray outline-none w-full"
                  type="email"
                  placeholder="Email address"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  autoFocus
                />
              </p>
              <p className="flex border-b border-white gap-2 w-full bg-branco p-3 rounded-md">
                <Lock color="gray" width={30} height={30} />
                <input
                  className="text-gray outline-none w-full"
                  type="password"
                  placeholder="Password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
                <button
                  type="button"
                  className="hover:cursor-pointer text-black/40 ml-auto italic font-extralight text-sm"
                >Forgot?</button>
              </p>

              <input
                type="submit"
                value="Log in"
                className="text-black bg-white w-[80%] m-auto h-12 rounded-md hover:cursor-pointer hover:bg-verdeMtClaro hover:pb-1"
              />
              <div className="flex items-center justify-between px-4">
                <label className="text-white flex items-center gap-1 hover:cursor-pointer">
                  <input type="checkbox" />
                  Remember me
                </label>
                <button
                  className="text-white hover:cursor-pointer"
                  type="button"
                  onClick={() => { setIsLoginPage(false); setEtapaCadastro(1); }}
                >New here? <strong>Register!</strong></button>
              </div>
            </form>
          ) : etapaCadastro === 1 ? (
            <form className="flex flex-col gap-4 w-[90%] py-6">
              <p className="text-white text-center font-medium">
                Qual é o seu tipo de cadastro?
              </p>

              {tiposCadastro.map((tipo) => (
                <button
                  key={tipo.valor}
                  type="button"
                  onClick={() => escolherTipo(tipo.valor)}
                  className="flex flex-col gap-1 bg-white/95 text-left p-4 rounded-xl hover:bg-verdeMtClaro"
                >
                  <span className="text-black font-semibold">{tipo.rotulo}</span>
                  <span className="text-gray text-sm">{tipo.descricao}</span>
                </button>
              ))}

              <button
                type="button"
                className="text-white hover:cursor-pointer"
                onClick={() => setIsLoginPage(true)}
              >
                Already have an account?
                <b className="ml-1">Log in</b>
              </button>
            </form>
          ) : (
            <form onSubmit={handleRegister} className="flex flex-col gap-4 w-[90%] py-4">
              <button
                type="button"
                className="text-white text-left text-sm hover:cursor-pointer"
                onClick={voltarParaTipo}
              >
                ← Trocar tipo de cadastro
              </button>

              <p className="flex border-b border-white gap-2 w-full bg-branco p-3 rounded-md">
                <User color="gray" />
                <input
                  type="text"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  autoFocus
                  className="outline-none w-full text-gray"
                  placeholder="Full Name"
                />
              </p>

              <p className="flex border-b border-white gap-2 w-full bg-branco p-3 rounded-md">
                <Mail color="gray" />
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="outline-none w-full text-gray"
                  placeholder="Email"
                />
              </p>

              <p className="flex gap-2 w-full bg-branco p-3 rounded-md">
                <input
                  type="text"
                  inputMode="numeric"
                  value={cpf}
                  onChange={(e) => setCpf(e.target.value)}
                  maxLength={14}
                  className="outline-none w-full text-gray"
                  placeholder="CPF"
                  required
                />
              </p>

              <p className="flex flex-col gap-1 w-full bg-branco p-3 rounded-md">
                <label htmlFor="dataNascimento" className="text-sm text-gray">Data de nascimento</label>
                <input
                  id="dataNascimento"
                  type="date"
                  value={birthDate}
                  onChange={(e) => setBirthDate(e.target.value)}
                  className="outline-none w-full text-gray"
                  required
                />
              </p>

              <p className="flex flex-col gap-1 w-full bg-branco p-3 rounded-md">
                <label htmlFor="unidade" className="text-sm text-gray">Unidade de destino</label>
                <select
                  id="unidade"
                  value={unidadeId}
                  onChange={(e) => setUnidadeId(e.target.value === "" ? "" : Number(e.target.value))}
                  className="outline-none w-full text-gray"
                  required
                >
                  <option value="">Selecione a unidade</option>
                  {unidades.map((unidade) => (
                    <option key={unidade.id} value={unidade.id}>{unidade.nome}</option>
                  ))}
                </select>
              </p>

              {tipoCadastro === "Nutricionista" && (
                <p className="flex border-b border-white gap-2 w-full bg-branco p-3 rounded-md">
                  <input
                    type="text"
                    value={crn}
                    onChange={(e) => setCrn(e.target.value)}
                    className="outline-none w-full text-gray"
                    placeholder="CRN (ex.: CRN-3-12345)"
                  />
                </p>
              )}

              <p className="flex border-b border-white gap-2 w-full bg-branco p-3 rounded-md">
                <KeyRound color="gray" />
                <input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="outline-none w-full text-gray"
                  placeholder="Password"
                />
              </p>

              <button
                type="submit"
                disabled={cadastrando}
                className="text-black bg-white w-[80%] m-auto h-12 rounded-md hover:cursor-pointer hover:bg-verdeMtClaro disabled:opacity-50"
              >
                {cadastrando ? "Cadastrando..." : "Register"}
              </button>

              <button
                type="button"
                className="text-white hover:cursor-pointer"
                onClick={() => setIsLoginPage(true)}
              >
                Already have an account?
                <b className="ml-1">Log in</b>
              </button>
            </form>
          )}
        </div>
      </div>
    </section>
  )
}
