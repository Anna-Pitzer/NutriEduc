
export default function Header() {

    return (
        <header className="relative h-60 w-full overflow-hidden">

            <svg
                className="absolute inset-0 h-full w-full"
                viewBox="0 0 1000 300"
                preserveAspectRatio="none"
            >
                <path
                    d="
                        M 0 0
                        H 1000
                        V -15
                        C 1000 30, 970 65, 920 85
                        C 850 115, 790 110, 720 125
                        C 620 145, 540 140, 470 150
                        C 380 165, 340 180, 300 215
                        C 270 240, 230 250, 180 250
                        H 0
                        Z
                    "
                    fill="#D92567"
                />
            </svg>

            <div className=" relative z-10 flex h-full items-start justify-between px-8 py-8 md:px-12">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight text-branco md:text-5xl lg:text-6xl">NutriEduc</h1>
                    <p className=" mt-1 text-sm font-light text-white/75 md:text-base">Alimentação que educa, nutrição que transforma</p>
                </div>

                
            </div>


        </header>
    );
}