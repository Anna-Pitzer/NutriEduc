using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Relatorios;

namespace Ntc.Database;

/// <summary>
/// Leitura somente-leitura dos cinco relatórios (Spec 019, RNF2/RNF3). O escopo já chega
/// resolvido em <see cref="FiltroDeRelatorio"/>; aqui só se aplica o que foi autorizado.
/// Nenhuma escrita e nenhuma regra de permissão vivem nesta classe.
/// </summary>
public sealed class RelatorioRepository : IRelatorioRepository
{
    private const int DiasDaSemana = 5;

    private readonly ConnectionContext _context;

    public RelatorioRepository(ConnectionContext context) => _context = context;

    public TabelaDeRelatorio Obter(TipoDeRelatorio tipo, FiltroDeRelatorio filtro) => tipo switch
    {
        TipoDeRelatorio.Cardapios => Cardapios(filtro),
        TipoDeRelatorio.AlunosRestricoes => Alunos(filtro),
        TipoDeRelatorio.Ocorrencias => Ocorrencias(filtro),
        TipoDeRelatorio.SolicitacoesFeedbacks => SolicitacoesFeedbacks(filtro),
        TipoDeRelatorio.IndicadoresAdministrativos => Indicadores(filtro),
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    // ---------- Cardápios (modelo da foto: refeições × dias da semana) ----------

    private TabelaDeRelatorio Cardapios(FiltroDeRelatorio filtro)
    {
        var semana = DiasUteis(filtro.De);

        var colunas = new List<string> { "Refeição" };
        colunas.AddRange(semana.Select(dia => $"{NomeDoDia(dia.DayOfWeek)} {dia:dd/MM}"));

        var cardapio = _context.Cardapios.AsNoTracking()
            .Include(c => c.Refeicoes)
            .ThenInclude(r => r.Itens)
            .Where(c => filtro.UnidadeId == null || c.UnidadeId == filtro.UnidadeId)
            .Where(c => filtro.EscolaId == null || c.EscolaId == filtro.EscolaId)
            .Where(c => c.Periodo.Inicio <= filtro.Ate && c.Periodo.Fim >= filtro.De)
            .OrderByDescending(c => c.Id)
            .FirstOrDefault();

        if (cardapio is null)
        {
            return TabelaDeRelatorio.Vazia(
                "Cardápios", colunas, "Nenhum cardápio vigente no período.");
        }

        var alimentos = _context.Alimentos.AsNoTracking()
            .ToDictionary(a => a.Id, a => a.Nome);

        var linhas = new List<IReadOnlyList<string>>();

        foreach (var refeicao in cardapio.Refeicoes.OrderBy(r => r.Ordem))
        {
            var itens = string.Join(", ", refeicao.Itens
                .OrderBy(item => item.AlimentoId)
                .Select(item => alimentos.TryGetValue(item.AlimentoId, out var nome)
                    ? nome
                    : $"#{item.AlimentoId}"));

            var linha = new List<string> { refeicao.Nome };
            linha.AddRange(Enumerable.Repeat(itens, semana.Count));
            linhas.Add(linha);
        }

        var unidades = NomesDeUnidade();
        var escolas = NomesDeEscola();

        var unidade = unidades.TryGetValue(cardapio.UnidadeId, out var nomeUnidade)
            ? nomeUnidade
            : $"Unidade {cardapio.UnidadeId}";

        var escola = cardapio.EscolaId is { } escolaId &&
                     escolas.TryGetValue(escolaId, out var nomeEscola)
            ? $" — {nomeEscola}"
            : string.Empty;

        var titulo =
            $"CARDÁPIO — {unidade}{escola} — {cardapio.Nome} " +
            $"({cardapio.Periodo.Inicio:dd/MM/yyyy} a {cardapio.Periodo.Fim:dd/MM/yyyy})";

        var autor = string.IsNullOrWhiteSpace(cardapio.NutricionistaNome)
            ? "nutricionista responsável"
            : cardapio.NutricionistaNome;

        var crn = string.IsNullOrWhiteSpace(cardapio.NutricionistaCrn)
            ? string.Empty
            : $", CRN {cardapio.NutricionistaCrn}";

        var rodape =
            $"Cardápio único para o período de {filtro.De:dd/MM/yyyy} a {filtro.Ate:dd/MM/yyyy}. " +
            $"{autor}{crn}, nutricionista. Cardápio sujeito a alterações conforme entrega de " +
            "fornecedores e maturação de frutas e legumes.";

        return new TabelaDeRelatorio(titulo, colunas, linhas, rodape);
    }

    // ---------- Alunos e restrições ----------

    private TabelaDeRelatorio Alunos(FiltroDeRelatorio filtro)
    {
        var alunos = _context.Alunos.AsNoTracking()
            .Where(a => filtro.UnidadeId == null || a.UnidadeId == filtro.UnidadeId)
            .Where(a => filtro.EscolaId == null || a.EscolaId == filtro.EscolaId)
            .OrderBy(a => a.Nome)
            .ToList();

        var escolas = NomesDeEscola();
        var unidades = NomesDeUnidade();

        var linhas = alunos
            .Select(aluno => (IReadOnlyList<string>)new[]
            {
                aluno.Nome,
                aluno.EscolaId is { } escolaId && escolas.TryGetValue(escolaId, out var escola)
                    ? escola
                    : "—",
                aluno.UnidadeId is { } unidadeId && unidades.TryGetValue(unidadeId, out var unidade)
                    ? unidade
                    : "—",
                aluno.Restricoes.Count == 0
                    ? "Nenhuma"
                    : string.Join(", ", aluno.Restricoes),
                aluno.Anafilaxia ? "Sim" : "Não"
            })
            .ToList();

        return new TabelaDeRelatorio(
            "Alunos e restrições",
            new[] { "Aluno", "Escola", "Unidade", "Restrições", "Anafilaxia" },
            linhas,
            "Nenhuma coluna expõe CPF, e-mail, telefone ou foto.");
    }

    // ---------- Ocorrências ----------

    private TabelaDeRelatorio Ocorrencias(FiltroDeRelatorio filtro)
    {
        var inicio = InicioUtc(filtro.De);
        var fim = InicioUtc(filtro.Ate.AddDays(1));

        var ocorrencias = _context.Ocorrencias.AsNoTracking()
            .Where(o => filtro.UnidadeId == null || o.UnidadeId == filtro.UnidadeId)
            .Where(o => filtro.EscolaId == null || o.EscolaId == filtro.EscolaId)
            .Where(o => o.CriadoEmUtc >= inicio && o.CriadoEmUtc < fim)
            .OrderByDescending(o => o.CriadoEmUtc)
            .ThenBy(o => o.Id)
            .ToList();

        var escolas = NomesDeEscola();
        var unidades = NomesDeUnidade();

        var linhas = ocorrencias
            .Select(ocorrencia => (IReadOnlyList<string>)new[]
            {
                ocorrencia.CriadoEmUtc.ToString("dd/MM/yyyy HH:mm"),
                ocorrencia.Tipo.ToString(),
                ocorrencia.Status.ToString(),
                unidades.TryGetValue(ocorrencia.UnidadeId, out var unidade) ? unidade : "—",
                ocorrencia.EscolaId is { } escolaId && escolas.TryGetValue(escolaId, out var escola)
                    ? escola
                    : "—",
                ocorrencia.AlunoNome ?? "—",
                ocorrencia.Descricao
            })
            .ToList();

        return new TabelaDeRelatorio(
            "Ocorrências",
            new[] { "Data", "Tipo", "Status", "Unidade", "Escola", "Aluno", "Descrição" },
            linhas,
            "Datas em UTC. Ordenação: mais recente primeiro.");
    }

    // ---------- Solicitações e feedbacks ----------

    private TabelaDeRelatorio SolicitacoesFeedbacks(FiltroDeRelatorio filtro)
    {
        var inicio = InicioUtc(filtro.De);
        var fim = InicioUtc(filtro.Ate.AddDays(1));

        var solicitacoes = _context.Solicitacoes.AsNoTracking()
            .Where(s => filtro.UnidadeId == null || s.UnidadeId == filtro.UnidadeId)
            .Where(s => filtro.EscolaId == null || s.EscolaId == filtro.EscolaId)
            .Where(s => s.CriadoEmUtc >= inicio && s.CriadoEmUtc < fim)
            .Select(s => new
            {
                s.Tipo,
                s.Status,
                Solicitante = s.SolicitanteNome,
                s.UnidadeId,
                s.CriadoEmUtc,
                s.AtualizadoEmUtc
            })
            .ToList();

        var feedbacks = _context.Feedbacks.AsNoTracking()
            .Where(f => filtro.UnidadeId == null || f.UnidadeId == filtro.UnidadeId)
            .Where(f => filtro.EscolaId == null || f.EscolaId == filtro.EscolaId)
            .Where(f => f.CriadoEmUtc >= inicio && f.CriadoEmUtc < fim)
            .Select(f => new
            {
                f.Status,
                f.AutorNome,
                f.UnidadeId,
                f.CriadoEmUtc,
                f.AtualizadoEmUtc
            })
            .ToList();

        var unidades = NomesDeUnidade();

        var linhas = new List<(DateTime Data, IReadOnlyList<string> Celulas)>();

        foreach (var solicitacao in solicitacoes)
        {
            linhas.Add((solicitacao.CriadoEmUtc, new[]
            {
                "Solicitação",
                solicitacao.Tipo.ToString(),
                solicitacao.Status.ToString(),
                solicitacao.Solicitante,
                unidades.TryGetValue(solicitacao.UnidadeId, out var unidade) ? unidade : "—",
                solicitacao.CriadoEmUtc.ToString("dd/MM/yyyy HH:mm"),
                solicitacao.AtualizadoEmUtc.ToString("dd/MM/yyyy HH:mm")
            }));
        }

        foreach (var feedback in feedbacks)
        {
            linhas.Add((feedback.CriadoEmUtc, new[]
            {
                "Feedback",
                "Feedback",
                feedback.Status.ToString(),
                feedback.AutorNome,
                unidades.TryGetValue(feedback.UnidadeId, out var unidade) ? unidade : "—",
                feedback.CriadoEmUtc.ToString("dd/MM/yyyy HH:mm"),
                feedback.AtualizadoEmUtc.ToString("dd/MM/yyyy HH:mm")
            }));
        }

        return new TabelaDeRelatorio(
            "Solicitações e feedbacks",
            new[]
            {
                "Origem", "Tipo", "Status", "Solicitante", "Unidade", "Criado em", "Atualizado em"
            },
            linhas.OrderByDescending(linha => linha.Data)
                .Select(linha => linha.Celulas)
                .ToList(),
            "Datas em UTC. Ordenação: mais recente primeiro.");
    }

    // ---------- Indicadores administrativos ----------

    private TabelaDeRelatorio Indicadores(FiltroDeRelatorio filtro)
    {
        var inicio = InicioUtc(filtro.De);
        var fim = InicioUtc(filtro.Ate.AddDays(1));

        var escolasAtivas = _context.Escolas.AsNoTracking()
            .Where(e => filtro.UnidadeId == null || e.UnidadeId == filtro.UnidadeId)
            .Count(e => e.Ativa);

        var usuariosAtivos = _context.Usuarios.AsNoTracking()
            .Where(u => filtro.UnidadeId == null || u.UnidadeId == filtro.UnidadeId)
            .Count(u => u.Ativo);

        var alunos = _context.Alunos.AsNoTracking()
            .Where(a => filtro.UnidadeId == null || a.UnidadeId == filtro.UnidadeId)
            .Where(a => filtro.EscolaId == null || a.EscolaId == filtro.EscolaId)
            .Count();

        var publicados = _context.Cardapios.AsNoTracking()
            .Where(c => filtro.UnidadeId == null || c.UnidadeId == filtro.UnidadeId)
            .Where(c => filtro.EscolaId == null || c.EscolaId == filtro.EscolaId)
            .Count(c => c.Situacao == SituacaoCardapio.Publicado &&
                        c.PublicadoEmUtc != null &&
                        c.PublicadoEmUtc >= inicio &&
                        c.PublicadoEmUtc < fim);

        var ocorrenciasAbertas = _context.Ocorrencias.AsNoTracking()
            .Where(o => filtro.UnidadeId == null || o.UnidadeId == filtro.UnidadeId)
            .Where(o => filtro.EscolaId == null || o.EscolaId == filtro.EscolaId)
            .Count(o => o.Status == StatusOcorrencia.Aberta ||
                        o.Status == StatusOcorrencia.EmAnalise);

        var solicitacoesAbertas = _context.Solicitacoes.AsNoTracking()
            .Where(s => filtro.UnidadeId == null || s.UnidadeId == filtro.UnidadeId)
            .Where(s => filtro.EscolaId == null || s.EscolaId == filtro.EscolaId)
            .Count(s => s.Status == StatusSolicitacao.Aberta ||
                        s.Status == StatusSolicitacao.EmAnalise);

        var linhas = new List<IReadOnlyList<string>>
        {
            new[] { "Escolas ativas", escolasAtivas.ToString() },
            new[] { "Usuários ativos", usuariosAtivos.ToString() },
            new[] { "Alunos", alunos.ToString() },
            new[] { "Cardápios publicados no período", publicados.ToString() },
            new[] { "Ocorrências abertas", ocorrenciasAbertas.ToString() },
            new[] { "Solicitações abertas", solicitacoesAbertas.ToString() }
        };

        return new TabelaDeRelatorio(
            "Indicadores administrativos",
            new[] { "Indicador", "Valor" },
            linhas,
            $"Período de {filtro.De:dd/MM/yyyy} a {filtro.Ate:dd/MM/yyyy} (UTC).");
    }

    // ---------- Apoio ----------

    private Dictionary<int, string> NomesDeUnidade() =>
        _context.Unidades.AsNoTracking().ToDictionary(unidade => unidade.Id, unidade => unidade.Nome);

    private Dictionary<int, string> NomesDeEscola() =>
        _context.Escolas.AsNoTracking().ToDictionary(escola => escola.Id, escola => escola.Nome);

    private static DateTime InicioUtc(DateOnly dia) =>
        dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private static IReadOnlyList<DateOnly> DiasUteis(DateOnly referencia)
    {
        var dias = new List<DateOnly>(DiasDaSemana);
        var dia = referencia;

        while (dias.Count < DiasDaSemana)
        {
            if (dia.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                dias.Add(dia);
            }

            dia = dia.AddDays(1);
        }

        return dias;
    }

    private static string NomeDoDia(DayOfWeek dia) => dia switch
    {
        DayOfWeek.Monday => "2ª feira",
        DayOfWeek.Tuesday => "3ª feira",
        DayOfWeek.Wednesday => "4ª feira",
        DayOfWeek.Thursday => "5ª feira",
        DayOfWeek.Friday => "6ª feira",
        DayOfWeek.Saturday => "Sábado",
        _ => "Domingo"
    };
}
