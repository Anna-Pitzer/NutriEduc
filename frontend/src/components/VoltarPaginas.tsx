import { ChevronLeft } from "lucide-react";

export default function VoltarPaginas() {
  const voltarPagina = () => window.history.back();

  return (
    <button
      type="button"
      onClick={voltarPagina}
      className="group mx-auto flex w-full max-w-7xl cursor-pointer items-center justify-start"
    >
      <ChevronLeft
        size={50}
        className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
      />
    </button>
  )
}