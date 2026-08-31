using FolhadePagamento.Aplicacao.Lotes;
using FolhadePagamento.Infra.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FolhadePagamento.Infra.Persistencia.Repositorios;

/// <summary>
/// Implementação do repositório de lotes de processamento.
/// </summary>
public class LoteRepositorio : ILoteRepositorio
{
    private readonly FolhaDbContext _contexto;

    public LoteRepositorio(FolhaDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    // ========================================================================
    // GRAVAÇÃO - LOTE
    // ========================================================================

    public async Task CriarLoteAsync(
        LoteProcessamentoPersistencia lote,
        IEnumerable<ItemLotePersistencia> itens,
        CancellationToken cancellationToken = default)
    {
        var loteDb = new LoteProcessamentoDb
        {
            LoteId = lote.LoteId,
            CompetenciaAno = lote.CompetenciaAno,
            CompetenciaMes = lote.CompetenciaMes,
            Status = lote.Status.ToString(),
            TotalItens = lote.TotalItens,
            ItensConcluidos = lote.ItensConcluidos,
            ItensComFalha = lote.ItensComFalha,
            ItensIgnorados = lote.ItensIgnorados,
            CriadoEm = lote.CriadoEm,
            IniciadoEm = lote.IniciadoEm,
            ConcluidoEm = lote.ConcluidoEm,
            UsuarioId = lote.UsuarioId,
            Observacao = lote.Observacao
        };

        await _contexto.LotesProcessamento.AddAsync(loteDb, cancellationToken);

        foreach (var item in itens)
        {
            var itemDb = new ItemLoteDb
            {
                ItemLoteId = item.ItemLoteId,
                LoteId = item.LoteId,
                FuncionarioId = item.FuncionarioId,
                Status = item.Status.ToString(),
                ProcessamentoVersaoId = item.ProcessamentoVersaoId,
                VersaoNumero = item.VersaoNumero,
                MensagemErro = item.MensagemErro,
                Tentativas = item.Tentativas,
                IniciadoEm = item.IniciadoEm,
                ConcluidoEm = item.ConcluidoEm
            };

            await _contexto.ItensLote.AddAsync(itemDb, cancellationToken);
        }

        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarStatusLoteAsync(
        Guid loteId,
        StatusLote status,
        DateTime? iniciadoEm = null,
        DateTime? concluidoEm = null,
        CancellationToken cancellationToken = default)
    {
        var lote = await _contexto.LotesProcessamento
            .FirstOrDefaultAsync(l => l.LoteId == loteId, cancellationToken);

        if (lote is not null)
        {
            lote.Status = status.ToString();
            if (iniciadoEm.HasValue) lote.IniciadoEm = iniciadoEm.Value;
            if (concluidoEm.HasValue) lote.ConcluidoEm = concluidoEm.Value;
            await _contexto.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AtualizarContadoresLoteAsync(
        Guid loteId,
        int itensConcluidos,
        int itensComFalha,
        int itensIgnorados,
        CancellationToken cancellationToken = default)
    {
        var lote = await _contexto.LotesProcessamento
            .FirstOrDefaultAsync(l => l.LoteId == loteId, cancellationToken);

        if (lote is not null)
        {
            lote.ItensConcluidos = itensConcluidos;
            lote.ItensComFalha = itensComFalha;
            lote.ItensIgnorados = itensIgnorados;
            await _contexto.SaveChangesAsync(cancellationToken);
        }
    }

    // ========================================================================
    // GRAVAÇÃO - ITEM
    // ========================================================================

    public async Task AtualizarItemAsync(
        Guid itemLoteId,
        StatusItemLote status,
        Guid? processamentoVersaoId = null,
        int? versaoNumero = null,
        string? mensagemErro = null,
        int? tentativas = null,
        DateTime? iniciadoEm = null,
        DateTime? concluidoEm = null,
        CancellationToken cancellationToken = default)
    {
        var item = await _contexto.ItensLote
            .FirstOrDefaultAsync(i => i.ItemLoteId == itemLoteId, cancellationToken);

        if (item is not null)
        {
            item.Status = status.ToString();
            if (processamentoVersaoId.HasValue) item.ProcessamentoVersaoId = processamentoVersaoId.Value;
            if (versaoNumero.HasValue) item.VersaoNumero = versaoNumero.Value;
            if (mensagemErro is not null) item.MensagemErro = mensagemErro;
            if (tentativas.HasValue) item.Tentativas = tentativas.Value;
            if (iniciadoEm.HasValue) item.IniciadoEm = iniciadoEm.Value;
            if (concluidoEm.HasValue) item.ConcluidoEm = concluidoEm.Value;
            await _contexto.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task IniciarProcessamentoItemAsync(
        Guid itemLoteId,
        CancellationToken cancellationToken = default)
    {
        var item = await _contexto.ItensLote
            .FirstOrDefaultAsync(i => i.ItemLoteId == itemLoteId, cancellationToken);

        if (item is not null)
        {
            item.Status = StatusItemLote.EmProcessamento.ToString();
            item.IniciadoEm = DateTime.UtcNow;
            item.Tentativas++;
            await _contexto.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ConcluirItemComSucessoAsync(
        Guid itemLoteId,
        Guid processamentoVersaoId,
        int versaoNumero,
        CancellationToken cancellationToken = default)
    {
        var item = await _contexto.ItensLote
            .FirstOrDefaultAsync(i => i.ItemLoteId == itemLoteId, cancellationToken);

        if (item is not null)
        {
            item.Status = StatusItemLote.Sucesso.ToString();
            item.ProcessamentoVersaoId = processamentoVersaoId;
            item.VersaoNumero = versaoNumero;
            item.ConcluidoEm = DateTime.UtcNow;
            await _contexto.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ConcluirItemComFalhaAsync(
        Guid itemLoteId,
        string mensagemErro,
        CancellationToken cancellationToken = default)
    {
        var item = await _contexto.ItensLote
            .FirstOrDefaultAsync(i => i.ItemLoteId == itemLoteId, cancellationToken);

        if (item is not null)
        {
            item.Status = StatusItemLote.Falha.ToString();
            item.MensagemErro = mensagemErro?.Length > 1000 ? mensagemErro[..1000] : mensagemErro;
            item.ConcluidoEm = DateTime.UtcNow;
            await _contexto.SaveChangesAsync(cancellationToken);
        }
    }

    // ========================================================================
    // CONSULTA
    // ========================================================================

    public async Task<LoteProcessamentoConsulta?> ObterLotePorIdAsync(
        Guid loteId,
        CancellationToken cancellationToken = default)
    {
        var lote = await _contexto.LotesProcessamento
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LoteId == loteId, cancellationToken);

        if (lote is null) return null;

        return MapearParaConsulta(lote);
    }

    public async Task<IReadOnlyList<ItemLoteConsulta>> ListarItensDoLoteAsync(
        Guid loteId,
        CancellationToken cancellationToken = default)
    {
        var itens = await _contexto.ItensLote
            .Include(i => i.Funcionario)
            .Include(i => i.ProcessamentoVersao)
                .ThenInclude(p => p!.Resultado)
            .AsNoTracking()
            .Where(i => i.LoteId == loteId)
            .OrderBy(i => i.Funcionario!.Nome)
            .ToListAsync(cancellationToken);

        return itens.Select(MapearItemParaConsulta).ToList();
    }

    public async Task<ItemLotePersistencia?> ObterProximoItemPendenteAsync(
        Guid loteId,
        CancellationToken cancellationToken = default)
    {
        var item = await _contexto.ItensLote
            .AsNoTracking()
            .Where(i => i.LoteId == loteId && i.Status == "Pendente")
            .OrderBy(i => i.ItemLoteId)
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null) return null;

        return new ItemLotePersistencia
        {
            ItemLoteId = item.ItemLoteId,
            LoteId = item.LoteId,
            FuncionarioId = item.FuncionarioId,
            Status = Enum.Parse<StatusItemLote>(item.Status),
            ProcessamentoVersaoId = item.ProcessamentoVersaoId,
            VersaoNumero = item.VersaoNumero,
            MensagemErro = item.MensagemErro,
            Tentativas = item.Tentativas,
            IniciadoEm = item.IniciadoEm,
            ConcluidoEm = item.ConcluidoEm
        };
    }

    public async Task<IReadOnlyList<LoteResumoConsulta>> ListarLotesAtivosAsync(
        CancellationToken cancellationToken = default)
    {
        var lotes = await _contexto.LotesProcessamento
            .AsNoTracking()
            .Where(l => l.Status == "Pendente" || l.Status == "EmProcessamento")
            .OrderByDescending(l => l.CriadoEm)
            .ToListAsync(cancellationToken);

        return lotes.Select(MapearParaResumo).ToList();
    }

    public async Task<IReadOnlyList<LoteResumoConsulta>> ListarLotesPorCompetenciaAsync(
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default)
    {
        var lotes = await _contexto.LotesProcessamento
            .AsNoTracking()
            .Where(l => l.CompetenciaAno == competenciaAno && l.CompetenciaMes == competenciaMes)
            .OrderByDescending(l => l.CriadoEm)
            .ToListAsync(cancellationToken);

        return lotes.Select(MapearParaResumo).ToList();
    }

    public async Task<bool> ExisteLoteAtivoParaCompetenciaAsync(
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default)
    {
        return await _contexto.LotesProcessamento
            .AnyAsync(l => l.CompetenciaAno == competenciaAno
                && l.CompetenciaMes == competenciaMes
                && (l.Status == "Pendente" || l.Status == "EmProcessamento"),
                cancellationToken);
    }

    public async Task<Dictionary<StatusItemLote, int>> ContarItensPorStatusAsync(
        Guid loteId,
        CancellationToken cancellationToken = default)
    {
        var contagem = await _contexto.ItensLote
            .Where(i => i.LoteId == loteId)
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var resultado = new Dictionary<StatusItemLote, int>();
        foreach (var item in contagem)
        {
            if (Enum.TryParse<StatusItemLote>(item.Status, out var status))
            {
                resultado[status] = item.Count;
            }
        }

        return resultado;
    }

    // ========================================================================
    // MAPEADORES
    // ========================================================================

    private static LoteProcessamentoConsulta MapearParaConsulta(LoteProcessamentoDb lote)
    {
        var itensPendentes = lote.TotalItens - lote.ItensConcluidos - lote.ItensComFalha - lote.ItensIgnorados;
        var percentual = lote.TotalItens > 0
            ? Math.Round((decimal)(lote.ItensConcluidos + lote.ItensComFalha + lote.ItensIgnorados) / lote.TotalItens * 100, 2)
            : 0;

        return new LoteProcessamentoConsulta
        {
            LoteId = lote.LoteId,
            CompetenciaAno = lote.CompetenciaAno,
            CompetenciaMes = lote.CompetenciaMes,
            Status = lote.Status,
            TotalItens = lote.TotalItens,
            ItensConcluidos = lote.ItensConcluidos,
            ItensComFalha = lote.ItensComFalha,
            ItensIgnorados = lote.ItensIgnorados,
            ItensPendentes = itensPendentes,
            PercentualConcluido = percentual,
            CriadoEm = lote.CriadoEm,
            IniciadoEm = lote.IniciadoEm,
            ConcluidoEm = lote.ConcluidoEm,
            DuracaoTotal = lote.ConcluidoEm.HasValue && lote.IniciadoEm.HasValue
                ? lote.ConcluidoEm.Value - lote.IniciadoEm.Value
                : null,
            UsuarioId = lote.UsuarioId,
            Observacao = lote.Observacao
        };
    }

    private static LoteResumoConsulta MapearParaResumo(LoteProcessamentoDb lote)
    {
        var percentual = lote.TotalItens > 0
            ? Math.Round((decimal)(lote.ItensConcluidos + lote.ItensComFalha + lote.ItensIgnorados) / lote.TotalItens * 100, 2)
            : 0;

        return new LoteResumoConsulta
        {
            LoteId = lote.LoteId,
            CompetenciaAno = lote.CompetenciaAno,
            CompetenciaMes = lote.CompetenciaMes,
            Status = lote.Status,
            TotalItens = lote.TotalItens,
            PercentualConcluido = percentual,
            CriadoEm = lote.CriadoEm,
            ConcluidoEm = lote.ConcluidoEm
        };
    }

    private static ItemLoteConsulta MapearItemParaConsulta(ItemLoteDb item)
    {
        return new ItemLoteConsulta
        {
            ItemLoteId = item.ItemLoteId,
            LoteId = item.LoteId,
            FuncionarioId = item.FuncionarioId,
            FuncionarioNome = item.Funcionario?.Nome ?? "N/A",
            Status = item.Status,
            ProcessamentoVersaoId = item.ProcessamentoVersaoId,
            VersaoNumero = item.VersaoNumero,
            SalarioLiquido = item.ProcessamentoVersao?.Resultado?.SalarioLiquido,
            MensagemErro = item.MensagemErro,
            Tentativas = item.Tentativas,
            IniciadoEm = item.IniciadoEm,
            ConcluidoEm = item.ConcluidoEm,
            Duracao = item.ConcluidoEm.HasValue && item.IniciadoEm.HasValue
                ? item.ConcluidoEm.Value - item.IniciadoEm.Value
                : null
        };
    }
}
