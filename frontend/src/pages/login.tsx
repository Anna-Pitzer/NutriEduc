import { useState } from "react"
import { KeyRound, Lock, Mail, User } from "lucide-react"
import { useAuth } from "../context/authContext"

import background from '../assets/background1.png'

export function LoginPage() {
  const [isLoginPage, setIsLoginPage] = useState<boolean>(true)
  const [fullName, setFullName] = useState<string>()
  const [email, setEmail] = useState<string>()
  const [password, setPassword] = useState<string>()
  const [birthday, setBirthday] = useState<string>()

  const { user, login } = useAuth()

  function handlesubmit(e: React.SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    login({
      id: "1",
      name: "Joao",
      role: "user",
    })

  }
  console.log(user)

  return (
    <section
      className="h-screen   flex flex-col justify-center"
      style={{
        backgroundImage: `url(${background})`,
      }}
    >

      <div className="bg-branco w-100 min-h-120 flex 
      justify-center items-center flex-col
      mx-auto rounded-xl
      shadow-gray shadow-2xl/50
       ">
        <h1 className="text-4xl font-semibold p-4">
          <b className="bg-verdeEscuro bg-clip-text text-transparent ">Nutri</b>
          <b className="bg-linear-to-r from-verdeClaro/90 to-verdeClaro bg-clip-text text-transparent ">Educ</b>
        </h1>


        <div className="bg-primaria w-full h-full flex items-center justify-center rounded-tl-[50px] rounded-b-xl">
          {isLoginPage ?
            <form
              onSubmit={handlesubmit}
              className="flex flex-col gap-8 w-[90%]"
            >

              <p className="flex border-b border-white gap-2 w-full bg-branco p-3 rounded-md " >
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
                <Lock color="gray" />
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
                className="text-black bg-white w-[80%] m-auto h-12 rounded-md hover:cursor-pointer hover:bg-[#CFD9C1] hover:pb-1"
              />
              <div className="flex items-center justify-between px-4">
                <label className="text-white flex items-center gap-1 hover:cursor-pointer">
                  <input
                    type="checkbox"
                  />
                  Remember me
                </label>
                <button
                  className="text-white hover:cursor-pointer"
                  type="button"
                  onClick={() => setIsLoginPage(false)}

                >New here? <strong>Sign up!</strong></button>
              </div>
            </form>

            :

            <form
              onSubmit={handlesubmit}
              className="flex flex-col gap-4 w-[90%]"
            >
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
                  type="text"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="outline-none w-full text-gray"
                  placeholder="Email"
                />
              </p>

              <p className="flex border-b border-white gap-2 w-full bg-branco p-3 rounded-md">
                <KeyRound color="gray" />
                <input
                  type="text"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="outline-none w-full text-gray"
                  placeholder="Password"
                />
              </p>

              <p className="flex border-b border-white gap-2 w-full bg-branco p-3 rounded-md">
                <label htmlFor="userDate"></label>
                <input
                  id="userDate"
                  value={birthday}
                  onChange={(e) => setBirthday(e.target.value)}
                  type="date"
                  className="outline-none w-full border-white text-gray"

                />
              </p>

              <button
                className="text-black bg-white w-[80%] m-auto h-12 rounded-md hover:cursor-pointer hover:bg-blue-200 hover:pb-1"
              >Sign up</button>

              <button
                className="text-white"
              >Already have an account?
                <b
                  onClick={() => setIsLoginPage(true)}
                  className="text-white ml-1 hover:cursor-pointer"
                >Log in</b></button>
            </form>
          }
        </div>

      </div>

    </section>
  )
}