# Depuração medida — `dotnet build` falha no shell sem `APPDATA`/`ProgramFiles`

Registro de **um** caso de depuração com antes/depois **medidos** e comparação entre a técnica
aplicada e as alternativas descartadas. Corresponde ao defeito **PEND-016** (registrado em
2026-10-28 como workaround), agora revalidado com medição em vez de relato.

| Campo | Valor |
| --- | --- |
| Data das medições | 2026-11-18 |
| Commit base | `9544088` (`main`), árvore de trabalho com alterações não commitadas |
| SO / shell | Windows (MINGW64_NT-10.0, Git Bash) |
| SDK | `dotnet --version` → `10.0.302` |
| Projeto afetado | `backend/backend.csproj` |
| Natureza | defeito de **ambiente**, não de código: nenhum arquivo do projeto foi alterado |

---

## 1. Sintoma

Comando executado no shell do agente:

```bash
dotnet build backend/backend.csproj
```

Saída (literal, execução 1 de 3):

```text
  Determinando os projetos a serem restaurados...
C:\Program Files\dotnet\sdk\10.0.302\NuGet.targets(782,5): error : Value cannot be null. (Parameter 'path1') [C:\Users\Notebook-Maria\OneDrive\Área de Trabalho\NutriEduc\backend\backend.csproj]

FALHA da compilação.

    0 Aviso(s)
    1 Erro(s)
```

Três informações úteis no texto do erro:

1. O erro é de **restore** (`Determinando os projetos a serem restaurados`, `NuGet.targets`), não
   de compilação.
2. `Parameter 'path1'` é a assinatura de `Path.Combine` recebendo `null` como primeiro argumento.
3. A mensagem não cita nenhum arquivo `.cs` — o compilador nem chegou a rodar.

## 2. Medição “antes”

O mesmo comando, três vezes, com o ambiente do shell preservado (sem `APPDATA` e sem
`ProgramFiles` — confirmado com `echo "[$APPDATA] [$ProgramFiles]"` → `[] []`):

```bash
for i in 1 2 3; do
  env -u APPDATA -u ProgramFiles dotnet build backend/backend.csproj
done
```

| Execução | Código de saída | Tempo de parede | Erro |
| --- | --- | --- | --- |
| 1 | `1` | 1.995 ms | `path1` em `NuGet.targets(782,5)` |
| 2 | `1` | 2.743 ms | idem |
| 3 | `1` | 2.615 ms | idem |
| **Resultado** | **falha 3/3** | — | — |

Controle que isola a causa: `dotnet --version` **funciona** sem as variáveis (`10.0.302`,
saída `0`). Logo o SDK está instalado e acessível; o que quebra é o alvo de restore do NuGet, que
monta caminhos a partir do ambiente.

## 3. Técnica aplicada

Injetar as duas variáveis **só para o processo do comando**, sem alterar a sessão:

```bash
env APPDATA='C:\Users\Notebook-Maria\AppData\Roaming' \
    ProgramFiles='C:\Program Files' \
    dotnet build backend/backend.csproj
```

## 4. Medição “depois”

Mesma rotina, três vezes, com a técnica aplicada:

| Execução | Código de saída | Tempo de parede | Resultado |
| --- | --- | --- | --- |
| 1 | `0` | 2.955 ms | `Compilação com êxito.` — 0 erros |
| 2 | `0` | 3.864 ms | `Compilação com êxito.` — 0 erros |
| 3 | `0` | 3.024 ms | `Compilação com êxito.` — 0 erros |
| **Resultado** | **sucesso 3/3** | — | — |

Os tempos são de parede, incluem a partida do shell e do MSBuild e **não** são benchmark: o
“antes” termina mais cedo justamente porque falha no restore. A comparação relevante é a taxa de
sucesso (0/3 → 3/3) e o desaparecimento do `path1`.

## 5. Alternativas avaliadas e por que foram descartadas

Cada alternativa foi executada no mesmo ambiente. A coluna “Medido” é o código de saída obtido.

