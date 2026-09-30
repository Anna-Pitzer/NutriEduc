
import { createContext, useContext, useEffect, useState } from "react";

type UserLogin = {
  email: string;
  password: string;
};

type UserSession = {
  id: number;
  email: string;
  nome: string;
};

type UserRegister = {
  name: string;
  password: string;
  email: string;
  birthDate: string;
  cpf: string;
};

type AuthContextData = {
  userLogin: UserSession | null;
  userRegister: UserRegister | null;
  isAuthenticated: boolean;
  loading: boolean;
  atualizarUsuario: (usuario: UserSession) => void;
  login: (user: UserLogin) => Promise<void>;
  register: (user: UserRegister) => Promise<void>;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextData | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [userLogin, setUserLogin] = useState<UserSession | null>(null);
  const [userRegister, setUserRegister] = useState<UserRegister | null>(null)
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const controller = new AbortController();

    async function recuperarSessao() {
      try {
        const resposta = await fetch("/api/usuario/me", {
          credentials: "include",
          signal: controller.signal,
        });

        if (!resposta.ok) {
          setUserLogin(null);
          return;
        }

        const usuario: UserSession = await resposta.json();
        setUserLogin(usuario);
      } catch {
        if (!controller.signal.aborted) {
          setUserLogin(null);
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    }

    recuperarSessao();

    return () => controller.abort();
  }, []);

  const login = async (user: UserLogin) => {
    const resposta = await fetch("/api/usuario/login", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        email: user.email,
        senha: user.password,
      }),
    });

    if (!resposta.ok) {
      if (resposta.status === 404 || resposta.status === 401) {
        throw new Error("Email ou senha incorretos.");
      }

      throw new Error(
        "Não foi possível entrar. Verifique o terminal do backend."
      );
    }

    const usuario: UserSession = await resposta.json();
    setUserLogin(usuario);
  };

  const register = async (user: UserRegister) => {
    const resposta = await fetch("/api/usuario/registrar", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        nome: user.name,
        cpf: user.cpf,
        dataNascimento: user.birthDate,
        email: user.email,
        senha: user.password,
      }),
    });

    if (!resposta.ok) {
      const mensagem = await resposta.text();
      throw new Error(mensagem || "Não foi possível cadastrar.");
    }

    setUserRegister(user);
  };

  const logout = async () => {
    const resposta = await fetch("/api/usuario/logout", {
      method: "POST",
      credentials: "include",
    });

    if (!resposta.ok && resposta.status !== 401) {
      throw new Error("Não foi possível sair. Tente novamente");
    }

    setUserLogin(null);
  }
  
  const atualizarUsuario = (usuario: UserSession) => {
    setUserLogin(usuario);
  };

  return (
    <AuthContext.Provider
      value={{
        userLogin,
        userRegister,
        isAuthenticated: userLogin !== null,
        loading,
        login,
        register,
        logout,
        atualizarUsuario,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error("UseAuth deve ser usado dentro de um AuthProvider")
  }

  return context
}