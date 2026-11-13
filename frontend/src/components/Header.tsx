import { useState } from "react";
import { Link } from "react-router-dom";
import { CircleUserRound } from "lucide-react";
import { useAuth } from "../context/authContext";
import { SinoNotificacoes } from "./SinoNotificacoes";

export default function Header() {
    const { logout } = useAuth();
    const [saindo, setSaindo] = useState(false);

    async function handleLogout() {
        setSaindo(true);

        try {
            await logout();
        } catch (erro) {
            alert(
                erro instanceof Error
                    ? erro.message
                    : "Não foi possível sair."
            );
        } finally {
            setSaindo(false);
        }
    }

    // Sem `overflow-hidden`: o painel de notificações é maior que o header e precisa poder
    // escapar da caixa para rolar a lista completa.
    return (
        <header className="relative h-60 w-full">
            {/*
              A faixa rosa precisa de profundidade constante: a versão anterior descia
              até ~30 unidades no canto direito (de 300) e o botão "Sair" ficava abaixo da
              curva. A curva agora termina em ~70 no canto e ~150 a 10% da borda, o que
              mantém qualquer ação da direita dentro do rosa em qualquer largura.
            */}
            <svg
                className="absolute inset-0 h-full w-full"
                viewBox="0 0 1000 300"
                preserveAspectRatio="none"
                aria-hidden="true"
            >
                <path
                    d="
                        M 0 0
                        H 1000
                        C 1000 70, 960 120, 900 150
                        C 780 200, 650 210, 520 215
                        C 350 222, 160 228, 0 230
                        Z
                    "
                    fill="#D92567"
                />
            </svg>

            {/*
              Mesma coluna de conteúdo das páginas: as ações ficam alinhadas ao cartão.
              O padding direito é maior que o esquerdo de propósito: as ações (Sair/Perfil)
              encostavam na borda direita e na parte mais fina da curva rosa.
            */}
            <div className="relative z-10 mx-auto flex h-full w-full max-w-7xl items-start justify-between py-8 pl-4 pr-8 md:pl-6 md:pr-12">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight text-branco md:text-5xl lg:text-6xl">NutriEduc</h1>
                    <p className=" mt-1 text-sm font-light text-white/75 md:text-base">Alimentação que educa, nutrição que transforma</p>
                </div>

                <div className="flex items-center gap-2">
                    <SinoNotificacoes />
                    <button
                        type="button"
                        onClick={handleLogout}
                        disabled={saindo}
                        className="rounded-lg bg-white px-4 py-2 text-sm font-semibold text-[#D92567] shadow-sm hover:cursor-pointer hover:bg-gray-100 disabled:cursor-wait disabled:opacity-50"
                    >
                        {saindo ? "Saindo..." : "Sair"}
                    </button>

                    <Link
                        to="/perfil"
                        aria-label="Abrir perfil"
                        className="flex items-center gap-2 rounded-lg bg-white px-4 py-2 text-sm font-semibold text-roxo shadow-sm hover:bg-gray-100"
                    >
                        <CircleUserRound size={16} />
                        Perfil
                    </Link>
                </div>
            </div>
        </header>
    );
}