| # | Alternativa | Comando | Medido | Por que foi descartada |
| --- | --- | --- | --- | --- |
| 1 | `export` persistente no shell | `export APPDATA=...; export ProgramFiles=...; dotnet build ...` | `0` | Funciona, mas fixa o caminho absoluto da máquina na sessão inteira, vale para todo comando posterior e não documenta o requisito. Em outro shell (PowerShell, CI, VS Code task) o defeito volta. Foi rejeitada por **escopo**, não por efeito |
| 2 | Só `APPDATA` | `env APPDATA=... dotnet build ...` | `1` | Insuficiente: continua falhando com `path1` |
| 3 | Só `ProgramFiles` | `env ProgramFiles='C:\Program Files' dotnet build ...` | `1` | Insuficiente: continua falhando com `path1`. Juntas, 2 e 3 provam que **as duas** são necessárias |
| 4 | `NUGET_PACKAGES` explícito | `env -u APPDATA -u ProgramFiles NUGET_PACKAGES='C:\Users\...\.nuget\packages' dotnet build ...` | `1` | Hipótese “pasta de pacotes ausente” **descartada**: apontar a pasta não contorna o `Path.Combine` que recebe `null` |
| 5 | `--no-restore` | `env -u APPDATA -u ProgramFiles dotnet build ... --no-restore` | `0` (3/3) | Funciona apenas porque o restore já havia sido feito pela execução bem-sucedida. Em clone novo ou após `obj/` limpo, falha. É atalho, não correção |
| 6 | Delegar ao shell do Windows | `cmd.exe //c "dotnet build backend\backend.csproj"` | `1` | O `cmd` herda o mesmo ambiente do MSYS, então falha igual; além disso perde quoting e caminhos POSIX usados pelos comandos do projeto |

Descarte de hipótese por medição — `--no-restore` foi o experimento decisivo: com ele o build passa
sem as variáveis, o que confirma que o defeito está **exclusivamente no restore**.

## 6. Causa confirmada

- O alvo de restore do NuGet (`NuGet.targets(782,5)`) chama `Path.Combine` com um caminho derivado
  do ambiente; no shell do agente `APPDATA` e `ProgramFiles` chegam vazios → `null` → 
  `ArgumentNullException("path1")`.
- **As duas** variáveis são necessárias (alternativas 2 e 3 falharam isoladamente).
- O defeito **não é de código**: `--no-restore` compila o mesmo projeto sem as variáveis.
- Não é instalação do SDK: `dotnet --version` responde sem as variáveis.

## 7. Resultado

Com a técnica aplicada, os gates do projeto voltaram a rodar neste shell. Sessão de 2026-11-18:

| Gate | Comando | Resultado |
| --- | --- | --- |
| Build | `dotnet build backend/backend.csproj` (com as variáveis) | 0 erros, 3/3 execuções |
| Testes | `dotnet test backend.Tests/backend.tests.csproj -c Release --no-build` | 881/881 aprovados |

## 8. Como reproduzir

```bash
cd <raiz do repositório>

# 1. confirmar que o shell não tem as variáveis
echo "[$APPDATA] [$ProgramFiles]"        # esperado: [] []

# 2. reproduzir a falha (antes)
env -u APPDATA -u ProgramFiles dotnet build backend/backend.csproj   # exit 1, path1

# 3. aplicar a técnica (depois), sem alterar a sessão
#    o primeiro valor é o %APPDATA% real da máquina
env APPDATA="$HOME/AppData/Roaming" ProgramFiles='C:\Program Files' \
    dotnet build backend/backend.csproj                              # exit 0

# 4. conferir o recorte do defeito
env -u APPDATA -u ProgramFiles dotnet build backend/backend.csproj --no-restore  # exit 0
```

## 9. Limitações deste registro

- Medido **apenas** neste ambiente (um Windows, um shell MSYS, um SDK). Não foi testado em
  PowerShell, WSL ou CI.
- `--no-restore` foi medido com `obj/` já povoado; por isso ele é classificado como atalho.
- O build é incremental nas três execuções “depois”; o `backend.dll` é reescrito, mas avisos de
  compilação não são reemitidos quando o arquivo já está compilado. Por isso a coluna de resultado
  registra **0 erros**, e não “0 avisos”.
- Os tempos de parede servem para mostrar o custo do ciclo, não para comparar desempenho.
- `env -u` simula o shell do agente; não foi verificado se outros shells de agente herdam o mesmo
  estado.
