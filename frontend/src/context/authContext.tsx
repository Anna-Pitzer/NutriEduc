
import { createContext, useContext, useState } from "react";

type UserLogin = {
  email: string;
  password: string;
};

type UserRegister = {
  name: string;
  password: string;
  email: string;
};

type AuthContextData = {
  userLogin: UserLogin | null;
  userRegister: UserRegister | null;
  isAuthenticated: boolean;
  login: (user: UserLogin) => void;
  register: (user: UserRegister) => void;
}

export const AuthContext = createContext<AuthContextData | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [userLogin, setUserLogin] = useState<UserLogin | null>(null);
  const [userRegister, setUserRegister] = useState<UserRegister | null>(null)

  const login = (user: UserLogin) => {
    setUserLogin(user)
  }

  const register = (user: UserRegister) => {
    setUserRegister(user)
  }



  return (
    <AuthContext.Provider
      value={{
        userLogin,
        userRegister,
        isAuthenticated: userLogin !== null,
        login,
        register,
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