import { useNavigate } from "react-router-dom";
import type { ReactNode, ButtonHTMLAttributes } from "react";

type ButtonVariant = "normal" | "cancelar" | "alterarSenha" | "alteracao" | "limpar" | "ver" | "login" | "cadastro" | "link" | "apagar";

interface ButtonProps
    extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, "onClick"> {
    children: ReactNode;
    onClick?: () => void;
    variant?: ButtonVariant;
    to?: string;
}

function Button({
    children,
    onClick,
    variant = "normal",
    to,
    type = "button",
    disabled = false,
    ...props
}: ButtonProps) {
    const navigate = useNavigate();

    const handleClick = () => {
        if (onClick) {
            onClick();
        } else if (to) {
            navigate(to);
        }
    };

    const styles: Record<ButtonVariant, string> = {
        normal:"rounded-2xl bg-primaria px-5 py-3 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:bg-[#b31f55] hover:shadow-md active:scale-[0.98]",

        cancelar: "rounded-2xl border border-gray-200 bg-white px-5 py-3 text-sm font-medium text-gray-600 transition-all duration-200 hover:border-gray-300 hover:bg-gray-50 active:scale-[0.98]",  

        alterarSenha: "rounded-2xl border border-amarelo bg-[#fffae5] px-5 py-3 text-sm font-medium text-amarelo transition-all duration-200 hover:border-amarelo hover:bg-[#fff3c4] active:scale-[0.98]",

        alteracao: "rounded-2xl bg-amarelo px-5 py-3 text-sm font-medium text-white hover:bg-[#c9a91f]",

        limpar: "rounded-2xl border border-verdeClaro bg-white px-5 py-3 text-sm font-medium text-verdeClaro transition-all duration-200 hover:border-verdeClaro hover:bg-verdeClaro/20 active:scale-[0.98]",

        ver: "rounded-2xl bg-laranja px-5 py-3 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:brightness-90 hover:shadow-md active:scale-[0.98]",

        login: "",

        cadastro:"",

        link: "rounded-2xl bg-verdeClaro px-5 py-3 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:brightness-90 hover:shadow-md active:scale-[0.98]",

        apagar: "flex items-center justify-center rounded-xl p-2 text-red-500 transition hover:bg-red-50 hover:text-red-600",
    };

    const { className: extraClassName, ...rest } = props;

    return (
        <button
            type={type}
            onClick={handleClick}
            disabled={disabled}
            className={[styles[variant], extraClassName].filter(Boolean).join(" ")}
            {...rest}
        >
            {children}
        </button>
    );
}

export default Button;