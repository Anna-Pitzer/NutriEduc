

export default function Footer() {
    return (
        <footer className="bg-[#D9631E] text-white">
            <div className="mx-auto flex max-w-7xl flex-col gape-6 px-6 py-8 md:flex-row md:items-center md:justify-between">
                <div><h2 className="text-lg font-bold">NutriEduc</h2></div>
                <div><p className="mt-1 text-sm text-white/80">Sistema de gestão de cardápios escolares</p></div>

                <nav className="flex flex-wrap gap-x-6 gap-y2 text-sm">
                    <a href="/suporte" className="transition-opacity hover:opacity-70">Suporte</a>
                    <a href="/politicadeprivacidade" className="transition-opacity hover:opacity-70">Política de privacidade</a>
                    <a href="termosdeuso" className="transition-opacity hover:opacity-70">Termos de uso</a>
                </nav>
                <div className="text-sm text-white/80 md:text-right">© 2026 NutriEduc <br /> Todos os direitos reservados</div>
            </div>
        </footer>
    );
}