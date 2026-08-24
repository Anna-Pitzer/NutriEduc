
import { createContext, useContext, useState } from "react";

type User = {
  id: string;
  name: string;
  role: "user" | "admin";
};

type AuthContextData = {
  user: User | null;
  isAuthenticated: boolean;
  login: (user: User) => void
}

export const AuthContext = createContext<AuthContextData | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);

  const login = (user: User) => {
    setUser(user)
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: user !== null,
        login,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(){
  const context = useContext(AuthContext)

  if (!context){
    throw new Error("UseAuth deve ser usado dentro de um AuthProvider")
  }

  return context 
}