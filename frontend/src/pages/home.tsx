import Header from "../components/Header";
import Navbar from "../components/Navbar";
import Footer from "../components/Footer";
import background2 from '../assets/background2.png'

export function HomePage() {
  return (
    <div className="relative min-h-screen bg-[#FAF9F5]">
      <div
        className="fixed inset-0 z-0 bg-cover bg-center bg-no-repeat opacity-60"
        style={{
          backgroundImage: `url(${background2})`,
        }}
      />
      <div className="relative z-20 flex min-h-screen flex-col">
        <Header />
        <div className="mx-auto flex w-full max-w-350 flex-1 gap-6 px-4 pb-8 md:px-8">
          <Navbar />
          <main className="flex-1 rounded-2xl border border-[#E8E8E8]/20 bg-[rgba(214,211,205,0.07)] p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)] backdrop-blur-[1px]">
            <div className="mb-6">
              <h2 className="text-2xl font-bold text-vermelho">
                Bem-vindo ao NutriEduc!
              </h2>
              <p className="mt-2 text-sm text-gray-500">
                Gerencie as informações da plataforma de forma simples e organizada.
              </p>
            </div>
          </main>
        </div>
        <Footer />
      </div>
    </div>
  );
}