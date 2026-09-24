
import { AlertTriangle } from "lucide-react";
import { toast } from "react-toastify";

export function confirmarExclusao() {
  return new Promise((resolve) => {
    toast(
      ({ closeToast }) => (
        <div className="w-full h-full flex-col">

          <p className=" flex flex-nowrap gap-2"><AlertTriangle fill="yellow" className="text-black"/>Tem certeza que deseja excluir?</p>

          <div className="flex justify-center gap-4">
            <button
              className="border border-verdeClaro bg-verdeClaro/20 rounded-3xl px-2 hover:cursor-pointer hover:text-verdeClaro hover:border-black"
              onClick={() => {
                closeToast();
                resolve(true);
              }}
            >
              Sim
            </button>

            <button
              className="border border-vermelho bg-vermelho/20 rounded-3xl px-2 hover:cursor-pointer hover:text-vermelho hover:border-black "
              onClick={() => {
                closeToast();
                resolve(false);
              }}
            >
              Não
            </button>
          </div>
        </div>
      ),
      {
        autoClose: false,
        closeOnClick: false,
      }
    );
  });
}
